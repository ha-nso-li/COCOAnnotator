using LabelAnnotator.Services;
using LabelAnnotator.Views;
using Prism.DryIoc;
using Prism.Ioc;
using System.Windows;

namespace LabelAnnotator {
    public partial class App : PrismApplication {
        protected override Window CreateShell() {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry) {
            containerRegistry.RegisterSingleton<CommonDialogService>();
            containerRegistry.RegisterSingleton<SettingService>();
            containerRegistry.RegisterSingleton<SerializationService>();

            containerRegistry.RegisterDialog<SettingDialog>();
            containerRegistry.RegisterDialog<ManageDialog>();
        }
    }
}
