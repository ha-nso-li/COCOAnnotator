using COCOAnnotator.Events;
using COCOAnnotator.Records;
using COCOAnnotator.Services.Utilities;
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

namespace COCOAnnotator.UserControls {
    public partial class ImageViewport : UserControl {
        public ImageViewport() {
            InitializeComponent();

            Panel.SetZIndex(ViewImageControl, ZIndex_Image);
        }

        private const int ZIndex_Image = 0;
        private const int ZIndex_PreviewBbox = 1;
        private const int ZIndex_Crosshair = 2;
        private const int ZIndex_Bbox = 3;
        private const int Tag_HorizontalCrosshair = 0;
        private const int Tag_VerticalCrosshair = 1;
        private const int Tag_PreviewBbox = 2;
        private const int Tag_UncommittedBbox = 3;

        private Point? DragStartPoint = null;
        private ContentControl? PreviewBbox = null;

        #region Dependency Properties
        public static readonly DependencyProperty MainImageUriProperty = DependencyProperty.Register(nameof(MainImageUri), typeof(Uri), typeof(ImageViewport), new(MainImageUriChanged));
        private static void MainImageUriChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is ImageViewport uc) {
                if (e.NewValue is Uri bitmapUri) {
                    BitmapImage bitmap = new();
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
                } else {
                    uc.ViewImageControl.Source = null;
                }
            }
        }
        public Uri? MainImageUri {
            get => (Uri?)GetValue(MainImageUriProperty);
            set => SetValue(MainImageUriProperty, value);
        }

        public static readonly DependencyProperty FitViewportProperty = DependencyProperty.Register(nameof(FitViewport), typeof(bool), typeof(ImageViewport), new(FitViewportChanged));
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

        public static readonly DependencyProperty AnnotationsProperty = DependencyProperty.Register(nameof(Annotations), typeof(IEnumerable<AnnotationRecord>), typeof(ImageViewport),
            new(AnnotationsChanged));
        private static void AnnotationsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is ImageViewport uc) {
                if (e.OldValue is INotifyCollectionChanged old) old.CollectionChanged -= uc.AnnotationsCollectionChanged;
                if (e.NewValue is INotifyCollectionChanged @new) @new.CollectionChanged += uc.AnnotationsCollectionChanged;
                uc.UpdateBoundaryBoxes();
            }
        }
        private void AnnotationsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
            switch (e.Action) {
            case NotifyCollectionChangedAction.Reset:
                ClearBoundaryBoxes();
                break;
            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems is null) return;
                ContentControl[] delete = ViewImageCanvas.Children.OfType<ContentControl>().Where(s => e.OldItems.Contains(s.Tag)).ToArray();
                foreach (ContentControl j in delete) ViewImageCanvas.Children.Remove(j);
                break;
            case NotifyCollectionChangedAction.Add:
                if (e.NewItems is null) return;
                foreach (AnnotationRecord? i in e.NewItems) {
                    if (i is null) continue;
                    AddBoundaryBox(ZIndex_Bbox, i, i.Left, i.Top, i.Width, i.Height, i.Category, true);
                }
                break;
            }
        }
        public IEnumerable<AnnotationRecord>? Annotations {
            get => (IEnumerable<AnnotationRecord>?)GetValue(AnnotationsProperty);
            set => SetValue(AnnotationsProperty, value);
        }

        public static readonly DependencyProperty BboxInsertModeProperty = DependencyProperty.Register(nameof(BboxInsertMode), typeof(bool), typeof(ImageViewport), new(BboxInsertModeChanged));
        private static void BboxInsertModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is ImageViewport uc && e.NewValue is bool bboxInsertMode && !bboxInsertMode) {
                // 크로스헤어 있으면 삭제
                Line[] line = uc.ViewImageCanvas.Children.OfType<Line>().ToArray();
                foreach (Line i in line) uc.ViewImageCanvas.Children.Remove(i);
            }
        }
        public bool BboxInsertMode {
            get => (bool)GetValue(BboxInsertModeProperty);
            set => SetValue(BboxInsertModeProperty, value);
        }

        public static readonly DependencyProperty CurrentCategoryProperty = DependencyProperty.Register(nameof(CurrentCategory), typeof(CategoryRecord), typeof(ImageViewport));
        public CategoryRecord? CurrentCategory {
            get => (CategoryRecord?)GetValue(CurrentCategoryProperty);
            set => SetValue(CurrentCategoryProperty, value);
        }
        #endregion

        #region Private Logics
        private double CurrentScale;

        private void UpdateBoundaryBoxes() {
            if (Annotations is null) return;
            ClearBoundaryBoxes();
            if (ViewImageControl.Source is BitmapSource bitmap) {
                Dispatcher.Invoke(() => {
                    CurrentScale = ViewImageControl.ActualWidth / bitmap.PixelWidth;
                }, DispatcherPriority.Loaded);
            }
            foreach (AnnotationRecord i in Annotations) {
                AddBoundaryBox(ZIndex_Bbox, i, i.Left, i.Top, i.Width, i.Height, i.Category, true);
            }
        }
        private void ClearBoundaryBoxes() {
            Dispatcher.Invoke(() => {
                ContentControl[] delete = ViewImageCanvas.Children.OfType<ContentControl>().ToArray();
                foreach (ContentControl i in delete) ViewImageCanvas.Children.Remove(i);
            });
        }
        /// <summary>주어진 라벨에 기반한 새로운 경계 상자를 화면에 추가합니다.</summary>
        /// <param name="tag">해당 경계 상자에 해당하는 <seealso cref="AnnotationRecord"/> 객체입니다. 특수 객체일 경우 정수 값을 가집니다.</param>
        /// <param name="needScale">크기 변환 여부입니다. 경계 상자 좌표값이 이미지 내 좌표이면 <see langword="true"/>, 컨트롤 내 위치 좌표이면 <see langword="false"/>입니다.</param>
        /// <returns>추가한 경계 상자의 시각화 컨트롤을 반환합니다.</returns>
        private ContentControl AddBoundaryBox(int zindex, object tag, double left, double top, double width, double height, CategoryRecord category, bool needScale) {
            return Dispatcher.Invoke(() => {
                // 화면의 배율에 맞춰 스케일링
                if (needScale) {
                    left *= CurrentScale;
                    top *= CurrentScale;
                    width *= CurrentScale;
                    height *= CurrentScale;
                }
                ContentControl cont = new() {
                    Width = width,
                    Height = height,
                    Template = (ControlTemplate)FindResource("DesignerItemTemplate"),
                    DataContext = category,
                    Tag = tag,
                };
                Canvas.SetLeft(cont, left);
                Canvas.SetTop(cont, top);
                Panel.SetZIndex(cont, zindex);
                if (tag is AnnotationRecord) {
                    MenuItem delete = new() {
                        Header = "삭제",
                        Tag = tag,
                    };
                    delete.Click += DeleteLabel;
                    ContextMenu context = new();
                    context.Items.Add(delete);
                    cont.ContextMenu = context;
                    cont.ToolTip = tag.ToString();
                }
                ViewImageCanvas.Children.Add(cont);
                return cont;
            });
        }
        private void DeleteLabel(object sender, RoutedEventArgs e) {
            if (sender is not MenuItem mn) return;
            // 대응되는 경계상자 숨김
            ContentControl bbox = ViewImageCanvas.Children.OfType<ContentControl>().First(s => mn.Tag.Equals(s.Tag));
            bbox.Visibility = Visibility.Collapsed;
        }
        private void RefreshBoundaryBoxes() {
            if (ViewImageControl.Source is not BitmapSource bitmap) return;
            // 경계 상자 위치 갱신은 UI 이미지 크기 조정에 수반되는 경우가 많기 때문에 UI 로드가 끝난 다음에 수행함.
            Dispatcher.Invoke(() => {
                double afterScale = ViewImageControl.ActualWidth / bitmap.PixelWidth;
                IEnumerable<ContentControl> boundingBoxes = ViewImageCanvas.Children.OfType<ContentControl>();
                foreach (ContentControl box in boundingBoxes) {
                    double newLeft = Canvas.GetLeft(box) / CurrentScale * afterScale;
                    double newTop = Canvas.GetTop(box) / CurrentScale * afterScale;
                    double newWidth = box.Width / CurrentScale * afterScale;
                    double newHeight = box.Height / CurrentScale * afterScale;
                    AnnotationRecord? realBox = Annotations?.FirstOrDefault(s => s == box.Tag);
                    if (realBox is not null) {
                        // 원본에서 스케일링 한 결과와 UI 박스에서 스케일링 한 결과의 오차가 작으면 원본에서 스케일링한 결과로 반영
                        double errorThreshold = Math.Max(afterScale > CurrentScale ? afterScale : CurrentScale, 1);
                        double newLeftFromOriginal = realBox.Left * afterScale;
                        double newTopFromOriginal = realBox.Top * afterScale;
                        double newWidthFromOriginal = realBox.Width * afterScale;
                        double newHeightFromOriginal = realBox.Height * afterScale;
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
            if (!BboxInsertMode || CurrentCategory is null) {
                // 크로스헤어 있으면 삭제
                Line[] line = ViewImageCanvas.Children.OfType<Line>().ToArray();
                foreach (Line i in line) ViewImageCanvas.Children.Remove(i);
            } else {
                Point current = e.GetPosition(ViewImageControl);
                // 크로스헤어
                IEnumerable<Line> line = ViewImageCanvas.Children.OfType<Line>();
                if (line.Any()) {
                    foreach (Line i in line) {
                        if (i.Tag is int tag) {
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
                } else {
                    Line hline = new() {
                        X1 = 0,
                        X2 = ViewImageCanvas.ActualWidth,
                        Y1 = current.Y,
                        Y2 = current.Y,
                        Stroke = Brushes.Black,
                        StrokeThickness = 2,
                        Tag = Tag_HorizontalCrosshair,
                    };
                    Line vline = new() {
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
                }
                // 미리보기 상자
                if (DragStartPoint is not null) {
                    if (e.LeftButton == MouseButtonState.Pressed) {
                        if (PreviewBbox is null) {
                            double startX = DragStartPoint.Value.X;
                            double startY = DragStartPoint.Value.Y;
                            double endX = current.X;
                            double endY = current.Y;
                            Miscellaneous.SortTwoValues(ref startX, ref endX);
                            Miscellaneous.SortTwoValues(ref startY, ref endY);
                            ContentControl bbox = AddBoundaryBox(ZIndex_PreviewBbox, Tag_PreviewBbox, startX, startY, endX - startX, endY - startY, CurrentCategory, false);
                            PreviewBbox = bbox;
                        } else {
                            double startX = DragStartPoint.Value.X;
                            double startY = DragStartPoint.Value.Y;
                            double endX = current.X;
                            double endY = current.Y;
                            Miscellaneous.SortTwoValues(ref startX, ref endX);
                            Miscellaneous.SortTwoValues(ref startY, ref endY);
                            PreviewBbox.Width = endX - startX;
                            PreviewBbox.Height = endY - startY;
                            Canvas.SetLeft(PreviewBbox, startX);
                            Canvas.SetTop(PreviewBbox, startY);
                        }
                    } else if (e.LeftButton == MouseButtonState.Released) {
                        // 마우스를 이미지 밖에서 뗀 경우, 마지막 정보를 기준으로 경계 상자로 반영함.
                        if (PreviewBbox is not null) {
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
            Miscellaneous.SortTwoValues(ref startX, ref endX);
            Miscellaneous.SortTwoValues(ref startY, ref endY);
            PreviewBbox.Width = endX - startX;
            PreviewBbox.Height = endY - startY;
            Canvas.SetLeft(PreviewBbox, startX);
            Canvas.SetTop(PreviewBbox, startY);
            PreviewBbox.Tag = Tag_UncommittedBbox;
            DragStartPoint = null;
            PreviewBbox = null;
        }
        private void ImageViewport_Unloaded(object sender, RoutedEventArgs e) {
            if (Annotations is INotifyCollectionChanged lbl) {
                lbl.CollectionChanged -= AnnotationsCollectionChanged;
            }
        }
        private void ViewImageCanvas_MouseWheel(object sender, MouseWheelEventArgs e) {
            if (FitViewport || BboxInsertMode) return;
            if (ViewImageControl.Source is BitmapSource bitmap && Keyboard.IsKeyDown(Key.LeftCtrl)) {
                double newScale = e.Delta > 0 ? Math.Clamp(CurrentScale * 1.1, 0.5, 2) : Math.Clamp(CurrentScale / 1.1, 0.5, 2);
                double newWidth = bitmap.PixelWidth * newScale;
                double newHeight = bitmap.PixelHeight * newScale;
                ViewImageControl.MaxWidth = newWidth;
                ViewImageControl.MaxHeight = newHeight;
                ViewGrid.Width = newWidth;
                ViewGrid.Height = newHeight;
                RefreshBoundaryBoxes();
                e.Handled = true;
            }
        }
        #endregion

        public event CommitBboxEventHandler? CommitBbox;

        public void TryCommitBbox() {
            if (ViewImageControl.Source is not BitmapSource bitmap) return;

            List<AnnotationRecord> deleted = new();
            List<AnnotationRecord> added = new();
            List<AnnotationRecord> changed_old = new();
            List<AnnotationRecord> changed_new = new();
            IEnumerable<ContentControl> bboxes = ViewImageCanvas.Children.OfType<ContentControl>();
            foreach (ContentControl bbox in bboxes) {
                if (bbox.Visibility == Visibility.Collapsed) {
                    // 삭제
                    AnnotationRecord? realBox = Annotations?.FirstOrDefault(s => s == bbox.Tag);
                    if (realBox is not null) deleted.Add(realBox);
                } else if (bbox.Tag is int tag && tag == Tag_UncommittedBbox) {
                    // 추가
                    if (CurrentCategory is not null) {
                        float left = (float)Math.Clamp(Canvas.GetLeft(bbox) / CurrentScale, 0, bitmap.PixelWidth);
                        float top = (float)Math.Clamp(Canvas.GetTop(bbox) / CurrentScale, 0, bitmap.PixelHeight);
                        float width = (float)Math.Clamp(bbox.Width / CurrentScale, 0, bitmap.PixelWidth - left);
                        float height = (float)Math.Clamp(bbox.Height / CurrentScale, 0, bitmap.PixelHeight - top);
                        added.Add(new(new(), left, top, width, height, CurrentCategory));
                    }
                } else {
                    // 이동
                    float left = (float)Math.Clamp(Canvas.GetLeft(bbox) / CurrentScale, 0, bitmap.PixelWidth);
                    float top = (float)Math.Clamp(Canvas.GetTop(bbox) / CurrentScale, 0, bitmap.PixelHeight);
                    float width = (float)Math.Clamp(bbox.Width / CurrentScale, 0, bitmap.PixelWidth - left);
                    float height = (float)Math.Clamp(bbox.Height / CurrentScale, 0, bitmap.PixelHeight - top);
                    AnnotationRecord? realBox = Annotations?.FirstOrDefault(s => s == bbox.Tag);
                    if (realBox is not null) {
                        double errorThreshold = Math.Max(1 / CurrentScale, 1);
                        int notChangedPositionsCount = 0;
                        // 새 좌표와 현재 좌표의 오차가 작으면 새 좌표 무시. (좌표 변환 과정에서의 잠재적 오차 감안)
                        if (Math.Abs(realBox.Left - left) < errorThreshold) {
                            notChangedPositionsCount++;
                            left = realBox.Left;
                        }
                        if (Math.Abs(realBox.Top - top) < errorThreshold) {
                            notChangedPositionsCount++;
                            top = realBox.Top;
                        }
                        if (Math.Abs(realBox.Width - width) < errorThreshold) {
                            notChangedPositionsCount++;
                            width = realBox.Width;
                        }
                        if (Math.Abs(realBox.Height - height) < errorThreshold) {
                            notChangedPositionsCount++;
                            height = realBox.Height;
                        }
                        if (notChangedPositionsCount < 4) {
                            changed_old.Add(realBox);
                            changed_new.Add(new(realBox.Image, left, top, width, height, realBox.Category));
                        }
                    }
                }
            }

            if (added.Count > 0 || deleted.Count > 0 || changed_old.Count > 0) CommitBbox?.Invoke(this, new(added, changed_old, changed_new, deleted));
        }
    }
}
