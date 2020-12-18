using LabelAnnotator.Records.Enums;
using LabelAnnotator.Views;
using System;
using System.Globalization;
using System.Windows.Data;

namespace LabelAnnotator.UserControls.Converters {
    [ValueConversion(typeof(TacticsForSplitLabel), typeof(bool))]
    public class TacticForSplitLabelConverter : EnumConverter<TacticsForSplitLabel> {
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if ((string)parameter == nameof(ManageDialog.TxtNValueForSplitLabel)) {
                // TxtNValueForSplitLabel.IsEnabled
                return !value.Equals(TacticsForSplitLabel.SplitToSubFolders);
            }
            return base.Convert(value, targetType, parameter, culture);
        }
    }
}
