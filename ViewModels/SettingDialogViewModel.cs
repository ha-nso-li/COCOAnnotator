using COCOAnnotator.Records.Enums;
using COCOAnnotator.ViewModels.Commons;
using Prism.Commands;
using Prism.Services.Dialogs;
using System.Windows.Input;

namespace COCOAnnotator.ViewModels {
    public class SettingDialogViewModel : DialogViewModelBase {
        public SettingDialogViewModel() {
            Title = "설정";
            Color = SettingService.Color;

            CmdClose = new DelegateCommand(Close);
        }

        private SettingColors _Color;
        public SettingColors Color {
            get => _Color;
            set => SetProperty(ref _Color, value);
        }

        public override void OnDialogClosed() {
            SettingService.Color = Color;
            SettingService.Write();
        }

        public ICommand CmdClose { get; }
        private void Close() {
            RaiseRequestClose(new DialogResult());
        }
    }
}
