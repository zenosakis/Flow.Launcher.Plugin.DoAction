using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DoAction.Core;

namespace Flow.Launcher.Plugin.DoAction;

public sealed class Main : IPlugin, ISettingProvider, IResultUpdated, IPluginI18n, IDisposable
{
    private PluginInitContext context = null!;
    private Controller controller = null!;
    private Window? settingsWindow;
    private Query? lastQuery;
    public event ResultUpdatedEventHandler? ResultsUpdated;
    private const string Icon = "Images/doaction.ico";

    public string GetTranslatedPluginTitle() => "DoAction";
    public string GetTranslatedPluginDescription() => L.T("Mouse/keyboard automation and scheduled PC shutdown or restart", "마우스·키보드 자동 입력과 PC 종료·재부팅 예약");
    public void OnCultureInfoChanged(System.Globalization.CultureInfo culture)
    {
        L.SetCulture(culture);
        Application.Current.Dispatcher.Invoke(() =>
        {
            settingsWindow?.Close();
            if (lastQuery is { } query)
                ResultsUpdated?.Invoke(this, new ResultUpdatedEventArgs { Query = query, Results = Query(query) });
        });
    }
    public void Init(PluginInitContext context)
    {
        this.context = context;
        var settings = context.API.LoadSettingJsonStorage<Settings>();
        Application.Current.Dispatcher.Invoke(() =>
        {
            controller = new Controller(settings, text => context.API.ShowMsg("DoAction", text, Icon));
            controller.Changed += () =>
            {
                context.API.LogInfo(nameof(Main), controller.LastEvent + " · " + controller.Status);
                if (lastQuery is { } query)
                    ResultsUpdated?.Invoke(this, new ResultUpdatedEventArgs { Query = query, Results = Query(query) });
            };
        });
    }

    public List<Result> Query(Query query)
    {
        return Application.Current.Dispatcher.Invoke(() =>
        {
            lastQuery = query;
            var active = controller.Schedule.Active;
            var inputRunning = controller.Automation.IsRunning;
            var results = new List<(string Search, Result Result)>
            {
                (inputRunning ? L.T("stop off", "stop off 중지 끄기") : L.T("start on", "start on 시작 켜기"), Result(
                    inputRunning ? L.T("Stop automation", "자동 입력 중지") : L.T("Start automation", "자동 입력 시작"),
                    inputRunning ? L.T("Stop all automation · keep power schedule", "이동·클릭·키보드 모두 중지 · PC 예약 유지") : L.T($"{ModeName(controller.Settings.Mode)} · {controller.Settings.IntervalSeconds} second interval · fullscreen {(controller.Settings.FullScreen ? "On" : "Off")}", $"{ModeName(controller.Settings.Mode)} · {controller.Settings.IntervalSeconds}초 간격 · 전체화면 {(controller.Settings.FullScreen ? "켜짐" : "꺼짐")}"),
                    () => { if (inputRunning) controller.StopAutomation(); else controller.StartAutomation(); }, 100)),
                (active == null ? L.T("schedule power shutdown restart", "schedule power 예약 종료 재부팅") : L.T("cancel schedule", "cancel 예약 취소"), Result(
                    active == null ? L.T($"PC {Controller.PowerName(controller.Settings.Power)} schedule", $"PC {Controller.PowerName(controller.Settings.Power)} 예약") : L.T("Cancel power schedule", "PC 예약 취소"),
                    active == null ? ScheduleDescription() : L.T($"{active.Due.LocalDateTime:yyyy-MM-dd HH:mm:ss} PC {Controller.PowerName(active.Power)} · independent of automation", $"{active.Due.LocalDateTime:yyyy-MM-dd HH:mm:ss} PC {Controller.PowerName(active.Power)} · 자동 입력과 별도"),
                    () => { if (active == null) controller.ArmPower(); else controller.CancelPower(); }, 90)),
                (L.T("settings", "settings 설정"), Result(L.T("DoAction settings", "DoAction 설정"), L.T("Configure automation, fullscreen and power schedules", "자동 입력 동작·전체화면·PC 예약 시각 설정"), OpenSettings, 80)),
                (L.T("status", "status 상태"), Result(L.T("Status: ", "상태: ") + controller.Status,
                    (controller.LastEvent.Length == 0 ? "" : controller.LastEvent + " · ") + L.T("Select to view the active power schedule.", "선택하면 PC 예약 상세 정보를 확인합니다."), controller.ShowPowerStatus, 10))
            };
            var term = query.Search.Trim();
            return results.Where(r => term.Length == 0 || (r.Search + " " + r.Result.Title).Contains(term, StringComparison.OrdinalIgnoreCase)).Select(r => r.Result).ToList();
        });
    }

    private Result Result(string title, string subtitle, Action action, int score) => new()
    {
        Title = title, SubTitle = subtitle, IcoPath = Icon, Score = score,
        Action = _ =>
        {
            Application.Current.Dispatcher.BeginInvoke(async () =>
            {
                try
                {
                    // Flow 2.x HideMainWindow returns before its animation/requery ends.
                    // Own the hide exactly once, then hand focus to the automation window.
                    context.API.HideMainWindow();
                    var wait = Stopwatch.StartNew();
                    while (context.API.IsMainWindowVisible())
                    {
                        if (wait.Elapsed > TimeSpan.FromSeconds(5))
                            throw new InvalidOperationException(L.T("The Flow search window did not close. Close it and start again.", "Flow 검색창이 닫히지 않았습니다. 검색창을 닫고 다시 시작해 주세요."));
                        await Task.Delay(25);
                    }
                    action();
                }
                catch (Exception ex) { context.API.ShowMsgError("DoAction", ex.Message); }
            });
            // Returning true would make Flow hide again after our fullscreen opens.
            return false;
        }
    };

    private string ScheduleDescription() => controller.Settings.Schedule == ScheduleKind.AfterMinutes
        ? L.T($"Select to schedule in {controller.Settings.AfterMinutes} minutes · warning {controller.Settings.WarningSeconds} seconds before · canceled when Flow exits", $"선택하면 {controller.Settings.AfterMinutes}분 후 · 실행 전 {controller.Settings.WarningSeconds}초 안내 · Flow 종료 시 취소")
        : L.T($"{controller.Settings.AtLocal:yyyy-MM-dd HH:mm} · warning {controller.Settings.WarningSeconds} seconds before · canceled when Flow exits", $"{controller.Settings.AtLocal:yyyy-MM-dd HH:mm} · 실행 전 {controller.Settings.WarningSeconds}초 안내 · Flow 종료 시 취소");

    internal static string ModeName(ActionMode mode) => mode switch
    {
        ActionMode.Move => L.T("Mouse movement", "마우스 이동"), ActionMode.Click => L.T("Mouse clicks", "마우스 클릭"), ActionMode.MoveClick => L.T("Move → click", "이동 → 클릭"),
        ActionMode.Keyboard => L.T("Keyboard input", "키보드 입력"), ActionMode.MoveKeyboard => L.T("Move → type", "이동 → 키보드"), _ => L.T("Move → click → type", "이동 → 클릭 → 키보드")
    };

    public Control CreateSettingPanel()
    {
        controller.StopAutomation(L.T("Automation stopped because settings were opened.", "설정을 열어 자동 입력을 중지했습니다."));
        return new SettingsPanel(controller.Settings, () => context.API.SaveSettingJsonStorage<Settings>());
    }

    private void OpenSettings()
    {
        if (settingsWindow != null) { settingsWindow.Activate(); return; }
        settingsWindow = new Window { Title = L.T("DoAction settings", "DoAction 설정"), Width = 660, Height = 760, MinWidth = 420, MinHeight = 400, WindowStartupLocation = WindowStartupLocation.CenterScreen, Content = CreateSettingPanel() };
        Ui.Surface(settingsWindow);
        settingsWindow.Closed += (_, _) => settingsWindow = null;
        settingsWindow.Show();
    }

    public void Dispose()
    {
        if (Application.Current?.Dispatcher is { } dispatcher && !dispatcher.HasShutdownStarted)
            dispatcher.Invoke(() => { settingsWindow?.Close(); controller?.Dispose(); });
    }
}
