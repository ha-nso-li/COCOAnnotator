using COCOAnnotator.Events;
using COCOAnnotator.Records;
using COCOAnnotator.Records.Enums;
using COCOAnnotator.Services.Utilities;
using COCOAnnotator.ViewModels.Commons;
using COCOAnnotator.Views;
using Prism.Commands;
using Prism.Services.Dialogs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace COCOAnnotator.ViewModels {
    public sealed class MainWindowViewModel : ViewModel {
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
            _Dataset = new();
            VisibleAnnotations = [];

            CmdViewportDrop = new DelegateCommand<DragEventArgs>(ViewportDrop);
            CmdLoadDataset = new DelegateCommand(LoadDataset);
            CmdSaveDataset = new DelegateCommand(SaveDataset);
            CmdOpenFolderOrCloseDataset = new DelegateCommand(OpenFolderOrCloseDataset);
            CmdManageDataset = new DelegateCommand(ManageDataset);
            CmdSetting = new DelegateCommand(Setting);
            CmdFailedToLoadImage = new DelegateCommand<FailToLoadImageEventArgs>(FailedToLoadImage);
            CmdTryCommitBbox = new DelegateCommand(TryCommitBbox);
            CmdCommitBbox = new DelegateCommand<CommitBboxEventArgs>(CommitBbox);
            CmdToggleBboxMode = new DelegateCommand(ToggleBboxMode);
            CmdCategoryUp = new DelegateCommand(CategoryUp);
            CmdCategoryDown = new DelegateCommand(CategoryDown);
            CmdAddCategory = new DelegateCommand(AddCategory);
            CmdRenameCategory = new DelegateCommand(RenameCategory);
            CmdMoveCategory = new DelegateCommand<IList>(MoveCategory);
            CmdDeleteCategory = new DelegateCommand(DeleteCategory);
            CmdImageUp = new DelegateCommand(ImageUp);
            CmdImageDown = new DelegateCommand(ImageDown);
            CmdRefreshImagesList = new DelegateCommand(RefreshImagesList);
            CmdDeleteNegativeImage = new DelegateCommand(DeleteNegativeImage);
            CmdToggleFitToViewport = new DelegateCommand(ToggleFitToViewport);
            CmdWindowGotFocus = new DelegateCommand<RoutedEventArgs>(WindowGotFocus);
        }
        #endregion

        #region 바인딩되는 프로퍼티
        private DatasetRecord _Dataset;
        public DatasetRecord Dataset {
            get => _Dataset;
            set => SetProperty(ref _Dataset, value);
        }
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
                        string imageFullPath = Path.Combine(Dataset.BasePath, value.Path);
                        MainImageUri = imageFullPath.ToUri();
                        UpdateBoundaryBoxes();
                    }
                }
            }
        }
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
        private async void ViewportDrop(DragEventArgs e) {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files && files.Length >= 1) {
                await InternalLoadDataset(files[0]).ConfigureAwait(false);
            }
        }
        public ICommand CmdLoadDataset { get; }
        private async void LoadDataset() {
            if (CommonDialogService.OpenJsonFileDialog(out string filePath)) {
                await InternalLoadDataset(filePath).ConfigureAwait(false);
            }
        }
        public ICommand CmdSaveDataset { get; }
        private async void SaveDataset() {
            try {
                string jsonPath = await SerializationService.SerializeAsync(Dataset).ConfigureAwait(false);
                Title = $"COCO 데이터셋 편집기 - {jsonPath}";
                CommonDialogService.MessageBox("현재 데이터셋이 JSON 파일로 저장되었습니다.");
            } catch (ArgumentException) {
                CommonDialogService.MessageBox("열린 데이터셋이 없습니다.");
            }
        }
        public ICommand CmdOpenFolderOrCloseDataset { get; }
        private void OpenFolderOrCloseDataset() {
            if (Dataset.BasePath == "") {
                if (CommonDialogService.OpenFolderDialog(out string folderPath)) {
                    Dataset.BasePath = folderPath;
                    InternalRefreshImagesList();
                }
            } else {
                bool res = CommonDialogService.MessageBoxOKCancel("열려있는 데이터셋을 닫습니다. 저장하지 않은 변경 사항이 손실됩니다.");
                if (!res) return;
                Dataset.Images.Clear();
                Dataset.Categories.Clear();
                Dataset.BasePath = "";
                Title = "COCO 데이터셋 편집기";
            }
        }
        public ICommand CmdManageDataset { get; }
        private void ManageDataset() {
            UserDialogSerivce.ShowDialog(nameof(ManageDialog), new DialogParameters(), _ => { });
        }
        public ICommand CmdSetting { get; }
        private void Setting() {
            UserDialogSerivce.ShowDialog(nameof(SettingDialog), new DialogParameters(), _ => { });
        }
        public ICommand CmdFailedToLoadImage { get; }
        private void FailedToLoadImage(FailToLoadImageEventArgs e) {
            CommonDialogService.MessageBox($"이미지 파일을 읽어올 수 없습니다. 파일이 존재하지 않거나 지원하는 포맷이 아닙니다.{Environment.NewLine}{e.ImageUri.LocalPath}");
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

            SelectedImage.Annotations.AddRange(e.Added.Select(s => new AnnotationRecord(SelectedImage, s.Left, s.Top, s.Width, s.Height, s.Category)));
            foreach ((AnnotationRecord old, AnnotationRecord @new) in e.ChangedOldItems.Zip(e.ChangedNewItems)) {
                int idx = SelectedImage.Annotations.IndexOf(old);
                SelectedImage.Annotations[idx].Left = @new.Left;
                SelectedImage.Annotations[idx].Top = @new.Top;
                SelectedImage.Annotations[idx].Width = @new.Width;
                SelectedImage.Annotations[idx].Height = @new.Height;
            }
            SelectedImage.Annotations.RemoveAll(s => e.Deleted.Contains(s));
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
            if (Dataset.Categories.Count == 0) return;
            int target = SelectedCategory is null ? Dataset.Categories.Count - 1 : Math.Max(0, Dataset.Categories.IndexOf(SelectedCategory) - 1);
            SelectedCategory = Dataset.Categories[target];
            EventAggregator.GetEvent<ScrollViewCategoriesList>().Publish(Dataset.Categories[target]);
        }
        public ICommand CmdCategoryDown { get; }
        private void CategoryDown() {
            if (Dataset.Categories.Count == 0) return;
            int target = SelectedCategory is null ? 0 : Math.Min(Dataset.Categories.Count - 1, Dataset.Categories.IndexOf(SelectedCategory) + 1);
            SelectedCategory = Dataset.Categories[target];
            EventAggregator.GetEvent<ScrollViewCategoriesList>().Publish(Dataset.Categories[target]);
        }
        public ICommand CmdAddCategory { get; }
        private void AddCategory() {
            if (string.IsNullOrEmpty(CategoryNameToAdd)) return;
            CategoryRecord add = CategoryRecord.FromName(CategoryNameToAdd);
            if (Dataset.Categories.Contains(add)) return;
            if (Dataset.Categories.Count == 0) Dataset.Categories.Add(CategoryRecord.AsAll());
            Dataset.Categories.Add(add);
            RefreshColorOfCategories();
        }
        public ICommand CmdRenameCategory { get; }
        private void RenameCategory() {
            if (SelectedCategory is null || SelectedCategory.All || string.IsNullOrEmpty(CategoryNameToAdd) || CategoryNameToAdd == SelectedCategory.Name) return;
            CategoryRecord? rename = Dataset.Categories.FirstOrDefault(s => s.Name == CategoryNameToAdd);
            if (rename is null) {
                bool res = CommonDialogService.MessageBoxOKCancel($"분류가 {SelectedCategory}인 모든 경계 상자를 {CategoryNameToAdd}으로 변경합니다.");
                if (!res) return;
                rename = CategoryRecord.FromName(CategoryNameToAdd);
                CategoryRecord OldCategory = SelectedCategory;
                Dataset.Categories.Remove(OldCategory);
                Dataset.Categories.Add(rename);
                foreach (AnnotationRecord annotation in Dataset.Images.SelectMany(s => s.Annotations).Where(s => s.Category == OldCategory)) annotation.Category = rename;
                SelectedCategory = rename;
            } else {
                bool res = CommonDialogService.MessageBoxOKCancel($"분류가 {SelectedCategory}인 모든 경계 상자를 다른 분류 {CategoryNameToAdd}로 병합합니다.");
                if (!res) return;
                CategoryRecord OldCategory = SelectedCategory;
                Dataset.Categories.Remove(OldCategory);
                foreach (AnnotationRecord annotation in Dataset.Images.SelectMany(s => s.Annotations).Where(s => s.Category == OldCategory)) annotation.Category = rename;
                SelectedCategory = rename;
            }
            RefreshColorOfCategories();
            UpdateBoundaryBoxes();
        }
        public ICommand CmdMoveCategory { get; }
        private void MoveCategory(IList SelectedItems) {
            CategoryRecord[] SelectedCategories = [..SelectedItems.OfType<CategoryRecord>()];
            if (SelectedCategories.Length != 2 || SelectedCategories.Any(s => s.All)) {
                CommonDialogService.MessageBox("분류 2개를 선택하여야 합니다.");
                return;
            }
            CommonDialogService.MessageBox("첫번째로 선택한 분류를 두번째로 선택한 자리로 이동합니다.");
            int idx1 = Dataset.Categories.IndexOf(SelectedCategories[0]);
            int idx2 = Dataset.Categories.IndexOf(SelectedCategories[1]);
            Dataset.Categories.Move(idx1, idx2);
            RefreshColorOfCategories();
        }
        public ICommand CmdDeleteCategory { get; }
        private void DeleteCategory() {
            if (SelectedCategory is null || SelectedCategory.All) return;
            bool? res = CommonDialogService.MessageBoxYesNoCancel($"포함한 경계 상자의 분류가 {SelectedCategory} 뿐인 이미지를 음성 샘플로 남기기를 원하시면 '예', 아예 삭제하길 원하시면 '아니요'를 선택해 주세요.");
            switch (res) {
            case true: {
                foreach (ImageRecord i in Dataset.Images) {
                    i.Annotations.RemoveAll(s => s.Category == SelectedCategory);
                }
                if (Dataset.Categories.Count <= 2) {
                    Dataset.Categories.Clear();
                    SelectedCategory = null;
                } else {
                    Dataset.Categories.Remove(SelectedCategory);
                    SelectedCategory = Dataset.Categories.First(s => s.All);
                }
                RefreshColorOfCategories();
                break;
            }
            case false: {
                SortedSet<ImageRecord> ImagesFromDeletedClass = [];
                foreach (ImageRecord i in Dataset.Images) {
                    int deletedCount = i.Annotations.RemoveAll(s => s.Category == SelectedCategory);
                    if (deletedCount > 0) ImagesFromDeletedClass.Add(i);
                }
                Dataset.Images.RemoveAll(s => s.Annotations.Count == 0 && ImagesFromDeletedClass.Contains(s));
                if (Dataset.Categories.Count <= 2) {
                    Dataset.Categories.Clear();
                    SelectedCategory = null;
                } else {
                    Dataset.Categories.Remove(SelectedCategory);
                    SelectedCategory = Dataset.Categories.First(s => s.All);
                }
                RefreshColorOfCategories();
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
            if (Dataset.Images.Count == 0) return;
            int target = SelectedImage is null ? Dataset.Images.Count - 1 : Math.Max(0, Dataset.Images.IndexOf(SelectedImage) - 1);
            SelectedImage = Dataset.Images[target];
            EventAggregator.GetEvent<ScrollViewImagesList>().Publish(Dataset.Images[target]);
        }
        public ICommand CmdImageDown { get; }
        private void ImageDown() {
            if (Dataset.Images.Count == 0) return;
            int target = SelectedImage is null ? 0 : Math.Min(Dataset.Images.Count - 1, Dataset.Images.IndexOf(SelectedImage) + 1);
            SelectedImage = Dataset.Images[target];
            EventAggregator.GetEvent<ScrollViewImagesList>().Publish(Dataset.Images[target]);
        }
        public ICommand CmdRefreshImagesList { get; }
        private void RefreshImagesList() {
            InternalRefreshImagesList();
        }
        public ICommand CmdDeleteNegativeImage { get; }
        private void DeleteNegativeImage() {
            bool res = CommonDialogService.MessageBoxOKCancel("현재 데이터셋에 포함된 모든 음성 이미지를 디스크에서 삭제합니다. 이 작업은 되돌릴 수 없습니다.");
            if (!res) return;
            foreach (ImageRecord i in Dataset.Images.Where(s => s.Annotations.Count == 0)) {
                string imageFullPath = Path.Combine(Dataset.BasePath, i.Path);
                File.Delete(imageFullPath);
            }
            InternalRefreshImagesList();
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
            IEnumerable<AnnotationRecord> anns = SelectedCategory.All ? SelectedImage.Annotations : SelectedImage.Annotations.Where(s => s.Category == SelectedCategory);
            VisibleAnnotations.AddRange(anns);
        }
        private async Task InternalLoadDataset(string filePath) {
            if (!SerializationService.IsJsonPathValid(filePath)) {
                CommonDialogService.MessageBox("데이터셋 파일을 읽어올 수 없습니다. 파일명이 instances_*.json이며 드라이브 최상위 폴더 이외의 장소에 있어야 합니다.");
                return;
            }
            Dataset = await SerializationService.DeserializeAsync(filePath).ConfigureAwait(false);
            if (Dataset.Categories.Count > 0) {
                Dataset.Categories.Insert(0, CategoryRecord.AsAll());
                SelectedCategory = Dataset.Categories[0];
            }
            if (Dataset.Images.Count > 0) SelectedImage = Dataset.Images[0];
            Title = $"COCO 데이터셋 편집기 - {filePath}";
            RefreshColorOfCategories();
        }
        private void RefreshColorOfCategories() {
            switch (SettingService.Color) {
            case SettingColors.Fixed:
                IEnumerable<Color> colors = Miscellaneous.GenerateFixedColor(Dataset.Categories.Count(s => !s.All));
                foreach ((CategoryRecord category, Color color) in Enumerable.Zip(Dataset.Categories.Where(s => !s.All), colors)) {
                    SolidColorBrush brush = new(color);
                    brush.Freeze();
                    category.ColorBrush = brush;
                }
                break;
            case SettingColors.Random:
                double ColorDifferenceThreshold = 250;
                IEnumerable<Color> ExistingColors = Dataset.Categories.Select(s => s.ColorBrush.Color).Distinct().Append(Colors.White);
                foreach (CategoryRecord category in Dataset.Categories.Where(s => !s.All && s.ColorBrush.Color == Colors.Transparent)) {
                    while (true) {
                        Color? color = Miscellaneous.GenerateRandomColor(ExistingColors, ColorDifferenceThreshold);
                        if (color is not null) {
                            SolidColorBrush brush = new(color.Value);
                            brush.Freeze();
                            category.ColorBrush = brush;
                            break;
                        }
                        ColorDifferenceThreshold /= 1.2;
                    }
                }
                break;
            }
        }
        public void InternalRefreshImagesList() {
            ISet<string> ApprovedImageExtensions = SettingService.SetSupportedFormats;
            SortedSet<ImageRecord> currentImagesInFolder = new(
                Directory.EnumerateFiles(Dataset.BasePath, "*.*", SearchOption.AllDirectories).Where(s => ApprovedImageExtensions.Contains(Path.GetExtension(s)))
                    .Select(s => new ImageRecord(Path.GetRelativePath(Dataset.BasePath, s).NormalizePath()))
            );
            int removedCount = Dataset.Images.RemoveAll(s => !currentImagesInFolder.Contains(s));
            currentImagesInFolder.ExceptWith(Dataset.Images);
            foreach (ImageRecord i in currentImagesInFolder) {
                try {
                    i.LoadSize(Dataset.BasePath);
                } catch (NotSupportedException) {
                    i.Width = 0;
                    i.Height = 0;
                }
            }
            Dataset.Images.AddRange(currentImagesInFolder);
            int addedCount = currentImagesInFolder.Count;
            if (removedCount > 0 && addedCount > 0) CommonDialogService.MessageBox($"{addedCount}개의 이미지가 새로 추가되고 {removedCount}개의 이미지가 제거되었습니다.");
            else if (removedCount > 0 && addedCount == 0) CommonDialogService.MessageBox($"{removedCount}개의 이미지가 제거되었습니다.");
            else if (removedCount == 0 && addedCount > 0) CommonDialogService.MessageBox($"{addedCount}개의 이미지가 새로 추가되었습니다.");
            else CommonDialogService.MessageBox("데이터셋의 이미지 목록이 이미 최신입니다.");
        }
        #endregion
    }
}
