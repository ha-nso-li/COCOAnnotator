using System.Windows;

namespace LabelAnnotator.Views {
    public partial class ManageWindow : Window {
        public ManageWindow() {
            InitializeComponent();

            DataContext = new ViewModels.ManageWindowViewModel(this);
        }
    }
}
