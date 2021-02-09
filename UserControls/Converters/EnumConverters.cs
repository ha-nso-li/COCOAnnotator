using COCOAnnotator.Records.Enums;
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

    [ValueConversion(typeof(CSVFormat), typeof(bool))]
    public class CSVFormatConverter : EnumConverter<CSVFormat> { }

    [ValueConversion(typeof(SettingColors), typeof(bool))]
    public class SettingColorConverter : EnumConverter<SettingColors> { }

    [ValueConversion(typeof(TacticsForConvertDataset), typeof(bool))]
    public class TacticForConvertDatasetConverter : EnumConverter<TacticsForConvertDataset> { }

    [ValueConversion(typeof(TacticsForSplitDataset), typeof(bool))]
    public class TacticForSplitDatasetConverter : EnumConverter<TacticsForSplitDataset> { }

    [ValueConversion(typeof(TacticsForUndupeDataset), typeof(bool))]
    public class TacticForUndupeDatasetConverter : EnumConverter<TacticsForUndupeDataset> { }
}
