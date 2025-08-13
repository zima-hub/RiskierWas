using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace RiskierWas  // <-- WICHTIG: genau so, ohne ".Converters"
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
            => value is bool b && b ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type t, object p, CultureInfo c)
            => value is Visibility v && v == Visibility.Visible;
    }

    public class CorrectToTextConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
            => value is bool b && b ? "Richtig" : "Falsch";

        public object ConvertBack(object value, Type t, object p, CultureInfo c)
            => throw new NotImplementedException();
    }

    public class CorrectToSymbolConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
            => value is bool b && b ? "✓" : "✗";

        public object ConvertBack(object value, Type t, object p, CultureInfo c)
            => throw new NotImplementedException();
    }

    public class ReferenceEqualsConverter : IValueConverter
    {
        public object Convert(object value, Type t, object parameter, CultureInfo c)
            => ReferenceEquals(value, parameter);

        public object ConvertBack(object value, Type t, object p, CultureInfo c)
            => throw new NotImplementedException();
    }

    public class CorrectToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type t, object p, CultureInfo c)
        {
            if (value is bool b)
                return b
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#144B2F"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B1E1E"));
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type t, object p, CultureInfo c)
            => throw new NotImplementedException();
    }
}
