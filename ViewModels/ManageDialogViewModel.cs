using LabelAnnotator.Events;
using LabelAnnotator.Records;
using LabelAnnotator.Records.COCO;
using LabelAnnotator.Records.Enums;
using LabelAnnotator.Utilities;
using LabelAnnotator.ViewModels.Commons;
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

namespace LabelAnnotator.ViewModels {
    public class ManageDialogViewModel : DialogViewModelBase {
        #region 생성자
        public ManageDialogViewModel() {
            Title = "데이터셋 관리";

            _LogVerifyDataset = "";
            FilesForUnionDataset = new ObservableCollection<string>();
            _TacticForSplitLabel = TacticsForSplitLabel.DevideToNLabels;
            _TacticForConvertLabel = TacticsForConvertLabel.COCOToCSV;
            _NValueForSplitLabel = 2;
            _LogUndupeLabel = "";
            _IoUThreshold = 0.5;
            _UndupeWithoutClass = true;

            CmdVerifyLabel = new DelegateCommand(VerifyLabel);
            CmdDeleteUnusedImages = new DelegateCommand(DeleteUnusedImages);
            CmdExportVerifiedLabel = new DelegateCommand(ExportVerifiedLabel);
            CmdAddFileForUnionDataset = new DelegateCommand(AddFileForUnionDataset);
            CmdAddFolderForUnionDataset = new DelegateCommand(AddFolderForUnionDataset);
            CmdRemoveFileForUnionDataset = new DelegateCommand<IList>(RemoveFileForUnionDataset);
            CmdResetFileForUnionDataset = new DelegateCommand(ResetFileForUnionDataset);
            CmdExportUnionDataset = new DelegateCommand(ExportUnionDataset);
            CmdSplitLabel = new DelegateCommand(SplitLabel);
            CmdUndupeLabel = new DelegateCommand(UndupeLabel);
            CmdExportUndupedLabel = new DelegateCommand(ExportUndupeLabel);
            CmdConvertLabel = new DelegateCommand(ConvertLabel);
            CmdClose = new DelegateCommand(Close);
        }
        #endregion

        #region 필드, 바인딩되지 않는 프로퍼티
        private readonly SortedDictionary<CategoryRecord, int> AnnotationsCountByCategory = new SortedDictionary<CategoryRecord, int>();
        private readonly SortedDictionary<int, ImageRecord> ImagesForVerify = new SortedDictionary<int, ImageRecord>();
        private readonly SortedDictionary<int, CategoryRecord> CategoriesForVerify = new SortedDictionary<int, CategoryRecord>();
        private readonly SortedSet<ImageRecord> UnusedImagesForVerify = new SortedSet<ImageRecord>();
        private readonly List<AnnotationRecord> LabelsForUndupe = new List<AnnotationRecord>();
        private readonly SortedSet<ImageRecord> ImagesForUndupe = new SortedSet<ImageRecord>();
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
        private string? _SelectedFileForUnionLabel;
        public string? SelectedFileForUnionLabel {
            get => _SelectedFileForUnionLabel;
            set => SetProperty(ref _SelectedFileForUnionLabel, value);
        }
        private TacticsForSplitLabel _TacticForSplitLabel;
        public TacticsForSplitLabel TacticForSplitLabel {
            get => _TacticForSplitLabel;
            set => SetProperty(ref _TacticForSplitLabel, value);
        }
        private int _NValueForSplitLabel;
        public int NValueForSplitLabel {
            get => _NValueForSplitLabel;
            set => SetProperty(ref _NValueForSplitLabel, value);
        }
        private double _IoUThreshold;
        public double IoUThreshold {
            get => _IoUThreshold;
            set => SetProperty(ref _IoUThreshold, value);
        }
        private bool _UndupeWithoutClass;
        public bool UndupeWithoutClass {
            get => _UndupeWithoutClass;
            set => SetProperty(ref _UndupeWithoutClass, value);
        }
        private string _LogUndupeLabel;
        public string LogUndupeLabel {
            get => _LogUndupeLabel;
            set {
                if (SetProperty(ref _LogUndupeLabel, value)) {
                    EventAggregator.GetEvent<ScrollTxtLogUndupeLabel>().Publish();
                }
            }
        }
        private int _ProgressUndupeLabelValue;
        public int ProgressUndupeLabelValue {
            get => _ProgressUndupeLabelValue;
            set => SetProperty(ref _ProgressUndupeLabelValue, value);
        }
        private TacticsForConvertLabel _TacticForConvertLabel;
        public TacticsForConvertLabel TacticForConvertLabel {
            get => _TacticForConvertLabel;
            set => SetProperty(ref _TacticForConvertLabel, value);
        }
        private int _ProgressConvertLabelValue;
        public int ProgressConvertLabelValue {
            get => _ProgressConvertLabelValue;
            set => SetProperty(ref _ProgressConvertLabelValue, value);
        }
        #endregion

        #region 커맨드
        #region 데이터셋 분석
        public ICommand CmdVerifyLabel { get; }
        private void VerifyLabel() {
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
                AppendLogVerifyLabel($"\"{filePath}\"의 분석을 시작합니다.");
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
                            if (DuplicatedIDAlreadyDetected.Add(image.ID)) AppendLogVerifyLabel($"ID가 {image.ID}인 이미지가 2개 이상 발견되었습니다.");
                            continue;
                        }
                        if (!File.Exists(fullPath)) {
                            AppendLogVerifyLabel($"ID가 {image.ID}인 이미지가 주어진 경로에 존재하지 않습니다.");
                            continue;
                        }
                        ImageRecord imageRecord = new ImageRecord(fullPath, image.Width, image.Height);
                        if (ImageRecords.Contains(imageRecord)) {
                            if (DuplicatedImageAlreadyDetected.Add(imageRecord)) AppendLogVerifyLabel($"다음 경로의 이미지가 2번 이상 사용되었습니다: {fullPath}");
                            continue;
                        }
                        if (imageSizeCheck) {
                            try {
                                (int width, int height) = Utils.GetSizeOfImage(fullPath);
                                if (image.Width != width || image.Height != height) {
                                    AppendLogVerifyLabel($"ID가 {image.ID}인 이미지의 크기가 실제 크기와 다릅니다.");
                                    continue;
                                }
                            } catch (NotSupportedException) {
                                AppendLogVerifyLabel($"ID가 {image.ID}인 이미지의 크기를 읽어올 수 없습니다.");
                                continue;
                            }
                        } else {
                            if (image.Width <= 0 || image.Height <= 0) {
                                AppendLogVerifyLabel($"ID가 {image.ID}인 이미지의 크기가 유효하지 않습니다.");
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
                            if (DuplicationAlreadyDetected.Add(category.ID)) AppendLogVerifyLabel($"ID가 {category.ID}인 분류가 2개 이상 발견되었습니다.");
                            continue;
                        }
                        CategoryRecord categoryRecord = CategoryRecord.FromName(category.Name);
                        if (CategoryRecords.Contains(categoryRecord)) {
                            if (DuplicatedCategoryAlreadyDetected.Add(categoryRecord)) AppendLogVerifyLabel($"같은 이름의 분류가 2번 이상 사용되었습니다: {category.Name}");
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
                            if (DuplicationAlreadyDetected.Add(annotation.ID)) AppendLogVerifyLabel($"ID가 {annotation.ID}인 어노테이션이 2개 이상 발견되었습니다.");
                            continue;
                        }
                        if (!CategoriesForVerify.TryGetValue(annotation.CategoryID, out CategoryRecord? category)) {
                            AppendLogVerifyLabel($"ID가 {annotation.ID}인 어노테이션이 존재하지 않는 분류 ID를 참조합니다..");
                            continue;
                        }
                        if (!ImagesForVerify.TryGetValue(annotation.ImageID, out ImageRecord? image)) {
                            AppendLogVerifyLabel($"ID가 {annotation.ID}인 어노테이션이 존재하지 않는 이미지 ID를 참조합니다.");
                            continue;
                        }
                        if (annotation.BoundaryBox.Count != 4) {
                            AppendLogVerifyLabel($"ID가 {annotation.ID}인 어노테이션의 좌표가 유효하지 않습니다. 좌표 개수는 4개여야 합니다.");
                            continue;
                        }
                        double left = annotation.BoundaryBox[0];
                        double top = annotation.BoundaryBox[1];
                        double width = annotation.BoundaryBox[2];
                        double height = annotation.BoundaryBox[3];
                        if (left < 0 || top < 0 || left + width > image.Width || top + height > image.Height) {
                            AppendLogVerifyLabel($"ID가 {annotation.ID}인 어노테이션의 좌표가 유효하지 않습니다. 좌표가 이미지의 크기 밖에 있습니다.");
                            continue;
                        }
                        if (width <= 0 || height <= 0) {
                            AppendLogVerifyLabel($"ID가 {annotation.ID}인 어노테이션의 좌표가 유효하지 않습니다. 너비 또는 높이는 0 이하일 수 없습니다.");
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
                    AppendLogVerifyLabel("", $"사용된 이미지의 공통 부모 경로는 \"{CommonParentPath}\"입니다.");
                    UnusedImagesForVerify.UnionWith(Directory.EnumerateFiles(CommonParentPath, "*.*", SearchOption.AllDirectories)
                        .Where(s => Utils.ApprovedImageExtensions.Contains(Path.GetExtension(s))).Select(s => new ImageRecord(s)));
                    UnusedImagesForVerify.ExceptWith(ImagesForVerify.Values);
                    if (UnusedImagesForVerify.Count > 20) {
                        AppendLogVerifyLabel($"경로내에 존재하지만 유효한 어노테이션에 사용되고 있지 않은 {UnusedImagesForVerify.Count}개의 이미지가 있습니다. 일부를 출력합니다.");
                        AppendLogVerifyLabel(UnusedImagesForVerify.Select(s => s.FullPath).Take(20).ToArray());
                        SortedSet<string> FoldersOfUnusedImages = new SortedSet<string>(UnusedImagesForVerify.Select(s => Path.GetDirectoryName(s.FullPath) ?? ""));
                        if (FoldersOfUnusedImages.Count > 10) {
                            AppendLogVerifyLabel($"위 이미지들이 존재하는 폴더는 {FoldersOfUnusedImages.Count}종이 존재합니다. 일부를 출력합니다.");
                            AppendLogVerifyLabel(FoldersOfUnusedImages.Take(10).ToArray());
                        } else {
                            AppendLogVerifyLabel($"위 이미지들이 존재하는 폴더는 {FoldersOfUnusedImages.Count}종이 존재합니다.");
                            AppendLogVerifyLabel(FoldersOfUnusedImages.ToArray());
                        }
                    } else if (UnusedImagesForVerify.Count >= 1) {
                        AppendLogVerifyLabel($"경로내에 존재하지만 유효한 어노테이션에 사용되고 있지 않은 {UnusedImagesForVerify.Count}개의 이미지가 있습니다.");
                        AppendLogVerifyLabel(UnusedImagesForVerify.Select(s => s.FullPath).ToArray());
                    }
                }
                AppendLogVerifyLabel(
                    "",
                    "분석이 완료되었습니다.",
                    $"어노테이션 개수: {ImagesForVerify.Values.Sum(s => s.Annotations.Count)}",
                    $"어노테이션이 있는 이미지 개수: {ImagesForVerify.Values.Count(s => s.Annotations.Count > 0)}",
                    $"음성 이미지 개수: {ImagesForVerify.Values.Count(s => s.Annotations.Count == 0)}",
                    $"총 이미지 개수: {ImagesForVerify.Count}",
                    ""
                );
                AppendLogVerifyLabel(AnnotationsCountByCategory.Select(s =>
                    $"분류 이름: {s.Key}, 어노테이션 개수: {s.Value}, 어노테이션이 있는 이미지 개수: {ImagesForVerify.Values.Count(t => t.Annotations.Any(u => u.Class == s.Key))}").ToArray());
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
        public ICommand CmdExportVerifiedLabel { get; }
        private void ExportVerifiedLabel() {
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
            List<string> remove = SelectedItems.OfType<string>().ToList();
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
            if (CommonDialogService.SaveJsonFileDialog(out string outFilePath)) {
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
        }
        #endregion

        #region 데이터셋 분리
        public ICommand CmdSplitLabel { get; }
        private void SplitLabel() {
            if (!CommonDialogService.OpenCSVFileDialog(out string filePath)) return;
            Random r = new Random();
            List<AnnotationRecord> labels = new List<AnnotationRecord>();
            HashSet<ImageRecord> images = new HashSet<ImageRecord>();
            IEnumerable<string> lines = File.ReadLines(filePath);
            string basePath = Path.GetDirectoryName(filePath) ?? "";
            foreach (string line in lines) {
                (ImageRecord? img, AnnotationRecord? lbl) = SerializationService.CSVDeserialize(basePath, line, SettingService.Format);
                if (img is object) {
                    if (lbl is object) labels.Add(lbl);
                    images.Add(img);
                }
            }
            List<ImageRecord> shuffledImages = images.OrderBy(s => r.Next()).ToList();
            ILookup<ImageRecord, AnnotationRecord> labelsByImage = labels.ToLookup(s => s.Image);
            switch (TacticForSplitLabel) {
            case TacticsForSplitLabel.DevideToNLabels:
                // 균등 분할
                if (NValueForSplitLabel < 2 || NValueForSplitLabel > images.Count) {
                    CommonDialogService.MessageBox("입력한 숫자가 올바르지 않습니다.");
                    return;
                }
                List<StreamWriter> files = new List<StreamWriter>();
                var infoByPartition = new List<(HashSet<CategoryRecord> Classes, int ImagesCount)>();
                for (int i = 0; i < NValueForSplitLabel; i++) {
                    // 파일 이름: (원래 파일 이름).(파티션 번호 1부터 시작).(원래 확장자)
                    StreamWriter file = File.CreateText(Path.Combine($"{Path.GetDirectoryName(filePath)}", $"{Path.GetFileNameWithoutExtension(filePath)}.{i + 1}{Path.GetExtension(filePath)}"));
                    files.Add(file);
                    infoByPartition.Add((new HashSet<CategoryRecord>(), 0));
                }
                foreach (ImageRecord image in shuffledImages) {
                    IEnumerable<AnnotationRecord> labelsInImage = labelsByImage[image];
                    int idx;
                    if (labelsInImage.Any()) {
                        // 양성 이미지인 경우.
                        // 분류 다양성이 증가하는 정도가 가장 높은 순 -> 파티션에 포함된 이미지 개수가 적은 순.
                        (idx, _, _) = infoByPartition.Select((s, idx) => (idx, s.ImagesCount, labelsInImage.Select(t => t.Class).Except(s.Classes).Count())).OrderByDescending(s => s.Item3)
                                                     .ThenBy(s => s.ImagesCount).ThenBy(s => r.Next()).First();
                        foreach (AnnotationRecord label in labelsInImage) files[idx].WriteLine(SerializationService.CSVSerializeAsPositive(basePath, label, SettingService.Format));
                    } else {
                        // 음성 이미지인 경우.
                        // 파티션에 포함된 이미지 개수가 적은 순으로만 선택.
                        (idx, _) = infoByPartition.Select((s, idx) => (idx, s.ImagesCount)).OrderBy(s => s.ImagesCount).ThenBy(s => r.Next()).First();
                        files[idx].WriteLine(SerializationService.CSVSerializeAsNegative(basePath, image));
                    }
                    // 파티션별 정보 갱신
                    var (Classes, ImagesCount) = infoByPartition[idx];
                    Classes.UnionWith(labelsInImage.Select(s => s.Class));
                    ImagesCount++;
                    infoByPartition[idx] = (Classes, ImagesCount);
                }
                foreach (StreamWriter file in files) {
                    file.Dispose();
                }
                break;
            case TacticsForSplitLabel.TakeNSamples: {
                // 일부 추출
                if (NValueForSplitLabel < 1 || NValueForSplitLabel >= images.Count) {
                    CommonDialogService.MessageBox("입력한 숫자가 올바르지 않습니다.");
                    return;
                }
                using StreamWriter OutFileOriginal = File.CreateText(Path.Combine($"{Path.GetDirectoryName(filePath)}", $"{Path.GetFileNameWithoutExtension(filePath)}.1{Path.GetExtension(filePath)}"));
                using StreamWriter OutFileSplit = File.CreateText(Path.Combine($"{Path.GetDirectoryName(filePath)}", $"{Path.GetFileNameWithoutExtension(filePath)}.2{Path.GetExtension(filePath)}"));
                int ImageCountOfSplit = 0;
                HashSet<CategoryRecord> ClassesOriginal = new HashSet<CategoryRecord>();
                HashSet<CategoryRecord> ClassesSplit = new HashSet<CategoryRecord>();
                foreach ((int idx, ImageRecord image) in shuffledImages.Select((img, idx) => (idx, img))) {
                    IEnumerable<AnnotationRecord> labelsInImage = labelsByImage[image];
                    int DiversityDeltaOriginal = labelsInImage.Select(s => s.Class).Except(ClassesOriginal).Count();
                    int DiversityDeltaSplit = labelsInImage.Select(s => s.Class).Except(ClassesSplit).Count();
                    if (images.Count - idx + ImageCountOfSplit <= NValueForSplitLabel || (ImageCountOfSplit < NValueForSplitLabel && DiversityDeltaSplit >= DiversityDeltaOriginal)) {
                        // 아래 두 경우 중 하나일시 해당 이미지를 추출 데이터셋에 씀
                        // 1. 남은 이미지 전부를 추출해야만 추출량 목표치를 채울 수 있는 경우
                        // 2. 아직 추출량 목표치가 남아 있으며, 분류 다양성이 증가하는 정도가 추출 데이터셋 쪽이 더 높거나 같은 경우
                        if (labelsInImage.Any()) foreach (AnnotationRecord label in labelsInImage) OutFileSplit.WriteLine(SerializationService.CSVSerializeAsPositive(basePath, label, SettingService.Format));
                        else OutFileSplit.WriteLine(SerializationService.CSVSerializeAsNegative(basePath, image));
                        ImageCountOfSplit++;
                        ClassesSplit.UnionWith(labelsInImage.Select(s => s.Class));
                    } else {
                        if (labelsInImage.Any()) foreach (AnnotationRecord label in labelsInImage) OutFileOriginal.WriteLine(SerializationService.CSVSerializeAsPositive(basePath, label, SettingService.Format));
                        else OutFileOriginal.WriteLine(SerializationService.CSVSerializeAsNegative(basePath, image));
                        ClassesOriginal.UnionWith(labelsInImage.Select(s => s.Class));
                    }
                }
                break;
            }
            case TacticsForSplitLabel.SplitToSubFolders:
                // 하위 폴더로 분할
                IEnumerable<IGrouping<string, ImageRecord>> imagesByDir = images.GroupBy(s => Path.GetDirectoryName(s.FullPath) ?? "");
                foreach (IGrouping<string, ImageRecord> imagesInDir in imagesByDir) {
                    string TargetDir = Path.Combine(Path.GetDirectoryName(filePath) ?? "", imagesInDir.Key);
                    // 파일 이름: (원래 파일 이름).(최종 폴더 이름).(원래 확장자)
                    using StreamWriter OutputFile = File.CreateText(Path.Combine(TargetDir, $"{Path.GetFileNameWithoutExtension(filePath)}.{Path.GetFileName(imagesInDir.Key)}{Path.GetExtension(filePath)}"));
                    foreach (ImageRecord image in imagesInDir) {
                        IEnumerable<AnnotationRecord> labelsInImage = labelsByImage[image];
                        if (labelsInImage.Any()) foreach (AnnotationRecord label in labelsInImage) OutputFile.WriteLine(SerializationService.CSVSerializeAsPositive(TargetDir, label, SettingService.Format));
                        else OutputFile.WriteLine(SerializationService.CSVSerializeAsNegative(TargetDir, image));
                    }
                }
                break;
            }
        }
        #endregion

        #region 데이터셋 중복 제거
        public ICommand CmdUndupeLabel { get; }
        private void UndupeLabel() {
            if (CommonDialogService.OpenCSVFileDialog(out string filePath)) {
                LogUndupeLabel = "";
                ProgressUndupeLabelValue = 0;
                Task.Run(() => {
                    AppendLogUndupeLabel($"{filePath}에서 위치, 크기가 유사한 중복 경계상자를 제거합니다.");
                    // 로드
                    LabelsForUndupe.Clear();
                    ImagesForUndupe.Clear();
                    string basePath = Path.GetDirectoryName(filePath) ?? "";
                    string[] lines = File.ReadAllLines(filePath);
                    for (int i = 0; i < lines.Length; i++) {
                        (ImageRecord? img, AnnotationRecord? lbl) = SerializationService.CSVDeserialize(basePath, lines[i], SettingService.Format);
                        if (img is object) {
                            if (lbl is object) LabelsForUndupe.Add(lbl);
                            ImagesForUndupe.Add(img);
                        }
                    }
                    // 중복 제거
                    int TotalSuppressedBoxesCount = 0;
                    IEnumerable<IEnumerable<AnnotationRecord>> LabelsByShard;
                    if (UndupeWithoutClass) LabelsByShard = LabelsForUndupe.ToLookup(s => s.Image);
                    else LabelsByShard = LabelsForUndupe.ToLookup(s => (s.Image, s.Class));
                    int CountOfShard = LabelsByShard.Count();
                    SortedSet<ImageRecord> UndupedImages = new SortedSet<ImageRecord>();
                    foreach (var (idx, labelsInImage) in LabelsByShard.Select((s, idx) => (idx, s))) {
                        if (IsClosed) return;
                        ProgressUndupeLabelValue = (int)((double)(idx + 1) / CountOfShard * 100);
                        List<AnnotationRecord> sortedBySize = labelsInImage.OrderBy(s => s.Area).ToList(); // 넓이가 작은 경계 상자를 우선
                        while (sortedBySize.Count >= 2) {
                            // pick
                            AnnotationRecord pick = sortedBySize[0];
                            sortedBySize.Remove(pick);
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
                                LabelsForUndupe.Remove(i);
                                UndupedImages.Add(i.Image);
                            }
                            TotalSuppressedBoxesCount += labelsToSuppress.Count;
                        }
                    }
                    ProgressUndupeLabelValue = 100;
                    if (TotalSuppressedBoxesCount == 0) {
                        AppendLogUndupeLabel("분석이 완료되었습니다. 중복된 경계 상자가 없습니다.");
                    } else {
                        AppendLogUndupeLabel($"분석이 완료되었습니다. 중복된 경계 상자가 {UndupedImages.Count}개의 이미지에서 {TotalSuppressedBoxesCount}개 검출되었습니다.");
                        if (UndupedImages.Count > 20) {
                            AppendLogUndupeLabel("중복된 경계 상자가 있었던 이미지의 일부를 출력합니다.");
                            AppendLogUndupeLabel(UndupedImages.Select(s => s.FullPath).Take(20).ToArray());
                        } else {
                            AppendLogUndupeLabel("중복된 경계 상자가 있었던 이미지는 다음과 같습니다.");
                            AppendLogUndupeLabel(UndupedImages.Select(s => s.FullPath).ToArray());
                        }
                    }
                });
            }
        }
        public ICommand CmdExportUndupedLabel { get; }
        private void ExportUndupeLabel() {
            if (LabelsForUndupe.Count == 0 && ImagesForUndupe.Count == 0) {
                CommonDialogService.MessageBox("어노테이션 중복 제거를 실행한 적이 없습니다.");
                return;
            }
            if (CommonDialogService.SaveCSVFileDialog(out string filePath)) {
                string basePath = Path.GetDirectoryName(filePath) ?? "";
                using StreamWriter f = File.CreateText(filePath);
                ILookup<ImageRecord, AnnotationRecord> labelsByImage = LabelsForUndupe.ToLookup(s => s.Image);
                foreach (ImageRecord i in ImagesForUndupe) {
                    IEnumerable<AnnotationRecord> labelsInImage = labelsByImage[i];
                    if (labelsInImage.Any()) {
                        // 양성 데이터셋
                        foreach (AnnotationRecord j in labelsInImage) f.WriteLine(SerializationService.CSVSerializeAsPositive(basePath, j, SettingService.Format));
                    } else {
                        // 음성 데이터셋
                        f.WriteLine(SerializationService.CSVSerializeAsNegative(basePath, i));
                    }
                }
            }
        }
        #endregion

        #region 데이터셋 변환
        public ICommand CmdConvertLabel { get; }
        private void ConvertLabel() {
            switch (TacticForConvertLabel) {
            case TacticsForConvertLabel.COCOToCSV: {
                if (CommonDialogService.OpenJsonFileDialog(out string filePath)) {
                    string basePath = Path.GetDirectoryName(filePath) ?? "";
                    byte[] CocoContents = File.ReadAllBytes(filePath);
                    (ICollection<ImageRecord> images, _) = SerializationService.Deserialize(basePath, CocoContents);
                    string csvPath = Path.Combine(Path.GetDirectoryName(filePath) ?? "", Path.GetFileNameWithoutExtension(filePath) + ".csv");
                    using StreamWriter csv = File.CreateText(csvPath);
                    foreach (ImageRecord i in images) {
                        if (i.Annotations.Count > 0) {
                            // 양성 데이터셋
                            foreach (AnnotationRecord j in i.Annotations) csv.WriteLine(SerializationService.CSVSerializeAsPositive(basePath, j, SettingService.Format));
                        } else {
                            // 음성 데이터셋
                            csv.WriteLine(SerializationService.CSVSerializeAsNegative(basePath, i));
                        }
                    }
                }
                break;
            }
            case TacticsForConvertLabel.CSVToCOCO: {
                if (CommonDialogService.OpenCSVFileDialog(out string filePath)) {
                    Task.Run(() => {
                        ProgressConvertLabelValue = 0;
                        string basePath = Path.GetDirectoryName(filePath) ?? "";
                        string[] lines = File.ReadAllLines(filePath);
                        SortedSet<ImageRecord> images = new SortedSet<ImageRecord>();
                        SortedSet<CategoryRecord> categories = new SortedSet<CategoryRecord>();
                        for (int i = 0; i < lines.Length; i++) {
                            if (IsClosed) return;
                            ProgressConvertLabelValue = (int)((double)i / lines.Length * 100);
                            (ImageRecord? img, AnnotationRecord? lbl) = SerializationService.CSVDeserialize(basePath, lines[i], SettingService.Format);
                            if (img is object) {
                                if (images.TryGetValue(img, out var realImage)) {
                                    img = realImage;
                                } else {
                                    (img.Width, img.Height) = Utils.GetSizeOfImage(img.FullPath);
                                    images.Add(img);
                                }
                                if (lbl is object) {
                                    img.Annotations.Add(lbl);
                                    if (categories.TryGetValue(lbl.Class, out CategoryRecord? found)) lbl.Class = found;
                                    else categories.Add(lbl.Class);
                                }
                            }
                        }
                        string cocoPath = Path.Combine(Path.GetDirectoryName(filePath) ?? "", Path.GetFileNameWithoutExtension(filePath) + ".json");
                        byte[] CocoContents = SerializationService.Serialize(basePath, images, categories);
                        File.WriteAllBytes(cocoPath, CocoContents);
                        ProgressConvertLabelValue = 100;
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
        private void AppendLogVerifyLabel(params string[] logs) {
            LogVerifyDataset = LogVerifyDataset + string.Join(Environment.NewLine, logs) + Environment.NewLine;
        }
        private void AppendLogUndupeLabel(params string[] logs) {
            LogUndupeLabel = LogUndupeLabel + string.Join(Environment.NewLine, logs) + Environment.NewLine;
        }
        #endregion
    }
}
