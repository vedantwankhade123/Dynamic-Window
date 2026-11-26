using System.Globalization;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace Newton.App.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool flag = value is bool b && b;
        if (parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase))
        {
            flag = !flag;
        }
        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is Visibility v && v == Visibility.Visible;
}

public sealed class HexToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string hex && hex.StartsWith('#') && (hex.Length == 7 || hex.Length == 9))
        {
            try
            {
                byte a = 255;
                int offset = 1;
                if (hex.Length == 9)
                {
                    a = byte.Parse(hex.Substring(1, 2), NumberStyles.HexNumber);
                    offset = 3;
                }
                byte r = byte.Parse(hex.Substring(offset, 2), NumberStyles.HexNumber);
                byte g = byte.Parse(hex.Substring(offset + 2, 2), NumberStyles.HexNumber);
                byte b = byte.Parse(hex.Substring(offset + 4, 2), NumberStyles.HexNumber);
                return new SolidColorBrush(Color.FromArgb(a, r, g, b));
            }
            catch
            {
                // Fallback below
            }
        }
        return new SolidColorBrush(Color.FromArgb(255, 96, 165, 250));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}

public sealed class TimeSpanFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is TimeSpan ts)
        {
            return ts.TotalHours >= 1 ? ts.ToString(@"h\:mm\:ss") : ts.ToString(@"m\:ss");
        }
        return "0:00";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
