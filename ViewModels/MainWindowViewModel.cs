using COCOAnnotator.Events;
using COCOAnnotator.Records;
using COCOAnnotator.Records.Enums;
using COCOAnnotator.Utilities;
using COCOAnnotator.ViewModels.Commons;
using COCOAnnotator.Views;
using Prism.Commands;
using Prism.Services.Dialogs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace COCOAnnotator.ViewModels {
    public class MainWindowViewModel : ViewModelBase {
        #region 생성자
        public MainWindowViewModel() {
            Title = "COCO 데이터셋 편집기";

            ShortcutSaveBbox = Key.S;
            ShortcutImageUp = Key.E;
            ShortcutImageDown = Key.D;
            ShortcutToggleBboxMode = Key.W;
            ShortcutCategoryUp = Key.Q;
            ShortcutCategoryDown = Key.A;
            ShortcutToggleFitToViewport = Key.F;

            _BboxInsertMode = false;
            _FitViewport = true;
            _CategoryNameToAdd = "";
            Images = new FastObservableCollection<ImageRecord>();
            Categories = new ObservableCollection<CategoryRecord>();
            VisibleAnnotations = new ObservableCollection<AnnotationRecord>();

            CmdViewportDrop = new DelegateCommand<DragEventArgs>(ViewportDrop);
            CmdLoadDataset = new DelegateCommand(LoadDataset);
            CmdSaveDataset = new DelegateCommand(SaveDataset);
            CmdCloseDataset = new DelegateCommand(CloseDataset);
            CmdManageDataset = new DelegateCommand(ManageDataset);
            CmdSetting = new DelegateCommand(Setting);
            CmdTryCommitBbox = new DelegateCommand(TryCommitBbox);
            CmdCommitBbox = new DelegateCommand<CommitBboxEventArgs>(CommitBbox);
            CmdToggleBboxMode = new DelegateCommand(ToggleBboxMode);
            CmdCategoryUp = new DelegateCommand(CategoryUp);
            CmdCategoryDown = new DelegateCommand(CategoryDown);
            CmdAddCategory = new DelegateCommand(AddCategory);
            CmdRenameCategory = new DelegateCommand(RenameCategory);
            CmdDeleteCategory = new DelegateCommand(DeleteCategory);
            CmdImageUp = new DelegateCommand(ImageUp);
            CmdImageDown = new DelegateCommand(ImageDown);
            CmdAddImage = new DelegateCommand(AddImage);
            CmdDeleteImage = new DelegateCommand<IList>(DeleteImage);
            CmdDeleteNegativeImage = new DelegateCommand(DeleteNegativeImage);
            CmdImagesListDrop = new DelegateCommand<DragEventArgs>(ImagesListDrop);
            CmdToggleFitToViewport = new DelegateCommand(ToggleFitToViewport);
            CmdWindowGotFocus = new DelegateCommand<RoutedEventArgs>(WindowGotFocus);
        }
        #endregion

        #region 바인딩되는 프로퍼티
        public FastObservableCollection<ImageRecord> Images { get; }
        private ImageRecord? _SelectedImage;
        public ImageRecord? SelectedImage {
            get => _SelectedImage;
            set {
                if (SetProperty(ref _SelectedImage, value)) {
                    VisibleAnnotations.Clear();
                    if (value is null) {
                        BboxInsertMode = false;
                        MainImageUri = null;
                    } else {
                        try {
                            // 그림 업데이트
                            MainImageUri = value.FullPath.ToUri();
                            UpdateBoundaryBoxes();
                        } catch (FileNotFoundException) {
                            CommonDialogService.MessageBox($"해당하는 이미지 파일이 존재하지 않습니다. ({value.FullPath})");
                        } catch (NotSupportedException) {
                            CommonDialogService.MessageBox($"이미지 파일이 손상되어 읽어올 수 없습니다. ({value.FullPath})");
                        }
                    }
                }
            }
        }
        public ObservableCollection<CategoryRecord> Categories { get; }
        private CategoryRecord? _SelectedCategory;
        public CategoryRecord? SelectedCategory {
            get => _SelectedCategory;
            set {
                if (SetProperty(ref _SelectedCategory, value)) {
                    if (value is null || value.All) {
                        BboxInsertMode = false;
                        CategoryNameToAdd = "";
                    } else {
                        CategoryNameToAdd = value.Name;
                    }
                    UpdateBoundaryBoxes();
                }
            }
        }
        public ObservableCollection<AnnotationRecord> VisibleAnnotations { get; }

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
            set {
                if (value && (SelectedCategory is null || SelectedCategory.All)) return;
                SetProperty(ref _BboxInsertMode, value);
            }
        }
        private bool _FitViewport;
        public bool FitViewport {
            get => _FitViewport;
            set => SetProperty(ref _FitViewport, value);
        }
        private Uri? _MainImageUri;
        public Uri? MainImageUri {
            get => _MainImageUri;
            set => SetProperty(ref _MainImageUri, value);
        }
        private string _CategoryNameToAdd;
        public string CategoryNameToAdd {
            get => _CategoryNameToAdd;
            set => SetProperty(ref _CategoryNameToAdd, value);
        }
        #endregion

        #region 커맨드
        #region 레이블 불러오기, 내보내기, 설정
        public ICommand CmdViewportDrop { get; }
        private void ViewportDrop(DragEventArgs e) {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length >= 1) {
                InternalLoadDataset(files[0]);
            }
        }
        public ICommand CmdLoadDataset { get; }
        private void LoadDataset() {
            if (CommonDialogService.OpenJsonFileDialog(out string filePath)) {
                InternalLoadDataset(filePath);
            }
        }
        public ICommand CmdSaveDataset { get; }
        private void SaveDataset() {
            if (CommonDialogService.SaveJsonFileDialog(out string filePath)) {
                string basePath = Path.GetDirectoryName(filePath) ?? "";
                byte[] CocoContents = SerializationService.Serialize(basePath, Images, Categories.Where(s => !s.All));
                File.WriteAllBytes(filePath, CocoContents);
                Title = $"COCO 데이터셋 편집기 - {filePath}";
            }
        }
        public ICommand CmdCloseDataset { get; }
        private void CloseDataset() {
            bool res = CommonDialogService.MessageBoxOKCancel("현재 열려있는 레이블을 모두 초기화합니다");
            if (!res) return;
            Images.Clear();
            Categories.Clear();
            Title = "COCO 데이터셋 편집기";
        }
        public ICommand CmdManageDataset { get; }
        private void ManageDataset() {
            UserDialogSerivce.ShowDialog(nameof(ManageDialog), new DialogParameters(), _ => { });
        }
        public ICommand CmdSetting { get; }
        private void Setting() {
            UserDialogSerivce.ShowDialog(nameof(SettingDialog), new DialogParameters(), _ => { });
        }
        #endregion

        #region 경계 상자 수정
        public ICommand CmdTryCommitBbox { get; }
        private void TryCommitBbox() {
            EventAggregator.GetEvent<TryCommitBbox>().Publish();
        }
        public ICommand CmdCommitBbox { get; }
        private void CommitBbox(CommitBboxEventArgs e) {
            if (SelectedImage is null) return;

            foreach (AnnotationRecord i in e.Added) {
                SelectedImage.Annotations.Add(new AnnotationRecord(SelectedImage, i.Left, i.Top, i.Width, i.Height, i.Category));
            }
            foreach ((AnnotationRecord old, AnnotationRecord @new) in e.ChangedOldItems.Zip(e.ChangedNewItems)) {
                int idx = SelectedImage.Annotations.IndexOf(old);
                SelectedImage.Annotations[idx].Left = @new.Left;
                SelectedImage.Annotations[idx].Top = @new.Top;
                SelectedImage.Annotations[idx].Width = @new.Width;
                SelectedImage.Annotations[idx].Height = @new.Height;
            }
            foreach (AnnotationRecord i in e.Deleted) {
                SelectedImage.Annotations.Remove(i);
            }
            UpdateBoundaryBoxes();
        }
        public ICommand CmdToggleBboxMode { get; }
        private void ToggleBboxMode() {
            BboxInsertMode = !BboxInsertMode;
        }
        #endregion

        #region 분류 수정
        public ICommand CmdCategoryUp { get; }
        private void CategoryUp() {
            if (Categories.Count == 0) return;
            int target = SelectedCategory is null ? Categories.Count - 1 : Math.Max(0, Categories.IndexOf(SelectedCategory) - 1);
            SelectedCategory = Categories[target];
            EventAggregator.GetEvent<ScrollViewCategoriesList>().Publish(Categories[target]);
        }
        public ICommand CmdCategoryDown { get; }
        private void CategoryDown() {
            if (Categories.Count == 0) return;
            int target = SelectedCategory is null ? 0 : Math.Min(Categories.Count - 1, Categories.IndexOf(SelectedCategory) + 1);
            SelectedCategory = Categories[target];
            EventAggregator.GetEvent<ScrollViewCategoriesList>().Publish(Categories[target]);
        }
        public ICommand CmdAddCategory { get; }
        private void AddCategory() {
            if (string.IsNullOrEmpty(CategoryNameToAdd)) return;
            CategoryRecord add = CategoryRecord.FromName(CategoryNameToAdd);
            if (Categories.Contains(add)) return;
            if (Categories.Count == 0) Categories.Add(CategoryRecord.AllLabel());
            bool addedflag = false;
            for (int i = 0; i < Categories.Count; i++) {
                if (Categories[i] >= add) {
                    Categories.Insert(i, add);
                    addedflag = true;
                    break;
                }
            }
            if (!addedflag) Categories.Add(add);
            RefreshColorOfCategories();
        }
        public ICommand CmdRenameCategory { get; }
        private void RenameCategory() {
            if (SelectedCategory is null || SelectedCategory.All || string.IsNullOrEmpty(CategoryNameToAdd) || CategoryNameToAdd == SelectedCategory.Name) return;
            CategoryRecord? rename = Categories.FirstOrDefault(s => s.Name == CategoryNameToAdd);
            if (rename is null) {
                bool res = CommonDialogService.MessageBoxOKCancel($"분류가 {SelectedCategory}인 모든 경계 상자를 {CategoryNameToAdd}으로 변경합니다.");
                if (!res) return;
                rename = CategoryRecord.FromName(CategoryNameToAdd);
                CategoryRecord OldCategory = SelectedCategory;
                Categories.Remove(OldCategory);
                bool addedflag = false;
                for (int i = 0; i < Categories.Count; i++) {
                    if (Categories[i] >= rename) {
                        Categories.Insert(i, rename);
                        addedflag = true;
                        break;
                    }
                }
                if (!addedflag) Categories.Add(rename);
                foreach (AnnotationRecord label in Images.SelectMany(s => s.Annotations).Where(s => s.Category == OldCategory)) {
                    label.Category = rename;
                }
                SelectedCategory = rename;
            } else {
                bool res = CommonDialogService.MessageBoxOKCancel($"분류가 {SelectedCategory}인 모든 경계 상자를 다른 분류 {CategoryNameToAdd}로 병합합니다.");
                if (!res) return;
                CategoryRecord OldCategory = SelectedCategory;
                Categories.Remove(OldCategory);
                foreach (AnnotationRecord label in Images.SelectMany(s => s.Annotations).Where(s => s.Category == OldCategory)) {
                    label.Category = rename;
                }
                SelectedCategory = rename;
            }
            RefreshColorOfCategories();
            UpdateBoundaryBoxes();
        }
        public ICommand CmdDeleteCategory { get; }
        private void DeleteCategory() {
            if (SelectedCategory is null || SelectedCategory.All) return;
            bool? res = CommonDialogService.MessageBoxYesNoCancel($"포함한 경계 상자의 분류가 {SelectedCategory} 뿐인 이미지를 음성 샘플로 남기기를 원하시면 '예', 아예 삭제하길 원하시면 '아니요'를 선택해 주세요.");
            switch (res) {
            case true: {
                foreach (ImageRecord i in Images) {
                    i.Annotations.RemoveAll(s => s.Category == SelectedCategory);
                }
                if (Categories.Count <= 2) {
                    Categories.Clear();
                    SelectedCategory = null;
                } else {
                    Categories.Remove(SelectedCategory);
                    SelectedCategory = Categories.First(s => s.All);
                }
                RefreshColorOfCategories();
                break;
            }
            case false: {
                SortedSet<ImageRecord> ImagesFromDeletedClass = new SortedSet<ImageRecord>();
                foreach (ImageRecord i in Images) {
                    int deletedCount = i.Annotations.RemoveAll(s => s.Category == SelectedCategory);
                    if (deletedCount > 0) ImagesFromDeletedClass.Add(i);
                }
                Images.RemoveAll(s => s.Annotations.Count == 0 && ImagesFromDeletedClass.Contains(s));
                if (Categories.Count <= 2) {
                    Categories.Clear();
                    SelectedCategory = null;
                } else {
                    Categories.Remove(SelectedCategory);
                    SelectedCategory = Categories.First(s => s.All);
                }
                RefreshColorOfCategories();
                RefreshCommonPath();
                break;
            }
            case null:
                return;
            }
        }
        #endregion

        #region 이미지 수정
        public ICommand CmdImageUp { get; }
        private void ImageUp() {
            if (Images.Count == 0) return;
            int target = SelectedImage is null ? Images.Count - 1 : Math.Max(0, Images.IndexOf(SelectedImage) - 1);
            SelectedImage = Images[target];
            EventAggregator.GetEvent<ScrollViewImagesList>().Publish(Images[target]);
        }
        public ICommand CmdImageDown { get; }
        private void ImageDown() {
            if (Images.Count == 0) return;
            int target = SelectedImage is null ? 0 : Math.Min(Images.Count - 1, Images.IndexOf(SelectedImage) + 1);
            SelectedImage = Images[target];
            EventAggregator.GetEvent<ScrollViewImagesList>().Publish(Images[target]);
        }
        public ICommand CmdAddImage { get; }
        private void AddImage() {
            if (CommonDialogService.OpenImagesDialog(out string[] filePaths)) {
                InternelAddImage(filePaths);
            }
        }
        public ICommand CmdDeleteImage { get; }
        private void DeleteImage(IList SelectedItems) {
            bool? res = CommonDialogService.MessageBoxYesNoCancel("현재 선택한 이미지에 포함된 모든 경계 상자를 지웁니다. 해당 이미지를 음성 샘플로 남기기를 원하시면 '예', 아예 삭제하길 원하시면 '아니요'를 선택해 주세요.");
            switch (res) {
            case true: {
                SortedSet<ImageRecord> SelectedImages = new SortedSet<ImageRecord>(SelectedItems.OfType<ImageRecord>());
                foreach (ImageRecord i in SelectedImages) i.Annotations.Clear();
                VisibleAnnotations.Clear();
                break;
            }
            case false: {
                SortedSet<ImageRecord> SelectedImages = new SortedSet<ImageRecord>(SelectedItems.OfType<ImageRecord>());
                if (SelectedImages.Count == 0) return;
                Images.RemoveAll(s => SelectedImages.Contains(s));
                RefreshCommonPath();
                break;
            }
            case null:
                return;
            }
        }
        public ICommand CmdDeleteNegativeImage { get; }
        private void DeleteNegativeImage() {
            bool res = CommonDialogService.MessageBoxOKCancel("현재 레이블 파일에 포함된 모든 음성 샘플 이미지를 지웁니다.");
            if (!res) return;
            Images.RemoveAll(s => s.Annotations.Count == 0);
            RefreshCommonPath();
        }
        public ICommand CmdImagesListDrop { get; }
        private void ImagesListDrop(DragEventArgs e) {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length >= 1) {
                InternelAddImage(files);
            }
        }
        #endregion

        #region 이미지 표출
        public ICommand CmdToggleFitToViewport { get; }
        private void ToggleFitToViewport() {
            FitViewport = !FitViewport;
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
        /// <summary>화면에 표출된 모든 경계 상자를 삭제하고, 현재 선택된 파일과 카테고리에 해당하는 경계 상자를 화면에 표출합니다.</summary>
        private void UpdateBoundaryBoxes() {
            if (SelectedCategory is null || SelectedImage is null) return;
            VisibleAnnotations.Clear();
            IEnumerable<AnnotationRecord> visibleLabels;
            if (SelectedCategory.All) visibleLabels = SelectedImage.Annotations;
            else visibleLabels = SelectedImage.Annotations.Where(s => s.Category == SelectedCategory);
            foreach (AnnotationRecord i in visibleLabels) VisibleAnnotations.Add(i);
        }
        private void RefreshCommonPath() {
            string CommonPath = Utils.GetCommonParentPath(Images);
            foreach (ImageRecord i in Images) {
                i.DisplayFilename = Utils.GetRelativePath(CommonPath, i.FullPath);
            }
        }
        private void InternalLoadDataset(string filePath) {
            if (!Path.GetExtension(filePath).Equals(".json", StringComparison.OrdinalIgnoreCase)) return;
            Images.Clear();
            Categories.Clear();
            string basePath = Path.GetDirectoryName(filePath) ?? "";
            byte[] CocoContents = File.ReadAllBytes(filePath);
            (ICollection<ImageRecord> images, ICollection<CategoryRecord> categories) = SerializationService.Deserialize(basePath, CocoContents);
            foreach (ImageRecord i in images) Images.Add(i);
            if (Images.Count > 0) SelectedImage = Images[0];
            RefreshCommonPath();
            if (categories.Count >= 1) {
                Categories.Add(CategoryRecord.AllLabel());
                foreach (CategoryRecord classname in categories) Categories.Add(classname);
                SelectedCategory = Categories[0];
            }
            RefreshColorOfCategories();
            Title = $"COCO 데이터셋 편집기 - {filePath}";
        }
        private void RefreshColorOfCategories() {
            switch (SettingService.Color) {
            case SettingColors.Fixed:
                Color[] colors = Utils.GenerateFixedColor(Categories.Count - 1).ToArray();
                // 클래스 중에 제일 앞에 있는 하나는 (전체) 이므로 빼고 진행.
                for (int i = 1; i < Categories.Count; i++) Categories[i].ColorBrush = new SolidColorBrush(colors[i - 1]);
                break;
            case SettingColors.Random:
                IEnumerable<Color> ExistingColors = Categories.Select(s => s.ColorBrush.Color).Distinct().Append(Colors.White);
                for (int i = 1; i < Categories.Count; i++) {
                    if (Categories[i].ColorBrush.Color == Colors.Transparent) Categories[i].ColorBrush = new SolidColorBrush(Utils.GenerateRandomColor(ExistingColors, 100));
                }
                break;
            }
        }
        private void InternelAddImage(string[] filePaths) {
            SortedSet<ImageRecord> add = new SortedSet<ImageRecord>(filePaths.Where(s => Utils.ApprovedImageExtensions.Contains(Path.GetExtension(s))).Select(s => {
                (int Width, int Height) = Utils.GetSizeOfImage(s);
                return new ImageRecord(s, Width, Height);
            }));
            int ImagesCountToAdd = add.Count;
            add.ExceptWith(Images);
            foreach (ImageRecord img in add) Images.Add(img);
            int ImagesCountAdded = add.Count;
            if (ImagesCountToAdd != ImagesCountAdded) CommonDialogService.MessageBox("선택한 이미지 중 일부가 이미 데이터셋에 포함되어 있습니다. 해당 이미지를 무시했습니다.");
            if (ImagesCountAdded > 0) RefreshCommonPath();
        }
        #endregion
    }
}
