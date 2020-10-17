using System.Windows;

namespace LabelAnnotator.Views {
    public partial class SettingWindow : Window {
        public SettingWindow() {
            InitializeComponent();

            switch (SettingManager.Format) {
                case "LTRB":
                    RadLTRB.IsChecked = true;
                    RadCXCYWH.IsChecked = false;
                    break;
                case "CXCYWH":
                    RadLTRB.IsChecked = false;
                    RadCXCYWH.IsChecked = true;
                    break;
            }
        }

        private void OnClose(object sender, RoutedEventArgs e) {
            Close();
        }

        private void OnRadioClick(object sender, RoutedEventArgs e) {
            if (RadLTRB.IsChecked.GetValueOrDefault()) SettingManager.Format = "LTRB";
            else if (RadCXCYWH.IsChecked.GetValueOrDefault()) SettingManager.Format = "CXCYWH";
        }
    }
}
