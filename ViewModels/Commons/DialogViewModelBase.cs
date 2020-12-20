using Prism.Services.Dialogs;
using System;

namespace COCOAnnotator.ViewModels.Commons {
    public abstract class DialogViewModelBase : ViewModelBase, IDialogAware {
        protected bool IsClosed;
        public event Action<IDialogResult>? RequestClose;

        protected void RaiseRequestClose(IDialogResult result) {
            RequestClose?.Invoke(result);
        }

        public virtual bool CanCloseDialog() {
            return !IsClosed;
        }

        public virtual void OnDialogClosed() {
            IsClosed = true;
        }

        public virtual void OnDialogOpened(IDialogParameters parameters) {

        }

        protected DialogViewModelBase() {
            IsClosed = false;
        }
    }
}
