using LabelAnnotator.Events;
using Prism.Events;
using System.Windows.Controls;

namespace LabelAnnotator.Views {
    public partial class ManageDialog : UserControl {
        public ManageDialog(IEventAggregator EventAggregator) {
            InitializeComponent();
            EventAggregator.GetEvent<ScrollTxtLogVerifyLabel>().Subscribe(ScrollTxtLogVerifyLabel, ThreadOption.UIThread);
            EventAggregator.GetEvent<ScrollTxtLogUndupeLabel>().Subscribe(ScrollTxtLogUndupeLabel, ThreadOption.UIThread);
        }

        private void ScrollTxtLogVerifyLabel() {
            TxtLogVerifyLabel.ScrollToEnd();
        }

        private void ScrollTxtLogUndupeLabel() {
            TxtLogUndupeLabel.ScrollToEnd();
        }
    }
}
