using System.Globalization;
using System.Windows.Data;

namespace NexMedia.WebImageOptimiser.Desktop.Converters;

internal sealed class FileSizeConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        if (value is not IConvertible convertible)
        {
            return "—";
        }

        double bytes;

        try
        {
            bytes = convertible.ToDouble(CultureInfo.InvariantCulture);
        }
        catch (Exception exception)
            when (exception is FormatException or
                  InvalidCastException or
                  OverflowException)
        {
            return "—";
        }

        if (bytes < 1000)
        {
            return $"{bytes:0} B";
        }

        if (bytes < 1_000_000)
        {
            return $"{bytes / 1000:0.#} KB";
        }

        if (bytes < 1_000_000_000)
        {
            return $"{bytes / 1_000_000:0.##} MB";
        }

        return $"{bytes / 1_000_000_000:0.##} GB";
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture) =>
        throw new NotSupportedException();
}
