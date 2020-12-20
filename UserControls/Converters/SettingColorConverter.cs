using COCOAnnotator.Records.Enums;
using System.Windows.Data;

namespace COCOAnnotator.UserControls.Converters {
    [ValueConversion(typeof(SettingColors), typeof(bool))]
    public class SettingColorConverter : EnumConverter<SettingColors> { }
}
