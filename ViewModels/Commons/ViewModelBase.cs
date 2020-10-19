using CommonServiceLocator;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace LabelAnnotator.ViewModels.Commons {
    public abstract class ViewModelBase : BindableBase {
        private string _Title;
        public string Title {
            get => _Title;
            set => SetProperty(ref _Title, value);
        }

        protected IEventAggregator EventAggregator;
        protected IDialogService UserDialogSerivce;
        protected Services.CommonDialogService CommonDialogService;
        protected Services.PathService PathService;
        protected Services.SerializationService SerializationService;
        protected Services.SettingService SettingService;

        protected ViewModelBase() {
            _Title = "";

            EventAggregator = ServiceLocator.Current.GetInstance<IEventAggregator>();
            UserDialogSerivce = ServiceLocator.Current.GetInstance<IDialogService>();
            CommonDialogService = ServiceLocator.Current.GetInstance<Services.CommonDialogService>();
            PathService = ServiceLocator.Current.GetInstance<Services.PathService>();
            SerializationService = ServiceLocator.Current.GetInstance<Services.SerializationService>();
            SettingService = ServiceLocator.Current.GetInstance<Services.SettingService>();
        }
    }
}
