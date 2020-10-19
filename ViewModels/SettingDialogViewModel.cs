using Prism.Commands;
using Prism.Services.Dialogs;
using System.Windows.Input;

namespace LabelAnnotator.ViewModels {
    public class SettingDialogViewModel : Commons.DialogViewModelBase {
        public SettingDialogViewModel() {
            _LTRB = SettingService.Format == Utility.SettingNames.FormatLTRB;

            CmdClose = new DelegateCommand(Close);
        }

        private bool _LTRB;
        public bool LTRB {
            get => _LTRB;
            set {
                if (SetProperty(ref _LTRB, value)) {
                    RaisePropertyChanged(nameof(CXCYWH));
                }
            }
        }

        public bool CXCYWH {
            get => !_LTRB;
            set {
                if (SetProperty(ref _LTRB, !value)) {
                    RaisePropertyChanged(nameof(LTRB));
                }
            }
        }

        public ICommand CmdClose { get; }
        private void Close() {
            RaiseRequestClose(new DialogResult());
        }
    }
}
