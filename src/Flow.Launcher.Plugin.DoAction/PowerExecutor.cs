using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DoAction.Core;

namespace Flow.Launcher.Plugin.DoAction;

internal static class PowerExecutor
{
    // Countdown is owned by the plugin. /t > 0 implies /f, so always use /t 0.
    internal static string Arguments(Reservation r) => $"{(r.Power == PowerKind.Shutdown ? "/s" : "/r")} /t 0{(r.ForceClose ? " /f" : "")}";
    public static async Task Execute(Reservation reservation)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.SystemDirectory, "shutdown.exe"),
            Arguments = Arguments(reservation), UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardError = true, RedirectStandardOutput = true
        }) ?? throw new InvalidOperationException(L.T("Unable to execute the power request.", "종료 요청을 실행하지 못했습니다."));
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
            throw new InvalidOperationException(L.T($"Power request failed ({process.ExitCode}): {await error} {await output}", $"종료 요청 실패 ({process.ExitCode}): {await error} {await output}"));
    }
}
