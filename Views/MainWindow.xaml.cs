using System.Windows;

namespace LabelAnnotator.Views {
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();

            DataContext = new ViewModels.MainWindowViewModel(this);
        }
    }
}
