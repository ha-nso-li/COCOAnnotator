using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using WinForm = System.Windows.Forms;

namespace LabelAnnotator {
    public class ManageWindowViewModel : BindableBase {
        #region 생성자
        public ManageWindowViewModel(ManageWindow View) {
            this.View = View;
            _LogVerifyLabel = "";
            FilesForUnionLabel = new ObservableCollection<string>();
            TacticForSplitLabel = TacticsForSplitLabel.DevideToNLabels;
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
        public ManageWindow View { get; }
        private readonly SortedDictionary<ClassRecord, List<LabelRecord>> PositiveLabelsByCategoryForVerify = new SortedDictionary<ClassRecord, List<LabelRecord>>();
        private readonly SortedSet<ImageRecord> PositiveImagesForVerify = new SortedSet<ImageRecord>();
        private readonly SortedSet<ImageRecord> NegativeImagesForVerify = new SortedSet<ImageRecord>();
        private readonly List<LabelRecord> LabelsForUndupe = new List<LabelRecord>();
        private readonly SortedSet<ImageRecord> ImagesForUndupe = new SortedSet<ImageRecord>();
        #endregion

        #region 바인딩되는 프로퍼티
        private string _LogVerifyLabel;
        public string LogVerifyLabel {
            get => _LogVerifyLabel;
            set {
                if (SetProperty(ref _LogVerifyLabel, value)) {
                    View.Dispatcher.Invoke(View.TxtLogVerifyLabel.ScrollToEnd);
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
            set {
                if (SetProperty(ref _TacticForSplitLabel, value)) {
                    View.TxtNValueForSplitLabel.IsEnabled = value != TacticsForSplitLabel.SplitToSubFolders;
                }
            }
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
                    View.Dispatcher.Invoke(View.TxtLogUndupeLabel.ScrollToEnd);
                }
            }
        }
        #endregion

        #region 커맨드
        #region 레이블 분석
        public ICommand CmdVerifyLabel { get; }
        private void VerifyLabel() {
            OpenFileDialog dlg = new OpenFileDialog {
                Filter = "CSV 파일|*.csv",
                Multiselect = false
            };
            if (!dlg.ShowDialog().GetValueOrDefault()) return;
            MessageBoxResult res_msg = MessageBox.Show(
                "검증을 시작합니다. 이미지 크기 검사를 하기 원하시면 예 아니면 아니오를 선택하세요. 이미지 크기 검사시 데이터셋 크기에 따라 시간이 오래 걸릴 수 있습니다.", "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (res_msg == MessageBoxResult.Cancel) return;
            bool imageSizeCheck = res_msg == MessageBoxResult.Yes;
            string fileName = dlg.FileName;
            LogVerifyLabel = "";
            Task.Run(() => {
                PositiveLabelsByCategoryForVerify.Clear();
                PositiveImagesForVerify.Clear();
                NegativeImagesForVerify.Clear();
                string basePath = Path.GetDirectoryName(fileName) ?? "";
                SortedSet<ImageRecord> images = new SortedSet<ImageRecord>(
                    Directory.EnumerateFiles(basePath, "*.*", SearchOption.AllDirectories).Where(s => Extensions.ApprovedImageExtension.Contains(Path.GetExtension(s))).Select(s => new ImageRecord(s))
                );
                string[] lines = File.ReadAllLines(fileName);
                ProgressVerifyLabelValue = 0;
                AppendLogVerifyLabel($"{fileName}의 분석을 시작합니다.");
                for (int i = 0; i < lines.Length; i++) {
                    ProgressVerifyLabelValue = (int)((double)i / lines.Length * 100);
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;
                    (ImageRecord? img, LabelRecord? lbl) = Extensions.DeserializeRecords(basePath, lines[i]);
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
                    if (PositiveLabelsByCategoryForVerify.TryGetValue(lbl.Class, out List<LabelRecord>? positiveLabelsInCategory)) {
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
                        List<LabelRecord> labels = new List<LabelRecord> { lbl };
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
                MessageBox.Show("레이블 파일을 분석한 적 없거나 분석한 레이블 파일 내에 유효 레이블이 없습니다.");
                return;
            }
            SaveFileDialog dlg = new SaveFileDialog {
                Filter = "CSV 파일|*.csv",
                DefaultExt = ".csv"
            };
            if (!dlg.ShowDialog().GetValueOrDefault()) return;
            MessageBox.Show("분석한 레이블 파일의 내용 중 유효한 레이블만 내보냅니다.");
            string saveBasePath = Path.GetDirectoryName(dlg.FileName) ?? "";
            using StreamWriter f = File.CreateText(dlg.FileName);
            // 양성 레이블
            List<LabelRecord> positiveLabels = PositiveLabelsByCategoryForVerify.SelectMany(s => s.Value).ToList();
            foreach (IGrouping<ImageRecord, LabelRecord> i in positiveLabels.GroupBy(s => s.Image)) {
                foreach (LabelRecord j in i) {
                    f.WriteLine(j.Serialize(saveBasePath));
                }
            }
            // 음성 레이블
            foreach (ImageRecord i in NegativeImagesForVerify) {
                f.WriteLine(i.SerializeAsNegative(saveBasePath));
            }
        }
        #endregion

        #region 레이블 병합
        public ICommand CmdAddFileForUnionLabel { get; }
        private void AddFileForUnionLabel() {
            OpenFileDialog dlg = new OpenFileDialog {
                Filter = "CSV 파일|*.csv",
                Multiselect = true
            };
            if (dlg.ShowDialog().GetValueOrDefault()) {
                foreach (string FileName in dlg.FileNames) {
                    FilesForUnionLabel.Add(FileName);
                }
            }
        }
        public ICommand CmdAddFolderForUnionLabel { get; }
        private void AddFolderForUnionLabel() {
            MessageBox.Show("선택한 폴더 아래에 존재하는 모든 csv 파일을 재귀적으로 탐색하여 목록에 추가합니다.");
            WinForm.FolderBrowserDialog dlg = new WinForm.FolderBrowserDialog();
            if (dlg.ShowDialog() == WinForm.DialogResult.OK) {
                foreach (string i in Directory.EnumerateFiles(dlg.SelectedPath, "*.csv", SearchOption.AllDirectories)) FilesForUnionLabel.Add(i);
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
            SaveFileDialog dlg = new SaveFileDialog {
                Filter = "CSV 파일|*.csv",
                DefaultExt = ".csv"
            };
            if (dlg.ShowDialog().GetValueOrDefault()) {
                // 로드
                List<LabelRecord> labels = new List<LabelRecord>();
                SortedSet<ImageRecord> images = new SortedSet<ImageRecord>();
                foreach (string labelPath in FilesForUnionLabel) {
                    string basePath = Path.GetDirectoryName(labelPath) ?? "";
                    IEnumerable<string> lines = File.ReadLines(labelPath);
                    foreach (string line in lines) {
                        (ImageRecord? img, LabelRecord? lbl) = Extensions.DeserializeRecords(basePath, line);
                        if (img is object) {
                            if (lbl is object) labels.Add(lbl);
                            images.Add(img);
                        }
                    }
                }
                // 저장
                string saveBasePath = Path.GetDirectoryName(dlg.FileName) ?? "";
                using StreamWriter f = File.CreateText(dlg.FileName);
                ILookup<ImageRecord, LabelRecord> labelsByImage = labels.ToLookup(s => s.Image);
                foreach (ImageRecord i in images) {
                    IEnumerable<LabelRecord> labelsInImage = labelsByImage[i];
                    if (labelsInImage.Any()) {
                        // 양성 레이블
                        foreach (LabelRecord j in labelsInImage) f.WriteLine(j.Serialize(saveBasePath));
                    } else {
                        // 음성 레이블
                        f.WriteLine(i.SerializeAsNegative(saveBasePath));
                    }
                }
            }
        }
        #endregion

        #region 레이블 분리
        public ICommand CmdSplitLabel { get; }
        private void SplitLabel() {
            OpenFileDialog dlg = new OpenFileDialog {
                Filter = "CSV 파일|*.csv",
            };
            if (!dlg.ShowDialog().GetValueOrDefault()) return;
            Random r = new Random();
            List<LabelRecord> labels = new List<LabelRecord>();
            HashSet<ImageRecord> images = new HashSet<ImageRecord>();
            IEnumerable<string> lines = File.ReadLines(dlg.FileName);
            string basePath = Path.GetDirectoryName(dlg.FileName) ?? "";
            foreach (string line in lines) {
                (ImageRecord? img, LabelRecord? lbl) = Extensions.DeserializeRecords(basePath, line);
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
                        MessageBox.Show("입력한 숫자가 올바르지 않습니다.", "", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    List<StreamWriter> files = new List<StreamWriter>();
                    for (int i = 0; i < NValueForSplitLabel; i++) {
                        // 파일 이름: (원래 파일 이름).(파티션 번호 1부터 시작).(원래 확장자)
                        StreamWriter file = File.CreateText(Path.Combine($"{Path.GetDirectoryName(dlg.FileName)}", $"{Path.GetFileNameWithoutExtension(dlg.FileName)}.{i + 1}{Path.GetExtension(dlg.FileName)}"));
                        files.Add(file);
                    }
                    foreach ((int idx, ImageRecord image) in shuffledImages.Select((img, idx) => (idx, img))) {
                        IEnumerable<LabelRecord> labelsInImage = labelsByImage[image];
                        if (labelsInImage.Any()) foreach (LabelRecord label in labelsInImage) files[idx % NValueForSplitLabel].WriteLine(label.Serialize(basePath));
                        else files[idx % NValueForSplitLabel].WriteLine(image.SerializeAsNegative(basePath));
                    }
                    foreach (StreamWriter file in files) {
                        file.Dispose();
                    }
                    break;
                case TacticsForSplitLabel.TakeNSamples:
                    // 일부 추출
                    if (NValueForSplitLabel < 1 || NValueForSplitLabel >= images.Count) {
                        MessageBox.Show("입력한 숫자가 올바르지 않습니다.", "", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    {
                        using StreamWriter OutFileOriginal = File.CreateText(Path.Combine($"{Path.GetDirectoryName(dlg.FileName)}", $"{Path.GetFileNameWithoutExtension(dlg.FileName)}.1{Path.GetExtension(dlg.FileName)}"));
                        using StreamWriter OutFileTook = File.CreateText(Path.Combine($"{Path.GetDirectoryName(dlg.FileName)}", $"{Path.GetFileNameWithoutExtension(dlg.FileName)}.2{Path.GetExtension(dlg.FileName)}"));
                        foreach ((int idx, ImageRecord image) in shuffledImages.Select((img, idx) => (idx, img))) {
                            IEnumerable<LabelRecord> labelsInImage = labelsByImage[image];
                            if (idx < NValueForSplitLabel) {
                                if (labelsInImage.Any()) foreach (LabelRecord label in labelsInImage) OutFileTook.WriteLine(label.Serialize(basePath));
                                else OutFileTook.WriteLine(image.SerializeAsNegative(basePath));
                            } else {
                                if (labelsInImage.Any()) foreach (LabelRecord label in labelsInImage) OutFileOriginal.WriteLine(label.Serialize(basePath));
                                else OutFileOriginal.WriteLine(image.SerializeAsNegative(basePath));
                            }
                        }
                    }
                    break;
                case TacticsForSplitLabel.SplitToSubFolders:
                    // 하위 폴더로 분할
                    IEnumerable<IGrouping<string, ImageRecord>> imagesByDir = images.GroupBy(s => Path.GetDirectoryName(s.FullPath) ?? "");
                    foreach (IGrouping<string, ImageRecord> imagesInDir in imagesByDir) {
                        string TargetDir = Path.Combine(Path.GetDirectoryName(dlg.FileName) ?? "", imagesInDir.Key);
                        // 파일 이름: (원래 파일 이름).(최종 폴더 이름).(원래 확장자)
                        using StreamWriter OutputFile = File.CreateText(Path.Combine(TargetDir, $"{Path.GetFileNameWithoutExtension(dlg.FileName)}.{Path.GetFileName(imagesInDir.Key)}{Path.GetExtension(dlg.FileName)}"));
                        foreach (ImageRecord image in imagesInDir) {
                            IEnumerable<LabelRecord> labelsInImage = labelsByImage[image];
                            if (labelsInImage.Any()) foreach (LabelRecord label in labelsInImage) OutputFile.WriteLine(label.Serialize(basePath));
                            else OutputFile.WriteLine(image.SerializeAsNegative(basePath));
                        }
                    }
                    break;
            }
        }
        #endregion

        #region 레이블 중복 제거
        public ICommand CmdUndupeLabel { get; }
        private void UndupeLabel() {
            OpenFileDialog dlg = new OpenFileDialog {
                Filter = "CSV 파일|*.csv",
                DefaultExt = ".csv"
            };
            if (dlg.ShowDialog().GetValueOrDefault()) {
                LogUndupeLabel = "";
                AppendLogNMSLabel($"{dlg.FileName}에서 위치, 크기가 유사한 중복 경계상자를 제거합니다.");
                // 로드
                LabelsForUndupe.Clear();
                ImagesForUndupe.Clear();
                string basePath = Path.GetDirectoryName(dlg.FileName) ?? "";
                IEnumerable<string> lines = File.ReadLines(dlg.FileName);
                foreach (string line in lines) {
                    (ImageRecord? img, LabelRecord? lbl) = Extensions.DeserializeRecords(basePath, line);
                    if (img is object) {
                        if (lbl is object) LabelsForUndupe.Add(lbl);
                        ImagesForUndupe.Add(img);
                    }
                }
                // 중복 제거
                int TotalSuppressedBoxesCount = 0;
                var labelsByImageAndCategory = LabelsForUndupe.ToLookup(s => (s.Image, s.Class));
                foreach (var labelsInImage in labelsByImageAndCategory) {
                    // 넓이가 작은 경계 상자를 우선
                    List<LabelRecord> sortedBySize = labelsInImage.OrderBy(s => s.Size).ToList();
                    int SuppressedBoxesCount = 0;
                    while (sortedBySize.Count > 0) {
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
                    if (SuppressedBoxesCount > 0) AppendLogNMSLabel($"다음 이미지에서 중복된 경계 상자가 {SuppressedBoxesCount}개 검출되었습니다: {labelsInImage.Key.Image.FullPath}");
                }
                if (TotalSuppressedBoxesCount == 0) AppendLogNMSLabel("분석이 완료되었습니다. 중복된 경계 상자가 없습니다.");
                else {
                    AppendLogNMSLabel($"분석이 완료되었습니다. 중복된 경계 상자가 총 {TotalSuppressedBoxesCount}개 검출되었습니다.");
                }
            }
        }
        public ICommand CmdExportUndupedLabel { get; }
        private void ExportUndupeLabel() {
            if (LabelsForUndupe.Count == 0 && ImagesForUndupe.Count == 0) {
                MessageBox.Show("레이블 중복 제거를 실행한 적이 없습니다.");
                return;
            }
            SaveFileDialog dlg = new SaveFileDialog {
                Filter = "CSV 파일|*.csv",
                DefaultExt = ".csv"
            };
            if (dlg.ShowDialog().GetValueOrDefault()) {
                string basePath = Path.GetDirectoryName(dlg.FileName) ?? "";
                using StreamWriter f = File.CreateText(dlg.FileName);
                ILookup<ImageRecord, LabelRecord> labelsByImage = LabelsForUndupe.ToLookup(s => s.Image);
                foreach (ImageRecord i in ImagesForUndupe) {
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
        #endregion

        public ICommand CmdClose { get; }
        private void Close() {
            View.Close();
        }
        #endregion

        #region 프라이빗 메서드
        private void AppendLogVerifyLabel(params string[] logs) {
            LogVerifyLabel = LogVerifyLabel + string.Join(Environment.NewLine, logs) + Environment.NewLine;
        }
        private void AppendLogNMSLabel(params string[] logs) {
            LogUndupeLabel = LogUndupeLabel + string.Join(Environment.NewLine, logs) + Environment.NewLine;
        }
        #endregion
    }
}
