using LabelAnnotator.Events;
using LabelAnnotator.Records;
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
using System.Windows.Media.Imaging;

namespace LabelAnnotator.ViewModels {
    public class ManageDialogViewModel : DialogViewModelBase {
        #region 생성자
        public ManageDialogViewModel() {
            Title = "레이블 관리";

            _LogVerifyLabel = "";
            FilesForUnionLabel = new ObservableCollection<string>();
            _TacticForSplitLabel = TacticsForSplitLabel.DevideToNLabels;
            _NValueForSplitLabel = 2;
            _LogUndupeLabel = "";
            _IoUThreshold = 0.5;

            CmdVerifyLabel = new DelegateCommand(VerifyLabel);
            CmdDeleteUnusedImages = new DelegateCommand(DeleteUnusedImages);
            CmdExportVerifiedLabel = new DelegateCommand(ExportVerifiedLabel);
            CmdAddFileForUnionLabel = new DelegateCommand(AddFileForUnionLabel);
            CmdAddFolderForUnionLabel = new DelegateCommand(AddFolderForUnionLabel);
            CmdRemoveFileForUnionLabel = new DelegateCommand<IList>(RemoveFileForUnionLabel);
            CmdResetFileForUnionLabel = new DelegateCommand(ResetFileForUnionLabel);
            CmdExportUnionLabel = new DelegateCommand(ExportUnionLabel);
            CmdSplitLabel = new DelegateCommand(SplitLabel);
            CmdUndupeLabel = new DelegateCommand(UndupeLabel);
            CmdExportUndupedLabel = new DelegateCommand(ExportUndupeLabel);
            CmdClose = new DelegateCommand(Close);
        }
        #endregion

        #region 필드, 바인딩되지 않는 프로퍼티
        private readonly SortedDictionary<ClassRecord, List<LabelRecord>> PositiveLabelsByCategoryForVerify = new SortedDictionary<ClassRecord, List<LabelRecord>>();
        private readonly SortedSet<ImageRecord> PositiveImagesForVerify = new SortedSet<ImageRecord>();
        private readonly SortedSet<ImageRecord> NegativeImagesForVerify = new SortedSet<ImageRecord>();
        private readonly SortedSet<ImageRecord> UnusedImagesForVerify = new SortedSet<ImageRecord>();
        private readonly List<LabelRecord> LabelsForUndupe = new List<LabelRecord>();
        private readonly SortedSet<ImageRecord> ImagesForUndupe = new SortedSet<ImageRecord>();
        #endregion

        #region 바인딩되는 프로퍼티
        private string _LogVerifyLabel;
        public string LogVerifyLabel {
            get => _LogVerifyLabel;
            set {
                if (SetProperty(ref _LogVerifyLabel, value)) {
                    EventAggregator.GetEvent<ScrollTxtLogVerifyLabel>().Publish();
                }
            }
        }
        private int _ProgressVerifyLabelValue;
        public int ProgressVerifyLabelValue {
            get => _ProgressVerifyLabelValue;
            set => SetProperty(ref _ProgressVerifyLabelValue, value);
        }
        public ObservableCollection<string> FilesForUnionLabel { get; }
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
        #endregion

        #region 커맨드
        #region 레이블 분석
        public ICommand CmdVerifyLabel { get; }
        private void VerifyLabel() {
            if (!CommonDialogService.OpenCSVFileDialog(out string filePath)) return;
            bool? res = CommonDialogService.MessageBoxYesNoCancel("검증을 시작합니다. 이미지 크기 검사를 하기 원하시면 예 아니면 아니오를 선택하세요. 이미지 크기 검사시 데이터셋 크기에 따라 시간이 오래 걸릴 수 있습니다.");
            if (res is null) return;
            bool imageSizeCheck = res.Value;
            LogVerifyLabel = "";
            ProgressVerifyLabelValue = 0;
            Task.Run(() => {
                PositiveLabelsByCategoryForVerify.Clear();
                PositiveImagesForVerify.Clear();
                NegativeImagesForVerify.Clear();
                UnusedImagesForVerify.Clear();
                string basePath = Path.GetDirectoryName(filePath) ?? "";
                var CacheForImageSize = new SortedDictionary<ImageRecord, (int Width, int Height)?>();
                string[] lines = File.ReadAllLines(filePath);
                bool DetectedFlag = false;
                AppendLogVerifyLabel($"\"{filePath}\"의 분석을 시작합니다.");
                for (int i = 0; i < lines.Length; i++) {
                    ProgressVerifyLabelValue = (int)((double)i / lines.Length * 100);
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;
                    (ImageRecord? img, LabelRecord? lbl) = SerializationService.Deserialize(basePath, lines[i], SettingService.Format);
                    if (img is null) {
                        AppendLogVerifyLabel($"{i + 1}번째 줄이 유효하지 않습니다. CSV 값 개수가 6개 미만이거나 좌표값이 숫자 값이 아닙니다.");
                        DetectedFlag = true;
                        continue;
                    }
                    if (!File.Exists(img.FullPath)) {
                        AppendLogVerifyLabel($"{i + 1}번째 줄이 유효하지 않습니다. 이미지가 해당 경로에 실존하지 않습니다.");
                        DetectedFlag = true;
                        continue;
                    }
                    if (NegativeImagesForVerify.Contains(img)) {
                        AppendLogVerifyLabel($"{i + 1}번째 줄이 유효하지 않습니다. 음성 샘플로 사용된 적이 있는 이미지가 한번 더 사용되었습니다.");
                        DetectedFlag = true;
                        continue;
                    }
                    if (lbl is null) {
                        if (PositiveImagesForVerify.Contains(img)) {
                            AppendLogVerifyLabel($"{i + 1}번째 줄이 유효하지 않습니다. 양성 레이블에서 사용된 적이 있는 이미지가 음성 샘플로 사용되었습니다.");
                            DetectedFlag = true;
                            continue;
                        } else {
                            // 음성 샘플
                            NegativeImagesForVerify.Add(img);
                            continue;
                        }
                    }
                    // 경계 상자 위치 좌표가 위아래 혹은 좌우가 뒤집혀있는지 검사.
                    if (lbl.Left >= lbl.Right || lbl.Top >= lbl.Bottom) {
                        AppendLogVerifyLabel($"{i + 1}번째 줄이 유효하지 않습니다. 경계 상자의 좌표 값이 부적절합니다. 상하좌우가 뒤집혀 있는지 확인하세요.");
                        DetectedFlag = true;
                        continue;
                    }
                    // 경계 상자 위치 좌표가 이미지 크기 밖으로 나가는지 검사.
                    if (imageSizeCheck) {
                        // 이미지의 크기 체크하기; 이미 이미지를 읽어온 적이 있으면 캐시에서 가져옴.
                        int width, height;
                        if (CacheForImageSize.TryGetValue(img, out var size)) {
                            if (size is null) continue;
                            (width, height) = size.Value;
                        } else {
                            try {
                                using FileStream stream = new FileStream(img.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                                BitmapFrame bitmap = BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                                width = bitmap.PixelWidth;
                                height = bitmap.PixelHeight;
                                CacheForImageSize.Add(img, (width, height));
                            } catch (NotSupportedException) {
                                AppendLogVerifyLabel($"{i + 1}번째 줄이 유효하지 않습니다. 이미지가 손상되어 읽어올 수 없습니다.");
                                DetectedFlag = true;
                                CacheForImageSize.Add(img, null);
                                continue;
                            }
                        }
                        if (lbl.Left < 0 || lbl.Top < 0 || lbl.Right > width || lbl.Bottom > height) {
                            AppendLogVerifyLabel($"{i + 1}번째 줄이 유효하지 않습니다. 경계 상자 좌표가 이미지의 크기 밖에 있습니다.");
                            DetectedFlag = true;
                            continue;
                        }
                    } else {
                        if (lbl.Left < 0 || lbl.Top < 0) {
                            AppendLogVerifyLabel($"{i + 1}번째 줄이 유효하지 않습니다. 경계 상자 좌표가 이미지의 크기 밖에 있습니다.");
                            DetectedFlag = true;
                            continue;
                        }
                    }
                    if (PositiveLabelsByCategoryForVerify.TryGetValue(lbl.Class, out List<LabelRecord>? positiveLabelsInCategory)) {
                        // 완전히 동일한 레이블이 이미 존재하는지 검사
                        if (positiveLabelsInCategory.Contains(lbl)) {
                            AppendLogVerifyLabel($"{i + 1}번째 줄이 유효하지 않습니다. 동일한 레이블이 이미 존재합니다.");
                            DetectedFlag = true;
                            continue;
                        } else {
                            // 유효한 양성 레이블. (같은 분류가 이미 있음)
                            PositiveImagesForVerify.Add(img);
                            positiveLabelsInCategory.Add(lbl);
                        }
                    } else {
                        // 유효한 양성 레이블. (같은 분류가 없음)
                        PositiveImagesForVerify.Add(img);
                        List<LabelRecord> labels = new List<LabelRecord> { lbl };
                        PositiveLabelsByCategoryForVerify.Add(lbl.Class, labels);
                    }
                }
                ProgressVerifyLabelValue = 100;
                if (!DetectedFlag) {
                    AppendLogVerifyLabel("유효하지 않은 레이블이 발견되지 않았습니다.");
                }
                // 사용되지 않은 이미지 검색
                {
                    AppendLogVerifyLabel("");
                    SortedSet<ImageRecord> AllImagesInLabel = new SortedSet<ImageRecord>(PositiveImagesForVerify.Concat(NegativeImagesForVerify));
                    string CommonParentPath = Utils.GetCommonParentPath(AllImagesInLabel);
                    AppendLogVerifyLabel($"사용된 이미지의 공통 부모 경로는 \"{CommonParentPath}\"입니다.");
                    UnusedImagesForVerify.UnionWith(Directory.EnumerateFiles(CommonParentPath, "*.*", SearchOption.AllDirectories)
                                                             .Where(s => Utils.ApprovedImageExtensions.Contains(Path.GetExtension(s))).Select(s => new ImageRecord(s)));
                    UnusedImagesForVerify.ExceptWith(AllImagesInLabel);
                    if (UnusedImagesForVerify.Count > 20) {
                        AppendLogVerifyLabel($"경로내에 존재하지만 유효한 레이블에 사용되고 있지 않은 {UnusedImagesForVerify.Count}개의 이미지가 있습니다. 일부를 출력합니다.");
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
                        AppendLogVerifyLabel($"경로내에 존재하지만 유효한 레이블에 사용되고 있지 않은 {UnusedImagesForVerify.Count}개의 이미지가 있습니다.");
                        AppendLogVerifyLabel(UnusedImagesForVerify.Select(s => s.FullPath).ToArray());
                    }
                }
                AppendLogVerifyLabel(
                    "",
                    "분석이 완료되었습니다.",
                    $"총 레이블 개수: {PositiveLabelsByCategoryForVerify.Sum(s => s.Value.Count) + NegativeImagesForVerify.Count}",
                    $"양성 레이블 개수: {PositiveLabelsByCategoryForVerify.Sum(s => s.Value.Count)}",
                    $"음성 레이블 개수: {NegativeImagesForVerify.Count}",
                    $"양성 레이블이 있는 이미지 개수: {PositiveImagesForVerify.Count}",
                    $"총 이미지 개수: {NegativeImagesForVerify.Count + PositiveImagesForVerify.Count}",
                    $"총 분류 개수: {PositiveLabelsByCategoryForVerify.Count}",
                    ""
                );
                AppendLogVerifyLabel(PositiveLabelsByCategoryForVerify.Select(s =>
                    $"분류 이름: {s.Key}, 레이블 개수: {s.Value.Count}, 레이블이 있는 이미지 개수: {s.Value.Select(s => s.Image).Distinct().Count()}").ToArray());
            });
        }
        public ICommand CmdDeleteUnusedImages { get; }
        private void DeleteUnusedImages() {
            if (UnusedImagesForVerify.Count == 0) {
                CommonDialogService.MessageBox("레이블 파일을 분석한 적 없거나 이미지 폴더 내에 레이블에 사용중이지 않은 이미지가 없습니다.");
                return;
            }
            bool res = CommonDialogService.MessageBoxOKCancel("이미지 폴더 내에 있지만 레이블에 사용중이지 않은 이미지를 삭제합니다.");
            if (!res) return;
            foreach (ImageRecord i in UnusedImagesForVerify) {
                File.Delete(i.FullPath);
            }
            UnusedImagesForVerify.Clear();
        }
        public ICommand CmdExportVerifiedLabel { get; }
        private void ExportVerifiedLabel() {
            if (NegativeImagesForVerify.Count == 0 && PositiveLabelsByCategoryForVerify.Count == 0) {
                CommonDialogService.MessageBox("레이블 파일을 분석한 적 없거나 분석한 레이블 파일 내에 유효 레이블이 없습니다.");
                return;
            }
            if (!CommonDialogService.SaveCSVFileDialog(out string filePath)) return;
            bool res = CommonDialogService.MessageBoxOKCancel("분석한 레이블 파일의 내용 중 유효한 레이블만 내보냅니다.");
            if (!res) return;
            string saveBasePath = Path.GetDirectoryName(filePath) ?? "";
            using StreamWriter f = File.CreateText(filePath);
            // 양성 레이블
            foreach (IGrouping<ImageRecord, LabelRecord> i in PositiveLabelsByCategoryForVerify.SelectMany(s => s.Value).GroupBy(s => s.Image)) {
                foreach (LabelRecord j in i) {
                    f.WriteLine(SerializationService.SerializeAsPositive(saveBasePath, j, SettingService.Format));
                }
            }
            // 음성 레이블
            foreach (ImageRecord i in NegativeImagesForVerify) {
                f.WriteLine(SerializationService.SerializeAsNegative(saveBasePath, i));
            }
        }
        #endregion

        #region 레이블 병합
        public ICommand CmdAddFileForUnionLabel { get; }
        private void AddFileForUnionLabel() {
            if (CommonDialogService.OpenCSVFilesDialog(out string[] filePaths)) {
                foreach (string FileName in filePaths) {
                    FilesForUnionLabel.Add(FileName);
                }
            }
        }
        public ICommand CmdAddFolderForUnionLabel { get; }
        private void AddFolderForUnionLabel() {
            if (CommonDialogService.OpenFolderDialog(out string folderPath)) {
                foreach (string i in Directory.EnumerateFiles(folderPath, "*.csv", SearchOption.AllDirectories)) FilesForUnionLabel.Add(i);
            }
        }
        public ICommand CmdRemoveFileForUnionLabel { get; }
        private void RemoveFileForUnionLabel(IList SelectedItems) {
            List<string> remove = SelectedItems.OfType<string>().ToList();
            foreach (string i in remove) {
                if (i is null) continue;
                FilesForUnionLabel.Remove(i);
            }
        }
        public ICommand CmdResetFileForUnionLabel { get; }
        private void ResetFileForUnionLabel() {
            FilesForUnionLabel.Clear();
        }
        public ICommand CmdExportUnionLabel { get; }
        private void ExportUnionLabel() {
            if (CommonDialogService.SaveCSVFileDialog(out string outFilePath)) {
                // 로드
                List<LabelRecord> labels = new List<LabelRecord>();
                SortedSet<ImageRecord> images = new SortedSet<ImageRecord>();
                foreach (string inFilePath in FilesForUnionLabel) {
                    string basePath = Path.GetDirectoryName(inFilePath) ?? "";
                    IEnumerable<string> lines = File.ReadLines(inFilePath);
                    foreach (string line in lines) {
                        (ImageRecord? img, LabelRecord? lbl) = SerializationService.Deserialize(basePath, line, SettingService.Format);
                        if (img is object) {
                            if (lbl is object) labels.Add(lbl);
                            images.Add(img);
                        }
                    }
                }
                // 저장
                string saveBasePath = Path.GetDirectoryName(outFilePath) ?? "";
                using StreamWriter f = File.CreateText(outFilePath);
                ILookup<ImageRecord, LabelRecord> labelsByImage = labels.ToLookup(s => s.Image);
                foreach (ImageRecord i in images) {
                    IEnumerable<LabelRecord> labelsInImage = labelsByImage[i];
                    if (labelsInImage.Any()) {
                        // 양성 레이블
                        foreach (LabelRecord j in labelsInImage) f.WriteLine(SerializationService.SerializeAsPositive(saveBasePath, j, SettingService.Format));
                    } else {
                        // 음성 레이블
                        f.WriteLine(SerializationService.SerializeAsNegative(saveBasePath, i));
                    }
                }
            }
        }
        #endregion

        #region 레이블 분리
        public ICommand CmdSplitLabel { get; }
        private void SplitLabel() {
            if (!CommonDialogService.OpenCSVFileDialog(out string filePath)) return;
            Random r = new Random();
            List<LabelRecord> labels = new List<LabelRecord>();
            HashSet<ImageRecord> images = new HashSet<ImageRecord>();
            IEnumerable<string> lines = File.ReadLines(filePath);
            string basePath = Path.GetDirectoryName(filePath) ?? "";
            foreach (string line in lines) {
                (ImageRecord? img, LabelRecord? lbl) = SerializationService.Deserialize(basePath, line, SettingService.Format);
                if (img is object) {
                    if (lbl is object) labels.Add(lbl);
                    images.Add(img);
                }
            }
            List<ImageRecord> shuffledImages = images.OrderBy(s => r.Next()).ToList();
            ILookup<ImageRecord, LabelRecord> labelsByImage = labels.ToLookup(s => s.Image);
            switch (TacticForSplitLabel) {
            case TacticsForSplitLabel.DevideToNLabels:
                // 균등 분할
                if (NValueForSplitLabel < 2 || NValueForSplitLabel > images.Count) {
                    CommonDialogService.MessageBox("입력한 숫자가 올바르지 않습니다.");
                    return;
                }
                List<StreamWriter> files = new List<StreamWriter>();
                var infoByPartition = new List<(HashSet<ClassRecord> Classes, int ImagesCount)>();
                for (int i = 0; i < NValueForSplitLabel; i++) {
                    // 파일 이름: (원래 파일 이름).(파티션 번호 1부터 시작).(원래 확장자)
                    StreamWriter file = File.CreateText(Path.Combine($"{Path.GetDirectoryName(filePath)}", $"{Path.GetFileNameWithoutExtension(filePath)}.{i + 1}{Path.GetExtension(filePath)}"));
                    files.Add(file);
                    infoByPartition.Add((new HashSet<ClassRecord>(), 0));
                }
                foreach (ImageRecord image in shuffledImages) {
                    IEnumerable<LabelRecord> labelsInImage = labelsByImage[image];
                    int idx;
                    if (labelsInImage.Any()) {
                        // 양성 이미지인 경우.
                        // 분류 다양성이 증가하는 정도가 가장 높은 순 -> 파티션에 포함된 이미지 개수가 적은 순.
                        (idx, _, _) = infoByPartition.Select((s, idx) => (idx, s.ImagesCount, labelsInImage.Select(t => t.Class).Except(s.Classes).Count())).OrderByDescending(s => s.Item3)
                                                     .ThenBy(s => s.ImagesCount).ThenBy(s => r.Next()).First();
                        foreach (LabelRecord label in labelsInImage) files[idx].WriteLine(SerializationService.SerializeAsPositive(basePath, label, SettingService.Format));
                    } else {
                        // 음성 이미지인 경우.
                        // 파티션에 포함된 이미지 개수가 적은 순으로만 선택.
                        (idx, _) = infoByPartition.Select((s, idx) => (idx, s.ImagesCount)).OrderBy(s => s.ImagesCount)
                                                  .ThenBy(s => r.Next()).First();
                        files[idx].WriteLine(SerializationService.SerializeAsNegative(basePath, image));
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
                HashSet<ClassRecord> ClassesOriginal = new HashSet<ClassRecord>();
                HashSet<ClassRecord> ClassesSplit = new HashSet<ClassRecord>();
                foreach ((int idx, ImageRecord image) in shuffledImages.Select((img, idx) => (idx, img))) {
                    IEnumerable<LabelRecord> labelsInImage = labelsByImage[image];
                    int DiversityDeltaOriginal = labelsInImage.Select(s => s.Class).Except(ClassesOriginal).Count();
                    int DiversityDeltaSplit = labelsInImage.Select(s => s.Class).Except(ClassesSplit).Count();
                    if (images.Count - idx + ImageCountOfSplit <= NValueForSplitLabel || (ImageCountOfSplit < NValueForSplitLabel && DiversityDeltaSplit >= DiversityDeltaOriginal)) {
                        // 아래 두 경우 중 하나일시 해당 이미지를 추출 레이블에 씀
                        // 1. 남은 이미지 전부를 추출해야만 추출량 목표치를 채울 수 있는 경우
                        // 2. 아직 추출량 목표치가 남아 있으며, 분류 다양성이 증가하는 정도가 추출 레이블 쪽이 더 높거나 같은 경우
                        if (labelsInImage.Any()) foreach (LabelRecord label in labelsInImage) OutFileSplit.WriteLine(SerializationService.SerializeAsPositive(basePath, label, SettingService.Format));
                        else OutFileSplit.WriteLine(SerializationService.SerializeAsNegative(basePath, image));
                        ImageCountOfSplit++;
                        ClassesSplit.UnionWith(labelsInImage.Select(s => s.Class));
                    } else {
                        if (labelsInImage.Any()) foreach (LabelRecord label in labelsInImage) OutFileOriginal.WriteLine(SerializationService.SerializeAsPositive(basePath, label, SettingService.Format));
                        else OutFileOriginal.WriteLine(SerializationService.SerializeAsNegative(basePath, image));
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
                        IEnumerable<LabelRecord> labelsInImage = labelsByImage[image];
                        if (labelsInImage.Any()) foreach (LabelRecord label in labelsInImage) OutputFile.WriteLine(SerializationService.SerializeAsPositive(basePath, label, SettingService.Format));
                        else OutputFile.WriteLine(SerializationService.SerializeAsNegative(basePath, image));
                    }
                }
                break;
            }
        }
        #endregion

        #region 레이블 중복 제거
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
                        ProgressUndupeLabelValue = (int)((double)i / lines.Length * 20); // 0 to 20
                        (ImageRecord? img, LabelRecord? lbl) = SerializationService.Deserialize(basePath, lines[i], SettingService.Format);
                        if (img is object) {
                            if (lbl is object) LabelsForUndupe.Add(lbl);
                            ImagesForUndupe.Add(img);
                        }
                    }
                    ProgressUndupeLabelValue = 20;
                    // 중복 제거
                    int TotalSuppressedBoxesCount = 0;
                    var labelsByImageAndCategory = LabelsForUndupe.ToLookup(s => (s.Image, s.Class));
                    int TotalUniqueImageAndCategoryCount = labelsByImageAndCategory.Count;
                    foreach (var (idx, labelsInImage) in labelsByImageAndCategory.Select((s, idx) => (idx, s))) {
                        ProgressUndupeLabelValue = (int)((double)idx / TotalUniqueImageAndCategoryCount * 80 + 20); // 20 to 100
                        List<LabelRecord> sortedBySize = labelsInImage.OrderBy(s => s.Size).ToList(); // 넓이가 작은 경계 상자를 우선
                        int SuppressedBoxesCount = 0;
                        while (sortedBySize.Count >= 2) {
                            // pick
                            LabelRecord pick = sortedBySize[0];
                            sortedBySize.Remove(pick);
                            // compare
                            List<LabelRecord> labelsToSuppress = new List<LabelRecord>();
                            foreach (LabelRecord i in sortedBySize) {
                                double left = Math.Max(pick.Left, i.Left);
                                double top = Math.Max(pick.Top, i.Top);
                                double right = Math.Min(pick.Right, i.Right);
                                double bottom = Math.Min(pick.Bottom, i.Bottom);
                                if (left >= right || top >= bottom) continue;
                                double sizeIntersection = (right - left) * (bottom - top);
                                double sizeUnion = pick.Size + i.Size - sizeIntersection;
                                double iou = sizeIntersection / sizeUnion;
                                if (iou < IoUThreshold) continue;
                                labelsToSuppress.Add(i);
                            }
                            // suppress
                            foreach (LabelRecord i in labelsToSuppress) {
                                sortedBySize.Remove(i);
                                LabelsForUndupe.Remove(i);
                            }
                            SuppressedBoxesCount += labelsToSuppress.Count;
                            TotalSuppressedBoxesCount += labelsToSuppress.Count;
                        }
                        if (SuppressedBoxesCount > 0) AppendLogUndupeLabel($"다음 이미지에서 중복된 경계 상자가 {SuppressedBoxesCount}개 검출되었습니다: {labelsInImage.Key.Image.FullPath}");
                    }
                    ProgressUndupeLabelValue = 100;
                    if (TotalSuppressedBoxesCount == 0) AppendLogUndupeLabel("분석이 완료되었습니다. 중복된 경계 상자가 없습니다.");
                    else {
                        AppendLogUndupeLabel($"분석이 완료되었습니다. 중복된 경계 상자가 총 {TotalSuppressedBoxesCount}개 검출되었습니다.");
                    }
                });
            }
        }
        public ICommand CmdExportUndupedLabel { get; }
        private void ExportUndupeLabel() {
            if (LabelsForUndupe.Count == 0 && ImagesForUndupe.Count == 0) {
                CommonDialogService.MessageBox("레이블 중복 제거를 실행한 적이 없습니다.");
                return;
            }
            if (CommonDialogService.SaveCSVFileDialog(out string filePath)) {
                string basePath = Path.GetDirectoryName(filePath) ?? "";
                using StreamWriter f = File.CreateText(filePath);
                ILookup<ImageRecord, LabelRecord> labelsByImage = LabelsForUndupe.ToLookup(s => s.Image);
                foreach (ImageRecord i in ImagesForUndupe) {
                    IEnumerable<LabelRecord> labelsInImage = labelsByImage[i];
                    if (labelsInImage.Any()) {
                        // 양성 레이블
                        foreach (LabelRecord j in labelsInImage) f.WriteLine(SerializationService.SerializeAsPositive(basePath, j, SettingService.Format));
                    } else {
                        // 음성 레이블
                        f.WriteLine(SerializationService.SerializeAsNegative(basePath, i));
                    }
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
            LogVerifyLabel = LogVerifyLabel + string.Join(Environment.NewLine, logs) + Environment.NewLine;
        }
        private void AppendLogUndupeLabel(params string[] logs) {
            LogUndupeLabel = LogUndupeLabel + string.Join(Environment.NewLine, logs) + Environment.NewLine;
        }
        #endregion
    }
}
