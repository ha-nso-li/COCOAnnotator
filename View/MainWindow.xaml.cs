using System.Windows;

namespace LabelAnnotator {
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();

            DataContext = new MainWindowViewModel(this);
        }
    }
}
