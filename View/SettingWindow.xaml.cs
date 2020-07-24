using System.Windows;

namespace LabelAnnotator {
    public partial class SettingWindow : Window {
        public SettingWindow() {
            InitializeComponent();

            switch (SettingManager.Format) {
                case "LTRB":
                    RadLTRB.IsChecked = true;
                    RadXYWH.IsChecked = false;
                    break;
                case "XYWH":
                    RadLTRB.IsChecked = false;
                    RadXYWH.IsChecked = true;
                    break;
            }
        }

        private void OnClose(object sender, RoutedEventArgs e) {
            Close();
        }

        private void OnRadioClick(object sender, RoutedEventArgs e) {
            if (RadLTRB.IsChecked.GetValueOrDefault()) SettingManager.Format = "LTRB";
            else if (RadXYWH.IsChecked.GetValueOrDefault()) SettingManager.Format = "XYWH";
        }
    }
}
