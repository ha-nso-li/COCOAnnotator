using LabelAnnotator.Records.Enums;
using System.Windows.Data;

namespace LabelAnnotator.UserControls.Converters {
    [ValueConversion(typeof(SettingFormats), typeof(bool))]
    public class SettingFormatConverter : EnumConverter<SettingFormats> { }
}
