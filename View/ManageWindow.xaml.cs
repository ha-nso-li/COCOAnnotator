using System.Windows;

namespace LabelAnnotator {
    public partial class ManageWindow : Window {
        public ManageWindow() {
            InitializeComponent();

            DataContext = new ManageWindowViewModel(this);
        }
    }
}
