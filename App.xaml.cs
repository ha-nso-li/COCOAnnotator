using Prism.DryIoc;
using Prism.Ioc;
using System.Windows;

namespace LabelAnnotator {
    public partial class App : PrismApplication {
        protected override Window CreateShell() {
            return Container.Resolve<Views.MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry) {
            containerRegistry.RegisterSingleton<Services.DialogService>();
        }
    }
}
