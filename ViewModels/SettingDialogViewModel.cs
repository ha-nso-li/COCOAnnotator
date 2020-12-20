using COCOAnnotator.Records.Enums;
using Prism.Commands;
using Prism.Services.Dialogs;
using System.Windows.Input;

namespace COCOAnnotator.ViewModels {
    public class SettingDialogViewModel : Commons.DialogViewModelBase {
        public SettingDialogViewModel() {
            Title = "설정";
            Format = SettingService.Format;
            Color = SettingService.Color;

            CmdClose = new DelegateCommand(Close);
        }

        private SettingFormats _Format;
        public SettingFormats Format {
            get => _Format;
            set => SetProperty(ref _Format, value);
        }
        private SettingColors _Color;
        public SettingColors Color {
            get => _Color;
            set => SetProperty(ref _Color, value);
        }

        public override void OnDialogClosed() {
            SettingService.Format = Format;
            SettingService.Color = Color;
        }

        public ICommand CmdClose { get; }
        private void Close() {
            RaiseRequestClose(new DialogResult());
        }
    }
}
