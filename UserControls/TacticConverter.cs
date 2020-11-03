using LabelAnnotator.Records;
using System;
using System.Globalization;
using System.Windows.Data;

namespace LabelAnnotator.UserControls {
    [ValueConversion(typeof(TacticsForSplitLabel), typeof(bool))]
    public class TacticConverter : IValueConverter {
        // enum to bool
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (Enum.TryParse((string)parameter, out TacticsForSplitLabel tactic)) {
                return value.Equals(tactic);
            } else if ((string)parameter == nameof(Views.ManageDialog.TxtNValueForSplitLabel)) {
                return !value.Equals(TacticsForSplitLabel.SplitToSubFolders);
            } else {
                return Binding.DoNothing;
            }
        }

        // bool to enum
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (Enum.TryParse((string)parameter, out TacticsForSplitLabel tactic)) {
                return (bool)value ? tactic : Binding.DoNothing;
            } else {
                return Binding.DoNothing;
            }
        }
    }
}
