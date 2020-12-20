using COCOAnnotator.Events;
using COCOAnnotator.Records;
using Prism.Events;
using System.Windows;

namespace COCOAnnotator.Views {
    public partial class MainWindow : Window {
        public MainWindow(IEventAggregator EventAggregator) {
            InitializeComponent();

            EventAggregator.GetEvent<ScrollViewCategoriesList>().Subscribe(ScrollViewCategoriesList);
            EventAggregator.GetEvent<ScrollViewImagesList>().Subscribe(ScrollViewImagesList);
            EventAggregator.GetEvent<TryCommitBbox>().Subscribe(TryCommitBbox);
        }

        private void ScrollViewCategoriesList(CategoryRecord e) {
            ViewCategoriesList.ScrollIntoView(e);
        }

        private void ScrollViewImagesList(ImageRecord e) {
            ViewImagesList.ScrollIntoView(e);
        }

        private void TryCommitBbox() {
            ViewViewport.TryCommitBbox();
        }
    }
}
