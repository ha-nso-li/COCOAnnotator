using System;
using System.Globalization;
using System.Windows.Data;

namespace COCOAnnotator.UserControls.Converters {
    public class ButtonNameConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (parameter.Equals("CloseDataset")) {
                if (value.Equals("")) return "이미지 폴더 지정";
                else return "데이터셋 닫기";
            }
            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return Binding.DoNothing;
        }
    }
}
