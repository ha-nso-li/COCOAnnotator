using Microsoft.Win32;
using WinForm = System.Windows.Forms;
using WPF = System.Windows;

namespace COCOAnnotator.Services.Utilities {
    public static class CommonDialogService {
        public static bool OpenCSVFileDialog(out string FilePath) {
            OpenFileDialog dlg = new OpenFileDialog {
                Filter = "CSV 파일|*.csv",
                Multiselect = false
            };
            bool result = dlg.ShowDialog().GetValueOrDefault();
            FilePath = dlg.FileName;
            return result;
        }

        public static bool OpenJsonFileDialog(out string FilePath) {
            OpenFileDialog dlg = new OpenFileDialog {
                Filter = "JSON 파일|*.json",
                Multiselect = false
            };
            bool result = dlg.ShowDialog().GetValueOrDefault();
            FilePath = dlg.FileName;
            return result;
        }

        public static bool OpenJsonFilesDialog(out string[] FilePath) {
            OpenFileDialog dlg = new OpenFileDialog {
                Filter = "JSON 파일|*.json",
                Multiselect = true
            };
            bool result = dlg.ShowDialog().GetValueOrDefault();
            FilePath = dlg.FileNames;
            return result;
        }

        public static bool SaveJsonFileDialog(out string FilePath) {
            SaveFileDialog dlg = new SaveFileDialog {
                Filter = "JSON 파일|*.json",
                DefaultExt = ".json"
            };
            bool result = dlg.ShowDialog().GetValueOrDefault();
            FilePath = dlg.FileName;
            return result;
        }

        public static bool OpenFolderDialog(out string FolderPath) {
            WinForm.FolderBrowserDialog dlg = new WinForm.FolderBrowserDialog();
            WinForm.DialogResult result = dlg.ShowDialog();
            FolderPath = dlg.SelectedPath;
            return result == WinForm.DialogResult.OK;
        }

        public static void MessageBox(string Message) {
            WPF.MessageBox.Show(Message, "", WPF.MessageBoxButton.OK, WPF.MessageBoxImage.Warning);
        }

        public static bool MessageBoxOKCancel(string Message) {
            WPF.MessageBoxResult result = WPF.MessageBox.Show(Message, "", WPF.MessageBoxButton.OKCancel, WPF.MessageBoxImage.Information);
            return result == WPF.MessageBoxResult.OK;
        }

        public static bool? MessageBoxYesNoCancel(string Message) {
            WPF.MessageBoxResult result = WPF.MessageBox.Show(Message, "", WPF.MessageBoxButton.YesNoCancel, WPF.MessageBoxImage.Question);
            return result switch {
                WPF.MessageBoxResult.Yes => true,
                WPF.MessageBoxResult.No => false,
                _ => null,
            };
        }
    }
}
