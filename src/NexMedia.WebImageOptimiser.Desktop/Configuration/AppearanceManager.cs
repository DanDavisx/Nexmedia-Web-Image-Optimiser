// Manages which application theme is shown.

using System.Windows;
using System.Windows.Controls.Primitives;
using NexMedia.WebImageOptimiser.Core.Configuration;

namespace NexMedia.WebImageOptimiser.Desktop.Configuration;

internal static class AppearanceManager
{
    public static void ApplyTheme(AppTheme theme)
    {
        var palette = new ResourceDictionary
        {
            Source = new Uri($"pack://application:,,,/NexMedia.WebImageOptimiser.Desktop;component/Themes/{theme}Palette.xaml", UriKind.Absolute)
        };
        foreach (object key in palette.Keys)
            Application.Current.Resources[key] = palette[key];
    }

    public static void ApplyAnimations(bool enabled) =>
        Application.Current.Resources["InterfacePopupAnimation"] =
            enabled ? PopupAnimation.Fade : PopupAnimation.None;
}
