using COCOAnnotator.Events;
using COCOAnnotator.Records;
using COCOAnnotator.Records.COCO;
using COCOAnnotator.Records.Enums;
using COCOAnnotator.Services.Utilities;
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
    public class ManageDialogViewModel : DialogViewModel {
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
        private string BasePathForVerify = "";
        private readonly SortedDictionary<int, ImageRecord> ImagesForVerify = new SortedDictionary<int, ImageRecord>();
        private readonly SortedDictionary<int, CategoryRecord> CategoriesForVerify = new SortedDictionary<int, CategoryRecord>();
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
            if (!SerializationService.IsJsonPathValid(filePath)) {
                CommonDialogService.MessageBox("데이터셋 파일을 읽어올 수 없습니다. 파일명이 instances_XX.json이며 상위 폴더가 존재해야 합니다.");
                return;
            }
            bool? res = CommonDialogService.MessageBoxYesNoCancel("검증을 시작합니다. 이미지 크기 검사를 하기 원하시면 예 아니면 아니오를 선택하세요. 이미지 크기 검사시 데이터셋 크기에 따라 시간이 오래 걸릴 수 있습니다.");
            if (res is null) return;
            bool imageSizeCheck = res.Value;
            LogVerifyDataset = "";
            ProgressVerifyDataset = 0;
            Task.Run(async () => {
                ImagesForVerify.Clear();
                CategoriesForVerify.Clear();
                DatasetCOCO datasetcoco = await SerializationService.DeserializeRawAsync(filePath).ConfigureAwait(false);
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string instanceName = fileName[(fileName.IndexOf('_')+1)..];
                BasePathForVerify = Path.GetFullPath($@"..\..\{instanceName}", filePath);
                AppendLogVerifyDataset($"\"{filePath}\"의 분석을 시작합니다.");
                if (!Directory.Exists(BasePathForVerify)) {
                    AppendLogVerifyDataset($"이미지 폴더인 \"{BasePathForVerify}\"가 존재하지 않습니다.");
                    return;
                } else {
                    AppendLogVerifyDataset($"이미지 폴더는 \"{BasePathForVerify}\"입니다.");
                }
                int total = datasetcoco.Images.Count + datasetcoco.Annotations.Count + datasetcoco.Categories.Count;
                {
                    SortedSet<int> DuplicatedIDAlreadyDetected = new SortedSet<int>();
                    SortedSet<ImageRecord> ImageRecords = new SortedSet<ImageRecord>();
                    SortedSet<ImageRecord> DuplicatedImageAlreadyDetected = new SortedSet<ImageRecord>();
                    foreach ((int idx, ImageCOCO image) in datasetcoco.Images.Select((s, idx) => (idx, s))) {
                        if (IsClosed) return;
                        ProgressVerifyDataset = (int)((double)idx / total * 100);
                        string fullPath = Path.GetFullPath(image.FileName, BasePathForVerify);
                        if (ImagesForVerify.ContainsKey(image.ID)) {
                            if (DuplicatedIDAlreadyDetected.Add(image.ID)) AppendLogVerifyDataset($"ID가 {image.ID}인 이미지가 2개 이상 발견되었습니다.");
                            continue;
                        }
                        if (!File.Exists(fullPath)) {
                            AppendLogVerifyDataset($"ID가 {image.ID}인 이미지가 주어진 경로에 존재하지 않습니다.");
                            continue;
                        }
                        ImageRecord imageRecord = new ImageRecord(image.FileName, image.Width, image.Height);
                        if (ImageRecords.Contains(imageRecord)) {
                            if (DuplicatedImageAlreadyDetected.Add(imageRecord)) AppendLogVerifyDataset($"다음 경로의 이미지가 2번 이상 사용되었습니다: {fullPath}");
                            continue;
                        }
                        if (imageSizeCheck) {
                            try {
                                if (imageRecord.LoadSize(BasePathForVerify)) {
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
                    foreach ((int idx, CategoryCOCO category) in datasetcoco.Categories.Select((s, idx) => (idx, s))) {
                        if (IsClosed) return;
                        ProgressVerifyDataset = (int)((double)(datasetcoco.Images.Count + idx) / total * 100);
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
                    foreach ((int idx, AnnotationCOCO annotation) in datasetcoco.Annotations.Select((s, idx) => (idx, s))) {
                        ProgressVerifyDataset = (int)((double)(datasetcoco.Images.Count + datasetcoco.Categories.Count + idx) / total * 100);
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
                        float left = annotation.BoundaryBox[0];
                        float top = annotation.BoundaryBox[1];
                        float width = annotation.BoundaryBox[2];
                        float height = annotation.BoundaryBox[3];
                        if (left < 0 || top < 0 || left + width > image.Width || top + height > image.Height) {
                            AppendLogVerifyDataset($"ID가 {annotation.ID}인 어노테이션의 좌표가 유효하지 않습니다. 좌표가 이미지의 크기 밖에 있습니다.");
                            continue;
                        }
                        if (width <= 0 || height <= 0) {
                            AppendLogVerifyDataset($"ID가 {annotation.ID}인 어노테이션의 좌표가 유효하지 않습니다. 너비 또는 높이는 0 이하일 수 없습니다.");
                            continue;
                        }
                        image.Annotations.Add(new AnnotationRecord(image, left, top, width, height, category));
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
                AppendLogVerifyDataset(
                    CategoriesForVerify.Select(s =>
                        $"분류 이름: {s.Value}, " +
                        $"어노테이션 개수: {ImagesForVerify.Values.SelectMany(t => t.Annotations).Count(t => t.Category == s.Value)}, " +
                        $"어노테이션이 있는 이미지 개수: {ImagesForVerify.Values.Count(t => t.Annotations.Any(u => u.Category == s.Value))}"
                    ).ToArray()
                );
                ProgressVerifyDataset = 100;
            });
        }
        public ICommand CmdExportVerifiedDataset { get; }
        private async void ExportVerifiedDataset() {
            if (ImagesForVerify.Count == 0 && CategoriesForVerify.Count == 0) {
                CommonDialogService.MessageBox("데이터셋을 분석한 적 없거나 분석한 데이터셋 내에 유효한 내용이 없습니다.");
                return;
            }
            if (!CommonDialogService.MessageBoxOKCancel("분석한 데이터셋의 내용 중 유효한 내용만 남깁니다.")) return;
            await SerializationService.SerializeAsync(new DatasetRecord(BasePathForVerify, ImagesForVerify.Values, CategoriesForVerify.Values)).ConfigureAwait(false);
        }
        #endregion

        #region 데이터셋 병합
        public ICommand CmdAddFileForUnionDataset { get; }
        private void AddFileForUnionDataset() {
            if (CommonDialogService.OpenJsonFilesDialog(out string[] filePaths)) {
                bool error = false;
                foreach (string filePath in filePaths) {
                    if (SerializationService.IsJsonPathValid(filePath)) FilesForUnionDataset.Add(filePath);
                    else error = true;
                }
                if (error) CommonDialogService.MessageBox("일부 데이터셋 파일을 읽어올 수 없습니다. 파일명이 instances_XX.json이며 상위 폴더가 존재해야 합니다.");
            }
        }
        public ICommand CmdAddFolderForUnionDataset { get; }
        private void AddFolderForUnionDataset() {
            if (CommonDialogService.OpenFolderDialog(out string folderPath)) {
                bool error = false;
                foreach (string filePath in Directory.EnumerateFiles(folderPath, "*.json", SearchOption.AllDirectories)) {
                    if (SerializationService.IsJsonPathValid(filePath)) FilesForUnionDataset.Add(filePath);
                    else error = true;
                }
                if (error) CommonDialogService.MessageBox("일부 데이터셋 파일을 읽어올 수 없습니다. 파일명이 instances_XX.json이며 상위 폴더가 존재해야 합니다.");
            }
        }
        public ICommand CmdRemoveFileForUnionDataset { get; }
        private void RemoveFileForUnionDataset(IList SelectedItems) {
            string[] remove = SelectedItems.OfType<string>().ToArray();
            foreach (string i in remove) {
                FilesForUnionDataset.Remove(i);
            }
        }
        public ICommand CmdResetFileForUnionDataset { get; }
        private void ResetFileForUnionDataset() {
            FilesForUnionDataset.Clear();
        }
        public ICommand CmdExportUnionDataset { get; }
        private async void ExportUnionDataset() {
            if (!CommonDialogService.SaveJsonFileDialog(out string outFilePath)) return;
            if (!SerializationService.IsJsonPathValid(outFilePath)) {
                CommonDialogService.MessageBox("데이터셋 파일을 저장할 수 없습니다. 파일명이 instances_XX.json이며 상위 폴더가 존재해야 합니다.");
                return;
            }
            if (!CommonDialogService.MessageBoxOKCancel("기존 이미지를 복사하여 병합된 새 데이터셋을 만듭니다. 분류의 배열 순서는 병합 전후 유지되지 않을 수 있습니다.")) return;
            // 로드
            string outFileName = Path.GetFileNameWithoutExtension(outFilePath);
            string outInstanceName = outFileName[(outFileName.IndexOf('_') + 1)..];
            string outBasePath = Path.GetFullPath($@"..\..\{outInstanceName}", outFilePath);
            SortedSet<ImageRecord> Images = new SortedSet<ImageRecord>();
            SortedSet<CategoryRecord> Categories = new SortedSet<CategoryRecord>();
            foreach (string inFilePath in FilesForUnionDataset) {
                DatasetRecord dataset = await SerializationService.DeserializeAsync(inFilePath).ConfigureAwait(false);
                foreach (ImageRecord image in dataset.Images) CopyFile(Path.Combine(dataset.BasePath, image.Path), Path.Combine(outBasePath, image.Path));
                Images.UnionWith(dataset.Images);
                Categories.UnionWith(dataset.Categories);
            }
            // 저장
            await SerializationService.SerializeAsync(new DatasetRecord(outBasePath, Images, Categories)).ConfigureAwait(false);
        }
        #endregion

        #region 데이터셋 분리
        public ICommand CmdSplitDataset { get; }
        private async void SplitDataset() {
            if (!CommonDialogService.OpenJsonFileDialog(out string inFilePath)) return;
            if (!SerializationService.IsJsonPathValid(inFilePath)) {
                CommonDialogService.MessageBox("데이터셋 파일을 읽어올 수 없습니다. 파일명이 instances_XX.json이며 상위 폴더가 존재해야 합니다.");
                return;
            }
            DatasetRecord inDataset = await SerializationService.DeserializeAsync(inFilePath).ConfigureAwait(false);
            IEnumerable<ImageRecord> shuffledImages = inDataset.Images.Shuffle();
            string inFileName = Path.GetFileNameWithoutExtension(inFilePath);
            string inInstanceName = inFileName[(inFileName.IndexOf('_') + 1)..];
            if (!CommonDialogService.MessageBoxOKCancel("기존 이미지를 복사하여 분할된 새 데이터셋을 만듭니다.")) return;
            switch (TacticForSplitDataset) {
            case TacticsForSplitDataset.DevideToN:
                // 균등 분할
                if (NValueForSplitDataset < 2 || NValueForSplitDataset > inDataset.Images.Count) {
                    CommonDialogService.MessageBox("입력한 숫자가 올바르지 않습니다.");
                    return;
                }
                List<DatasetRecord> outDatasetByPartition = new List<DatasetRecord>();
                List<SortedSet<CategoryRecord>> categoriesByPartition = new List<SortedSet<CategoryRecord>>();
                for (int i = 0; i < NValueForSplitDataset; i++) {
                    outDatasetByPartition.Add(new DatasetRecord(Path.GetFullPath($@"..\{inInstanceName}_{i + 1}", inDataset.BasePath), Enumerable.Empty<ImageRecord>(), inDataset.Categories));
                    categoriesByPartition.Add(new SortedSet<CategoryRecord>());
                }
                foreach (ImageRecord image in shuffledImages) {
                    if (image.Annotations.Count > 0) {
                        // 양성 이미지인 경우.
                        // 분류 다양성이 증가하는 정도가 가장 높은 순 -> 파티션에 포함된 이미지 개수가 적은 순.
                        DatasetRecord datasetOfPartition = outDatasetByPartition
                            .OrderByDescending(s => image.Annotations.Select(t => t.Category).Except(s.Images.SelectMany(t => t.Annotations).Select(s => s.Category)).Count())
                            .ThenBy(s => s.Images.Count).First();
                        CopyFile(Path.Combine(inDataset.BasePath, image.Path), Path.Combine(datasetOfPartition.BasePath, image.Path));
                        datasetOfPartition.Images.Add(image);
                    } else {
                        // 음성 이미지인 경우.
                        // 파티션에 포함된 이미지 개수가 적은 순으로만 선택.
                        DatasetRecord datasetOfPartition = outDatasetByPartition.OrderBy(s => s.Images.Count).First();
                        CopyFile(Path.Combine(inDataset.BasePath, image.Path), Path.Combine(datasetOfPartition.BasePath, image.Path));
                        datasetOfPartition.Images.Add(image);
                    }
                }
                foreach (DatasetRecord i in outDatasetByPartition) await SerializationService.SerializeAsync(i).ConfigureAwait(false);
                break;
            case TacticsForSplitDataset.TakeNSamples:
                // 일부 추출
                if (NValueForSplitDataset < 1 || NValueForSplitDataset >= inDataset.Images.Count) {
                    CommonDialogService.MessageBox("입력한 숫자가 올바르지 않습니다.");
                    return;
                }
                DatasetRecord OriginalDataset = new DatasetRecord(Path.GetFullPath($@"..\{inInstanceName}_1", inDataset.BasePath), Enumerable.Empty<ImageRecord>(), inDataset.Categories);
                DatasetRecord SplitDataset = new DatasetRecord(Path.GetFullPath($@"..\{inInstanceName}_2", inDataset.BasePath), Enumerable.Empty<ImageRecord>(), inDataset.Categories);
                foreach (ImageRecord image in shuffledImages) {
                    int DiversityDeltaOriginal = image.Annotations.Select(s => s.Category).Except(OriginalDataset.Images.SelectMany(s => s.Annotations).Select(s => s.Category)).Count();
                    int DiversityDeltaSplit = image.Annotations.Select(s => s.Category).Except(SplitDataset.Images.SelectMany(s => s.Annotations).Select(s => s.Category)).Count();
                    // 아래 두 경우 중 하나일시 해당 이미지를 추출 데이터셋에 씀
                    // 1. 남은 이미지 전부를 추출해야만 추출량 목표치를 채울 수 있는 경우
                    // 2. 아직 추출량 목표치가 남아 있으며, 분류 다양성이 증가하는 정도가 추출 데이터셋 쪽이 더 높거나 같은 경우
                    if (OriginalDataset.Images.Count + NValueForSplitDataset + 1 >= inDataset.Images.Count ||
                        (SplitDataset.Images.Count < NValueForSplitDataset && DiversityDeltaSplit >= DiversityDeltaOriginal)) {
                        CopyFile(Path.Combine(inDataset.BasePath, image.Path), Path.Combine(SplitDataset.BasePath, image.Path));
                        SplitDataset.Images.Add(image);
                    } else {
                        CopyFile(Path.Combine(inDataset.BasePath, image.Path), Path.Combine(OriginalDataset.BasePath, image.Path));
                        OriginalDataset.Images.Add(image);
                    }
                }
                await SerializationService.SerializeAsync(OriginalDataset).ConfigureAwait(false);
                await SerializationService.SerializeAsync(SplitDataset).ConfigureAwait(false);
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
            Task.Run(async () => {
                AppendLogUndupeDataset($"{filePath}에서 위치, 크기가 유사한 중복 경계상자를 제거합니다.");
                // 로드
                DatasetRecord dataset = await SerializationService.DeserializeAsync(filePath).ConfigureAwait(false);
                ImagesForUndupe.AddRange(dataset.Images);
                CategoriesForUndupe.AddRange(dataset.Categories);
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
                    int LocalSuppressedBoxesCount = ImagesForUndupe[i].Annotations.Count - AnnotationsForUndupe.Count;
                    if (LocalSuppressedBoxesCount > 0) {
                        ImagesForUndupe[i].Annotations.Clear();
                        ImagesForUndupe[i].Annotations.AddRange(AnnotationsForUndupe);
                        TotalSuppressedBoxesCount += LocalSuppressedBoxesCount;
                        SuppressedImages.Add(ImagesForUndupe[i]);
                    }
                }
                ProgressUndupeDatasetValue = 100;
                if (TotalSuppressedBoxesCount == 0) {
                    AppendLogUndupeDataset("분석이 완료되었습니다. 중복된 경계 상자가 없습니다.");
                } else {
                    AppendLogUndupeDataset($"분석이 완료되었습니다. 중복된 경계 상자가 {SuppressedImages.Count}개의 이미지에서 {TotalSuppressedBoxesCount}개 검출되었습니다.");
                    SortedSet<string> UniqueImagePaths = new SortedSet<string>
                        (SuppressedImages.Select(s => Path.GetRelativePath(Path.GetDirectoryName(filePath) ?? "", s.Path).Replace('\\', '/')));
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
        private async void ExportUndupedDataset() {
            if (ImagesForUndupe.Count == 0 && CategoriesForUndupe.Count == 0) {
                CommonDialogService.MessageBox("어노테이션 중복 제거를 실행한 적이 없습니다.");
                return;
            }
            if (!CommonDialogService.SaveJsonFileDialog(out string filePath)) return;
            await SerializationService.SerializeAsync(filePath, ImagesForUndupe, CategoriesForUndupe).ConfigureAwait(false);
        }
        #endregion

        #region 데이터셋 변환
        public ICommand CmdConvertDataset { get; }
        private async void ConvertDataset() {
            switch (TacticForConvertDataset) {
            case TacticsForConvertDataset.COCOToCSV:
                if (CommonDialogService.OpenJsonFileDialog(out string jsonFilePath)) {
                    (ICollection<ImageRecord> images, _) = await SerializationService.DeserializeAsync(jsonFilePath).ConfigureAwait(false);
                    await SerializationService.SerializeCSVAsync(Path.Combine(Path.GetDirectoryName(jsonFilePath) ?? "", Path.GetFileNameWithoutExtension(jsonFilePath) + ".csv"), images, CSVFormat)
                        .ConfigureAwait(false);
                }
                break;
            case TacticsForConvertDataset.CSVToCOCO:
                if (CommonDialogService.OpenCSVFileDialog(out string csvFilePath)) {
                    _ = Task.Run(async () => {
                        ProgressConvertDatasetValue = 0;
                        DatasetRecord dataset = await SerializationService.DeserializeCSVAsync(csvFilePath, CSVFormat).ConfigureAwait(false);
                        foreach ((int idx, ImageRecord image) in dataset.Images.Select((s, idx) => (idx, s))) {
                            if (IsClosed) return;
                            ProgressConvertDatasetValue = (int)((double)idx / dataset.Images.Count * 100);
                            image.LoadSize(dataset.BasePath);
                        }
                        await SerializationService.SerializeAsync(Path.Combine(Path.GetDirectoryName(csvFilePath) ?? "", Path.GetFileNameWithoutExtension(csvFilePath) + ".json"), dataset.Images,
                            dataset.Categories).ConfigureAwait(false);
                        ProgressConvertDatasetValue = 100;
                    });
                }
                break;
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
            while (sortedBySize.Count > 0) {
                // pick
                AnnotationRecord pick = sortedBySize[0];
                sortedBySize.RemoveAt(0);
                // supress
                for (int i = 0; i < sortedBySize.Count; i++) {
                    double left = Math.Max(pick.Left, sortedBySize[i].Left);
                    double top = Math.Max(pick.Top, sortedBySize[i].Top);
                    double right = Math.Min(pick.Left + pick.Width, sortedBySize[i].Left + sortedBySize[i].Width);
                    double bottom = Math.Min(pick.Top + pick.Height, sortedBySize[i].Top + sortedBySize[i].Height);
                    if (left >= right || top >= bottom) continue;
                    double sizeIntersection = (right - left) * (bottom - top);
                    double sizeUnion = pick.Area + sortedBySize[i].Area - sizeIntersection;
                    double iou = sizeIntersection / sizeUnion;
                    if (iou < IoUThreshold) continue;
                    sortedBySize.RemoveAt(i);
                    i--;
                }
                yield return pick;
            }
        }
        private void CopyFile(string FromPath, string ToPath) {
            string? ToDirectory = Path.GetDirectoryName(ToPath);
            if (ToDirectory is null) return;
            Directory.CreateDirectory(ToDirectory);
            File.Copy(FromPath, ToPath);
        }
        #endregion
    }
}
