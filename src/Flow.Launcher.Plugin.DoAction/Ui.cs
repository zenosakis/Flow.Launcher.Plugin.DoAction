using System.Windows;
using System.Windows.Controls;
using System.Windows.Automation;
using System.Windows.Media;

namespace Flow.Launcher.Plugin.DoAction;

internal static class Ui
{
    public static void Surface(Control control)
    {
        control.SetResourceReference(Control.BackgroundProperty, SystemColors.WindowBrushKey);
        control.SetResourceReference(Control.ForegroundProperty, SystemColors.WindowTextBrushKey);
        control.FontFamily = new FontFamily("Malgun Gothic, Segoe UI");
        control.FontSize = 14;
    }

    public static TextBlock Text(string text, double size = 14) => new()
    { Text = text, FontSize = size, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12) };

    public static Button Button(string text, RoutedEventHandler click)
    {
        var button = new Button { Content = text, MinHeight = 36, Padding = new Thickness(16, 6, 16, 6), Margin = new Thickness(0, 8, 8, 0) };
        AutomationProperties.SetName(button, text);
        button.Click += click;
        return button;
    }
}
