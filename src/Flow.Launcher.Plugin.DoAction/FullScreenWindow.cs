using DoAction.Core;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Input;

namespace Flow.Launcher.Plugin.DoAction;

internal sealed class FullScreenWindow : Window
{
    private readonly TextBox input;
    public IntPtr Handle => new WindowInteropHelper(this).Handle;
    public System.Drawing.Rectangle GetInputArea()
    {
        var p = input.PointToScreen(new Point(24, 24));
        var q = input.PointToScreen(new Point(Math.Max(25, input.ActualWidth - 24), Math.Max(25, input.ActualHeight - 24)));
        return System.Drawing.Rectangle.FromLTRB((int)p.X, (int)p.Y, (int)q.X, (int)q.Y);
    }

    public FullScreenWindow(System.Drawing.Rectangle bounds, bool primary, Action stop)
    {
        Title = L.T("DoAction · Automation", "DoAction · 자동 입력");
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = primary;
        ShowActivated = primary;
        Topmost = true;
        Ui.Surface(this);
        var grid = new Grid { Margin = new Thickness(48) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.Children.Add(Ui.Text(L.T("DoAction is running", "DoAction 실행 중"), 28));
        var hint = Ui.Text(L.T("Press any physical key to stop automation completely. Power schedules remain active.", "실제 키보드에서 아무 키나 누르면 완전히 중지합니다. PC 예약은 유지됩니다."));
        Grid.SetRow(hint, 1);
        grid.Children.Add(hint);
        input = new TextBox
        {
            AcceptsReturn = true, AcceptsTab = true, TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Hidden, Padding = new Thickness(20),
            FontSize = 20, ContextMenu = new ContextMenu(), IsUndoEnabled = false
        };
        System.Windows.Automation.AutomationProperties.SetName(input, L.T("Dedicated automation input area", "자동 입력 전용 영역"));
        ContextMenuService.SetIsEnabled(input, false);
        input.TextChanged += (_, _) => { if (input.Text.Length > 2000) input.Clear(); };
        Grid.SetRow(input, 2);
        grid.Children.Add(input);
        var button = Ui.Button(L.T("Stop automation", "자동 동작 중지"), (_, _) => stop());
        button.HorizontalAlignment = HorizontalAlignment.Left;
        Grid.SetRow(button, 3);
        grid.Children.Add(button);
        Content = grid;
        SourceInitialized += (_, _) => Native.SetWindowPos(Handle, new IntPtr(-1), bounds.X, bounds.Y, bounds.Width, bounds.Height, 0x10);
        Loaded += (_, _) =>
        {
            Native.SetWindowPos(Handle, new IntPtr(-1), bounds.X, bounds.Y, bounds.Width, bounds.Height, 0x10);
            UpdateLayout();
            if (primary) { Activate(); Keyboard.Focus(input); }
        };
    }
}
