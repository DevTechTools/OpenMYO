using System.Globalization;
using Avalonia.Data.Converters;

namespace OpenMyo.App.Converters;

/// <summary>Formats a <see cref="TimeSpan"/> as "3:07" or "1:02:45".</summary>
public sealed class DurationConverter : IValueConverter
{
    public static DurationConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is TimeSpan span && span > TimeSpan.Zero ? Format(span, culture) : string.Empty;

    /// <summary>"3:07", "1:02:45", or "0:00" for zero or less.</summary>
    public static string Format(TimeSpan span, IFormatProvider culture) => span switch
    {
        { TotalHours: >= 1 } => span.ToString(@"h\:mm\:ss", culture),
        _ when span > TimeSpan.Zero => span.ToString(@"m\:ss", culture),
        _ => "0:00",
    };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
