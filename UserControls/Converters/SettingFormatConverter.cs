using LabelAnnotator.Records.Enums;
using System;
using System.Globalization;
using System.Windows.Data;

namespace LabelAnnotator.UserControls.Converters {
    [ValueConversion(typeof(SettingFormats), typeof(bool))]
    public class SettingFormatConverter : IValueConverter {
        // enum to bool
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (Enum.TryParse((string)parameter, out SettingFormats format)) {
                return value.Equals(format);
            } else {
                return Binding.DoNothing;
            }
        }

        // bool to enum
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (Enum.TryParse((string)parameter, out SettingFormats format)) {
                return (bool)value ? format : Binding.DoNothing;
            } else {
                return Binding.DoNothing;
            }
        }
    }
}
