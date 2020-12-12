using LabelAnnotator.Records.Enums;
using System.Windows.Data;

namespace LabelAnnotator.UserControls.Converters {
    [ValueConversion(typeof(SettingColors), typeof(bool))]
    public class SettingColorConverter : EnumConverter<SettingColors> { }
}
