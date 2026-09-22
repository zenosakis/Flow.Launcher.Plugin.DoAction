using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DoAction.Core;
using Flow.Launcher.Plugin.DoAction;

internal static class Program
{
    private static int passed;
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Contains("--diagnose")) { Diagnose(); return; }
        if (args.Contains("--live")) { Live(); return; }
        Check("all six action combinations", () =>
        {
            var expected = new[] { (true, false, false), (false, true, false), (true, true, false), (false, false, true), (true, false, true), (true, true, true) };
            foreach (var mode in Enum.GetValues<ActionMode>())
            {
                var s = new Settings { Mode = mode };
                Equal((s.HasMove, s.HasClick, s.HasKeyboard), expected[(int)mode]);
            }
        });
        Check("injected keys never stop or release physical keys", () =>
        {
            var gate = new PhysicalKeyGate(new[] { 13 });
            Equal(gate.ShouldStop(65, true, true), false);
            Equal(gate.ShouldStop(13, false, true), false);
            Equal(gate.InitialKeysReleased, false);
            Equal(gate.ShouldStop(13, true, false), false);
            Equal(gate.ShouldStop(13, false, false), false);
            Equal(gate.InitialKeysReleased, true);
            Equal(gate.ShouldStop(13, true, false), true);
        });
        Check("first physical modifier stops without waiting for chord", () =>
        {
            var gate = new PhysicalKeyGate(Array.Empty<int>());
            Equal(gate.ShouldStop(65, true, true), false);
            Equal(gate.ShouldStop(0xA2, true, false), true);
        });
        Check("invalid inputs rejected", () =>
        {
            foreach (var n in new[] { double.NaN, double.PositiveInfinity, 0, -1, 86401 })
                Assert(new Settings { IntervalSeconds = n }.ValidateAutomation() != null);
            Assert(new Settings { Mode = ActionMode.Keyboard, Keyboard = KeyboardKind.Text, Text = "" }.ValidateAutomation() != null);
            Assert(new Settings { Mode = ActionMode.Keyboard, Keyboard = KeyboardKind.Text, Text = "a\nb" }.ValidateAutomation() != null);
            Assert(new Settings { Mode = ActionMode.Keyboard, Keyboard = KeyboardKind.Text, Text = "한글 😀" }.ValidateAutomation() == null);
        });
        var start = new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.FromHours(9));
        Check("reservation warning then one execution only", () =>
        {
            var s = new PowerSchedule();
            s.Arm(new Settings { AfterMinutes = 2, WarningSeconds = 10 }, start);
            for (var i = 1; i < 110; i++) Equal(s.Tick(start.AddSeconds(i)), ScheduleEvent.None);
            Equal(s.Tick(start.AddSeconds(110)), ScheduleEvent.Warning);
            for (var i = 111; i < 120; i++) Equal(s.Tick(start.AddSeconds(i)), ScheduleEvent.None);
            Equal(s.Tick(start.AddSeconds(120)), ScheduleEvent.Execute);
            Equal(s.Tick(start.AddSeconds(121)), ScheduleEvent.None);
            Assert(s.Active == null);
        });
        Check("cancel during countdown prevents execution", () =>
        {
            var s = new PowerSchedule(); s.Arm(new Settings { AfterMinutes = 2, WarningSeconds = 10 }, start);
            for (var i = 1; i <= 110; i++) s.Tick(start.AddSeconds(i));
            s.Cancel();
            Equal(s.Tick(start.AddSeconds(120)), ScheduleEvent.None);
        });
        Check("sleep and stalled dispatcher expire reservation", () =>
        {
            var s = new PowerSchedule(); s.Arm(new Settings(), start);
            Equal(s.Tick(start.AddHours(1)), ScheduleEvent.Expired);
            Assert(s.Active == null);
        });
        Check("clock rollback expires reservation", () =>
        {
            var s = new PowerSchedule(); s.Arm(new Settings(), start);
            Equal(s.Tick(start.AddMinutes(-1)), ScheduleEvent.Expired);
        });
        Check("settings edits cannot change active reservation", () =>
        {
            var settings = new Settings(); var s = new PowerSchedule(); s.Arm(settings, start);
            settings.Power = PowerKind.Restart; settings.AfterMinutes = 2; settings.ForceClose = true;
            Equal(s.Active!.Power, PowerKind.Shutdown);
            Equal(s.Active.Due, start.AddMinutes(30));
            Equal(s.Active.ForceClose, false);
            Throws(() => s.Arm(settings, start));
        });
        Check("automation stop policy leaves reservation intact", () =>
        {
            var s = new PowerSchedule(); s.Arm(new Settings(), start);
            var gate = new PhysicalKeyGate(Array.Empty<int>());
            Assert(gate.ShouldStop(65, true, false));
            Assert(s.Active != null);
        });
        Check("past absolute time and too-short countdown rejected", () =>
        {
            Throws(() => new Settings { Schedule = ScheduleKind.AtDateTime, AtLocal = DateTime.Today.AddDays(-1) }.ResolveDue(DateTimeOffset.Now));
            Throws(() => new Settings { AfterMinutes = 1, WarningSeconds = 60 }.ResolveDue(start));
        });
        Check("shutdown commands never implicitly force", () =>
        {
            Equal(PowerExecutor.Arguments(new(PowerKind.Shutdown, false, start, 60)), "/s /t 0");
            Equal(PowerExecutor.Arguments(new(PowerKind.Restart, false, start, 60)), "/r /t 0");
            Equal(PowerExecutor.Arguments(new(PowerKind.Restart, true, start, 60)), "/r /t 0 /f");
        });
        Check("native INPUT ABI size", () => Equal(Marshal.SizeOf<Native.Input>(), IntPtr.Size == 8 ? 40 : 28));
        // Offscreen rendering only. No keys, clicks, shutdown or reservation are sent to Windows.
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        Check("start waits for Flow hide and does not request a second host hide", () =>
        {
            using var main = new Flow.Launcher.Plugin.DoAction.Main();
            var api = DispatchProxy.Create<Flow.Launcher.Plugin.IPublicAPI, FakeApi>();
            var fake = (FakeApi)(object)api;
            fake.Values.StartDelaySeconds = 60;
            fake.Visible = true;
            var metadata = new Flow.Launcher.Plugin.PluginMetadata { ExecuteFileName = "Flow.Launcher.Plugin.DoAction.dll", IcoPath = "Images/doaction.ico" };
            typeof(Flow.Launcher.Plugin.PluginMetadata).GetProperty("PluginDirectory")!.SetValue(metadata, AppContext.BaseDirectory);
            var context = new Flow.Launcher.Plugin.PluginInitContext { API = api };
            typeof(Flow.Launcher.Plugin.PluginInitContext).GetProperty("CurrentPluginMetadata")!.SetValue(context, metadata);
            main.Init(context);
            var query = new Flow.Launcher.Plugin.Query();
            typeof(Flow.Launcher.Plugin.Query).GetProperty("Search")!.SetValue(query, "");
            var result = main.Query(query).First();
            var hostShouldHide = result.Action(new Flow.Launcher.Plugin.ActionContext());
            var controller = (Controller)typeof(Flow.Launcher.Plugin.DoAction.Main).GetField("controller", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(main)!;
            try
            {
                PumpUntil(Task.Delay(80));
                Assert(!controller.Automation.IsRunning);
                Equal(fake.HideCalls, 1);
                Equal(hostShouldHide, false);
                fake.Visible = false;
                PumpUntil(Task.Delay(80));
                Assert(controller.Automation.IsRunning);
            }
            finally { controller.StopAutomation(); PumpUntil(controller.Automation.Completion); }
        });
        Check("power registration feedback reflects active reservation and rejects false success", () =>
        {
            var messages = new System.Collections.Generic.List<string>();
            var settings = new Settings { Power = PowerKind.Restart, ForceClose = true, AfterMinutes = 30, WarningSeconds = 45 };
            using var controller = new Controller(settings, messages.Add);
            controller.ArmPower();
            Equal(messages.Count, 1);
            var active = controller.Schedule.Active!;
            Assert(messages[0].Contains("PC 재부팅"));
            Assert(messages[0].Contains(active.Due.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss")));
            Assert(messages[0].Contains("45초 전"));
            Assert(messages[0].Contains("강제 종료: 켜짐"));
            Assert(messages[0].Contains("PC 예약 취소"));
            settings.Power = PowerKind.Shutdown;
            settings.ForceClose = false;
            controller.ShowPowerStatus();
            Equal(messages[1], messages[0]);
            Throws(controller.ArmPower);
            Equal(messages.Count, 2);
            controller.CancelPower();
            controller.ShowPowerStatus();
            Equal(messages[2], "등록된 PC 예약이 없습니다.");
            settings.AfterMinutes = 0;
            Throws(controller.ArmPower);
            Equal(messages.Count, 3);
        });
        Check("actual keyboard hook installs and disposes without input", () =>
        {
            using var monitor = new PhysicalKeyboardMonitor(() => { });
            Assert(monitor.Ready.Wait(TimeSpan.FromSeconds(5)));
        });
        Check("controller stops automation without canceling power", () =>
        {
            using var controller = new Controller(new Settings { StartDelaySeconds = 60 }, _ => { });
            controller.ArmPower();
            controller.StartAutomation();
            controller.StopAutomation();
            PumpUntil(controller.Automation.Completion);
            Assert(!controller.Automation.IsRunning);
            Assert(controller.Schedule.Active != null);
            controller.CancelPower();
            Assert(controller.Schedule.Active == null);
        });
        Check("canceling power leaves automation enabled until explicitly stopped", () =>
        {
            using var controller = new Controller(new Settings { StartDelaySeconds = 60 }, _ => { });
            controller.StartAutomation();
            controller.ArmPower();
            controller.CancelPower();
            Assert(controller.Automation.IsRunning);
            Assert(controller.Schedule.Active == null);
            controller.StopAutomation();
            PumpUntil(controller.Automation.Completion);
            Assert(!controller.Automation.IsRunning);
        });
        Check("Flow Init Query settings and Dispose integration", () =>
        {
            using var main = new Flow.Launcher.Plugin.DoAction.Main();
            var api = DispatchProxy.Create<Flow.Launcher.Plugin.IPublicAPI, FakeApi>();
            var metadata = new Flow.Launcher.Plugin.PluginMetadata { ExecuteFileName = "Flow.Launcher.Plugin.DoAction.dll", IcoPath = "Images/doaction.ico" };
            typeof(Flow.Launcher.Plugin.PluginMetadata).GetProperty("PluginDirectory")!.SetValue(metadata, AppContext.BaseDirectory);
            var context = new Flow.Launcher.Plugin.PluginInitContext { API = api };
            typeof(Flow.Launcher.Plugin.PluginInitContext).GetProperty("CurrentPluginMetadata")!.SetValue(context, metadata);
            main.Init(context);
            var query = new Flow.Launcher.Plugin.Query();
            typeof(Flow.Launcher.Plugin.Query).GetProperty("Search")!.SetValue(query, "");
            var results = main.Query(query);
            Equal(results.Count, 4);
            Assert(results.Any(r => r.Title == "자동 입력 시작"));
            Assert(results.Any(r => r.Title == "PC 종료 예약"));
            Assert((object)main is Flow.Launcher.Plugin.IResultUpdated);
            Flow.Launcher.Plugin.ResultUpdatedEventArgs? update = null;
            ((Flow.Launcher.Plugin.IResultUpdated)(object)main).ResultsUpdated += (_, e) => update = e;
            var controller = (Controller)typeof(Flow.Launcher.Plugin.DoAction.Main).GetField("controller", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(main)!;
            controller.ArmPower();
            Assert(update != null && ReferenceEquals(update.Query, query));
            Assert(update!.Results.Any(r => r.Title == "PC 예약 취소"));
            var due = controller.Schedule.Active!.Due.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            Assert(update.Results.Single(r => r.Title.StartsWith("상태:")).Title.Contains(due));
            // Other activity messages must not overwrite the reservation summary.
            typeof(Controller).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(controller, new object[] { "자동 입력 중지" });
            Assert(update.Results.Single(r => r.Title.StartsWith("상태:")).Title.Contains(due));
            controller.CancelPower();
            Assert(update.Results.Any(r => r.Title == "PC 종료 예약"));
            Assert(update.Results.Single(r => r.Title.StartsWith("상태:")).Title.Contains("PC 예약 없음"));
            Assert(main.CreateSettingPanel() is SettingsPanel);
            typeof(Flow.Launcher.Plugin.Query).GetProperty("Search")!.SetValue(query, "settings");
            Equal(main.Query(query).Single().Title, "DoAction 설정");
        });
        Check("settings render at narrow width and all modes", () =>
        {
            foreach (var mode in Enum.GetValues<ActionMode>())
            {
                var panel = new SettingsPanel(new Settings { Mode = mode }, () => { });
                panel.Measure(new Size(420, 720)); panel.Arrange(new Rect(0, 0, 420, 720)); panel.UpdateLayout();
                Equal(panel.ActualWidth, 420d);
            }
        });
        if (args.Length > 0)
        {
            Directory.CreateDirectory(args[0]);
            var settings = new Settings { Mode = ActionMode.MoveClickKeyboard, Keyboard = KeyboardKind.Text, FullScreen = true };
            Render(new SettingsPanel(settings, () => { }), 660, 1000, Path.Combine(args[0], "settings-input.png"));
            var powerPanel = new SettingsPanel(settings, () => { });
            powerPanel.Measure(new Size(660, 1000)); powerPanel.Arrange(new Rect(0, 0, 660, 1000)); powerPanel.UpdateLayout();
            ((ScrollViewer)powerPanel.Content).ScrollToEnd();
            Render(powerPanel, 660, 1000, Path.Combine(args[0], "settings-power.png"));
        }
        Console.WriteLine($"PASS: {passed} checks. No real input or PC power action executed.");
        app.Shutdown();
    }

    private static void Render(FrameworkElement control, int width, int height, string file)
    {
        control.Measure(new Size(width, height)); control.Arrange(new Rect(0, 0, width, height)); control.UpdateLayout();
        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(control);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(file); encoder.Save(stream);
    }
    private static void Diagnose()
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var held = Enumerable.Range(8, 248).Where(k => (Native.GetAsyncKeyState(k) & 0x8000) != 0);
        Console.WriteLine("Initially held VKs: " + string.Join(",", held));
        var runner = new AutomationRunner();
        runner.Activated += () => Console.WriteLine("ACTIVATED");
        runner.Finished += reason => Console.WriteLine("FINISHED: " + reason);
        runner.Start(new Settings { StartDelaySeconds = 1 }, () => { Console.WriteLine("TARGET GUARD REACHED"); return false; }, () => null);
        PumpUntil(Task.WhenAny(runner.Completion, Task.Delay(4000)));
        Console.WriteLine("Running after wait: " + runner.IsRunning);
        runner.Stop();
        PumpUntil(runner.Completion);
        app.Shutdown();
    }
    private static void Live()
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        System.Threading.SynchronizationContext.SetSynchronizationContext(null);
        Native.GetCursorPos(out var original);
        using var controller = new Controller(new Settings
        {
            Mode = ActionMode.MoveClickKeyboard, FullScreen = true, StartDelaySeconds = 1,
            IntervalSeconds = 0.1, Keyboard = KeyboardKind.Text, Text = "test", MovePixels = 4
        }, Console.WriteLine);
        Console.WriteLine("CONTEXT " + System.Threading.SynchronizationContext.Current?.GetType().Name);
        controller.Changed += () => Console.WriteLine("STATE " + controller.Status);
        controller.Automation.Activated += () => Console.WriteLine("ACTIVATED");
        controller.StartAutomation();
        var windows = app.Windows.Cast<Window>().OfType<FullScreenWindow>().ToArray();
        app.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
        var foreground = Native.GetForegroundWindow();
        var foregroundThread = Native.GetWindowThreadProcessId(foreground, out var foregroundPid);
        Console.WriteLine($"PREVIOUS FOREGROUND pid={foregroundPid}");
        var attached = AttachThreadInput(Native.GetCurrentThreadId(), foregroundThread, true);
        try { Console.WriteLine("FOREGROUND REQUEST " + SetForegroundWindow(windows.Last().Handle)); }
        finally { if (attached) AttachThreadInput(Native.GetCurrentThreadId(), foregroundThread, false); }
        var clicks = 0;
        foreach (var window in windows) window.AddHandler(System.Windows.Input.Mouse.PreviewMouseDownEvent, new System.Windows.Input.MouseButtonEventHandler((_, _) => clicks++), true);
        var sampling = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        sampling.Tick += (_, _) =>
        {
            Console.WriteLine($"SAMPLE running={controller.Automation.IsRunning} foreground={Native.GetForegroundWindow():X} windows={string.Join(',', windows.Select(w => w.Handle.ToString("X")))}");
        };
        sampling.Start();
        var characters = 0;
        try
        {
            PumpUntil(Task.Delay(4000));
            var texts = windows.Select(w => ((Grid)w.Content).Children.OfType<TextBox>().Single().Text).ToArray();
            characters = texts.Sum(t => t.Length);
            Console.WriteLine($"INPUT characters={characters} clicks={clicks}");
        }
        finally
        {
            sampling.Stop();
            controller.StopAutomation();
            PumpUntil(controller.Automation.Completion);
            Assert(windows.All(w => !w.IsVisible));
            Native.SetCursorPos(original.X, original.Y);
            app.Shutdown();
        }
        Assert(characters > 0 && clicks > 0);
        Console.WriteLine("PASS live fullscreen input and worker-thread cleanup");
    }
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr window);
    [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint source, uint target, bool attach);
    private static void Check(string name, Action action) { action(); passed++; Console.WriteLine("PASS " + name); }
    private static void PumpUntil(Task task)
    {
        var frame = new DispatcherFrame();
        var timeout = DateTime.UtcNow.AddSeconds(5);
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
        timer.Tick += (_, _) => { if (task.IsCompleted || DateTime.UtcNow >= timeout) frame.Continue = false; };
        timer.Start(); Dispatcher.PushFrame(frame); timer.Stop();
        Assert(task.IsCompleted);
        task.GetAwaiter().GetResult();
    }
    private static void Assert(bool result) { if (!result) throw new Exception("Assertion failed"); }
    private static void Equal<T>(T actual, T expected) { if (!Equals(actual, expected)) throw new Exception($"Expected {expected}, got {actual}"); }
    private static void Throws(Action action) { try { action(); } catch (ArgumentException) { return; } catch (InvalidOperationException) { return; } throw new Exception("Expected validation failure"); }
}

public class FakeApi : DispatchProxy
{
    public Settings Values { get; } = new();
    public bool Visible { get; set; }
    public int HideCalls { get; private set; }
    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        if (method!.Name == "LoadSettingJsonStorage") return Values;
        if (method.Name == "HideMainWindow") { HideCalls++; return null; }
        if (method.Name == "IsMainWindowVisible") return Visible;
        if (method.ReturnType == typeof(void)) return null;
        return method.ReturnType.IsValueType ? Activator.CreateInstance(method.ReturnType) : null;
    }
}
