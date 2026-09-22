using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DoAction.Core;
using Microsoft.Win32;

namespace Flow.Launcher.Plugin.DoAction;

internal sealed class Controller : IDisposable
{
    public Settings Settings { get; }
    public AutomationRunner Automation { get; } = new();
    public PowerSchedule Schedule { get; } = new();
    public string Status => $"자동 입력 {(Automation.IsRunning ? "켜짐" : "꺼짐")} · " +
        (Schedule.Active is { } active
            ? $"PC {PowerName(active.Power)} 예약: {active.Due.LocalDateTime:yyyy-MM-dd HH:mm:ss}"
            : powerExecuting ? "PC 전원 작업 요청 중" : "PC 예약 없음");
    public string LastEvent { get; private set; } = "";
    public event Action? Changed;
    private readonly Dispatcher ui;
    private readonly Action<string> notify;
    private readonly DispatcherTimer timer;
    private readonly List<FullScreenWindow> screens = new();
    private PowerWarningWindow? warning;
    private bool closingScreens, disposed, powerExecuting, ticking;

    public Controller(Settings settings, Action<string> notify)
    {
        Settings = settings;
        this.notify = notify;
        ui = Application.Current.Dispatcher;
        Automation.Activated += () => ui.BeginInvoke(() => Update("자동 입력 실행 중 · 실제 키를 누르면 중지"));
        Automation.Finished += reason =>
        {
            if (!ui.HasShutdownStarted)
                ui.Invoke(() => { CloseScreens(); Update(reason); });
        };
        timer = new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.Normal, Tick, ui);
        SystemEvents.PowerModeChanged += PowerChanged;
        SystemEvents.SessionSwitch += SessionChanged;
    }

    public void StartAutomation()
    {
        if (disposed || Automation.IsRunning) return;
        if (warning != null || powerExecuting) throw new InvalidOperationException("PC 종료 안내 중에는 자동 입력을 시작할 수 없습니다.");
        if (Settings.ValidateAutomation() is { } error) throw new ArgumentException(error);
        CloseScreens();
        var settings = Settings.Snapshot();
        if (settings.FullScreen)
        {
            foreach (var screen in System.Windows.Forms.Screen.AllScreens.OrderBy(s => s.Primary))
            {
                var window = new FullScreenWindow(screen.Bounds, screen.Primary, () => StopAutomation());
                window.Closed += (_, _) => { if (!closingScreens) StopAutomation(); };
                screens.Add(window);
                window.Show();
            }
        }
        // The primary fullscreen input surface is the only automation target.
        var target = screens.LastOrDefault();
        var targetHandle = target?.Handle ?? IntPtr.Zero;
        Automation.Start(settings, () =>
        {
            var foreground = Native.GetForegroundWindow();
            if (foreground == IntPtr.Zero) return false;
            if (targetHandle != IntPtr.Zero) return foreground == targetHandle;
            Native.GetWindowThreadProcessId(foreground, out var pid);
            return pid != (uint)Environment.ProcessId;
        }, () => target == null ? null : ui.Invoke(target.GetInputArea));
        Update($"시작 준비 · 키를 뗀 뒤 {settings.StartDelaySeconds}초 후 실행");
    }

    public void StopAutomation(string reason = "자동 입력 중지 · PC 예약은 유지됩니다.")
    { Automation.Stop(reason); if (!Automation.IsRunning) CloseScreens(); }

    public void ArmPower()
    {
        if (disposed || powerExecuting) return;
        Schedule.Arm(Settings.Snapshot(), DateTimeOffset.Now);
        Update($"{Schedule.Active!.Due.LocalDateTime:MM-dd HH:mm:ss} PC {PowerName(Schedule.Active.Power)} 예약됨");
        ShowPowerStatus();
    }

    public void ShowPowerStatus()
    {
        var active = Schedule.Active;
        notify(active == null ? "등록된 PC 예약이 없습니다." :
            $"PC {PowerName(active.Power)} 예약이 등록되어 있습니다.\n" +
            $"실행 시각: {active.Due.LocalDateTime:yyyy-MM-dd HH:mm:ss} (PC 현지 시각)\n" +
            $"실행 전 안내: {active.WarningSeconds}초 전\n" +
            $"강제 종료: {(active.ForceClose ? "켜짐 · 저장하지 않은 내용이 사라질 수 있습니다." : "꺼짐")}\n\n" +
            "취소: da → PC 예약 취소\n" +
            "Flow 종료·절전 시 예약이 취소됩니다.");
    }

    public void CancelPower()
    {
        Schedule.Cancel();
        var old = warning;
        warning = null;
        // When invoked by the window's Closing handler, do not recursively Close it.
        if (old != null) ui.BeginInvoke(old.Dismiss);
        Update("PC 예약 취소 · 자동 입력 상태는 유지됩니다.");
    }

    public static string PowerName(PowerKind kind) => kind == PowerKind.Shutdown ? "종료" : "재부팅";

    private async void Tick(object? sender, EventArgs e)
    {
        if (disposed || powerExecuting || ticking) return;
        ticking = true;
        try { await TickCore(); }
        catch (Exception ex)
        {
            CancelPower();
            Update("예약 처리 오류: " + ex.Message);
            notify(LastEvent);
        }
        finally { ticking = false; }
    }

    private async Task TickCore()
    {
        var reservation = Schedule.Active;
        var change = Schedule.Tick(DateTimeOffset.Now);
        if (change == ScheduleEvent.Warning && reservation != null)
        {
            StopAutomation("PC 예약 실행 안내를 위해 자동 입력을 종료했습니다.");
            await Automation.Completion;
            if (disposed || Schedule.Active != reservation) return;
            warning = new PowerWarningWindow(reservation, CancelPower);
            warning.Show();
        }
        else if (change == ScheduleEvent.Expired)
        {
            CancelPower();
            Update("절전 또는 시간 변경으로 PC 예약이 만료되었습니다.");
            notify(LastEvent);
        }
        else if (change == ScheduleEvent.Execute && reservation != null)
        {
            powerExecuting = true;
            warning?.Dismiss();
            warning = null;
            Update("PC " + PowerName(reservation.Power) + " 요청 중");
            try { await PowerExecutor.Execute(reservation); Update("PC " + PowerName(reservation.Power) + " 요청을 전달했습니다."); }
            catch (Exception ex) { Update(ex.Message); notify(ex.Message); }
            finally { powerExecuting = false; }
        }
        if (Schedule.Active is { } active) warning?.Update(active);
    }

    private void PowerChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode is PowerModes.Suspend or PowerModes.Resume)
            ui.BeginInvoke(() => { StopAutomation("절전으로 자동 입력 중지"); if (Schedule.Active != null) { CancelPower(); Update("절전으로 PC 예약이 취소되었습니다."); } });
    }

    private void SessionChanged(object sender, SessionSwitchEventArgs e)
    {
        if (e.Reason is SessionSwitchReason.SessionLock or SessionSwitchReason.SessionLogoff)
            ui.BeginInvoke(() => StopAutomation("화면 잠금으로 자동 입력을 종료했습니다."));
    }

    private void CloseScreens()
    {
        closingScreens = true;
        foreach (var window in screens.ToArray()) window.Close();
        screens.Clear();
        closingScreens = false;
    }

    private void Update(string text)
    {
        if (disposed) return;
        LastEvent = text;
        Changed?.Invoke();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Automation.Stop();
        Schedule.Cancel();
        timer.Stop();
        SystemEvents.PowerModeChanged -= PowerChanged;
        SystemEvents.SessionSwitch -= SessionChanged;
        warning?.Dismiss();
        warning = null;
        CloseScreens();
    }
}
