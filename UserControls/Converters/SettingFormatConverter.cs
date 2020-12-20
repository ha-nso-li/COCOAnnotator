using COCOAnnotator.Records.Enums;
using System.Windows.Data;

namespace COCOAnnotator.UserControls.Converters {
    [ValueConversion(typeof(SettingFormats), typeof(bool))]
    public class SettingFormatConverter : EnumConverter<SettingFormats> { }
}
