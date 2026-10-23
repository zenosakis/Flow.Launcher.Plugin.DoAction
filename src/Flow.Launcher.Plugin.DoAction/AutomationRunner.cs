using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DoAction.Core;

namespace Flow.Launcher.Plugin.DoAction;

internal sealed class AutomationRunner
{
    private CancellationTokenSource? cancellation;
    private Task completion = Task.CompletedTask;
    private string stopReason = L.T("Stop automation", "자동 입력 중지");
    public bool IsRunning => cancellation != null;
    public Task Completion => completion;
    public event Action<string>? Finished;
    public event Action? Activated;

    // Called only on UI dispatcher. Foreground/bounds guards run on the worker.
    public void Start(Settings settings, Func<bool> safeTarget, Func<System.Drawing.Rectangle?> inputArea)
    {
        if (IsRunning) return;
        if (settings.ValidateAutomation() is { } error) throw new ArgumentException(error);
        var source = new CancellationTokenSource();
        cancellation = source;
        stopReason = L.T("Stop automation", "자동 입력 중지");
        completion = Run(settings.Snapshot(), source, safeTarget, inputArea);
    }

    public void Stop(string? reason = null)
    {
        stopReason = reason ?? L.T("Stop automation", "자동 입력 중지");
        cancellation?.Cancel();
    }

    private async Task Run(Settings settings, CancellationTokenSource source, Func<bool> safeTarget, Func<System.Drawing.Rectangle?> inputArea)
    {
        var token = source.Token;
        try
        {
            await Task.Run(async () =>
            {
                using var monitor = new PhysicalKeyboardMonitor(() =>
                {
                    stopReason = L.T("Automation stopped by physical keyboard input.", "실제 키보드 입력으로 자동 동작을 종료했습니다.");
                    source.Cancel();
                });
                await monitor.Ready;
                var releaseWait = Stopwatch.StartNew();
                while (!monitor.InitialKeysReleased)
                {
                    if (releaseWait.Elapsed > TimeSpan.FromSeconds(15))
                        throw new InvalidOperationException(L.T("Release the held key and start again.", "시작 키가 눌려 있습니다. 키를 뗀 뒤 다시 시작해 주세요."));
                    await Task.Delay(20, token);
                }
                await Task.Delay(TimeSpan.FromSeconds(settings.StartDelaySeconds), token);
                token.ThrowIfCancellationRequested();
                Activated?.Invoke();
                while (true)
                {
                    Guard(token, safeTarget);
                    await Cycle(settings, token, safeTarget, inputArea());
                    var wait = Stopwatch.StartNew();
                    while (wait.Elapsed.TotalSeconds < settings.IntervalSeconds)
                    {
                        Guard(token, safeTarget);
                        await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(settings.IntervalSeconds - wait.Elapsed.TotalSeconds, 0.001, 0.1)), token);
                    }
                }
            }, token);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { stopReason = ex.Message; }
        finally
        {
            cancellation = null;
            source.Dispose();
            Finished?.Invoke(stopReason);
        }
    }

    private static void Guard(CancellationToken token, Func<bool> safeTarget)
    {
        token.ThrowIfCancellationRequested();
        if (!safeTarget()) throw new InvalidOperationException(L.T("Automation stopped because the target changed. Start again to resume.", "입력 대상이 바뀌어 자동 동작을 종료했습니다. 다시 시작해 주세요."));
    }

    private static async Task Cycle(Settings s, CancellationToken token, Func<bool> safeTarget, System.Drawing.Rectangle? area)
    {
        if (s.HasMove || (s.HasClick && area != null))
        {
            Guard(token, safeTarget);
            if (!Native.GetCursorPos(out var p)) throw new InvalidOperationException(L.T("Unable to read the mouse position.", "마우스 위치를 읽을 수 없습니다."));
            if (area is { } bounds && !bounds.Contains(p.X, p.Y))
            { p.X = bounds.Left + bounds.Width / 2; p.Y = bounds.Top + bounds.Height / 2; }
            if (s.HasMove)
            {
                var direction = s.Direction == MoveDirection.Random ? Random.Shared.Next(8) : (int)s.Direction - 1;
                var offsets = new[] { (0, -1), (0, 1), (-1, 0), (1, 0), (-1, -1), (1, -1), (-1, 1), (1, 1) };
                p.X += offsets[direction].Item1 * s.MovePixels;
                p.Y += offsets[direction].Item2 * s.MovePixels;
            }
            if (area is { } r)
            { p.X = Math.Clamp(p.X, r.Left, r.Right - 1); p.Y = Math.Clamp(p.Y, r.Top, r.Bottom - 1); }
            Guard(token, safeTarget);
            if (!Native.SetCursorPos(p.X, p.Y)) throw new InvalidOperationException(L.T("Unable to move the mouse.", "마우스를 이동할 수 없습니다."));
        }
        if (s.HasClick)
        {
            Guard(token, safeTarget);
            Native.Click(s.Click == ClickKind.Right);
            if (s.Click == ClickKind.Double)
            {
                await Task.Delay(70, token);
                Guard(token, safeTarget);
                Native.Click(false);
            }
        }
        if (!s.HasKeyboard) return;
        if (s.Keyboard is KeyboardKind.Text or KeyboardKind.Random)
        {
            const string alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            var text = s.Keyboard == KeyboardKind.Text ? s.Text : alphabet[Random.Shared.Next(alphabet.Length)].ToString();
            foreach (var c in text)
            {
                Guard(token, safeTarget);
                Native.Key(c, true);
                await Task.Delay(15, token);
            }
        }
        else
        {
            Guard(token, safeTarget);
            Native.Key(s.Keyboard switch { KeyboardKind.Space => 0x20, KeyboardKind.Enter => 0x0D, KeyboardKind.Tab => 0x09, _ => 0x7E }, false);
        }
    }
}
