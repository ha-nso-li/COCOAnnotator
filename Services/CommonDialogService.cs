using Microsoft.Win32;
using System.Linq;
using System.Windows;
using WinForm = System.Windows.Forms;

namespace LabelAnnotator.Services {
    public class CommonDialogService {
        public bool OpenCSVFileDialog(out string FilePath) {
            OpenFileDialog dlg = new OpenFileDialog {
                Filter = "CSV 파일|*.csv",
                Multiselect = false
            };
            bool result = dlg.ShowDialog().GetValueOrDefault();
            FilePath = dlg.FileName;
            return result;
        }

        public bool OpenCSVFilesDialog(out string[] FilePath) {
            OpenFileDialog dlg = new OpenFileDialog {
                Filter = "CSV 파일|*.csv",
                Multiselect = true
            };
            bool result = dlg.ShowDialog().GetValueOrDefault();
            FilePath = dlg.FileNames;
            return result;
        }

        public bool SaveCSVFileDialog(out string FilePath) {
            SaveFileDialog dlg = new SaveFileDialog {
                Filter = "CSV 파일|*.csv",
                DefaultExt = ".csv"
            };
            bool result = dlg.ShowDialog().GetValueOrDefault();
            FilePath = dlg.FileName;
            return result;
        }

        public bool OpenImagesDialog(out string[] FilePaths) {
            OpenFileDialog dlg = new OpenFileDialog {
                Filter = $"이미지 파일|{string.Join(";", Extensions.ApprovedImageExtension.Select(s => $"*{s}"))}",
                Multiselect = true,
            };
            bool result = dlg.ShowDialog().GetValueOrDefault();
            FilePaths = dlg.FileNames;
            return result;
        }

        public bool OpenFolderDialog(out string FolderPath) {
            WinForm.FolderBrowserDialog dlg = new WinForm.FolderBrowserDialog();
            WinForm.DialogResult result = dlg.ShowDialog();
            FolderPath = dlg.SelectedPath;
            return result == WinForm.DialogResult.OK;
        }

        public void MessageBox(string Message) {
            System.Windows.MessageBox.Show(Message, "", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public bool MessageBoxOKCancel(string Message) {
            MessageBoxResult result = System.Windows.MessageBox.Show(Message, "", MessageBoxButton.OKCancel, MessageBoxImage.Information);
            return result == MessageBoxResult.OK;
        }

        public bool? MessageBoxYesNoCancel(string Message) {
            MessageBoxResult result = System.Windows.MessageBox.Show(Message, "", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            return result switch
            {
                MessageBoxResult.Yes => true,
                MessageBoxResult.No => false,
                _ => null,
            };
        }
    }
}
