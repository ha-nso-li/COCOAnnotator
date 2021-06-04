using System;
using System.Globalization;
using System.Windows.Data;

namespace COCOAnnotator.UserControls.Converters {
    public class ButtonNameConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => parameter.Equals("CloseDataset") ? (value.Equals("") ? "이미지 폴더 지정" : "데이터셋 닫기") : Binding.DoNothing;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
}
