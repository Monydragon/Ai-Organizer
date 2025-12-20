using Avalonia.Data.Converters;

namespace Ai_Organizer.Infrastructure;

public static class ObjectConverters
{
    public static IValueConverter IsNotNull { get; } = new FuncValueConverter<object?, bool>(value => value is not null);

    public static IValueConverter IsNull { get; } = new FuncValueConverter<object?, bool>(value => value is null);

    public static IValueConverter IsZero { get; } = new FuncValueConverter<int, bool>(value => value == 0);

    public static IValueConverter IsGreaterThanZero { get; } = new FuncValueConverter<int, bool>(value => value > 0);
}
