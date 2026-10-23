# Flow.Launcher.Plugin.DoAction

A Windows plugin for Flow Launcher that provides two independent features: mouse/keyboard automation and scheduled PC shutdown or restart.

[Releases](https://github.com/zenosakis/Flow.Launcher.Plugin.DoAction/releases) · [Builds](https://github.com/zenosakis/Flow.Launcher.Plugin.DoAction/actions) · [Publishing guide](docs/publishing.md)

## Language

The interface follows Flow Launcher's language setting. Korean is supported; English is used for all other languages. Menus, settings, validation messages, fullscreen instructions, and power notifications are translated. Reopen the settings panel after changing languages. Dates use PC local time; scheduled times are shown in an explicit year-month-day and 24-hour format.

## Installation

Download and extract the latest release ZIP. Close Flow Launcher, run the included `Install.ps1`, and restart Flow. Alternatively, install the release ZIP through Flow's plugin manager using `pm install <ZIP URL>`.

The default installation directory is `%APPDATA%\FlowLauncher\Plugins\Flow.Launcher.Plugin.DoAction-<version>`. For portable installations, pass `Install.ps1 -PluginRoot '<portable Flow>\UserData\Plugins'`.

Type `da`, open **DoAction settings**, save your preferences, then choose **Start automation** or a power scheduling action. Saving settings or restarting Flow never starts either feature automatically.

## Automation

- Move, click, move then click, type, move then type, or move then click then type.
- Repeat every 0.1–86400 seconds, with a 1–60 second start delay.
- Configurable movement direction and distance; left, right, or double clicks.
- Type random letters/digits, custom text up to 500 characters, or Space/Enter/Tab/F15.
- Optional fullscreen surfaces on every monitor; input goes to the primary monitor's dedicated area.

**Press any physical keyboard key to stop all automation and close fullscreen.** Automation never resumes automatically. Keys held when starting must be released first. New physical input during preparation cancels the start. Injected keyboard events do not count as physical input. If keyboard monitoring fails, automation does not start.

Without fullscreen, input goes to the active window. Switching to Flow or leaving the fullscreen target stops automation. Locking the session or sleeping also stops automation. You can stop from the `da` menu. The plugin creates no tray icon.

Cancellation cannot undo characters or clicks already delivered. Text is sent in short steps with cancellation checks. Windows may block input from a normal-permission Flow process into an elevated application. Secure UAC and lock screens are not supported targets.

## Power schedules

Choose shutdown or restart, then either a delay of 1–10080 minutes or a local date and time. Activate the schedule explicitly from `da`; saving settings does not activate it. Only one schedule can be active. Changing settings does not change an active schedule.

Registration displays the action, exact local execution time, warning period, force-close setting, and cancellation instructions. Select **Status** in `da` to view the active schedule again. The result list refreshes when the schedule changes.

A cancelable warning appears 10–300 seconds before execution (60 seconds by default). Automation stops before the warning. Closing the warning window cancels the schedule. Force close is off by default; applications with unsaved work may prevent shutdown. Enabling force close can discard unsaved work.

**Stopping automation does not cancel a power schedule.** Use `da` → **Cancel power schedule**. Once the actual power request reaches Windows, the plugin cannot cancel it.

Schedules are one-time, session-only actions. Exiting Flow or sleeping cancels them; they are never restored automatically. Execution gaps longer than 10 seconds or significant clock changes expire the schedule. Session locking alone does not cancel it.

The plugin manages the countdown internally and sends `shutdown.exe /s /t 0` or `/r /t 0` only at execution. `/f` is added only when explicitly enabled. It does not cancel shutdown requests created by other programs.

## Development and verification

Requires Windows and .NET 8 SDK. Tested with Flow Launcher 2.1.4. `Flow.Launcher.Plugin` 4.7.0 is a compile-time reference; the host provides the runtime API assembly.

```powershell
./scripts/Build.ps1
```

Automated checks cover input filtering, cancellation, schedule expiry, power arguments, Flow integration, language selection, and offscreen WPF rendering. They never execute actual input or PC power operations. Packages and verification images are written under `artifacts/`.

For an explicit live automation check, run:

```powershell
dotnet run --project tests/DoAction.Tests -c Release -- --live
```

This types and clicks only inside a temporary test fullscreen for approximately four seconds. It does not execute power actions. Real application, multi-monitor, and final shutdown behavior require separate environment testing.

## Structure and contribution

- `src/DoAction.Core`: settings validation, physical-key policy, power schedule state, language selection.
- `src/Flow.Launcher.Plugin.DoAction`: Flow integration, settings, fullscreen, native input, and power warnings.
- `tests/DoAction.Tests`: executable regression checks.
- `scripts`: build, packaging, icon export, and local installation.

Write documentation, code comments, commit messages, and PR descriptions in English. Keep user-facing messages translated and preserve existing plugin IDs and settings compatibility.

## Icon and references

The original cursor-and-repeat-arrow icon was generated without reference images. PNG and multi-resolution ICO assets are in `src/Flow.Launcher.Plugin.DoAction/Images`. See [generation details](docs/icon-generation.md). Regenerate the ICO with `./scripts/Export-Icon.ps1`.

- [Flow plugin development](https://www.flowlauncher.com/docs/#/plugin-dev)
- [Windows keyboard events](https://learn.microsoft.com/en-us/windows/win32/api/winuser/ns-winuser-kbdllhookstruct)
- [Windows shutdown command](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/shutdown)
