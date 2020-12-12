using LabelAnnotator.Records.Enums;
using Prism.Commands;
using Prism.Services.Dialogs;
using System.Windows.Input;

namespace LabelAnnotator.ViewModels {
    public class SettingDialogViewModel : Commons.DialogViewModelBase {
        public SettingDialogViewModel() {
            Title = "설정";
            Format = SettingService.Format;

            CmdClose = new DelegateCommand(Close);
        }

        private SettingFormats _Format;
        public SettingFormats Format {
            get => _Format;
            set => SetProperty(ref _Format, value);
        }

        public override void OnDialogClosed() {
            SettingService.Format = Format;
        }

        public ICommand CmdClose { get; }
        private void Close() {
            RaiseRequestClose(new DialogResult());
        }
    }
}
