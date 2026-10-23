namespace DoAction.Core;

public enum ScheduleEvent { None, Warning, Execute, Expired }
public sealed record Reservation(PowerKind Power, bool ForceClose, DateTimeOffset Due, int WarningSeconds);

// Driven by the UI dispatcher: cancellation, warning and execution are serialized.
// No persisted active state: restarting Flow never re-arms a reservation.
public sealed class PowerSchedule
{
    public Reservation? Active { get; private set; }
    private DateTimeOffset lastTick;
    private bool warned;

    public void Arm(Settings settings, DateTimeOffset now)
    {
        if (Active is not null) throw new InvalidOperationException(L.T("Cancel the existing schedule first.", "기존 예약을 먼저 취소해 주세요."));
        Active = new(settings.Power, settings.ForceClose, settings.ResolveDue(now), settings.WarningSeconds);
        lastTick = now;
        warned = false;
    }

    public void Cancel() { Active = null; warned = false; }

    public ScheduleEvent Tick(DateTimeOffset now)
    {
        if (Active is not { } active) return ScheduleEvent.None;
        // A suspended/stalled process must not suddenly shut the PC down on return.
        if (now < lastTick.AddSeconds(-2) || now - lastTick > TimeSpan.FromSeconds(10))
        {
            Cancel();
            return ScheduleEvent.Expired;
        }
        lastTick = now;
        if (now >= active.Due)
        {
            var result = warned ? ScheduleEvent.Execute : ScheduleEvent.Expired;
            Cancel();
            return result;
        }
        if (!warned && active.Due - now <= TimeSpan.FromSeconds(active.WarningSeconds))
        {
            warned = true;
            return ScheduleEvent.Warning;
        }
        return ScheduleEvent.None;
    }
}
