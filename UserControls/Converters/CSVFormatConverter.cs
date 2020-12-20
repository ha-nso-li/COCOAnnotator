using COCOAnnotator.Records.Enums;
using System.Windows.Data;

namespace COCOAnnotator.UserControls.Converters {
    [ValueConversion(typeof(CSVFormat), typeof(bool))]
    public class CSVFormatConverter : EnumConverter<CSVFormat> { }
}
