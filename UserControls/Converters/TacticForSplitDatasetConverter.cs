using COCOAnnotator.Records.Enums;
using COCOAnnotator.Views;
using System;
using System.Globalization;
using System.Windows.Data;

namespace COCOAnnotator.UserControls.Converters {
    [ValueConversion(typeof(TacticsForSplitDataset), typeof(bool))]
    public class TacticForSplitDatasetConverter : EnumConverter<TacticsForSplitDataset> {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if ((string)parameter == nameof(ManageDialog.TxtNValueForSplitDataset)) {
                // TxtNValueForSplitDataset.IsEnabled
                return !value.Equals(TacticsForSplitDataset.SplitToSubFolders);
            }
            return base.Convert(value, targetType, parameter, culture);
        }
    }
}
