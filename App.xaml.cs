using Prism.DryIoc;
using Prism.Ioc;
using System.Windows;

namespace LabelAnnotator {
    public partial class App : PrismApplication {
        protected override Window CreateShell() {
            return Container.Resolve<Views.MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry) {
            containerRegistry.RegisterSingleton<Services.CommonDialogService>();
            containerRegistry.RegisterSingleton<Services.SettingService>();
            containerRegistry.RegisterSingleton<Services.PathService>();
            containerRegistry.RegisterSingleton<Services.SerializationService>();

            containerRegistry.RegisterDialog<Views.SettingDialog>();
            containerRegistry.RegisterDialog<Views.ManageDialog>();
        }
    }
}
