using Microsoft.Win32;
using System.Linq;
using System.Windows;
using WinForm = System.Windows.Forms;

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

        public static bool SaveCSVFileDialog(out string FilePath) {
            SaveFileDialog dlg = new SaveFileDialog {
                Filter = "CSV 파일|*.csv",
                DefaultExt = ".csv"
            };
            bool result = dlg.ShowDialog().GetValueOrDefault();
            FilePath = dlg.FileName;
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

        public static bool OpenImagesDialog(out string[] FilePaths) {
            OpenFileDialog dlg = new OpenFileDialog {
                Filter = $"이미지 파일|{string.Join(";", Miscellaneous.ApprovedImageExtensions.Select(s => $"*{s}"))}",
                Multiselect = true,
            };
            bool result = dlg.ShowDialog().GetValueOrDefault();
            FilePaths = dlg.FileNames;
            return result;
        }

        public static bool OpenFolderDialog(out string FolderPath) {
            WinForm.FolderBrowserDialog dlg = new WinForm.FolderBrowserDialog();
            WinForm.DialogResult result = dlg.ShowDialog();
            FolderPath = dlg.SelectedPath;
            return result == WinForm.DialogResult.OK;
        }

        public static void MessageBox(string Message) {
            System.Windows.MessageBox.Show(Message, "", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public static bool MessageBoxOKCancel(string Message) {
            MessageBoxResult result = System.Windows.MessageBox.Show(Message, "", MessageBoxButton.OKCancel, MessageBoxImage.Information);
            return result == MessageBoxResult.OK;
        }

        public static bool? MessageBoxYesNoCancel(string Message) {
            MessageBoxResult result = System.Windows.MessageBox.Show(Message, "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            return result switch {
                MessageBoxResult.Yes => true,
                MessageBoxResult.No => false,
                _ => null,
            };
        }
    }
}
