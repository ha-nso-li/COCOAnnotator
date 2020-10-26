using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LabelAnnotator.UserControls {
    public partial class ImageViewport : UserControl {
        public ImageViewport() {
            InitializeComponent();

            Panel.SetZIndex(ViewImageControl, ZIndex_Image);
        }

        private const int ZIndex_Image = 0;
        private const int ZIndex_PreviewBbox = 1;
        private const int ZIndex_Crosshair = 2;
        private const int ZIndex_Bbox = 3;
        private const int Tag_HorizontalCrosshair = -1;
        private const int Tag_VerticalCrosshair = -2;
        private const int Tag_PreviewBbox = -3;
        private const int Tag_UncommittedBbox = -4;

        private Point? DragStartPoint = null;
        private ContentControl? PreviewBbox = null;

        #region Dependency Properties
        public static readonly DependencyProperty MainImageUriProperty = DependencyProperty.Register(nameof(MainImageUri), typeof(Uri), typeof(ImageViewport), new PropertyMetadata(MainImageUriChanged));
        private static void MainImageUriChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is ImageViewport uc) {
                if (e.NewValue is null) {
                    uc.ViewImageControl.Source = null;
                } else if (e.NewValue is Uri bitmapUri) {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bitmap.UriSource = bitmapUri;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    uc.ViewImageControl.Source = bitmap;
                    if (uc.FitViewport) {
                        uc.ViewImageControl.MaxWidth = uc.ViewViewport.ViewportWidth;
                        uc.ViewImageControl.MaxHeight = uc.ViewViewport.ViewportHeight;
                    } else {
                        uc.ViewImageControl.MaxWidth = bitmap.PixelWidth;
                        uc.ViewImageControl.MaxHeight = bitmap.PixelHeight;
                    }
                    uc.UpdateBoundaryBoxes();
                }
            }
        }
        public Uri? MainImageUri {
            get => (Uri?)GetValue(MainImageUriProperty);
            set => SetValue(MainImageUriProperty, value);
        }

        public static readonly DependencyProperty FitViewportProperty = DependencyProperty.Register(nameof(FitViewport), typeof(bool), typeof(ImageViewport), new PropertyMetadata(FitViewportChanged));
        private static void FitViewportChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is ImageViewport uc && uc.ViewImageControl.Source is BitmapSource bitmap && e.NewValue is bool FitToViewport) {
                if (FitToViewport) {
                    uc.ViewViewport.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                    uc.ViewViewport.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
                    uc.ViewGrid.Width = double.NaN;
                    uc.ViewGrid.Height = double.NaN;
                    uc.ViewGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
                    uc.ViewGrid.VerticalAlignment = VerticalAlignment.Stretch;
                    uc.Dispatcher.Invoke(() => {
                        uc.ViewImageControl.MaxWidth = uc.ViewViewport.ViewportWidth;
                        uc.ViewImageControl.MaxHeight = uc.ViewViewport.ViewportHeight;
                    }, DispatcherPriority.Input);
                } else {
                    uc.ViewViewport.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                    uc.ViewViewport.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
                    uc.ViewImageControl.MaxWidth = bitmap.PixelWidth;
                    uc.ViewImageControl.MaxHeight = bitmap.PixelHeight;
                    uc.ViewGrid.Width = bitmap.PixelWidth;
                    uc.ViewGrid.Height = bitmap.PixelHeight;
                    uc.ViewGrid.HorizontalAlignment = HorizontalAlignment.Left;
                    uc.ViewGrid.VerticalAlignment = VerticalAlignment.Top;
                }
                uc.RefreshBoundaryBoxes();
            }
        }
        public bool FitViewport {
            get => (bool)GetValue(FitViewportProperty);
            set => SetValue(FitViewportProperty, value);
        }

        public static readonly DependencyProperty LabelsProperty = DependencyProperty.Register(nameof(Labels), typeof(IEnumerable<Records.LabelRecordWithIndex>), typeof(ImageViewport), new PropertyMetadata(LabelsChanged));
        private static void LabelsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is ImageViewport uc) {
                if (e.OldValue is INotifyCollectionChanged old) old.CollectionChanged -= uc.LabelsCollectionChanged;
                if (e.NewValue is INotifyCollectionChanged @new) @new.CollectionChanged += uc.LabelsCollectionChanged;
                uc.UpdateBoundaryBoxes();
            }
        }
        private void LabelsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
                case NotifyCollectionChangedAction.Reset:
                    ClearBoundaryBoxes();
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (Records.LabelRecordWithIndex? i in e.OldItems) {
                        if (i is null) continue;
                        (int idx, _) = i;
                        List<ContentControl> delete = ViewImageCanvas.Children.OfType<ContentControl>().Where(s => (int)s.Tag == idx).ToList();
                        foreach (ContentControl j in delete) ViewImageCanvas.Children.Remove(j);
                    }
                    break;
                case NotifyCollectionChangedAction.Add:
                    foreach (Records.LabelRecordWithIndex? i in e.NewItems) {
                        if (i is null) continue;
                        (int idx, Records.LabelRecord lbl) = i;
                        AddBoundaryBox(ZIndex_Bbox, idx, lbl.Left, lbl.Top, lbl.Right, lbl.Bottom, lbl.Class, true);
                    }
                    break;
            }
        }
        public IEnumerable<Records.LabelRecordWithIndex>? Labels {
            get => (IEnumerable<Records.LabelRecordWithIndex>?)GetValue(LabelsProperty);
            set => SetValue(LabelsProperty, value);
        }

        public static readonly DependencyProperty BboxInsertModeProperty = DependencyProperty.Register(nameof(BboxInsertMode), typeof(bool), typeof(ImageViewport));
        public bool BboxInsertMode {
            get => (bool)GetValue(BboxInsertModeProperty);
            set => SetValue(BboxInsertModeProperty, value);
        }

        public static readonly DependencyProperty CurrentClassProperty = DependencyProperty.Register(nameof(CurrentClass), typeof(Records.ClassRecord), typeof(ImageViewport));
        public Records.ClassRecord? CurrentClass {
            get => (Records.ClassRecord?)GetValue(CurrentClassProperty);
            set => SetValue(CurrentClassProperty, value);
        }
        #endregion

        #region Private Logics
        private double CurrentScale;

        private void UpdateBoundaryBoxes() {
            if (Labels is null) return;
            ClearBoundaryBoxes();
            if (ViewImageControl.Source is BitmapSource bitmap) {
                Dispatcher.Invoke(() => {
                    CurrentScale = ViewImageControl.ActualWidth / bitmap.PixelWidth;
                }, DispatcherPriority.Loaded);
            }
            foreach ((int idx, Records.LabelRecord lbl) in Labels) {
                AddBoundaryBox(ZIndex_Bbox, idx, lbl.Left, lbl.Top, lbl.Right, lbl.Bottom, lbl.Class, true);
            }
        }
        private void ClearBoundaryBoxes() {
            List<ContentControl> delete = ViewImageCanvas.Children.OfType<ContentControl>().ToList();
            foreach (ContentControl i in delete) ViewImageCanvas.Children.Remove(i);
        }
        /// <summary>주어진 라벨에 기반한 새로운 경계 상자를 화면에 추가합니다.</summary>
        /// <param name="tag">
        /// 경계 상자가 내부 컬렉션에서 차지하는 인덱스 번호와 같습니다. 경계 상자 컨트롤의 Tag 값으로 사용됩니다.
        /// 0 미만의 값이라면 임시 경계 상자로 간주하며, 삭제 컨텍스트 메뉴를 추가하지 않습니다.
        /// </param>
        /// <param name="needScale">크기 스케일링 여부입니다. <see langword="true"/>이면 주어진 좌표를 이미지의 화면 크기에 맞게 변환합니다.</param>
        /// <returns>추가한 경계 상자의 시각화 컨트롤을 반환합니다.</returns>
        private ContentControl AddBoundaryBox(int zindex, int tag, double left, double top, double right, double bottom, Records.ClassRecord category, bool needScale) {
            // 화면의 배율에 맞춰 스케일링
            if (needScale) {
                left *= CurrentScale;
                top *= CurrentScale;
                right *= CurrentScale;
                bottom *= CurrentScale;
            }
            double width = Math.Max(0, right - left);
            double height = Math.Max(0, bottom - top);
            ContentControl cont = new ContentControl {
                Width = width,
                Height = height,
                Template = (ControlTemplate)FindResource("DesignerItemTemplate"),
                DataContext = category,
                ToolTip = category.ToString()
            };
            Canvas.SetLeft(cont, left);
            Canvas.SetTop(cont, top);
            Panel.SetZIndex(cont, zindex);
            cont.Tag = tag;
            if (tag >= 0) {
                MenuItem delete = new MenuItem {
                    Header = "삭제",
                    Tag = tag,
                };
                delete.Click += DeleteLabel;
                ContextMenu context = new ContextMenu();
                context.Items.Add(delete);
                cont.ContextMenu = context;
            }
            ViewImageCanvas.Children.Add(cont);
            return cont;
        }
        private void DeleteLabel(object sender, RoutedEventArgs e) {
            if (!(sender is MenuItem mn)) return;
            // 대응되는 경계상자 숨김
            List<ContentControl> bbox = ViewImageCanvas.Children.OfType<ContentControl>().Where(s => mn.Tag.Equals(s.Tag)).ToList();
            foreach (ContentControl i in bbox) i.Visibility = Visibility.Collapsed;
        }
        private void RefreshBoundaryBoxes() {
            if (!(ViewImageControl.Source is BitmapSource bitmap)) return;
            // 경계 상자 위치 갱신은 UI 이미지 크기 조정에 수반되는 경우가 많기 때문에 UI 로드가 끝난 다음에 수행함.
            Dispatcher.Invoke(() => {
                double afterScale = ViewImageControl.ActualWidth / bitmap.PixelWidth;
                IEnumerable<ContentControl> boundingBoxes = ViewImageCanvas.Children.OfType<ContentControl>();
                foreach (ContentControl box in boundingBoxes) {
                    double newLeft = Canvas.GetLeft(box) / CurrentScale * afterScale;
                    double newTop = Canvas.GetTop(box) / CurrentScale * afterScale;
                    double newWidth = box.Width / CurrentScale * afterScale;
                    double newHeight = box.Height / CurrentScale * afterScale;
                    Records.LabelRecordWithIndex? realBox = Labels.FirstOrDefault(s => s.Index == (int)box.Tag);
                    if (realBox is object) {
                        // 원본에서 스케일링 한 결과와 UI 박스에서 스케일링 한 결과의 오차가 작으면 원본에서 스케일링한 결과로 반영
                        double errorThreshold = Math.Max(afterScale > CurrentScale ? afterScale : CurrentScale, 1);
                        double newLeftFromOriginal = realBox.Label.Left * afterScale;
                        double newTopFromOriginal = realBox.Label.Top * afterScale;
                        double newWidthFromOriginal = (realBox.Label.Right - realBox.Label.Left) * afterScale;
                        double newHeightFromOriginal = (realBox.Label.Bottom - realBox.Label.Top) * afterScale;
                        newLeft = Math.Abs(newLeftFromOriginal - newLeft) > errorThreshold ? newLeft : newLeftFromOriginal;
                        newTop = Math.Abs(newTopFromOriginal - newTop) > errorThreshold ? newTop : newTopFromOriginal;
                        newWidth = Math.Abs(newWidthFromOriginal - newWidth) > errorThreshold ? newWidth : newWidthFromOriginal;
                        newHeight = Math.Abs(newHeightFromOriginal - newHeight) > errorThreshold ? newHeight : newHeightFromOriginal;
                    }
                    // 원본이 없는 경우 (새로 추가될 경계상자이거나 기존 경계 상자의 위치를 조절한 경우가 해당)
                    // 창 축소와 확대를 반복하다 보면 경계상자 위치가 약간씩 어긋나게 될 가능성이 있음.
                    // 이는 부동소수점 연산 과정에서 발생하는 오차가 누적되어서 발생하는 문제이며 해결 불가능.
                    Canvas.SetLeft(box, newLeft);
                    Canvas.SetTop(box, newTop);
                    box.Width = newWidth;
                    box.Height = newHeight;
                }
                CurrentScale = afterScale;
            }, DispatcherPriority.Loaded);
        }
        #endregion

        #region Local Events
        private void ViewImageCanvas_SizeChanged(object sender, SizeChangedEventArgs e) {
            // 이미지 크기 재조정
            if (FitViewport) {
                Dispatcher.Invoke(() => {
                    ViewImageControl.MaxWidth = Math.Max(ViewViewport.ViewportWidth, 0);
                    ViewImageControl.MaxHeight = Math.Max(ViewViewport.ViewportHeight, 0);
                }, DispatcherPriority.Loaded);
                RefreshBoundaryBoxes();
            }
        }
        private void ViewImageCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (!BboxInsertMode) return;
            DragStartPoint = e.GetPosition(ViewImageControl);
        }
        private void ViewImageCanvas_MouseMove(object sender, MouseEventArgs e) {
            if (!BboxInsertMode || CurrentClass is null) {
                // 크로스헤어 있으면 삭제
                List<Line> line = ViewImageCanvas.Children.OfType<Line>().ToList();
                foreach (Line i in line) {
                    ViewImageCanvas.Children.Remove(i);
                }
            } else {
                Point current = e.GetPosition(ViewImageControl);
                // 크로스헤어
                List<Line> line = ViewImageCanvas.Children.OfType<Line>().ToList();
                if (line.Count == 0) {
                    Line hline = new Line {
                        X1 = 0,
                        X2 = ViewImageCanvas.ActualWidth,
                        Y1 = current.Y,
                        Y2 = current.Y,
                        Stroke = Brushes.Black,
                        StrokeThickness = 2,
                        Tag = Tag_HorizontalCrosshair,
                    };
                    Line vline = new Line {
                        X1 = current.X,
                        X2 = current.X,
                        Y1 = 0,
                        Y2 = ViewImageCanvas.ActualHeight,
                        Stroke = Brushes.Black,
                        StrokeThickness = 2,
                        Tag = Tag_VerticalCrosshair,
                    };
                    Panel.SetZIndex(hline, ZIndex_Crosshair);
                    Panel.SetZIndex(vline, ZIndex_Crosshair);
                    ViewImageCanvas.Children.Add(hline);
                    ViewImageCanvas.Children.Add(vline);
                } else {
                    foreach (Line i in line) {
                        int tag = (int)i.Tag;
                        if (tag == Tag_HorizontalCrosshair) {
                            i.X2 = ViewImageCanvas.ActualWidth;
                            i.Y1 = current.Y;
                            i.Y2 = current.Y;
                        } else if (tag == Tag_VerticalCrosshair) {
                            i.X1 = current.X;
                            i.X2 = current.X;
                            i.Y2 = ViewImageCanvas.ActualHeight;
                        }
                    }
                }
                // 미리보기 상자
                if (DragStartPoint is object) {
                    if (e.LeftButton == MouseButtonState.Pressed) {
                        if (PreviewBbox is null) {
                            double startX = DragStartPoint.Value.X;
                            double startY = DragStartPoint.Value.Y;
                            double endX = current.X;
                            double endY = current.Y;
                            Utilities.Miscellaneous.SortTwoValues(ref startX, ref endX);
                            Utilities.Miscellaneous.SortTwoValues(ref startY, ref endY);
                            ContentControl bbox = AddBoundaryBox(ZIndex_PreviewBbox, Tag_PreviewBbox, startX, startY, endX - startX, endY - startY, CurrentClass, false);
                            PreviewBbox = bbox;
                        } else {
                            double startX = DragStartPoint.Value.X;
                            double startY = DragStartPoint.Value.Y;
                            double endX = current.X;
                            double endY = current.Y;
                            Utilities.Miscellaneous.SortTwoValues(ref startX, ref endX);
                            Utilities.Miscellaneous.SortTwoValues(ref startY, ref endY);
                            PreviewBbox.Width = endX - startX;
                            PreviewBbox.Height = endY - startY;
                            Canvas.SetLeft(PreviewBbox, startX);
                            Canvas.SetTop(PreviewBbox, startY);
                        }
                    } else if (e.LeftButton == MouseButtonState.Released) {
                        // 마우스를 이미지 밖에서 뗀 경우, 마지막 정보를 기준으로 경계 상자로 반영함.
                        if (PreviewBbox is object) {
                            PreviewBbox.Tag = Tag_UncommittedBbox;
                            PreviewBbox = null;
                            DragStartPoint = null;
                        }
                    }
                }
            }
        }
        private void ViewImageCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (!BboxInsertMode || PreviewBbox is null || DragStartPoint is null) return;
            Point dragEnd = e.GetPosition(ViewImageControl);
            double startX = DragStartPoint.Value.X;
            double startY = DragStartPoint.Value.Y;
            double endX = dragEnd.X;
            double endY = dragEnd.Y;
            Utilities.Miscellaneous.SortTwoValues(ref startX, ref endX);
            Utilities.Miscellaneous.SortTwoValues(ref startY, ref endY);
            PreviewBbox.Width = endX - startX;
            PreviewBbox.Height = endY - startY;
            Canvas.SetLeft(PreviewBbox, startX);
            Canvas.SetTop(PreviewBbox, startY);
            PreviewBbox.Tag = Tag_UncommittedBbox;
            DragStartPoint = null;
            PreviewBbox = null;
        }
        #endregion

        public event Events.CommitBboxEventHandler? CommitBbox;

        public void TryCommitBbox() {
            if (!(ViewImageControl.Source is BitmapSource bitmap)) return;

            List<Records.LabelRecordWithIndex> changed = new List<Records.LabelRecordWithIndex>();
            List<Records.LabelRecordWithIndex> deleted = new List<Records.LabelRecordWithIndex>();
            List<Records.LabelRecordWithoutImage> added = new List<Records.LabelRecordWithoutImage>();
            IEnumerable<ContentControl> bboxes = ViewImageCanvas.Children.OfType<ContentControl>();
            foreach (ContentControl bbox in bboxes) {
                if (bbox.Visibility == Visibility.Collapsed) {
                    // 삭제
                    Records.LabelRecordWithIndex? realBox = Labels.FirstOrDefault(s => s.Index == (int)bbox.Tag);
                    if (realBox is Records.LabelRecordWithIndex) deleted.Add(realBox);
                } else if ((int)bbox.Tag == Tag_UncommittedBbox) {
                    // 추가
                    if (CurrentClass is null) continue;
                    double left = Math.Clamp(Canvas.GetLeft(bbox) / CurrentScale, 0, bitmap.PixelWidth);
                    double top = Math.Clamp(Canvas.GetTop(bbox) / CurrentScale, 0, bitmap.PixelHeight);
                    double right = Math.Clamp((Canvas.GetLeft(bbox) + bbox.Width) / CurrentScale, 0, bitmap.PixelWidth);
                    double bottom = Math.Clamp((Canvas.GetTop(bbox) + bbox.Height) / CurrentScale, 0, bitmap.PixelHeight);
                    added.Add(new Records.LabelRecordWithoutImage(left, top, right, bottom, CurrentClass));
                } else {
                    // 이동
                    double left = Math.Clamp(Canvas.GetLeft(bbox) / CurrentScale, 0, bitmap.PixelWidth);
                    double top = Math.Clamp(Canvas.GetTop(bbox) / CurrentScale, 0, bitmap.PixelHeight);
                    double right = Math.Clamp((Canvas.GetLeft(bbox) + bbox.Width) / CurrentScale, 0, bitmap.PixelWidth);
                    double bottom = Math.Clamp((Canvas.GetTop(bbox) + bbox.Height) / CurrentScale, 0, bitmap.PixelHeight);
                    Records.LabelRecordWithIndex? realBox = Labels.FirstOrDefault(s => s.Index == (int)bbox.Tag);
                    if (realBox is Records.LabelRecordWithIndex realBox2) {
                        double errorThreshold = Math.Max(1 / CurrentScale, 1);
                        int notChangedPositionsCount = 0;
                        // 새 좌표와 현재 좌표의 오차가 작으면 기존 좌표 무시. (좌표 변환 과정에서의 잠재적 오차 감안)
                        if (Math.Abs(realBox2.Label.Left - left) < errorThreshold) {
                            notChangedPositionsCount++;
                            left = realBox2.Label.Left;
                        }
                        if (Math.Abs(realBox2.Label.Top - top) < errorThreshold) {
                            notChangedPositionsCount++;
                            top = realBox2.Label.Top;
                        }
                        if (Math.Abs(realBox2.Label.Right - right) < errorThreshold) {
                            notChangedPositionsCount++;
                            right = realBox2.Label.Right;
                        }
                        if (Math.Abs(realBox2.Label.Bottom - bottom) < errorThreshold) {
                            notChangedPositionsCount++;
                            bottom = realBox2.Label.Bottom;
                        }
                        if (notChangedPositionsCount < 4) {
                            changed.Add(new Records.LabelRecordWithIndex(realBox2.Index, new Records.LabelRecord(realBox2.Label.Image, left, top, right, bottom, realBox2.Label.Class)));
                        }
                    }
                }
            }

            if (added.Count > 0 || deleted.Count > 0 || changed.Count > 0) CommitBbox?.Invoke(this, new Events.CommitBboxEventArgs(added, changed, deleted));
        }
    }
}
