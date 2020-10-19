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
    public class ManageDialogViewModel : Commons.DialogViewModelBase {
        #region 생성자
        public ManageDialogViewModel() {
            Title = "레이블 관리";

            _LogVerifyLabel = "";
            FilesForUnionLabel = new ObservableCollection<string>();
            _TacticForSplitLabel = TacticsForSplitLabel.DevideToNLabels;
            _NValueForSplitLabel = 2;
            _LogUndupeLabel = "";
            IoUThreshold = 0.5;

            CmdVerifyLabel = new DelegateCommand(VerifyLabel);
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

        public enum TacticsForSplitLabel {
            DevideToNLabels, TakeNSamples, SplitToSubFolders
        }

        #region 필드, 바인딩되지 않는 프로퍼티
        private readonly SortedDictionary<Records.ClassRecord, List<Records.LabelRecord>> PositiveLabelsByCategoryForVerify = new SortedDictionary<Records.ClassRecord, List<Records.LabelRecord>>();
        private readonly SortedSet<Records.ImageRecord> PositiveImagesForVerify = new SortedSet<Records.ImageRecord>();
        private readonly SortedSet<Records.ImageRecord> NegativeImagesForVerify = new SortedSet<Records.ImageRecord>();
        private readonly List<Records.LabelRecord> LabelsForUndupe = new List<Records.LabelRecord>();
        private readonly SortedSet<Records.ImageRecord> ImagesForUndupe = new SortedSet<Records.ImageRecord>();
        #endregion

        #region 바인딩되는 프로퍼티
        private string _LogVerifyLabel;
        public string LogVerifyLabel {
            get => _LogVerifyLabel;
            set {
                if (SetProperty(ref _LogVerifyLabel, value)) {
                    EventAggregator.GetEvent<Events.ScrollTxtLogVerifyLabel>().Publish();
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
                    EventAggregator.GetEvent<Events.ScrollTxtLogUndupeLabel>().Publish();
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
            bool imageSizeCheck = res.GetValueOrDefault();
            LogVerifyLabel = "";
            ProgressVerifyLabelValue = 0;
            Task.Run(() => {
                PositiveLabelsByCategoryForVerify.Clear();
                PositiveImagesForVerify.Clear();
                NegativeImagesForVerify.Clear();
                string basePath = Path.GetDirectoryName(filePath) ?? "";
                SortedSet<Records.ImageRecord> images = new SortedSet<Records.ImageRecord>(
                    Directory.EnumerateFiles(basePath, "*.*", SearchOption.AllDirectories).Where(s => PathService.ApprovedImageExtension.Contains(Path.GetExtension(s))).Select(s => new Records.ImageRecord(s))
                );
                string[] lines = File.ReadAllLines(filePath);
                AppendLogVerifyLabel($"{filePath}의 분석을 시작합니다.");
                for (int i = 0; i < lines.Length; i++) {
                    ProgressVerifyLabelValue = (int)((double)i / lines.Length * 100);
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;
                    (Records.ImageRecord? img, Records.LabelRecord? lbl) = SerializationService.Deserialize(basePath, lines[i], SettingService.Format);
                    if (img is null) {
                        AppendLogVerifyLabel($"{i + 1}번째 줄이 유효하지 않습니다. CSV 값 개수가 6개 미만이거나 좌표값이 정수 값이 아닙니다.");
                        continue;
                    }
                    // 참조된 이미지가 실존하는지 검사.
                    // 음성 샘플일 경우 레이블이 2개 이상 존재할 수 없으므로, 참조된 이미지가 실존하더라도 이미 음성 샘플로 검출된 적이 있는 이미지라면 안됨.
                    if (NegativeImagesForVerify.Contains(img)) {
                        AppendLogVerifyLabel($"{i + 1}번째 줄이 유효하지 않습니다. 한번 음성 샘플로 사용된 이미지에 대한 레이블이 또 발견되었습니다.");
                        continue;
                    }
                    if (!images.Contains(img) && !PositiveImagesForVerify.Contains(img)) {
                        AppendLogVerifyLabel($"{i + 1}번째 줄이 유효하지 않습니다. 이미지가 해당 경로에 실존하지 않습니다.");
                        continue;
                    }
                    if (lbl is null) {
                        // 음성 샘플
                        images.Remove(img);
                        NegativeImagesForVerify.Add(img);
                        continue;
                    }
                    // 경계 상자 위치 좌표가 위아래 혹은 좌우가 뒤집혀있는지 검사.
                    if (lbl.Left >= lbl.Right || lbl.Top >= lbl.Bottom) {
                        AppendLogVerifyLabel($"{i + 1}번째 줄이 유효하지 않습니다. 경계 상자의 좌표 값이 부적절합니다. 상하좌우가 뒤집혀 있는지 확인하세요.");
                        continue;
                    }
                    // 경계 상자 위치 좌표가 이미지 크기 밖으로 나가는지 검사.
                    if (imageSizeCheck) {
                        int width, height;
                        FileStream? stream = null;
                        try {
                            stream = new FileStream(img.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                            BitmapFrame bitmap = BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                            width = bitmap.PixelWidth;
                            height = bitmap.PixelHeight;
                        } catch (NotSupportedException) {
                            AppendLogVerifyLabel($"{i + 1}번째 줄이 유효하지 않습니다. 이미지가 손상되어 읽어올 수 없습니다.");
                            continue;
                        } finally {
                            stream?.Dispose();
                        }
                        if (lbl.Left < 0 || lbl.Top < 0 || lbl.Right > width || lbl.Bottom > height) {
                            AppendLogVerifyLabel($"{i + 1}번째 줄이 유효하지 않습니다. 경계 상자 좌표가 이미지의 크기 밖에 있습니다.");
                            continue;
                        }
                    } else {
                        if (lbl.Left < 0 || lbl.Top < 0) {
                            AppendLogVerifyLabel($"{i + 1}번째 줄이 유효하지 않습니다. 경계 상자 좌표가 이미지의 크기 밖에 있습니다.");
                            continue;
                        }
                    }
                    if (PositiveLabelsByCategoryForVerify.TryGetValue(lbl.Class, out List<Records.LabelRecord>? positiveLabelsInCategory)) {
                        // 완전히 동일한 레이블이 이미 존재하는지 검사
                        if (positiveLabelsInCategory.Contains(lbl)) {
                            AppendLogVerifyLabel($"{i + 1}번째 줄이 유효하지 않습니다. 동일한 레이블이 이미 존재합니다.");
                            continue;
                        } else {
                            // 유효한 양성 레이블. (같은 분류가 이미 있음)
                            images.Remove(img);
                            PositiveImagesForVerify.Add(img);
                            positiveLabelsInCategory.Add(lbl);
                        }
                    } else {
                        // 유효한 양성 레이블. (같은 분류가 없음)
                        images.Remove(img);
                        PositiveImagesForVerify.Add(img);
                        List<Records.LabelRecord> labels = new List<Records.LabelRecord> { lbl };
                        PositiveLabelsByCategoryForVerify.Add(lbl.Class, labels);
                    }
                }
                ProgressVerifyLabelValue = 100;
                if (images.Count > 0) {
                    IEnumerable<string> unusedImages = images.Select(s => s.ToString()).Take(21);
                    if (unusedImages.Any()) {
                        if (unusedImages.Count() > 20) {
                            AppendLogVerifyLabel($"경로내에 존재하지만 유효한 레이블에 사용되고 있지 않은 {images.Count}개의 이미지가 있습니다. 일부를 출력합니다.");
                            AppendLogVerifyLabel(unusedImages.Take(20).ToArray());
                            IEnumerable<string> directoriesContainUnusedImages = images.Select(s => Path.GetDirectoryName(s.ToString()) ?? "").Distinct().Take(11);
                            if (directoriesContainUnusedImages.Any()) {
                                int dirCount = directoriesContainUnusedImages.Count();
                                if (dirCount > 10) {
                                    AppendLogVerifyLabel($"위 이미지들이 존재하는 폴더는 {dirCount}종이 존재합니다. 일부를 출력합니다.");
                                    AppendLogVerifyLabel(directoriesContainUnusedImages.Take(10).ToArray());
                                } else {
                                    AppendLogVerifyLabel($"위 이미지들이 존재하는 폴더는 {dirCount}종이 존재합니다.");
                                    AppendLogVerifyLabel(directoriesContainUnusedImages.ToArray());
                                }
                            }
                        } else {
                            AppendLogVerifyLabel($"경로내에 존재하지만 유효한 레이블에 사용되고 있지 않은 {images.Count}개의 이미지가 있습니다.");
                            AppendLogVerifyLabel(unusedImages.ToArray());
                        }
                    }
                }
                AppendLogVerifyLabel(
                    "분석이 완료되었습니다.",
                    $"유효한 레이블 개수: {PositiveLabelsByCategoryForVerify.Sum(s => s.Value.Count) + NegativeImagesForVerify.Count}",
                    $"유효한 양성 레이블 개수: {PositiveLabelsByCategoryForVerify.Sum(s => s.Value.Count)}",
                    $"유효한 음성 레이블 개수: {NegativeImagesForVerify.Count}",
                    $"유효한 양성 레이블에 사용된 이미지 개수: {PositiveImagesForVerify.Count}",
                    $"유효한 레이블에 사용된 이미지 개수: {NegativeImagesForVerify.Count + PositiveImagesForVerify.Count}",
                    $"총 분류 개수: {PositiveLabelsByCategoryForVerify.Count}",
                    "분류 목록:"
                );
                AppendLogVerifyLabel(PositiveLabelsByCategoryForVerify.Select(s => $"분류 이름: {s.Key}, 양성 레이블 개수: {s.Value.Count}").ToArray());
            });
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
            List<Records.LabelRecord> positiveLabels = PositiveLabelsByCategoryForVerify.SelectMany(s => s.Value).ToList();
            foreach (IGrouping<Records.ImageRecord, Records.LabelRecord> i in positiveLabels.GroupBy(s => s.Image)) {
                foreach (Records.LabelRecord j in i) {
                    f.WriteLine(SerializationService.SerializePositive(PathService.GetRelativePath(saveBasePath, i.Key.FullPath), j, SettingService.Format));
                }
            }
            // 음성 레이블
            foreach (Records.ImageRecord i in NegativeImagesForVerify) {
                f.WriteLine(SerializationService.SerializeAsNegative(PathService.GetRelativePath(saveBasePath, i.FullPath)));
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
                List<Records.LabelRecord> labels = new List<Records.LabelRecord>();
                SortedSet<Records.ImageRecord> images = new SortedSet<Records.ImageRecord>();
                foreach (string inFilePath in FilesForUnionLabel) {
                    string basePath = Path.GetDirectoryName(inFilePath) ?? "";
                    IEnumerable<string> lines = File.ReadLines(inFilePath);
                    foreach (string line in lines) {
                        (Records.ImageRecord? img, Records.LabelRecord? lbl) = SerializationService.Deserialize(basePath, line, SettingService.Format);
                        if (img is object) {
                            if (lbl is object) labels.Add(lbl);
                            images.Add(img);
                        }
                    }
                }
                // 저장
                string saveBasePath = Path.GetDirectoryName(outFilePath) ?? "";
                using StreamWriter f = File.CreateText(outFilePath);
                ILookup<Records.ImageRecord, Records.LabelRecord> labelsByImage = labels.ToLookup(s => s.Image);
                foreach (Records.ImageRecord i in images) {
                    IEnumerable<Records.LabelRecord> labelsInImage = labelsByImage[i];
                    if (labelsInImage.Any()) {
                        // 양성 레이블
                        foreach (Records.LabelRecord j in labelsInImage) f.WriteLine(SerializationService.SerializePositive(PathService.GetRelativePath(saveBasePath, i.FullPath), j, SettingService.Format));
                    } else {
                        // 음성 레이블
                        f.WriteLine(SerializationService.SerializeAsNegative(PathService.GetRelativePath(saveBasePath, i.FullPath)));
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
            List<Records.LabelRecord> labels = new List<Records.LabelRecord>();
            HashSet<Records.ImageRecord> images = new HashSet<Records.ImageRecord>();
            IEnumerable<string> lines = File.ReadLines(filePath);
            string basePath = Path.GetDirectoryName(filePath) ?? "";
            foreach (string line in lines) {
                (Records.ImageRecord? img, Records.LabelRecord? lbl) = SerializationService.Deserialize(basePath, line, SettingService.Format);
                if (img is object) {
                    if (lbl is object) labels.Add(lbl);
                    images.Add(img);
                }
            }
            List<Records.ImageRecord> shuffledImages = images.OrderBy(s => r.Next()).ToList();
            ILookup<Records.ImageRecord, Records.LabelRecord> labelsByImage = labels.ToLookup(s => s.Image);
            switch (TacticForSplitLabel) {
                case TacticsForSplitLabel.DevideToNLabels:
                    // 균등 분할
                    if (NValueForSplitLabel < 2 || NValueForSplitLabel > images.Count) {
                        CommonDialogService.MessageBox("입력한 숫자가 올바르지 않습니다.");
                        return;
                    }
                    List<StreamWriter> files = new List<StreamWriter>();
                    var infoByPartition = new List<(HashSet<Records.ClassRecord> Classes, int ImagesCount)>();
                    for (int i = 0; i < NValueForSplitLabel; i++) {
                        // 파일 이름: (원래 파일 이름).(파티션 번호 1부터 시작).(원래 확장자)
                        StreamWriter file = File.CreateText(Path.Combine($"{Path.GetDirectoryName(filePath)}", $"{Path.GetFileNameWithoutExtension(filePath)}.{i + 1}{Path.GetExtension(filePath)}"));
                        files.Add(file);
                        infoByPartition.Add((new HashSet<Records.ClassRecord>(), 0));
                    }
                    foreach (Records.ImageRecord image in shuffledImages) {
                        IEnumerable<Records.LabelRecord> labelsInImage = labelsByImage[image];
                        int idx;
                        if (labelsInImage.Any()) {
                            // 양성 이미지인 경우.
                            // 분류 다양성이 증가하는 정도가 가장 높은 순 -> 파티션에 포함된 이미지 갯수가 적은 순.
                            (idx, _, _) = infoByPartition.Select((s, idx) => (idx, s.ImagesCount, labelsInImage.Select(t => t.Class).Except(s.Classes).Count()))
                                                         .OrderByDescending(s => s.Item3).ThenBy(s => s.ImagesCount).ThenBy(s => r.Next()).First();
                            foreach (Records.LabelRecord label in labelsInImage) files[idx].WriteLine(SerializationService.SerializePositive(PathService.GetRelativePath(basePath, image.FullPath), label, SettingService.Format));
                        } else {
                            // 음성 이미지인 경우.
                            // 파티션에 포함된 이미지 갯수가 적은 순으로만 선택.
                            (idx, _) = infoByPartition.Select((s, idx) => (idx, s.ImagesCount)).OrderBy(s => s.ImagesCount)
                                                      .ThenBy(s => r.Next()).First();
                            files[idx].WriteLine(SerializationService.SerializeAsNegative(PathService.GetRelativePath(basePath, image.FullPath)));
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
                case TacticsForSplitLabel.TakeNSamples:
                    // 일부 추출
                    if (NValueForSplitLabel < 1 || NValueForSplitLabel >= images.Count) {
                        CommonDialogService.MessageBox("입력한 숫자가 올바르지 않습니다.");
                        return;
                    }
                    {
                        using StreamWriter OutFileOriginal = File.CreateText(Path.Combine($"{Path.GetDirectoryName(filePath)}", $"{Path.GetFileNameWithoutExtension(filePath)}.1{Path.GetExtension(filePath)}"));
                        using StreamWriter OutFileSplit = File.CreateText(Path.Combine($"{Path.GetDirectoryName(filePath)}", $"{Path.GetFileNameWithoutExtension(filePath)}.2{Path.GetExtension(filePath)}"));
                        int ImageCountOfSplit = 0;
                        HashSet<Records.ClassRecord> ClassesOriginal = new HashSet<Records.ClassRecord>();
                        HashSet<Records.ClassRecord> ClassesSplit = new HashSet<Records.ClassRecord>();
                        foreach ((int idx, Records.ImageRecord image) in shuffledImages.Select((img, idx) => (idx, img))) {
                            IEnumerable<Records.LabelRecord> labelsInImage = labelsByImage[image];
                            int DiversityDeltaOriginal = labelsInImage.Select(s => s.Class).Except(ClassesOriginal).Count();
                            int DiversityDeltaSplit = labelsInImage.Select(s => s.Class).Except(ClassesSplit).Count();
                            if (images.Count - idx + ImageCountOfSplit <= NValueForSplitLabel || (ImageCountOfSplit < NValueForSplitLabel && DiversityDeltaSplit >= 1 && DiversityDeltaSplit >= DiversityDeltaOriginal)) {
                                // 아래 두 경우 중 하나일시 해당 이미지를 추출 레이블에 씀
                                // 1. 남은 이미지 전부를 추출해야만 추출량 목표치를 채울 수 있는 경우
                                // 2. 아직 추출량 목표치가 남아 있으며, 분류 다양성이 증가하며, 그 정도가 추출 레이블 쪽이 더 높거나 같은 경우
                                if (labelsInImage.Any()) foreach (Records.LabelRecord label in labelsInImage) OutFileSplit.WriteLine(SerializationService.SerializePositive(PathService.GetRelativePath(basePath, image.FullPath), label, SettingService.Format));
                                else OutFileSplit.WriteLine(SerializationService.SerializeAsNegative(PathService.GetRelativePath(basePath, image.FullPath)));
                                ImageCountOfSplit++;
                                ClassesSplit.UnionWith(labelsInImage.Select(s => s.Class));
                            } else {
                                if (labelsInImage.Any()) foreach (Records.LabelRecord label in labelsInImage) OutFileOriginal.WriteLine(SerializationService.SerializePositive(PathService.GetRelativePath(basePath, image.FullPath), label, SettingService.Format));
                                else OutFileOriginal.WriteLine(SerializationService.SerializeAsNegative(PathService.GetRelativePath(basePath, image.FullPath)));
                                ClassesOriginal.UnionWith(labelsInImage.Select(s => s.Class));
                            }
                        }
                    }
                    break;
                case TacticsForSplitLabel.SplitToSubFolders:
                    // 하위 폴더로 분할
                    IEnumerable<IGrouping<string, Records.ImageRecord>> imagesByDir = images.GroupBy(s => Path.GetDirectoryName(s.FullPath) ?? "");
                    foreach (IGrouping<string, Records.ImageRecord> imagesInDir in imagesByDir) {
                        string TargetDir = Path.Combine(Path.GetDirectoryName(filePath) ?? "", imagesInDir.Key);
                        // 파일 이름: (원래 파일 이름).(최종 폴더 이름).(원래 확장자)
                        using StreamWriter OutputFile = File.CreateText(Path.Combine(TargetDir, $"{Path.GetFileNameWithoutExtension(filePath)}.{Path.GetFileName(imagesInDir.Key)}{Path.GetExtension(filePath)}"));
                        foreach (Records.ImageRecord image in imagesInDir) {
                            IEnumerable<Records.LabelRecord> labelsInImage = labelsByImage[image];
                            if (labelsInImage.Any()) foreach (Records.LabelRecord label in labelsInImage) OutputFile.WriteLine(SerializationService.SerializePositive(PathService.GetRelativePath(basePath, image.FullPath), label, SettingService.Format));
                            else OutputFile.WriteLine(SerializationService.SerializeAsNegative(PathService.GetRelativePath(basePath, image.FullPath)));
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
                        (Records.ImageRecord? img, Records.LabelRecord? lbl) = SerializationService.Deserialize(basePath, lines[i], SettingService.Format);
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
                        List<Records.LabelRecord> sortedBySize = labelsInImage.OrderBy(s => s.Size).ToList(); // 넓이가 작은 경계 상자를 우선
                        int SuppressedBoxesCount = 0;
                        while (sortedBySize.Count >= 2) {
                            // pick
                            Records.LabelRecord pick = sortedBySize[0];
                            sortedBySize.Remove(pick);
                            // compare
                            List<Records.LabelRecord> labelsToSuppress = new List<Records.LabelRecord>();
                            foreach (Records.LabelRecord i in sortedBySize) {
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
                            foreach (Records.LabelRecord i in labelsToSuppress) {
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
                ILookup<Records.ImageRecord, Records.LabelRecord> labelsByImage = LabelsForUndupe.ToLookup(s => s.Image);
                foreach (Records.ImageRecord i in ImagesForUndupe) {
                    IEnumerable<Records.LabelRecord> labelsInImage = labelsByImage[i];
                    if (labelsInImage.Any()) {
                        // 양성 레이블
                        foreach (Records.LabelRecord j in labelsInImage) f.WriteLine(SerializationService.SerializePositive(PathService.GetRelativePath(basePath, i.FullPath), j, SettingService.Format));
                    } else {
                        // 음성 레이블
                        f.WriteLine(SerializationService.SerializeAsNegative(PathService.GetRelativePath(basePath, i.FullPath)));
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
