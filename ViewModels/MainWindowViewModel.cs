using LabelAnnotator.Events;
using LabelAnnotator.Records;
using LabelAnnotator.Utilities;
using LabelAnnotator.ViewModels.Commons;
using LabelAnnotator.Views;
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

namespace LabelAnnotator.ViewModels {
    public class MainWindowViewModel : ViewModelBase {
        #region 생성자
        public MainWindowViewModel() {
            Title = "CSV 데이터셋 편집기";

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
            Images = new ObservableCollection<ImageRecord>();
            Categories = new ObservableCollection<ClassRecord>();
            VisibleLabels = new ObservableCollection<LabelRecordWithIndex>();

            CmdViewportDrop = new DelegateCommand<DragEventArgs>(ViewportDrop);
            CmdLoadLabel = new DelegateCommand(LoadLabel);
            CmdSaveLabel = new DelegateCommand(SaveLabel);
            CmdCloseLabel = new DelegateCommand(CloseLabel);
            CmdManageLabel = new DelegateCommand(ManageLabel);
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

        #region 필드, 바인딩되지 않는 프로퍼티
        private readonly List<LabelRecord> Labels = new List<LabelRecord>();
        #endregion

        #region 바인딩되는 프로퍼티
        public ObservableCollection<ImageRecord> Images { get; }
        private ImageRecord? _SelectedImage;
        public ImageRecord? SelectedImage {
            get => _SelectedImage;
            set {
                if (SetProperty(ref _SelectedImage, value)) {
                    VisibleLabels.Clear();
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
        public ObservableCollection<ClassRecord> Categories { get; }
        private ClassRecord? _SelectedCategory;
        public ClassRecord? SelectedCategory {
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
        public ObservableCollection<LabelRecordWithIndex> VisibleLabels { get; }

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
                InternalLoadLabel(files[0]);
            }
        }
        public ICommand CmdLoadLabel { get; }
        private void LoadLabel() {
            if (CommonDialogService.OpenCSVFileDialog(out string filePath)) {
                InternalLoadLabel(filePath);
            }
        }
        public ICommand CmdSaveLabel { get; }
        private void SaveLabel() {
            if (CommonDialogService.SaveCSVFileDialog(out string filePath)) {
                string basePath = Path.GetDirectoryName(filePath) ?? "";
                using StreamWriter f = File.CreateText(filePath);
                ILookup<ImageRecord, LabelRecord> labelsByImage = Labels.ToLookup(s => s.Image);
                foreach (ImageRecord i in Images) {
                    IEnumerable<LabelRecord> labelsInImage = labelsByImage[i];
                    if (labelsInImage.Any()) {
                        // 양성 레이블
                        foreach (LabelRecord j in labelsInImage) f.WriteLine(SerializationService.SerializeAsPositive(basePath, j, SettingService.Format));
                    } else {
                        // 음성 레이블
                        f.WriteLine(SerializationService.SerializeAsNegative(basePath, i));
                    }
                }
                Title = $"CSV 데이터셋 편집기 - {filePath}";
            }
        }
        public ICommand CmdCloseLabel { get; }
        private void CloseLabel() {
            bool res = CommonDialogService.MessageBoxOKCancel("현재 열려있는 레이블을 모두 초기화합니다");
            if (!res) return;
            Labels.Clear();
            Images.Clear();
            Categories.Clear();
            Title = "CSV 데이터셋 편집기";
        }
        public ICommand CmdManageLabel { get; }
        private void ManageLabel() {
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

            foreach (LabelRecordWithoutImage i in e.Added) {
                Labels.Add(i.WithImage(SelectedImage));
            }
            foreach (LabelRecordWithIndex i in e.Changed) {
                Labels[i.Index].Left = i.Label.Left;
                Labels[i.Index].Top = i.Label.Top;
                Labels[i.Index].Right = i.Label.Right;
                Labels[i.Index].Bottom = i.Label.Bottom;
            }
            List<LabelRecord> delete = new List<LabelRecord>();
            foreach (LabelRecordWithIndex i in e.Deleted) {
                delete.Add(Labels[i.Index]);
            }
            foreach (LabelRecord i in delete) {
                Labels.Remove(i);
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
            if (SelectedCategory is null) {
                int total = Categories.Count;
                if (total > 0) SelectedCategory = Categories[total - 1];
                return;
            }
            int current = Categories.IndexOf(SelectedCategory);
            int target = Math.Max(0, current - 1);
            SelectedCategory = Categories[target];
            EventAggregator.GetEvent<ScrollViewCategoriesList>().Publish(Categories[target]);
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
            EventAggregator.GetEvent<ScrollViewCategoriesList>().Publish(Categories[target]);
        }
        public ICommand CmdAddCategory { get; }
        private void AddCategory() {
            if (string.IsNullOrEmpty(CategoryNameToAdd)) return;
            ClassRecord add = ClassRecord.FromName(CategoryNameToAdd);
            if (Categories.Contains(add)) return;
            if (Categories.Count == 0) Categories.Add(ClassRecord.AllLabel());
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
            ClassRecord? rename = Categories.FirstOrDefault(s => s.Name == CategoryNameToAdd);
            if (rename is null) {
                bool res = CommonDialogService.MessageBoxOKCancel($"분류가 {SelectedCategory}인 모든 경계 상자를 {CategoryNameToAdd}으로 변경합니다.");
                if (!res) return;
                rename = ClassRecord.FromName(CategoryNameToAdd);
                ClassRecord OldCategory = SelectedCategory;
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
                foreach (LabelRecord label in Labels.Where(s => s.Class == OldCategory)) {
                    label.Class = rename;
                }
                SelectedCategory = rename;
            } else {
                bool res = CommonDialogService.MessageBoxOKCancel($"분류가 {SelectedCategory}인 모든 경계 상자를 다른 분류 {CategoryNameToAdd}로 병합합니다.");
                if (!res) return;
                ClassRecord OldCategory = SelectedCategory;
                Categories.Remove(OldCategory);
                foreach (LabelRecord label in Labels.Where(s => s.Class == OldCategory)) {
                    label.Class = rename;
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
                Labels.RemoveAll(s => s.Class == SelectedCategory);
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
                SortedSet<ImageRecord> ImagesFromDeletedClass = new SortedSet<ImageRecord>(Labels.Where(s => s.Class == SelectedCategory).Select(s => s.Image));
                Labels.RemoveAll(s => s.Class == SelectedCategory);
                SortedSet<ImageRecord> ImagesAfterDelete = new SortedSet<ImageRecord>(Labels.Select(s => s.Image));
                for (int i = 0; i < Images.Count; i++) {
                    if (ImagesFromDeletedClass.Contains(Images[i]) && !ImagesAfterDelete.Contains(Images[i])) {
                        Images.RemoveAt(i);
                        i--;
                    }
                }
                if (Categories.Count <= 2) {
                    Categories.Clear();
                    SelectedCategory = null;
                } else {
                    Categories.Remove(SelectedCategory);
                    SelectedCategory = Categories.First(s => s.All);
                }
                if (SelectedImage is null && Images.Count >= 1) {
                    SelectedImage = Images[0];
                    EventAggregator.GetEvent<ScrollViewImagesList>().Publish(SelectedImage);
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
            if (Images.Count == 0) return;
            int target = SelectedImage is null ? Images.Count - 1 : Math.Max(0, Images.IndexOf(SelectedImage) - 1);
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift && SelectedCategory is object && !SelectedCategory.All) {
                while (!Labels.Select(s => s.Image == Images[target] && s.Class == SelectedCategory).Any() && target > 0) {
                    target--;
                }
            }
            SelectedImage = Images[target];
            EventAggregator.GetEvent<ScrollViewImagesList>().Publish(Images[target]);
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
                if (SelectedImages.Count == 0) return;
                Labels.RemoveAll(s => SelectedImages.Contains(s.Image));
                VisibleLabels.Clear();
                break;
            }
            case false: {
                SortedSet<ImageRecord> SelectedImages = new SortedSet<ImageRecord>(SelectedItems.OfType<ImageRecord>());
                if (SelectedImages.Count == 0) return;
                if (SelectedImages.Count == Images.Count) {
                    Labels.Clear();
                    Images.Clear();
                    return;
                }
                Labels.RemoveAll(s => SelectedImages.Contains(s.Image));
                int DeletedMinIndex = -1;
                for (int i = 0; i < Images.Count; i++) {
                    if (SelectedImages.Contains(Images[i])) {
                        if (DeletedMinIndex < 0) DeletedMinIndex = i;
                        Images.RemoveAt(i);
                        i--;
                    }
                }
                if (Images.Count > 0) {
                    SelectedImage = Images[Math.Min(DeletedMinIndex, Images.Count - 1)];
                    EventAggregator.GetEvent<ScrollViewImagesList>().Publish(SelectedImage);
                }
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
            SortedSet<ImageRecord> NegativeImages = new SortedSet<ImageRecord>(Images);
            NegativeImages.ExceptWith(Labels.Select(s => s.Image));
            if (NegativeImages.Count == 0) return;
            int DeletedSelectedImageIndex = -1;
            for (int i = 0; i < Images.Count; i++) {
                if (NegativeImages.Contains(Images[i])) {
                    if (SelectedImage is object && Images[i] == SelectedImage) DeletedSelectedImageIndex = i;
                    Images.RemoveAt(i);
                    i--;
                }
            }
            if (DeletedSelectedImageIndex >= 0 && Images.Count > 0) {
                SelectedImage = Images[Math.Min(DeletedSelectedImageIndex, Images.Count - 1)];
                EventAggregator.GetEvent<ScrollViewImagesList>().Publish(SelectedImage);
            }
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
            VisibleLabels.Clear();
            IEnumerable<LabelRecordWithIndex> visibleLabels;
            if (SelectedCategory.All) visibleLabels = Labels.Select((s, idx) => new LabelRecordWithIndex(idx, s)).Where(s => s.Label.Image == SelectedImage);
            else visibleLabels = Labels.Select((s, idx) => new LabelRecordWithIndex(idx, s)).Where(s => s.Label.Image == SelectedImage && s.Label.Class == SelectedCategory);
            foreach (LabelRecordWithIndex i in visibleLabels) VisibleLabels.Add(i);
        }
        private void RefreshCommonPath() {
            string CommonPath = Utils.GetCommonParentPath(Images);
            foreach (ImageRecord i in Images) {
                i.DisplayFilename = Utils.GetRelativePath(CommonPath, i.FullPath);
            }
        }
        private void InternalLoadLabel(string filePath) {
            if (!Path.GetExtension(filePath).Equals(".csv", StringComparison.OrdinalIgnoreCase)) return;
            Labels.Clear();
            Images.Clear();
            Categories.Clear();
            string basePath = Path.GetDirectoryName(filePath) ?? "";
            IEnumerable<string> lines = File.ReadLines(filePath);
            SortedSet<ImageRecord> images = new SortedSet<ImageRecord>();
            SortedSet<ClassRecord> categories = new SortedSet<ClassRecord> { ClassRecord.AllLabel() };
            foreach (string line in lines) {
                (ImageRecord? img, LabelRecord? lbl) = SerializationService.Deserialize(basePath, line, SettingService.Format);
                if (img is object) {
                    images.Add(img);
                    if (lbl is object) {
                        Labels.Add(lbl);
                        if (categories.TryGetValue(lbl.Class, out ClassRecord? found)) lbl.Class = found;
                        else categories.Add(lbl.Class);
                    }
                }
            }
            foreach (ImageRecord i in images) Images.Add(i);
            RefreshCommonPath();
            foreach (ClassRecord classname in categories) Categories.Add(classname);
            RefreshColorOfCategories();
            if (Images.Count > 0) SelectedImage = Images[0];
            SelectedCategory = Categories[0];
            Title = $"CSV 데이터셋 편집기 - {filePath}";
        }
        private void RefreshColorOfCategories() {
            // 클래스 중에 제일 앞에 있는 하나는 (전체) 이므로 빼고 진행.
            List<Color> colors = Utils.GenerateColor(Categories.Count - 1).ToList();
            for (int i = 1; i < Categories.Count; i++) Categories[i].ColorBrush = new SolidColorBrush(colors[i - 1]);
        }
        private void InternelAddImage(string[] filePaths) {
            SortedSet<ImageRecord> add = new SortedSet<ImageRecord>(filePaths.Where(s => Utils.ApprovedImageExtensions.Contains(Path.GetExtension(s))).Select(s => new ImageRecord(s)));
            int ImagesCountToAdd = add.Count;
            add.ExceptWith(Images);
            foreach (ImageRecord img in add) {
                Images.Add(img);
            }
            int ImagesCountAdded = add.Count;
            if (ImagesCountToAdd != ImagesCountAdded) CommonDialogService.MessageBox("선택한 이미지 중 일부가 이미 데이터셋에 포함되어 있습니다. 해당 이미지를 무시했습니다.");
            if (ImagesCountAdded > 0) RefreshCommonPath();
        }
        #endregion
    }
}
