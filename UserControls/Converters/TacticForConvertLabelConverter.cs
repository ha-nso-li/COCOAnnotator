using LabelAnnotator.Records.Enums;
using System.Windows.Data;

namespace LabelAnnotator.UserControls.Converters {
    [ValueConversion(typeof(TacticsForConvertLabel), typeof(bool))]
    public class TacticForConvertLabelConverter : EnumConverter<TacticsForConvertLabel> { }
}
