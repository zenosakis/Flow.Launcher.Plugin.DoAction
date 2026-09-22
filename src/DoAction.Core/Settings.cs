namespace DoAction.Core;

public enum ActionMode { Move, Click, MoveClick, Keyboard, MoveKeyboard, MoveClickKeyboard }
public enum MoveDirection { Random, Up, Down, Left, Right }
public enum ClickKind { Left, Right, Double }
public enum KeyboardKind { Random, Text, Space, Enter, Tab, F15 }
public enum PowerKind { Shutdown, Restart }
public enum ScheduleKind { AfterMinutes, AtDateTime }

public sealed class Settings
{
    public ActionMode Mode { get; set; } = ActionMode.Move;
    public double IntervalSeconds { get; set; } = 5;
    public int StartDelaySeconds { get; set; } = 3;
    public int MovePixels { get; set; } = 1;
    public MoveDirection Direction { get; set; }
    public ClickKind Click { get; set; }
    public KeyboardKind Keyboard { get; set; }
    public string Text { get; set; } = "a";
    public bool FullScreen { get; set; }
    public PowerKind Power { get; set; }
    public ScheduleKind Schedule { get; set; }
    public int AfterMinutes { get; set; } = 30;
    public DateTime AtLocal { get; set; } = DateTime.Today.AddDays(1).AddHours(18);
    public int WarningSeconds { get; set; } = 60;
    public bool ForceClose { get; set; }

    public bool HasMove => Mode is ActionMode.Move or ActionMode.MoveClick or ActionMode.MoveKeyboard or ActionMode.MoveClickKeyboard;
    public bool HasClick => Mode is ActionMode.Click or ActionMode.MoveClick or ActionMode.MoveClickKeyboard;
    public bool HasKeyboard => Mode is ActionMode.Keyboard or ActionMode.MoveKeyboard or ActionMode.MoveClickKeyboard;
    public Settings Snapshot() => (Settings)MemberwiseClone();

    public string? ValidateAutomation()
    {
        if (!Enum.IsDefined(Mode) || !Enum.IsDefined(Direction) || !Enum.IsDefined(Click) || !Enum.IsDefined(Keyboard))
            return "동작 설정을 다시 선택해 주세요.";
        if (!double.IsFinite(IntervalSeconds) || IntervalSeconds < 0.1 || IntervalSeconds > 86400)
            return "반복 간격은 0.1~86400초로 입력해 주세요.";
        if (StartDelaySeconds is < 1 or > 60) return "시작 대기는 1~60초로 입력해 주세요.";
        if (MovePixels is < 1 or > 1000) return "이동 거리는 1~1000px로 입력해 주세요.";
        if (HasKeyboard && Keyboard == KeyboardKind.Text && (string.IsNullOrEmpty(Text) || Text.Length > 500 || Text.Any(char.IsControl)))
            return "입력 문자열은 제어 문자가 없는 1~500자로 입력해 주세요.";
        return null;
    }

    public string? ValidatePower()
    {
        if (!Enum.IsDefined(Power) || !Enum.IsDefined(Schedule)) return "예약 종류를 다시 선택해 주세요.";
        if (AfterMinutes is < 1 or > 10080) return "예약 시간은 1~10080분으로 입력해 주세요.";
        if (WarningSeconds is < 10 or > 300) return "실행 전 알림은 10~300초로 입력해 주세요.";
        return null;
    }

    public DateTimeOffset ResolveDue(DateTimeOffset now)
    {
        if (ValidatePower() is { } error) throw new ArgumentException(error);
        if (Schedule == ScheduleKind.AfterMinutes)
        {
            if (AfterMinutes * 60 <= WarningSeconds)
                throw new ArgumentException("예약 시간은 실행 전 알림 시간보다 길어야 합니다.");
            return now.AddMinutes(AfterMinutes);
        }
        var local = DateTime.SpecifyKind(AtLocal, DateTimeKind.Unspecified);
        if (TimeZoneInfo.Local.IsInvalidTime(local) || TimeZoneInfo.Local.IsAmbiguousTime(local))
            throw new ArgumentException("시간대 전환으로 불명확한 시각입니다. 다른 시각을 지정해 주세요.");
        var due = new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local));
        if (due <= now.AddSeconds(WarningSeconds))
            throw new ArgumentException($"예약 시각은 현재보다 {WarningSeconds}초 이상 뒤로 지정해 주세요.");
        return due;
    }
}
