using System;
using System.Globalization;
using System.Windows.Data;

namespace COCOAnnotator.UserControls.Converters {
    public abstract class EnumConverter<T> : IValueConverter where T : struct, Enum {
        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (Enum.TryParse((string)parameter, out T format)) {
                return value.Equals(format);
            } else {
                return Binding.DoNothing;
            }
        }
        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (Enum.TryParse((string)parameter, out T format)) {
                return (bool)value ? format : Binding.DoNothing;
            } else {
                return Binding.DoNothing;
            }
        }
    }
}
