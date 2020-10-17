using LabelAnnotator.Views;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LabelAnnotator.ViewModels {
    public class MainWindowViewModel : BindableBase {
        #region 생성자
        public MainWindowViewModel(MainWindow View) {
            ShortcutSaveBbox = Key.S;
            ShortcutImageUp = Key.E;
            ShortcutImageDown = Key.D;
            ShortcutToggleBboxMode = Key.W;
            ShortcutCategoryUp = Key.Q;
            ShortcutCategoryDown = Key.A;
            ShortcutToggleFitToViewport = Key.F;

            _BboxInsertMode = false;
            _FitToViewport = true;
            _MainImage = null;
            _CategoryNameToAdd = "";
            this.View = View;
            Images = new ObservableCollection<ImageRecord>();
            Categories = new ObservableCollection<ClassRecord>();

            Panel.SetZIndex(View.ViewImageControl, ZIndex_Image);

            CmdLoadLabel = new DelegateCommand(LoadLabel);
            CmdSaveLabel = new DelegateCommand(SaveLabel);
            CmdManageLabel = new DelegateCommand(ManageLabel);
            CmdSetting = new DelegateCommand(Setting);
            CmdCommitBbox = new DelegateCommand(CommitBbox);
            CmdToggleBboxMode = new DelegateCommand(ToggleBboxMode);
            CmdCanvasMouseLeftButtonDown = new DelegateCommand<MouseButtonEventArgs>(CanvasMouseLeftButtonDown);
            CmdCanvasMouseMove = new DelegateCommand<MouseEventArgs>(CanvasMouseMove);
            CmdCanvasMouseLeftButtonUp = new DelegateCommand<MouseButtonEventArgs>(CanvasMouseLeftButtonUp);
            CmdCategoryUp = new DelegateCommand(CategoryUp);
            CmdCategoryDown = new DelegateCommand(CategoryDown);
            CmdAddCategory = new DelegateCommand(AddCategory);
            CmdRenameCategory = new DelegateCommand(RenameCategory);
            CmdDeleteCategory = new DelegateCommand(DeleteCategory);
            CmdImageUp = new DelegateCommand(ImageUp);
            CmdImageDown = new DelegateCommand(ImageDown);
            CmdAddImage = new DelegateCommand(AddImage);
            CmdDeleteImage = new DelegateCommand(DeleteImage);
            CmdToggleFitToViewport = new DelegateCommand(ToggleFitToViewport);
            CmdCanvasSizeChanged = new DelegateCommand(CanvasSizeChanged);
            CmdWindowGotFocus = new DelegateCommand<RoutedEventArgs>(WindowGotFocus);
        }
        #endregion

        #region 필드, 바인딩되지 않는 프로퍼티
        private double CurrentScale;
        private readonly List<LabelRecord> Labels = new List<LabelRecord>();
        public MainWindow View { get; }
        private Point? DragStartPoint = null;
        private ContentControl? PreviewBbox = null;

        #region ZIndice, Tags
        private const int ZIndex_Image = 0;
        private const int ZIndex_PreviewBbox = 1;
        private const int ZIndex_Crosshair = 2;
        private const int ZIndex_Bbox = 3;
        private const int Tag_HorizontalCrosshair = -1;
        private const int Tag_VerticalCrosshair = -2;
        private const int Tag_PreviewBbox = -3;
        private const int Tag_UncommittedBbox = -4;
        #endregion
        #endregion

        #region 바인딩되는 프로퍼티
        public ObservableCollection<ImageRecord> Images { get; }
        private ImageRecord? _SelectedImage;
        public ImageRecord? SelectedImage {
            get => _SelectedImage;
            set {
                if (SetProperty(ref _SelectedImage, value)) {
                    if (value is null) BboxInsertMode = false;
                    RefreshImage();
                }
            }
        }
        public ObservableCollection<ClassRecord> Categories { get; }
        private ClassRecord? _SelectedCategory;
        public ClassRecord? SelectedCategory {
            get => _SelectedCategory;
            set {
                if (SetProperty(ref _SelectedCategory, value)) {
                    UpdateBoundaryBoxes();
                    if (value is null || value.All) BboxInsertMode = false;
                }
            }
        }

        #region 단축키
        private Key _ShortcutSaveBbox;
        public Key ShortcutSaveBbox {
            get => _ShortcutSaveBbox;
            set => SetProperty(ref _ShortcutSaveBbox, value);
        }
        private Key _ShortcutImageUp;
        public Key ShortcutImageUp {
            get => _ShortcutImageUp;
            set => SetProperty(ref _ShortcutImageUp, value);
        }
        private Key _ShortcutImageDown;
        public Key ShortcutImageDown {
            get => _ShortcutImageDown;
            set => SetProperty(ref _ShortcutImageDown, value);
        }
        private Key _ShortcutToggleBboxMode;
        public Key ShortcutToggleBboxMode {
            get => _ShortcutToggleBboxMode;
            set => SetProperty(ref _ShortcutToggleBboxMode, value);
        }
        private Key _ShortcutCategoryUp;
        public Key ShortcutCategoryUp {
            get => _ShortcutCategoryUp;
            set => SetProperty(ref _ShortcutCategoryUp, value);
        }
        private Key _ShortcutCategoryDown;
        public Key ShortcutCategoryDown {
            get => _ShortcutCategoryDown;
            set => SetProperty(ref _ShortcutCategoryDown, value);
        }
        private Key _ShortcutToggleFitToViewport;
        public Key ShortcutToggleFitToViewport {
            get => _ShortcutToggleFitToViewport;
            set => SetProperty(ref _ShortcutToggleFitToViewport, value);
        }
        #endregion

        private bool _BboxInsertMode;
        /// <summary>
        /// 경계 상자 삽입 모드의 활성화 여부입니다.
        /// </summary>
        public bool BboxInsertMode {
            get => _BboxInsertMode;
            set => SetProperty(ref _BboxInsertMode, value);
        }
        private bool _FitToViewport;
        public bool FitToViewport {
            get => _FitToViewport;
            set {
                if (SetProperty(ref _FitToViewport, value)) {
                    if (MainImage is null) return;
                    if (_FitToViewport) {
                        View.ViewViewport.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                        View.ViewViewport.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
                        View.ViewGrid.Width = double.NaN;
                        View.ViewGrid.Height = double.NaN;
                        View.ViewGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
                        View.ViewGrid.VerticalAlignment = VerticalAlignment.Stretch;
                        View.Dispatcher.Invoke(() => {
                            View.ViewImageControl.MaxWidth = View.ViewViewport.ViewportWidth;
                            View.ViewImageControl.MaxHeight = View.ViewViewport.ViewportHeight;
                        }, DispatcherPriority.Input);
                    } else {
                        View.ViewViewport.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
                        View.ViewViewport.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
                        View.ViewImageControl.MaxWidth = MainImage.PixelWidth;
                        View.ViewImageControl.MaxHeight = MainImage.PixelHeight;
                        View.ViewGrid.Width = MainImage.PixelWidth;
                        View.ViewGrid.Height = MainImage.PixelHeight;
                        View.ViewGrid.HorizontalAlignment = HorizontalAlignment.Left;
                        View.ViewGrid.VerticalAlignment = VerticalAlignment.Top;
                    }
                    View.Dispatcher.Invoke(UpdateBoundaryBoxes, DispatcherPriority.Loaded);
                }
            }
        }
        private BitmapSource? _MainImage;
        public BitmapSource? MainImage {
            get => _MainImage;
            set {
                if (SetProperty(ref _MainImage, value) && value is object) {
                    if (FitToViewport) {
                        View.ViewImageControl.MaxWidth = View.ViewViewport.ViewportWidth;
                        View.ViewImageControl.MaxHeight = View.ViewViewport.ViewportHeight;
                    } else {
                        View.ViewImageControl.MaxWidth = value.PixelWidth;
                        View.ViewImageControl.MaxHeight = value.PixelHeight;
                    }
                }
            }
        }
        private string _CategoryNameToAdd;
        public string CategoryNameToAdd {
            get => _CategoryNameToAdd;
            set => SetProperty(ref _CategoryNameToAdd, value);
        }
        #endregion

        #region 커맨드
        #region 레이블 불러오기, 내보내기, 설정
        public ICommand CmdLoadLabel { get; }
        private void LoadLabel() {
            OpenFileDialog dlg = new OpenFileDialog {
                Filter = "CSV 파일|*.csv",
                Multiselect = false
            };
            if (dlg.ShowDialog().GetValueOrDefault()) {
                ClearBoundaryBoxes();
                Labels.Clear();
                Images.Clear();
                Categories.Clear();
                string basePath = System.IO.Path.GetDirectoryName(dlg.FileName) ?? "";
                IEnumerable<string> lines = File.ReadLines(dlg.FileName);
                SortedSet<ImageRecord> images = new SortedSet<ImageRecord>();
                SortedSet<ClassRecord> categories = new SortedSet<ClassRecord> {
                    ClassRecord.AllLabel()
                };
                foreach (string line in lines) {
                    (ImageRecord? img, LabelRecord? lbl) = Extensions.DeserializeRecords(basePath, line);
                    if (img is object) {
                        images.Add(img);
                        if (lbl is object) {
                            Labels.Add(lbl);
                            if (categories.TryGetValue(lbl.Class, out ClassRecord? found)) {
                                lbl.Class = found;
                            } else {
                                lbl.Class.ColorBrush = GenerateColor(lbl.Class.Name, categories);
                                categories.Add(lbl.Class);
                            }
                        }
                    }
                }
                foreach (ImageRecord i in images) Images.Add(i);
                RefreshCommonPath();
                foreach (ClassRecord classname in categories) Categories.Add(classname);
                if (Images.Count > 0) SelectedImage = Images[0];
                SelectedCategory = Categories[0];
            }
        }
        public ICommand CmdSaveLabel { get; }
        private void SaveLabel() {
            SaveFileDialog dlg = new SaveFileDialog {
                Filter = "CSV 파일|*.csv",
                DefaultExt = ".csv"
            };
            if (dlg.ShowDialog().GetValueOrDefault()) {
                string basePath = System.IO.Path.GetDirectoryName(dlg.FileName) ?? "";
                using StreamWriter f = File.CreateText(dlg.FileName);
                ILookup<ImageRecord, LabelRecord> labelsByImage = Labels.ToLookup(s => s.Image);
                foreach (ImageRecord i in Images) {
                    IEnumerable<LabelRecord> labelsInImage = labelsByImage[i];
                    if (labelsInImage.Any()) {
                        // 양성 레이블
                        foreach (LabelRecord j in labelsInImage) f.WriteLine(j.Serialize(basePath));
                    } else {
                        // 음성 레이블
                        f.WriteLine(i.SerializeAsNegative(basePath));
                    }
                }
            }
        }
        public ICommand CmdManageLabel { get; }
        private void ManageLabel() {
            ManageWindow win = new ManageWindow();
            win.ShowDialog();
        }
        public ICommand CmdSetting { get; }
        private void Setting() {
            SettingWindow win = new SettingWindow();
            win.ShowDialog();
        }
        #endregion

        #region 경계 상자 수정
        public ICommand CmdCommitBbox { get; }
        private void CommitBbox() {
            if (SelectedImage is null || SelectedCategory is null || MainImage is null) return;

            double invScale = 1 / CurrentScale;
            int DeletedBboxesCount = 0;
            foreach (ContentControl i in View.ViewImageCanvas.Children.OfType<ContentControl>().OrderBy(s => (int)s.Tag)) {
                int tag = (int)i.Tag;
                int idx = tag - DeletedBboxesCount;
                double left = Math.Clamp(Canvas.GetLeft(i) * invScale, 0, MainImage.PixelWidth);
                double top = Math.Clamp(Canvas.GetTop(i) * invScale, 0, MainImage.PixelHeight);
                double right = Math.Clamp((Canvas.GetLeft(i) + i.Width) * invScale, 0, MainImage.PixelWidth);
                double bottom = Math.Clamp((Canvas.GetTop(i) + i.Height) * invScale, 0, MainImage.PixelHeight);
                if (i.Visibility == Visibility.Collapsed) {
                    // 삭제
                    Labels.RemoveAt(idx);
                    DeletedBboxesCount++;
                } else {
                    if (tag == Tag_UncommittedBbox) {
                        Labels.Add(new LabelRecord(SelectedImage, left, top, right, bottom, SelectedCategory));
                    } else if (tag >= 0) {
                        LabelRecord lbl = Labels[idx];
                        double errorThreshold = Math.Max(invScale, 1);
                        // 계산한 좌표와 현재 좌표의 오차가 클 때에만 반영. (스케일링 과정에서의 잠재적 오차 감안)
                        if (Math.Abs(lbl.Left - left) > errorThreshold) lbl.Left = left;
                        if (Math.Abs(lbl.Top - top) > errorThreshold) lbl.Top = top;
                        if (Math.Abs(lbl.Right - right) > errorThreshold) lbl.Right = right;
                        if (Math.Abs(lbl.Bottom - bottom) > errorThreshold) lbl.Bottom = bottom;
                    }
                }
            }
            // 경계 상자 다시 그리기
            UpdateBoundaryBoxes();
        }
        public ICommand CmdToggleBboxMode { get; }
        private void ToggleBboxMode() {
            BboxInsertMode = !BboxInsertMode;
        }
        #endregion

        #region 경계 상자 실시간 수정
        public ICommand CmdCanvasMouseLeftButtonDown { get; }
        private void CanvasMouseLeftButtonDown(MouseButtonEventArgs e) {
            if (!BboxInsertMode) return;
            DragStartPoint = e.GetPosition(View.ViewImageControl);
        }
        public ICommand CmdCanvasMouseMove { get; }
        private void CanvasMouseMove(MouseEventArgs e) {
            if (!BboxInsertMode || SelectedCategory is null || SelectedCategory.All) {
                // 크로스헤어 있으면 삭제
                List<Line> line = View.ViewImageCanvas.Children.OfType<Line>().ToList();
                foreach (Line i in line) {
                    View.ViewImageCanvas.Children.Remove(i);
                }
            } else {
                Point current = e.GetPosition(View.ViewImageControl);
                // 크로스헤어
                List<Line> line = View.ViewImageCanvas.Children.OfType<Line>().ToList();
                if (line.Count == 0) {
                    Line hline = new Line {
                        X1 = 0,
                        X2 = View.ViewImageCanvas.ActualWidth,
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
                        Y2 = View.ViewImageCanvas.ActualHeight,
                        Stroke = Brushes.Black,
                        StrokeThickness = 2,
                        Tag = Tag_VerticalCrosshair,
                    };
                    Panel.SetZIndex(hline, ZIndex_Crosshair);
                    Panel.SetZIndex(vline, ZIndex_Crosshair);
                    View.ViewImageCanvas.Children.Add(hline);
                    View.ViewImageCanvas.Children.Add(vline);
                } else {
                    foreach (Line i in line) {
                        int tag = (int)i.Tag;
                        if (tag == Tag_HorizontalCrosshair) {
                            i.X2 = View.ViewImageCanvas.ActualWidth;
                            i.Y1 = current.Y;
                            i.Y2 = current.Y;
                        } else if (tag == Tag_VerticalCrosshair) {
                            i.X1 = current.X;
                            i.X2 = current.X;
                            i.Y2 = View.ViewImageCanvas.ActualHeight;
                        }
                    }
                }
                // 미리보기 상자
                if (e.LeftButton == MouseButtonState.Pressed && DragStartPoint is object) {
                    if (PreviewBbox is null) {
                        double startX = DragStartPoint.Value.X;
                        double startY = DragStartPoint.Value.Y;
                        double endX = current.X;
                        double endY = current.Y;
                        Extensions.SortTwoValues(ref startX, ref endX);
                        Extensions.SortTwoValues(ref startY, ref endY);
                        ContentControl bbox = AddBoundaryBox(Tag_PreviewBbox, startX, startY, endX - startX, endY - startY, SelectedCategory, false);
                        Panel.SetZIndex(bbox, ZIndex_PreviewBbox);
                        PreviewBbox = bbox;
                    } else {
                        double startX = DragStartPoint.Value.X;
                        double startY = DragStartPoint.Value.Y;
                        double endX = current.X;
                        double endY = current.Y;
                        Extensions.SortTwoValues(ref startX, ref endX);
                        Extensions.SortTwoValues(ref startY, ref endY);
                        PreviewBbox.Width = endX - startX;
                        PreviewBbox.Height = endY - startY;
                        Canvas.SetLeft(PreviewBbox, startX);
                        Canvas.SetTop(PreviewBbox, startY);
                    }
                } else if (e.LeftButton == MouseButtonState.Released && DragStartPoint is object) {
                    // 마우스를 이미지 밖에서 뗀 경우, 마지막 정보를 기준으로 경계 상자로 반영함.
                    if (PreviewBbox is object) {
                        int currentBboxesCount = View.ViewImageCanvas.Children.OfType<ContentControl>().Count();
                        double startX = Canvas.GetLeft(PreviewBbox);
                        double startY = Canvas.GetTop(PreviewBbox);
                        double endX = startX + PreviewBbox.Width;
                        double endY = startY + PreviewBbox.Height;
                        AddBoundaryBox(currentBboxesCount, startX, startY, endX, endY, SelectedCategory, false);
                        PreviewBbox = null;
                    }
                }
            }
        }
        public ICommand CmdCanvasMouseLeftButtonUp { get; }
        private void CanvasMouseLeftButtonUp(MouseButtonEventArgs e) {
            if (!BboxInsertMode || SelectedCategory is null || SelectedCategory.All || PreviewBbox is null || DragStartPoint is null) return;
            Point dragEnd = e.GetPosition(View.ViewImageControl);
            View.ViewImageCanvas.Children.Remove(PreviewBbox);
            double startX = DragStartPoint.Value.X;
            double startY = DragStartPoint.Value.Y;
            double endX = dragEnd.X;
            double endY = dragEnd.Y;
            Extensions.SortTwoValues(ref startX, ref endX);
            Extensions.SortTwoValues(ref startY, ref endY);
            AddBoundaryBox(Tag_UncommittedBbox, startX, startY, endX, endY, SelectedCategory, false);
            DragStartPoint = null;
            PreviewBbox = null;
        }
        #endregion

        #region 분류 수정
        public ICommand CmdCategoryUp { get; }
        private void CategoryUp() {
            if (SelectedCategory is null) {
                int total = Categories.Count;
                if (total > 0) SelectedCategory = Categories[total - 1];
                return;
            }
            int current = Categories.IndexOf(SelectedCategory);
            int target = Math.Max(0, current - 1);
            SelectedCategory = Categories[target];
            View.ViewCategoriesList.ScrollIntoView(Categories[target]);
        }
        public ICommand CmdCategoryDown { get; }
        private void CategoryDown() {
            int total = Categories.Count;
            if (SelectedCategory is null) {
                if (total > 0) SelectedCategory = Categories[0];
                return;
            }
            int current = Categories.IndexOf(SelectedCategory);
            int target = Math.Min(current + 1, total - 1);
            SelectedCategory = Categories[target];
            View.ViewCategoriesList.ScrollIntoView(Categories[target]);
        }
        public ICommand CmdAddCategory { get; }
        private void AddCategory() {
            ClassRecord add = ClassRecord.FromName(CategoryNameToAdd);
            if (!Categories.Contains(add)) {
                add.ColorBrush = GenerateColor(CategoryNameToAdd, Categories);
                Categories.Add(add);
            }
        }
        public ICommand CmdRenameCategory { get; }
        private void RenameCategory() {
            if (SelectedCategory is null || SelectedCategory.All) return;
            MessageBoxResult res = MessageBox.Show($"분류가 {SelectedCategory}인 모든 경계 상자의 분류 이름을 {CategoryNameToAdd}으로 변경합니다.", "", MessageBoxButton.OKCancel);
            if (res == MessageBoxResult.Cancel) return;
            ClassRecord newCat = ClassRecord.FromName(CategoryNameToAdd);
            newCat.ColorBrush = GenerateColor(CategoryNameToAdd, Categories);
            int idx = Categories.IndexOf(SelectedCategory);
            foreach (LabelRecord label in Labels.Where(s => s.Class == SelectedCategory)) {
                label.Class = newCat;
            }
            Categories[idx] = newCat;
            SelectedCategory = newCat;
            UpdateBoundaryBoxes();
        }
        public ICommand CmdDeleteCategory { get; }
        private void DeleteCategory() {
            if (SelectedCategory is null || SelectedCategory.All) return;
            MessageBoxResult res = MessageBox.Show($"분류가 {SelectedCategory}인 모든 경계 상자를 삭제합니다.", "", MessageBoxButton.OKCancel);
            if (res == MessageBoxResult.Cancel) return;
            res = MessageBox.Show("포함한 경계 상자가 이 분류 뿐인 이미지를 음성 샘플로 남기기를 원하시면 '예', 아예 삭제하길 원하시면 '아니요'를 선택해 주세요.", "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (res == MessageBoxResult.Cancel) return;

            Categories.Remove(SelectedCategory);
            SelectedCategory = Categories.First(s => s.All);

            if (res == MessageBoxResult.No) {
                List<LabelRecord> delete = Labels.Where(s => s.Class == SelectedCategory).ToList();
                foreach (LabelRecord i in delete) Labels.Remove(i);
                foreach (ImageRecord i in delete.Select(s => s.Image).Distinct()) Images.Remove(i);
                if (SelectedImage is object) {
                    if (Images.Count > 0 && !Images.Contains(SelectedImage)) SelectedImage = Images[0];
                } else {
                    UpdateBoundaryBoxes();
                }
            } else {
                Labels.RemoveAll(s => s.Class == SelectedCategory);
                UpdateBoundaryBoxes();
            }
        }
        #endregion

        #region 이미지 수정
        public ICommand CmdImageUp { get; }
        private void ImageUp() {
            if (SelectedImage is null) {
                int total = Images.Count;
                if (total > 0) SelectedImage = Images[total - 1];
                return;
            }
            int current = Images.IndexOf(SelectedImage);
            int target = Math.Max(0, current - 1);
            SelectedImage = Images[target];
            View.ViewImagesList.ScrollIntoView(Images[target]);
        }
        public ICommand CmdImageDown { get; }
        private void ImageDown() {
            int total = Images.Count;
            if (SelectedImage is null) {
                if (total > 0) SelectedImage = Images[0];
                return;
            }
            int current = Images.IndexOf(SelectedImage);
            int target = Math.Min(current + 1, total - 1);
            SelectedImage = Images[target];
            View.ViewImagesList.ScrollIntoView(Images[target]);
        }
        public ICommand CmdAddImage { get; }
        private void AddImage() {
            OpenFileDialog dlg = new OpenFileDialog {
                Filter = $"이미지 파일|{string.Join(";", Extensions.ApprovedImageExtension.Select(s => $"*{s}"))}",
                Multiselect = true,
            };
            if (dlg.ShowDialog().GetValueOrDefault()) {
                SortedSet<ImageRecord> add = new SortedSet<ImageRecord>(dlg.FileNames.Select(s => new ImageRecord(s)));
                add.ExceptWith(Images);
                foreach (ImageRecord img in add) {
                    Images.Add(img);
                }
                if (dlg.FileNames.Length != add.Count) MessageBox.Show("선택한 이미지 중 일부가 이미 데이터셋에 포함되어 있습니다. 해당 이미지를 무시했습니다.", "", MessageBoxButton.OK, MessageBoxImage.Warning);
                if (add.Count > 0) RefreshCommonPath();
            }
        }
        public ICommand CmdDeleteImage { get; }
        private void DeleteImage() {
            MessageBoxResult res = MessageBox.Show("현재 선택한 이미지에 포함된 모든 경계 상자를 지웁니다. 해당 이미지를 음성 샘플로 남기기를 원하시면 '예', 아예 삭제하길 원하시면 '아니요'를 선택해 주세요.",
                "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (res == MessageBoxResult.Cancel) return;
            SortedSet<ImageRecord> selected = new SortedSet<ImageRecord>(View.ViewImagesList.SelectedItems.OfType<ImageRecord>());
            if (selected.Count == 0) return;
            Labels.RemoveAll(s => selected.Contains(s.Image));
            if (res == MessageBoxResult.No) {
                // 아예 삭제하는 경우 선택중인 이미지 삭제
                SelectedImage = null;
                foreach (ImageRecord i in selected) {
                    Images.Remove(i);
                }
                RefreshCommonPath();
            }
            ClearBoundaryBoxes();
        }
        #endregion

        #region 이미지 표출
        public ICommand CmdToggleFitToViewport { get; }
        private void ToggleFitToViewport() {
            FitToViewport = !FitToViewport;
        }
        public ICommand CmdCanvasSizeChanged { get; }
        private void CanvasSizeChanged() {
            if (!FitToViewport) return;
            // 이미지 크기 재조정
            if (MainImage is null) return;
            View.Dispatcher.Invoke(() => {
                View.ViewImageControl.MaxWidth = Math.Max(View.ViewViewport.ViewportWidth, 0);
                View.ViewImageControl.MaxHeight = Math.Max(View.ViewViewport.ViewportHeight, 0);
            }, DispatcherPriority.Loaded);
            View.Dispatcher.Invoke(() => {
                // 이미지 크기 조정이 완료되면 경계상자 크기 조정 실행
                double afterScale = View.ViewImageControl.ActualWidth / MainImage.PixelWidth;
                IEnumerable<ContentControl> boundingBoxes = View.ViewImageCanvas.Children.OfType<ContentControl>();
                foreach (ContentControl box in boundingBoxes) {
                    double newLeft = Canvas.GetLeft(box) / CurrentScale * afterScale;
                    double newTop = Canvas.GetTop(box) / CurrentScale * afterScale;
                    double newWidth = box.Width / CurrentScale * afterScale;
                    double newHeight = box.Height / CurrentScale * afterScale;
                    if (Labels.Count > (int)box.Tag) {
                        // 원본에서 스케일링 한 결과와 UI 박스에서 스케일링 한 결과의 오차가 작으면 원본에서 스케일링한 결과로 반영
                        LabelRecord realBox = Labels[(int)box.Tag];
                        double errorThreshold = Math.Max(afterScale > CurrentScale ? afterScale : CurrentScale, 1);
                        double newLeftFromOriginal = realBox.Left * afterScale;
                        double newTopFromOriginal = realBox.Top * afterScale;
                        double newWidthFromOriginal = (realBox.Right - realBox.Left) * afterScale;
                        double newHeightFromOriginal = (realBox.Bottom - realBox.Top) * afterScale;
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

        public ICommand CmdWindowGotFocus { get; }
        private void WindowGotFocus(RoutedEventArgs e) {
            if (e.OriginalSource is TextBox) {
                ShortcutSaveBbox = Key.None;
                ShortcutImageUp = Key.None;
                ShortcutImageDown = Key.None;
                ShortcutToggleBboxMode = Key.None;
                ShortcutCategoryUp = Key.None;
                ShortcutCategoryDown = Key.None;
                ShortcutToggleFitToViewport = Key.None;
            } else {
                ShortcutSaveBbox = Key.S;
                ShortcutImageUp = Key.E;
                ShortcutImageDown = Key.D;
                ShortcutToggleBboxMode = Key.W;
                ShortcutCategoryUp = Key.Q;
                ShortcutCategoryDown = Key.A;
                ShortcutToggleFitToViewport = Key.F;
            }
        }
        #endregion

        #region 프라이빗 메서드
        /// <summary>화면에 표출되어있는 모든 경계 상자를 삭제합니다.</summary>
        private void ClearBoundaryBoxes() {
            List<ContentControl> delete = View.ViewImageCanvas.Children.OfType<ContentControl>().ToList();
            foreach (ContentControl i in delete) View.ViewImageCanvas.Children.Remove(i);
        }
        /// <summary>화면에 표출된 모든 경계 상자를 삭제하고, 현재 선택된 파일과 카테고리에 해당하는 경계 상자를 화면에 표출합니다.</summary>
        private void UpdateBoundaryBoxes() {
            if (SelectedCategory is null || SelectedImage is null || MainImage is null) return;
            ClearBoundaryBoxes();
            CurrentScale = View.ViewImageControl.ActualWidth / MainImage.PixelWidth;
            if (SelectedCategory.All) {
                // 전체 카테고리 보기 모드에서는 경계상자 추가 모드 사용 불가
                BboxInsertMode = false;
                foreach ((int idx, LabelRecord lbl) in Labels.Select((lbl, idx) => (idx, lbl)).Where(s => s.lbl.Image == SelectedImage)) {
                    AddBoundaryBox(idx, lbl.Left, lbl.Top, lbl.Right, lbl.Bottom, lbl.Class, true);
                }
            } else {
                foreach ((int idx, LabelRecord lbl) in Labels.Select((lbl, idx) => (idx, lbl)).Where(s => s.lbl.Image == SelectedImage && s.lbl.Class == SelectedCategory)) {
                    AddBoundaryBox(idx, lbl.Left, lbl.Top, lbl.Right, lbl.Bottom, lbl.Class, true);
                }
            }
        }
        /// <summary>주어진 라벨에 기반한 새로운 경계 상자를 화면에 추가합니다.</summary>
        /// <param name="tag">
        /// 경계 상자가 내부 컬렉션에서 차지하는 인덱스 번호와 같습니다. 경계 상자 컨트롤의 Tag 값으로 사용됩니다.
        /// 0 미만의 값이라면 임시 경계 상자로 간주하며, 삭제 컨텍스트 메뉴를 추가하지 않습니다.
        /// </param>
        /// <param name="needScale">크기 스케일링 여부입니다. <see langword="true"/>이면 주어진 좌표를 이미지의 화면 크기에 맞게 변환합니다.</param>
        /// <returns>추가한 경계 상자의 시각화 컨트롤을 반환합니다.</returns>
        private ContentControl AddBoundaryBox(int tag, double left, double top, double right, double bottom, ClassRecord category, bool needScale) {
            // 화면의 배율에 맞춰 스케일링
            if (needScale && MainImage is object) {
                CurrentScale = View.ViewImageControl.ActualWidth / MainImage.PixelWidth;
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
                Template = (ControlTemplate)View.FindResource("DesignerItemTemplate"),
                DataContext = category,
                ToolTip = category.ToString()
            };
            Canvas.SetLeft(cont, left);
            Canvas.SetTop(cont, top);
            Panel.SetZIndex(cont, ZIndex_Bbox);
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
            View.ViewImageCanvas.Children.Add(cont);
            return cont;
        }
        private void DeleteLabel(object sender, RoutedEventArgs e) {
            if (!(sender is MenuItem mn)) return;
            // 대응되는 경계상자 숨김
            List<ContentControl> bbox = View.ViewImageCanvas.Children.OfType<ContentControl>().Where(s => mn.Tag.Equals(s.Tag)).ToList();
            foreach (ContentControl i in bbox) i.Visibility = Visibility.Collapsed;
        }
        private SolidColorBrush GenerateColor(string CategoryName, IEnumerable<ClassRecord> OldCategories, double Threshold = 100) {
            Random random = new Random(CategoryName.GetHashCode());
            while (true) {
                Color newColor = Color.FromRgb((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256));
                // 흰색과 유사한 색도 제외 (카테고리 리스트 배경이 흰색이므로...)
                if (OldCategories.All(s => newColor.GetColorDistance(s.ColorBrush.Color) >= Threshold) && newColor.GetColorDistance(Colors.White) >= Threshold) {
                    return new SolidColorBrush(newColor);
                }
            }
        }
        private void RefreshCommonPath() {
            string CommonPath = Extensions.GetCommonParentPath(Images.Select(s => s.FullPath));
            foreach (ImageRecord i in Images) i.CommonPath = CommonPath;
        }
        /// <summary>현재 선택된 이미지를 화면에 표시하고 경계 상자를 새로 그립니다.</summary>
        private void RefreshImage() {
            ClearBoundaryBoxes();
            MainImage = null;

            if (SelectedImage is null) return;

            try {
                // 그림 업데이트
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.UriSource = Extensions.FilePathToUri(SelectedImage.FullPath);
                bitmap.EndInit();
                bitmap.Freeze();
                MainImage = bitmap;

                // 이미지 화면 크기를 확정시킨 후에 실행해야 함.
                View.Dispatcher.Invoke(() => { UpdateBoundaryBoxes(); }, DispatcherPriority.Loaded);
            } catch (FileNotFoundException) {
                MessageBox.Show($"해당하는 이미지 파일이 존재하지 않습니다. ({SelectedImage.FullPath})");
            } catch (NotSupportedException) {
                MessageBox.Show($"이미지 파일이 손상되어 읽어올 수 없습니다. ({SelectedImage.FullPath})");
            }
        }
        #endregion
    }
}
