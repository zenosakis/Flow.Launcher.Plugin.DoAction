using System;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using DoAction.Core;

namespace Flow.Launcher.Plugin.DoAction;

internal sealed class SettingsPanel : UserControl
{
    private readonly Settings settings;
    private readonly Action save;
    private readonly ComboBox mode, direction, click, keyboard, power, schedule;
    private readonly TextBox interval, delay, distance, input, minutes, time, warning;
    private readonly CheckBox fullscreen, force;
    private readonly DatePicker date;
    private readonly StackPanel moveOptions, clickOptions, keyOptions, textOptions, relativeOptions, absoluteOptions;
    private readonly TextBlock message;

    public SettingsPanel(Settings settings, Action save)
    {
        this.settings = settings;
        this.save = save;
        Ui.Surface(this);
        var panel = new StackPanel { Margin = new Thickness(24), MaxWidth = 720, HorizontalAlignment = HorizontalAlignment.Stretch };
        panel.Children.Add(Ui.Text(L.T("DoAction settings", "DoAction 설정"), 24));
        panel.Children.Add(Ui.Text(L.T("Save settings, then type da in Flow to start automation or schedule a power action. These features operate independently.", "설정을 저장한 뒤 Flow에서 da를 입력해 시작하거나 예약하세요. 두 기능은 독립적으로 동작합니다.")));
        panel.Children.Add(Ui.Text(L.T("Automation", "자동 입력"), 19));
        mode = Choice(panel, L.T("Action mode", "동작 모드"), new[] { L.T("Mouse movement", "마우스 이동"), L.T("Mouse clicks", "마우스 클릭"), L.T("Move → click", "이동 → 클릭"), L.T("Keyboard input", "키보드 입력"), L.T("Move → type", "이동 → 키보드"), L.T("Move → click → type", "이동 → 클릭 → 키보드") }, (int)settings.Mode);
        interval = Field(panel, L.T("Interval (seconds, 0.1–86400)", "반복 간격 (초, 0.1~86400)"), settings.IntervalSeconds.ToString(CultureInfo.CurrentCulture));
        delay = Field(panel, L.T("Start delay (seconds, 1–60)", "시작 대기 (초, 1~60)"), settings.StartDelaySeconds.ToString());
        moveOptions = Section(panel);
        direction = Choice(moveOptions, L.T("Movement direction", "이동 방향"), new[] { L.T("Random (8 directions)", "무작위 (8방향)"), L.T("Up", "위"), L.T("Down", "아래"), L.T("Left", "왼쪽"), L.T("Right", "오른쪽") }, (int)settings.Direction);
        distance = Field(moveOptions, L.T("Movement distance (pixels, 1–1000)", "이동 거리 (px, 1~1000)"), settings.MovePixels.ToString());
        clickOptions = Section(panel);
        click = Choice(clickOptions, L.T("Click type", "클릭 종류"), new[] { L.T("Left click", "왼쪽 클릭"), L.T("Right click", "오른쪽 클릭"), L.T("Double left click", "왼쪽 더블 클릭") }, (int)settings.Click);
        keyOptions = Section(panel);
        keyboard = Choice(keyOptions, L.T("Keyboard input", "키보드 입력"), new[] { L.T("One random letter or digit", "무작위 영문·숫자 1자"), L.T("Custom text", "지정 문자열"), "Space", "Enter", "Tab", "F15" }, (int)settings.Keyboard);
        textOptions = Section(keyOptions);
        input = Field(textOptions, L.T("Input text (up to 500 characters)", "입력 문자열 (최대 500자)"), settings.Text);
        fullscreen = Check(panel, L.T("Fullscreen · show dedicated input surfaces on all monitors", "전체화면 사용 · 모든 모니터에 전용 입력 화면 표시"), settings.FullScreen);
        panel.Children.Add(Ui.Text(L.T("Press any physical key to stop all automation and close fullscreen. Automation never resumes automatically. Power schedules remain active.\nWithout fullscreen, input goes to the active window. Switching to Flow or leaving fullscreen stops automation.", "실제 키보드의 아무 키나 누르면 이동·클릭·입력이 모두 종료되고 전체화면이 닫힙니다. 자동 재개하지 않으며 PC 예약은 유지됩니다.\n전체화면을 끄면 현재 활성 창에 입력합니다. Flow 창이나 전체화면 밖으로 전환하면 자동 동작을 종료합니다.")));
        panel.Children.Add(new Separator { Margin = new Thickness(0, 12, 0, 20) });
        panel.Children.Add(Ui.Text(L.T("Schedule shutdown or restart", "PC 종료·재부팅 예약"), 19));
        power = Choice(panel, L.T("Power action", "예약 작업"), new[] { L.T("PC shutdown", "PC 종료"), L.T("PC restart", "PC 재부팅") }, (int)settings.Power);
        schedule = Choice(panel, L.T("Schedule type", "예약 방식"), new[] { L.T("After a delay", "몇 분 후"), L.T("At a date and time", "날짜·시각 지정") }, (int)settings.Schedule);
        relativeOptions = Section(panel);
        minutes = Field(relativeOptions, L.T("Delay in minutes (1–10080)", "몇 분 후 (1~10080)"), settings.AfterMinutes.ToString());
        absoluteOptions = Section(panel);
        date = new DatePicker { SelectedDate = settings.AtLocal.Date, MinHeight = 32, Margin = new Thickness(0, 0, 0, 12) };
        Label(absoluteOptions, L.T("Date (PC local time)", "예약 날짜 (PC 현지 시간)"), date);
        absoluteOptions.Children.Add(date);
        time = Field(absoluteOptions, L.T("Time (24-hour HH:mm)", "예약 시각 (24시간 HH:mm)"), settings.AtLocal.ToString("HH:mm"));
        warning = Field(panel, L.T("Warning (seconds, 10–300)", "실행 전 알림 (초, 10~300)"), settings.WarningSeconds.ToString());
        force = Check(panel, L.T("Force close apps · unsaved work may be lost", "앱 강제 종료 · 저장하지 않은 내용이 사라질 수 있음"), settings.ForceClose);
        panel.Children.Add(Ui.Text(L.T("Schedules run once while Flow is open. Exiting Flow or sleeping cancels them; they are never restored automatically.\nBefore execution, automation stops and a cancelable warning appears. With force close off, applications may prevent shutdown.", "Flow 실행 중에만 유지되는 1회 예약입니다. Flow 종료·절전 시 취소되며 다시 켜져도 복구되지 않습니다.\n실행 전에는 자동 입력을 중지하고 취소 가능한 안내 창을 표시합니다. 강제 종료를 끄면 앱이 PC 종료를 막을 수 있습니다.")));
        message = Ui.Text("");
        AutomationProperties.SetLiveSetting(message, AutomationLiveSetting.Polite);
        panel.Children.Add(Ui.Button(L.T("Save settings", "설정 저장"), (_, _) => Save()));
        panel.Children.Add(message);
        Content = new ScrollViewer { Content = panel, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        mode.SelectionChanged += (_, _) => UpdateVisibility();
        keyboard.SelectionChanged += (_, _) => UpdateVisibility();
        schedule.SelectionChanged += (_, _) => UpdateVisibility();
        UpdateVisibility();
    }

    private void Save()
    {
        try
        {
            var candidate = settings.Snapshot();
            candidate.Mode = (ActionMode)mode.SelectedIndex;
            candidate.IntervalSeconds = Number(interval, L.T("Interval", "반복 간격"));
            candidate.StartDelaySeconds = Integer(delay, L.T("Start delay", "시작 대기"));
            candidate.Direction = (MoveDirection)direction.SelectedIndex;
            candidate.MovePixels = Integer(distance, L.T("Movement distance", "이동 거리"));
            candidate.Click = (ClickKind)click.SelectedIndex;
            candidate.Keyboard = (KeyboardKind)keyboard.SelectedIndex;
            candidate.Text = input.Text;
            candidate.FullScreen = fullscreen.IsChecked == true;
            candidate.Power = (PowerKind)power.SelectedIndex;
            candidate.Schedule = (ScheduleKind)schedule.SelectedIndex;
            candidate.AfterMinutes = Integer(minutes, L.T("Schedule delay", "예약 시간"));
            candidate.WarningSeconds = Integer(warning, L.T("Warning", "실행 전 알림"));
            candidate.ForceClose = force.IsChecked == true;
            if (candidate.Schedule == ScheduleKind.AtDateTime)
            {
                if (date.SelectedDate == null || !TimeSpan.TryParseExact(time.Text.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out var clock))
                    throw new ArgumentException(L.T("Check the date and time. Example time: 18:05", "날짜와 시각을 확인해 주세요. 시각 예: 18:05"));
                candidate.AtLocal = date.SelectedDate.Value.Date.Add(clock);
            }
            if (candidate.ValidateAutomation() is { } automationError) throw new ArgumentException(automationError);
            if (candidate.ValidatePower() is { } powerError) throw new ArgumentException(powerError);
            // Persist preferences, never mutate an already-running snapshot/reservation.
            foreach (var property in typeof(Settings).GetProperties())
                if (property.CanWrite) property.SetValue(settings, property.GetValue(candidate));
            save();
            message.Text = L.T("Saved. Changes apply to the next start or schedule.", "저장했습니다. 다음 시작·예약부터 적용됩니다.");
        }
        catch (Exception ex) { message.Text = ex.Message; }
    }

    private void UpdateVisibility()
    {
        var state = new Settings { Mode = (ActionMode)mode.SelectedIndex };
        moveOptions.Visibility = state.HasMove ? Visibility.Visible : Visibility.Collapsed;
        clickOptions.Visibility = state.HasClick ? Visibility.Visible : Visibility.Collapsed;
        keyOptions.Visibility = state.HasKeyboard ? Visibility.Visible : Visibility.Collapsed;
        textOptions.Visibility = keyboard.SelectedIndex == (int)KeyboardKind.Text ? Visibility.Visible : Visibility.Collapsed;
        relativeOptions.Visibility = schedule.SelectedIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
        absoluteOptions.Visibility = schedule.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static StackPanel Section(Panel parent)
    { var panel = new StackPanel(); parent.Children.Add(panel); return panel; }
    private static void Label(Panel parent, string text, FrameworkElement target)
    {
        AutomationProperties.SetName(target, text);
        parent.Children.Add(new Label { Content = text, Target = target, Padding = new Thickness(0, 0, 0, 6) });
    }
    private static TextBox Field(Panel parent, string title, string value)
    {
        var box = new TextBox { Text = value, MinHeight = 32, Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(0, 0, 0, 12) };
        Label(parent, title, box); parent.Children.Add(box); return box;
    }
    private static ComboBox Choice(Panel parent, string title, string[] items, int selected)
    {
        var box = new ComboBox { ItemsSource = items, SelectedIndex = Math.Clamp(selected, 0, items.Length - 1), MinHeight = 32, Margin = new Thickness(0, 0, 0, 12) };
        Label(parent, title, box); parent.Children.Add(box); return box;
    }
    private static CheckBox Check(Panel parent, string title, bool value)
    {
        var box = new CheckBox { Content = Ui.Text(title), IsChecked = value, Margin = new Thickness(0, 8, 0, 8) };
        AutomationProperties.SetName(box, title); parent.Children.Add(box); return box;
    }
    private static double Number(TextBox field, string name) => double.TryParse(field.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out var value)
        ? value : throw new ArgumentException(L.T($"{name}: Enter a valid number.", $"{name}: 올바른 숫자를 입력해 주세요."));
    private static int Integer(TextBox field, string name) => int.TryParse(field.Text, out var value)
        ? value : throw new ArgumentException(L.T($"{name}: Enter a whole number.", $"{name}: 정수를 입력해 주세요."));
}
