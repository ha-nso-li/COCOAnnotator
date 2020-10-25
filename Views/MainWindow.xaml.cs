using Prism.Events;
using System.Windows;

namespace LabelAnnotator.Views {
    public partial class MainWindow : Window {
        public MainWindow(IEventAggregator EventAggregator) {
            InitializeComponent();

            EventAggregator.GetEvent<Events.ScrollViewCategoriesList>().Subscribe(ScrollViewCategoriesList);
            EventAggregator.GetEvent<Events.ScrollViewImagesList>().Subscribe(ScrollViewImagesList);
            EventAggregator.GetEvent<Events.TryCommitBbox>().Subscribe(TryCommitBbox);
        }

        private void ScrollViewCategoriesList(Records.ClassRecord e) {
            ViewCategoriesList.ScrollIntoView(e);
        }

        private void ScrollViewImagesList(Records.ImageRecord e) {
            ViewImagesList.ScrollIntoView(e);
        }

        private void TryCommitBbox() {
            ViewViewport.TryCommitBbox();
        }
    }
}
