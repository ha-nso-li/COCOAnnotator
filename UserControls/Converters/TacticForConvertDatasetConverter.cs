using COCOAnnotator.Records.Enums;
using System.Windows.Data;

namespace COCOAnnotator.UserControls.Converters {
    [ValueConversion(typeof(TacticsForConvertDataset), typeof(bool))]
    public class TacticForConvertDatasetConverter : EnumConverter<TacticsForConvertDataset> { }
}
