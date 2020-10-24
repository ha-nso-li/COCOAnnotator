using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace LabelAnnotator.UserControls {
    public class MoveThumb : Thumb {
        public MoveThumb() {
            DragDelta += MoveThumb_DragDelta;
        }

        private void MoveThumb_DragDelta(object sender, DragDeltaEventArgs e) {
            if (DataContext is Control designerItem) {
                double left = Canvas.GetLeft(designerItem);
                double top = Canvas.GetTop(designerItem);

                Canvas.SetLeft(designerItem, left + e.HorizontalChange);
                Canvas.SetTop(designerItem, top + e.VerticalChange);
            }
        }
    }
}
