using Prism.Services.Dialogs;
using System;

namespace LabelAnnotator.ViewModels.Commons {
    public abstract class DialogViewModelBase : ViewModelBase, IDialogAware {
        public event Action<IDialogResult>? RequestClose;

        protected void RaiseRequestClose(IDialogResult result) {
            RequestClose?.Invoke(result);
        }

        public virtual bool CanCloseDialog() {
            return true;
        }

        public virtual void OnDialogClosed() {

        }

        public virtual void OnDialogOpened(IDialogParameters parameters) {

        }
    }
}
