using COCOAnnotator.Events;
using COCOAnnotator.Records;
using COCOAnnotator.Records.COCO;
using COCOAnnotator.Records.Enums;
using COCOAnnotator.Utilities;
using COCOAnnotator.ViewModels.Commons;
using Prism.Commands;
using Prism.Services.Dialogs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace COCOAnnotator.ViewModels {
    public class ManageDialogViewModel : DialogViewModelBase {
        #region 생성자
        public ManageDialogViewModel() {
            Title = "데이터셋 관리";

            _LogVerifyDataset = "";
            FilesForUnionDataset = new ObservableCollection<string>();
            _TacticForSplitDataset = TacticsForSplitDataset.DevideToN;
            _NValueForSplitDataset = 2;
            _LogUndupeDataset = "";
            _IoUThreshold = 0.5;
            _UndupeWithoutCategory = true;
            _TacticForConvertDataset = TacticsForConvertDataset.COCOToCSV;
            _CSVFormat = CSVFormat.LTRB;

            CmdVerifyDataset = new DelegateCommand(VerifyDataset);
            CmdDeleteUnusedImages = new DelegateCommand(DeleteUnusedImages);
            CmdExportVerifiedDataset = new DelegateCommand(ExportVerifiedDataset);
            CmdAddFileForUnionDataset = new DelegateCommand(AddFileForUnionDataset);
            CmdAddFolderForUnionDataset = new DelegateCommand(AddFolderForUnionDataset);
            CmdRemoveFileForUnionDataset = new DelegateCommand<IList>(RemoveFileForUnionDataset);
            CmdResetFileForUnionDataset = new DelegateCommand(ResetFileForUnionDataset);
            CmdExportUnionDataset = new DelegateCommand(ExportUnionDataset);
            CmdSplitDataset = new DelegateCommand(SplitDataset);
            CmdUndupeDataset = new DelegateCommand(UndupeDataset);
            CmdExportUndupedDataset = new DelegateCommand(ExportUndupedDataset);
            CmdConvertDataset = new DelegateCommand(ConvertDataset);
            CmdClose = new DelegateCommand(Close);
        }
        #endregion

        #region 필드, 바인딩되지 않는 프로퍼티
        private readonly SortedDictionary<CategoryRecord, int> AnnotationsCountByCategory = new SortedDictionary<CategoryRecord, int>();
        private readonly SortedDictionary<int, ImageRecord> ImagesForVerify = new SortedDictionary<int, ImageRecord>();
        private readonly SortedDictionary<int, CategoryRecord> CategoriesForVerify = new SortedDictionary<int, CategoryRecord>();
        private readonly SortedSet<ImageRecord> UnusedImagesForVerify = new SortedSet<ImageRecord>();
        private readonly List<ImageRecord> ImagesForUndupe = new List<ImageRecord>();
        private readonly List<CategoryRecord> CategoriesForUndupe = new List<CategoryRecord>();
        #endregion

        #region 바인딩되는 프로퍼티
        private string _LogVerifyDataset;
        public string LogVerifyDataset {
            get => _LogVerifyDataset;
            set {
                if (SetProperty(ref _LogVerifyDataset, value)) {
                    EventAggregator.GetEvent<ScrollTxtLogVerifyDataset>().Publish();
                }
            }
        }
        private int _ProgressVerifyDataset;
        public int ProgressVerifyDataset {
            get => _ProgressVerifyDataset;
            set => SetProperty(ref _ProgressVerifyDataset, value);
        }
        public ObservableCollection<string> FilesForUnionDataset { get; }
        private TacticsForSplitDataset _TacticForSplitDataset;
        public TacticsForSplitDataset TacticForSplitDataset {
            get => _TacticForSplitDataset;
            set => SetProperty(ref _TacticForSplitDataset, value);
        }
        private int _NValueForSplitDataset;
        public int NValueForSplitDataset {
            get => _NValueForSplitDataset;
            set => SetProperty(ref _NValueForSplitDataset, value);
        }
        private double _IoUThreshold;
        public double IoUThreshold {
            get => _IoUThreshold;
            set => SetProperty(ref _IoUThreshold, value);
        }
        private bool _UndupeWithoutCategory;
        public bool UndupeWithoutCategory {
            get => _UndupeWithoutCategory;
            set => SetProperty(ref _UndupeWithoutCategory, value);
        }
        private string _LogUndupeDataset;
        public string LogUndupeDataset {
            get => _LogUndupeDataset;
            set {
                if (SetProperty(ref _LogUndupeDataset, value)) {
                    EventAggregator.GetEvent<ScrollTxtLogUndupeLabel>().Publish();
                }
            }
        }
        private int _ProgressUndupeDatasetValue;
        public int ProgressUndupeDatasetValue {
            get => _ProgressUndupeDatasetValue;
            set => SetProperty(ref _ProgressUndupeDatasetValue, value);
        }
        private TacticsForConvertDataset _TacticForConvertDataset;
        public TacticsForConvertDataset TacticForConvertDataset {
            get => _TacticForConvertDataset;
            set => SetProperty(ref _TacticForConvertDataset, value);
        }
        private int _ProgressConvertDatasetValue;
        public int ProgressConvertDatasetValue {
            get => _ProgressConvertDatasetValue;
            set => SetProperty(ref _ProgressConvertDatasetValue, value);
        }
        private CSVFormat _CSVFormat;
        public CSVFormat CSVFormat {
            get => _CSVFormat;
            set => SetProperty(ref _CSVFormat, value);
        }
        #endregion

        #region 커맨드
        #region 데이터셋 분석
        public ICommand CmdVerifyDataset { get; }
        private void VerifyDataset() {
            if (!CommonDialogService.OpenJsonFileDialog(out string filePath)) return;
            bool? res = CommonDialogService.MessageBoxYesNoCancel("검증을 시작합니다. 이미지 크기 검사를 하기 원하시면 예 아니면 아니오를 선택하세요. 이미지 크기 검사시 데이터셋 크기에 따라 시간이 오래 걸릴 수 있습니다.");
            if (res is null) return;
            bool imageSizeCheck = res.Value;
            LogVerifyDataset = "";
            ProgressVerifyDataset = 0;
            Task.Run(() => {
                ImagesForVerify.Clear();
                CategoriesForVerify.Clear();
                AnnotationsCountByCategory.Clear();
                UnusedImagesForVerify.Clear();
                string basePath = Path.GetDirectoryName(filePath) ?? "";
                byte[] CocoContents = File.ReadAllBytes(filePath);
                COCODataset cocodataset = SerializationService.DeserializeAsRaw(CocoContents);
                AppendLogVerifyDataset($"\"{filePath}\"의 분석을 시작합니다.");
                int total = cocodataset.Images.Count + cocodataset.Annotations.Count + cocodataset.Categories.Count;
                {
                    SortedSet<int> DuplicatedIDAlreadyDetected = new SortedSet<int>();
                    SortedSet<ImageRecord> ImageRecords = new SortedSet<ImageRecord>();
                    SortedSet<ImageRecord> DuplicatedImageAlreadyDetected = new SortedSet<ImageRecord>();
                    foreach ((int idx, ImageCOCO image) in cocodataset.Images.Select((s, idx) => (idx, s))) {
                        if (IsClosed) return;
                        ProgressVerifyDataset = (int)((double)idx / total * 99);
                        string fullPath = Path.Combine(basePath, image.FileName);
                        if (ImagesForVerify.ContainsKey(image.ID)) {
                            if (DuplicatedIDAlreadyDetected.Add(image.ID)) AppendLogVerifyDataset($"ID가 {image.ID}인 이미지가 2개 이상 발견되었습니다.");
                            continue;
                        }
                        if (!File.Exists(fullPath)) {
                            AppendLogVerifyDataset($"ID가 {image.ID}인 이미지가 주어진 경로에 존재하지 않습니다.");
                            continue;
                        }
                        ImageRecord imageRecord = new ImageRecord(fullPath, image.Width, image.Height);
                        if (ImageRecords.Contains(imageRecord)) {
                            if (DuplicatedImageAlreadyDetected.Add(imageRecord)) AppendLogVerifyDataset($"다음 경로의 이미지가 2번 이상 사용되었습니다: {fullPath}");
                            continue;
                        }
                        if (imageSizeCheck) {
                            try {
                                (int width, int height) = Utils.GetSizeOfImage(fullPath);
                                if (image.Width != width || image.Height != height) {
                                    AppendLogVerifyDataset($"ID가 {image.ID}인 이미지의 크기가 실제 크기와 다릅니다.");
                                    continue;
                                }
                            } catch (NotSupportedException) {
                                AppendLogVerifyDataset($"ID가 {image.ID}인 이미지의 크기를 읽어올 수 없습니다.");
                                continue;
                            }
                        } else {
                            if (image.Width <= 0 || image.Height <= 0) {
                                AppendLogVerifyDataset($"ID가 {image.ID}인 이미지의 크기가 유효하지 않습니다.");
                                continue;
                            }
                        }
                        ImageRecords.Add(imageRecord);
                        ImagesForVerify.Add(image.ID, imageRecord);
                    }
                }
                {
                    SortedSet<int> DuplicationAlreadyDetected = new SortedSet<int>();
                    SortedSet<CategoryRecord> CategoryRecords = new SortedSet<CategoryRecord>();
                    SortedSet<CategoryRecord> DuplicatedCategoryAlreadyDetected = new SortedSet<CategoryRecord>();
                    foreach ((int idx, CategoryCOCO category) in cocodataset.Categories.Select((s, idx) => (idx, s))) {
                        if (IsClosed) return;
                        ProgressVerifyDataset = (int)((double)(cocodataset.Images.Count + idx) / total * 99);
                        if (CategoriesForVerify.ContainsKey(category.ID)) {
                            if (DuplicationAlreadyDetected.Add(category.ID)) AppendLogVerifyDataset($"ID가 {category.ID}인 분류가 2개 이상 발견되었습니다.");
                            continue;
                        }
                        CategoryRecord categoryRecord = CategoryRecord.FromName(category.Name);
                        if (CategoryRecords.Contains(categoryRecord)) {
                            if (DuplicatedCategoryAlreadyDetected.Add(categoryRecord)) AppendLogVerifyDataset($"같은 이름의 분류가 2번 이상 사용되었습니다: {category.Name}");
                            continue;
                        }
                        CategoriesForVerify.Add(category.ID, categoryRecord);
                    }
                }
                {
                    SortedSet<int> DuplicationAlreadyDetected = new SortedSet<int>();
                    SortedSet<int> AnnotationAlreadyProcessed = new SortedSet<int>();
                    foreach ((int idx, AnnotationCOCO annotation) in cocodataset.Annotations.Select((s, idx) => (idx, s))) {
                        ProgressVerifyDataset = (int)((double)(cocodataset.Images.Count + cocodataset.Categories.Count + idx) / total * 99);
                        if (AnnotationAlreadyProcessed.Contains(annotation.ID)) {
                            if (DuplicationAlreadyDetected.Add(annotation.ID)) AppendLogVerifyDataset($"ID가 {annotation.ID}인 어노테이션이 2개 이상 발견되었습니다.");
                            continue;
                        }
                        if (!CategoriesForVerify.TryGetValue(annotation.CategoryID, out CategoryRecord? category)) {
                            AppendLogVerifyDataset($"ID가 {annotation.ID}인 어노테이션이 존재하지 않는 분류 ID를 참조합니다..");
                            continue;
                        }
                        if (!ImagesForVerify.TryGetValue(annotation.ImageID, out ImageRecord? image)) {
                            AppendLogVerifyDataset($"ID가 {annotation.ID}인 어노테이션이 존재하지 않는 이미지 ID를 참조합니다.");
                            continue;
                        }
                        if (annotation.BoundaryBox.Count != 4) {
                            AppendLogVerifyDataset($"ID가 {annotation.ID}인 어노테이션의 좌표가 유효하지 않습니다. 좌표 개수는 4개여야 합니다.");
                            continue;
                        }
                        double left = annotation.BoundaryBox[0];
                        double top = annotation.BoundaryBox[1];
                        double width = annotation.BoundaryBox[2];
                        double height = annotation.BoundaryBox[3];
                        if (left < 0 || top < 0 || left + width > image.Width || top + height > image.Height) {
                            AppendLogVerifyDataset($"ID가 {annotation.ID}인 어노테이션의 좌표가 유효하지 않습니다. 좌표가 이미지의 크기 밖에 있습니다.");
                            continue;
                        }
                        if (width <= 0 || height <= 0) {
                            AppendLogVerifyDataset($"ID가 {annotation.ID}인 어노테이션의 좌표가 유효하지 않습니다. 너비 또는 높이는 0 이하일 수 없습니다.");
                            continue;
                        }
                        image.Annotations.Add(new AnnotationRecord(image, left, top, width, height, category));
                        if (!AnnotationsCountByCategory.ContainsKey(category)) AnnotationsCountByCategory[category] = 0;
                        AnnotationsCountByCategory[category]++;
                    }
                }

                // 사용되지 않은 이미지 검색
                if (ImagesForVerify.Count > 0) {
                    string CommonParentPath = Utils.GetCommonParentPath(ImagesForVerify.Values);
                    AppendLogVerifyDataset("", $"사용된 이미지의 공통 부모 경로는 \"{CommonParentPath}\"입니다.");
                    UnusedImagesForVerify.UnionWith(Directory.EnumerateFiles(CommonParentPath, "*.*", SearchOption.AllDirectories)
                        .Where(s => Utils.ApprovedImageExtensions.Contains(Path.GetExtension(s))).Select(s => new ImageRecord(s)));
                    UnusedImagesForVerify.ExceptWith(ImagesForVerify.Values);
                    if (UnusedImagesForVerify.Count > 20) {
                        AppendLogVerifyDataset($"경로내에 존재하지만 유효한 어노테이션에 사용되고 있지 않은 {UnusedImagesForVerify.Count}개의 이미지가 있습니다. 일부를 출력합니다.");
                        AppendLogVerifyDataset(UnusedImagesForVerify.Select(s => s.FullPath).Take(20).ToArray());
                        SortedSet<string> FoldersOfUnusedImages = new SortedSet<string>(UnusedImagesForVerify.Select(s => Path.GetDirectoryName(s.FullPath) ?? ""));
                        if (FoldersOfUnusedImages.Count > 10) {
                            AppendLogVerifyDataset($"위 이미지들이 존재하는 폴더는 {FoldersOfUnusedImages.Count}종이 존재합니다. 일부를 출력합니다.");
                            AppendLogVerifyDataset(FoldersOfUnusedImages.Take(10).ToArray());
                        } else {
                            AppendLogVerifyDataset($"위 이미지들이 존재하는 폴더는 {FoldersOfUnusedImages.Count}종이 존재합니다.");
                            AppendLogVerifyDataset(FoldersOfUnusedImages.ToArray());
                        }
                    } else if (UnusedImagesForVerify.Count >= 1) {
                        AppendLogVerifyDataset($"경로내에 존재하지만 유효한 어노테이션에 사용되고 있지 않은 {UnusedImagesForVerify.Count}개의 이미지가 있습니다.");
                        AppendLogVerifyDataset(UnusedImagesForVerify.Select(s => s.FullPath).ToArray());
                    }
                }
                AppendLogVerifyDataset(
                    "",
                    "분석이 완료되었습니다.",
                    $"어노테이션 개수: {ImagesForVerify.Values.Sum(s => s.Annotations.Count)}",
                    $"어노테이션이 있는 이미지 개수: {ImagesForVerify.Values.Count(s => s.Annotations.Count > 0)}",
                    $"음성 이미지 개수: {ImagesForVerify.Values.Count(s => s.Annotations.Count == 0)}",
                    $"총 이미지 개수: {ImagesForVerify.Count}",
                    ""
                );
                AppendLogVerifyDataset(AnnotationsCountByCategory.Select(s =>
                    $"분류 이름: {s.Key}, 어노테이션 개수: {s.Value}, 어노테이션이 있는 이미지 개수: {ImagesForVerify.Values.Count(t => t.Annotations.Any(u => u.Category == s.Key))}").ToArray());
                ProgressVerifyDataset = 100;
            });
        }
        public ICommand CmdDeleteUnusedImages { get; }
        private void DeleteUnusedImages() {
            if (UnusedImagesForVerify.Count == 0) {
                CommonDialogService.MessageBox("데이터셋 파일을 분석한 적 없거나 이미지 폴더 내에 데이터셋에 사용중이지 않은 이미지가 없습니다.");
                return;
            }
            bool res = CommonDialogService.MessageBoxOKCancel("이미지 폴더 내에 있지만 데이터셋에 사용중이지 않은 이미지를 디스크에서 삭제합니다. 이 작업은 되돌릴 수 없습니다.");
            if (!res) return;
            foreach (ImageRecord i in UnusedImagesForVerify) {
                File.Delete(i.FullPath);
            }
            UnusedImagesForVerify.Clear();
        }
        public ICommand CmdExportVerifiedDataset { get; }
        private void ExportVerifiedDataset() {
            if (ImagesForVerify.Count == 0 && CategoriesForVerify.Count == 0) {
                CommonDialogService.MessageBox("데이터셋을 분석한 적 없거나 분석한 데이터셋 내에 유효한 내용이 없습니다.");
                return;
            }
            if (!CommonDialogService.SaveCSVFileDialog(out string filePath)) return;
            bool res = CommonDialogService.MessageBoxOKCancel("분석한 데이터셋의 내용 중 유효한 내용만 내보냅니다.");
            if (!res) return;
            string basePath = Path.GetDirectoryName(filePath) ?? "";
            byte[] CocoContents = SerializationService.Serialize(basePath, ImagesForVerify.Values, CategoriesForVerify.Values);
            File.WriteAllBytes(filePath, CocoContents);
        }
        #endregion

        #region 데이터셋 병합
        public ICommand CmdAddFileForUnionDataset { get; }
        private void AddFileForUnionDataset() {
            if (CommonDialogService.OpenJsonFilesDialog(out string[] filePaths)) {
                foreach (string FileName in filePaths) {
                    FilesForUnionDataset.Add(FileName);
                }
            }
        }
        public ICommand CmdAddFolderForUnionDataset { get; }
        private void AddFolderForUnionDataset() {
            if (CommonDialogService.OpenFolderDialog(out string folderPath)) {
                foreach (string i in Directory.EnumerateFiles(folderPath, "*.json", SearchOption.AllDirectories)) FilesForUnionDataset.Add(i);
            }
        }
        public ICommand CmdRemoveFileForUnionDataset { get; }
        private void RemoveFileForUnionDataset(IList SelectedItems) {
            string[] remove = SelectedItems.OfType<string>().ToArray();
            foreach (string i in remove) {
                if (i is null) continue;
                FilesForUnionDataset.Remove(i);
            }
        }
        public ICommand CmdResetFileForUnionDataset { get; }
        private void ResetFileForUnionDataset() {
            FilesForUnionDataset.Clear();
        }
        public ICommand CmdExportUnionDataset { get; }
        private void ExportUnionDataset() {
            if (!CommonDialogService.SaveJsonFileDialog(out string outFilePath)) return;
            // 로드
            SortedSet<ImageRecord> AllImages = new SortedSet<ImageRecord>();
            SortedSet<CategoryRecord> AllCategories = new SortedSet<CategoryRecord>();
            foreach (string inFilePath in FilesForUnionDataset) {
                string inBasePath = Path.GetDirectoryName(inFilePath) ?? "";
                byte[] InCocoContents = File.ReadAllBytes(inFilePath);
                (ICollection<ImageRecord> images, ICollection<CategoryRecord> categories) = SerializationService.Deserialize(inBasePath, InCocoContents);
                AllCategories.UnionWith(categories);
                foreach (ImageRecord image in images) {
                    if (AllImages.TryGetValue(image, out ImageRecord? oldImage)) oldImage.Annotations.AddRange(image.Annotations);
                    else AllImages.Add(image);
                }
            }
            // 저장
            string outBasePath = Path.GetDirectoryName(outFilePath) ?? "";
            byte[] OutCocoContents = SerializationService.Serialize(outBasePath, AllImages, AllCategories);
            File.WriteAllBytes(outFilePath, OutCocoContents);
        }
        #endregion

        #region 데이터셋 분리
        public ICommand CmdSplitDataset { get; }
        private void SplitDataset() {
            if (!CommonDialogService.OpenJsonFileDialog(out string inFilePath)) return;
            string inBasePath = Path.GetDirectoryName(inFilePath) ?? "";
            byte[] InCocoContents = File.ReadAllBytes(inFilePath);
            (ICollection<ImageRecord> images, ICollection<CategoryRecord> categories) = SerializationService.Deserialize(inBasePath, InCocoContents);
            IEnumerable<ImageRecord> shuffledImages = images.Shuffle();
            switch (TacticForSplitDataset) {
            case TacticsForSplitDataset.DevideToN:
                // 균등 분할
                if (NValueForSplitDataset < 2 || NValueForSplitDataset > images.Count) {
                    CommonDialogService.MessageBox("입력한 숫자가 올바르지 않습니다.");
                    return;
                }
                var infoByPartition = new List<(SortedSet<ImageRecord> Images, SortedSet<CategoryRecord> Categories)>();
                for (int i = 0; i < NValueForSplitDataset; i++) {
                    infoByPartition.Add((new SortedSet<ImageRecord>(), new SortedSet<CategoryRecord>()));
                }
                foreach (ImageRecord image in shuffledImages) {
                    if (image.Annotations.Count > 0) {
                        // 양성 이미지인 경우.
                        // 분류 다양성이 증가하는 정도가 가장 높은 순 -> 파티션에 포함된 이미지 개수가 적은 순.
                        (SortedSet<ImageRecord> imagesByPartition, SortedSet<CategoryRecord> categoriesByPartition) = infoByPartition
                            .OrderByDescending(s => image.Annotations.Select(t => t.Category).Except(s.Categories).Count()).ThenBy(s => s.Images.Count).First();
                        imagesByPartition.Add(image);
                        categoriesByPartition.UnionWith(image.Annotations.Select(s => s.Category));
                    } else {
                        // 음성 이미지인 경우.
                        // 파티션에 포함된 이미지 개수가 적은 순으로만 선택.
                        (SortedSet<ImageRecord> imagesByPartition, _) = infoByPartition.OrderBy(s => s.Images.Count).First();
                        imagesByPartition.Add(image);
                    }
                }
                for (int i = 0; i < NValueForSplitDataset; i++) {
                    // 출력 파일 이름: (원래 파일 이름).(파티션 번호 1부터 시작).json
                    string outFilePath = Path.Combine(inBasePath, $"{Path.GetFileNameWithoutExtension(inFilePath)}.{i + 1}.json");
                    byte[] outCocoContents = SerializationService.Serialize(inBasePath, infoByPartition[i].Images, infoByPartition[i].Categories);
                    File.WriteAllBytes(outFilePath, outCocoContents);
                }
                break;
            case TacticsForSplitDataset.TakeNSamples: {
                // 일부 추출
                if (NValueForSplitDataset < 1 || NValueForSplitDataset >= images.Count) {
                    CommonDialogService.MessageBox("입력한 숫자가 올바르지 않습니다.");
                    return;
                }
                SortedSet<ImageRecord> OriginalImages = new SortedSet<ImageRecord>();
                SortedSet<CategoryRecord> OriginalCategories = new SortedSet<CategoryRecord>();
                SortedSet<ImageRecord> SplitImages = new SortedSet<ImageRecord>();
                SortedSet<CategoryRecord> SplitCategories = new SortedSet<CategoryRecord>();
                foreach (ImageRecord image in shuffledImages) {
                    int DiversityDeltaOriginal = image.Annotations.Select(s => s.Category).Except(OriginalCategories).Count();
                    int DiversityDeltaSplit = image.Annotations.Select(s => s.Category).Except(SplitCategories).Count();
                    if (OriginalImages.Count + NValueForSplitDataset + 1 >= images.Count || (SplitImages.Count < NValueForSplitDataset && DiversityDeltaSplit >= DiversityDeltaOriginal)) {
                        // 아래 두 경우 중 하나일시 해당 이미지를 추출 데이터셋에 씀
                        // 1. 남은 이미지 전부를 추출해야만 추출량 목표치를 채울 수 있는 경우
                        // 2. 아직 추출량 목표치가 남아 있으며, 분류 다양성이 증가하는 정도가 추출 데이터셋 쪽이 더 높거나 같은 경우
                        SplitImages.Add(image);
                        SplitCategories.UnionWith(image.Annotations.Select(s => s.Category));
                    } else {
                        OriginalImages.Add(image);
                        OriginalCategories.UnionWith(image.Annotations.Select(s => s.Category));
                    }
                }
                string outOriginalFilePath = Path.Combine(inBasePath, $"{Path.GetFileNameWithoutExtension(inFilePath)}.1.json");
                byte[] OriginalCocoContents = SerializationService.Serialize(inBasePath, OriginalImages, OriginalCategories);
                File.WriteAllBytes(outOriginalFilePath, OriginalCocoContents);
                string outSplitFilePath = Path.Combine(inBasePath, $"{Path.GetFileNameWithoutExtension(inFilePath)}.2.json");
                byte[] SplitCocoContents = SerializationService.Serialize(inBasePath, SplitImages, SplitCategories);
                File.WriteAllBytes(outSplitFilePath, SplitCocoContents);
                break;
            }
            case TacticsForSplitDataset.SplitToSubFolders:
                // 하위 폴더로 분할
                IEnumerable<IGrouping<string, ImageRecord>> imagesByDir = images.GroupBy(s => Path.GetDirectoryName(s.FullPath) ?? "");
                foreach (IGrouping<string, ImageRecord> imagesInDir in imagesByDir) {
                    // 파일 이름: (원래 파일 이름).(최종 폴더 이름).json
                    string outFilePath = Path.Combine(imagesInDir.Key, $"{Path.GetFileNameWithoutExtension(inFilePath)}.{Path.GetFileName(imagesInDir.Key)}.json");
                    IEnumerable<CategoryRecord> LocalCategories = imagesInDir.Select(s => s.Annotations.Select(s => s.Category)).SelectMany(s => s).Distinct();
                    byte[] OutCocoContents = SerializationService.Serialize(inBasePath, imagesInDir, LocalCategories);
                    File.WriteAllBytes(outFilePath, OutCocoContents);
                }
                break;
            }
        }
        #endregion

        #region 데이터셋 중복 제거
        public ICommand CmdUndupeDataset { get; }
        private void UndupeDataset() {
            if (!CommonDialogService.OpenJsonFileDialog(out string filePath)) return;
            LogUndupeDataset = "";
            ProgressUndupeDatasetValue = 0;
            ImagesForUndupe.Clear();
            CategoriesForUndupe.Clear();
            Task.Run(() => {
                AppendLogUndupeDataset($"{filePath}에서 위치, 크기가 유사한 중복 경계상자를 제거합니다.");
                // 로드
                string basePath = Path.GetDirectoryName(filePath) ?? "";
                byte[] CocoContents = File.ReadAllBytes(filePath);
                (ICollection<ImageRecord> images, ICollection<CategoryRecord> categories) = SerializationService.Deserialize(basePath, CocoContents);
                ImagesForUndupe.AddRange(images);
                CategoriesForUndupe.AddRange(categories);
                // 중복 제거
                int TotalSuppressedBoxesCount = 0;
                List<ImageRecord> SuppressedImages = new List<ImageRecord>();
                for (int i = 0; i < ImagesForUndupe.Count; i++) {
                    if (IsClosed) return;
                    ProgressUndupeDatasetValue = (int)((double)i / ImagesForUndupe.Count * 100);
                    List<AnnotationRecord> AnnotationsForUndupe = new List<AnnotationRecord>();
                    if (UndupeWithoutCategory) {
                        AnnotationsForUndupe.AddRange(SuppressAnnotations(ImagesForUndupe[i].Annotations));
                    } else {
                        foreach (IGrouping<CategoryRecord, AnnotationRecord> annotations in ImagesForUndupe[i].Annotations.GroupBy(s => s.Category))
                            AnnotationsForUndupe.AddRange(SuppressAnnotations(annotations));
                    }
                    foreach (AnnotationRecord j in AnnotationsForUndupe) ImagesForUndupe[i].Annotations.Remove(j);
                    TotalSuppressedBoxesCount += AnnotationsForUndupe.Count;
                    if (AnnotationsForUndupe.Count > 0) SuppressedImages.Add(ImagesForUndupe[i]);
                }
                ProgressUndupeDatasetValue = 100;
                if (TotalSuppressedBoxesCount == 0) {
                    AppendLogUndupeDataset("분석이 완료되었습니다. 중복된 경계 상자가 없습니다.");
                } else {
                    AppendLogUndupeDataset($"분석이 완료되었습니다. 중복된 경계 상자가 {SuppressedImages.Count}개의 이미지에서 {TotalSuppressedBoxesCount}개 검출되었습니다.");
                    SortedSet<string> UniqueImagePaths = new SortedSet<string>(SuppressedImages.Select(s => s.FullPath));
                    if (UniqueImagePaths.Count > 20) {
                        AppendLogUndupeDataset("중복된 경계 상자가 있었던 이미지의 일부를 출력합니다.");
                        AppendLogUndupeDataset(UniqueImagePaths.Take(20).ToArray());
                    } else {
                        AppendLogUndupeDataset("중복된 경계 상자가 있었던 이미지는 다음과 같습니다.");
                        AppendLogUndupeDataset(UniqueImagePaths.ToArray());
                    }
                }
            });
        }
        public ICommand CmdExportUndupedDataset { get; }
        private void ExportUndupedDataset() {
            if (ImagesForUndupe.Count == 0 && CategoriesForUndupe.Count == 0) {
                CommonDialogService.MessageBox("어노테이션 중복 제거를 실행한 적이 없습니다.");
                return;
            }
            if (!CommonDialogService.SaveJsonFileDialog(out string filePath)) return;
            string basePath = Path.GetDirectoryName(filePath) ?? "";
            byte[] CocoContents = SerializationService.Serialize(basePath, ImagesForUndupe, CategoriesForUndupe);
            File.WriteAllBytes(filePath, CocoContents);
        }
        #endregion

        #region 데이터셋 변환
        public ICommand CmdConvertDataset { get; }
        private void ConvertDataset() {
            switch (TacticForConvertDataset) {
            case TacticsForConvertDataset.COCOToCSV: {
                if (CommonDialogService.OpenJsonFileDialog(out string filePath)) {
                    string basePath = Path.GetDirectoryName(filePath) ?? "";
                    byte[] CocoContents = File.ReadAllBytes(filePath);
                    (ICollection<ImageRecord> images, _) = SerializationService.Deserialize(basePath, CocoContents);
                    string csvPath = Path.Combine(Path.GetDirectoryName(filePath) ?? "", Path.GetFileNameWithoutExtension(filePath) + ".csv");
                    using StreamWriter csv = File.CreateText(csvPath);
                    foreach (ImageRecord i in images) {
                        if (i.Annotations.Count > 0) {
                            // 양성 데이터셋
                            foreach (AnnotationRecord j in i.Annotations) csv.WriteLine(SerializationService.CSVSerializeAsPositive(basePath, j, CSVFormat));
                        } else {
                            // 음성 데이터셋
                            csv.WriteLine(SerializationService.CSVSerializeAsNegative(basePath, i));
                        }
                    }
                }
                break;
            }
            case TacticsForConvertDataset.CSVToCOCO: {
                if (CommonDialogService.OpenCSVFileDialog(out string filePath)) {
                    Task.Run(() => {
                        ProgressConvertDatasetValue = 0;
                        string basePath = Path.GetDirectoryName(filePath) ?? "";
                        string[] lines = File.ReadAllLines(filePath);
                        SortedSet<ImageRecord> images = new SortedSet<ImageRecord>();
                        SortedSet<CategoryRecord> categories = new SortedSet<CategoryRecord>();
                        for (int i = 0; i < lines.Length; i++) {
                            if (IsClosed) return;
                            ProgressConvertDatasetValue = (int)((double)i / lines.Length * 100);
                            (ImageRecord? img, AnnotationRecord? lbl) = SerializationService.CSVDeserialize(basePath, lines[i], CSVFormat);
                            if (img is object) {
                                if (images.TryGetValue(img, out var realImage)) {
                                    img = realImage;
                                } else {
                                    (img.Width, img.Height) = Utils.GetSizeOfImage(img.FullPath);
                                    images.Add(img);
                                }
                                if (lbl is object) {
                                    img.Annotations.Add(lbl);
                                    if (categories.TryGetValue(lbl.Category, out CategoryRecord? found)) lbl.Category = found;
                                    else categories.Add(lbl.Category);
                                }
                            }
                        }
                        string cocoPath = Path.Combine(Path.GetDirectoryName(filePath) ?? "", Path.GetFileNameWithoutExtension(filePath) + ".json");
                        byte[] CocoContents = SerializationService.Serialize(basePath, images, categories);
                        File.WriteAllBytes(cocoPath, CocoContents);
                        ProgressConvertDatasetValue = 100;
                    });
                }
                break;
            }
            }
        }
        #endregion

        public ICommand CmdClose { get; }
        private void Close() {
            RaiseRequestClose(new DialogResult());
        }
        #endregion

        #region 프라이빗 메서드
        private void AppendLogVerifyDataset(params string[] logs) {
            LogVerifyDataset = LogVerifyDataset + string.Join(Environment.NewLine, logs) + Environment.NewLine;
        }
        private void AppendLogUndupeDataset(params string[] logs) {
            LogUndupeDataset = LogUndupeDataset + string.Join(Environment.NewLine, logs) + Environment.NewLine;
        }
        private IEnumerable<AnnotationRecord> SuppressAnnotations(IEnumerable<AnnotationRecord> Annotations) {
            List<AnnotationRecord> sortedBySize = Annotations.ToList(); // 넓이가 작은 경계 상자를 우선
            sortedBySize.Sort((a, b) => a.Area.CompareTo(b.Area));
            while (sortedBySize.Count >= 2) {
                // pick
                AnnotationRecord pick = sortedBySize[0];
                sortedBySize.RemoveAt(0);
                // compare
                List<AnnotationRecord> labelsToSuppress = new List<AnnotationRecord>();
                foreach (AnnotationRecord i in sortedBySize) {
                    double left = Math.Max(pick.Left, i.Left);
                    double top = Math.Max(pick.Top, i.Top);
                    double right = Math.Min(pick.Left + pick.Width, i.Left + i.Width);
                    double bottom = Math.Min(pick.Top + pick.Height, i.Top + i.Height);
                    if (left >= right || top >= bottom) continue;
                    double sizeIntersection = (right - left) * (bottom - top);
                    double sizeUnion = pick.Area + i.Area - sizeIntersection;
                    double iou = sizeIntersection / sizeUnion;
                    if (iou < IoUThreshold) continue;
                    labelsToSuppress.Add(i);
                }
                // suppress
                foreach (AnnotationRecord i in labelsToSuppress) {
                    sortedBySize.Remove(i);
                }
                yield return pick;
            }
        }
        #endregion
    }
}
