using System;
using System.Windows;
using System.Windows.Controls;
using DoAction.Core;

namespace Flow.Launcher.Plugin.DoAction;

internal sealed class PowerWarningWindow : Window
{
    private readonly TextBlock countdown;
    private bool completing;
    public PowerWarningWindow(Reservation reservation, Action cancel)
    {
        Title = L.T("DoAction · Power schedule", "DoAction · PC 예약");
        Width = 480;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ResizeMode = ResizeMode.NoResize;
        Topmost = true;
        Ui.Surface(this);
        var panel = new StackPanel { Margin = new Thickness(28) };
        countdown = Ui.Text("", 24);
        panel.Children.Add(countdown);
        panel.Children.Add(Ui.Text(L.T($"Scheduled time: {reservation.Due.LocalDateTime:yyyy-MM-dd HH:mm:ss}\nSave your open documents.", $"예약 시각: {reservation.Due.LocalDateTime:yyyy-MM-dd HH:mm:ss}\n열려 있는 문서를 저장해 주세요.")));
        panel.Children.Add(Ui.Text(reservation.ForceClose ? L.T("Force close is on. Unsaved work may be lost.", "강제 종료가 켜져 있습니다. 저장하지 않은 내용이 사라질 수 있습니다.") : L.T("Force close is off. Applications with unsaved work may delay shutdown.", "강제 종료 꺼짐 · 저장하지 않은 앱이 있으면 종료가 지연될 수 있습니다.")));
        panel.Children.Add(Ui.Button(L.T("Cancel schedule", "예약 취소"), (_, _) => cancel()));
        Content = panel;
        Closing += (_, e) => { if (!completing) { e.Cancel = true; cancel(); } };
        Update(reservation);
    }

    public void Update(Reservation r) => countdown.Text = L.T($"{Math.Max(0, Math.Ceiling((r.Due - DateTimeOffset.Now).TotalSeconds))} seconds until PC {(r.Power == PowerKind.Shutdown ? "shutdown" : "restart")}", $"{Math.Max(0, Math.Ceiling((r.Due - DateTimeOffset.Now).TotalSeconds))}초 후 PC {(r.Power == PowerKind.Shutdown ? "종료" : "재부팅")}");
    public void Dismiss() { completing = true; Close(); }
}
