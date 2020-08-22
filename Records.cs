using Prism.Mvvm;
using System;
using System.Windows.Media;

namespace LabelAnnotator {
    public class LabelRecord {
        public ImageRecord Image { get; set; }

        public double Left { get; set; }
        public double Top { get; set; }
        public double Right { get; set; }
        public double Bottom { get; set; }
        public ClassRecord Class { get; set; }

        public LabelRecord(ImageRecord Image, double Left, double Top, double Right, double Bottom, ClassRecord Classname) {
            this.Image = Image;
            this.Left = Left;
            this.Top = Top;
            this.Right = Right;
            this.Bottom = Bottom;
            Class = Classname;
        }

        public override string ToString() => $"{Class} ({Left},{Top},{Right},{Bottom})";

        public string Serialize(string BasePath) {
            string path = Extensions.GetRelativePath(BasePath, Image.FullPath);
            switch (SettingManager.Format) {
                case "LTRB":
                    return $"{path},{Math.Floor(Left):0},{Math.Floor(Top):0},{Math.Ceiling(Right):0},{Math.Ceiling(Bottom):0},{Class}";
                case "XYWH":
                    double x = (Left + Right) / 2;
                    double y = (Top + Bottom) / 2;
                    double w = Right - Left;
                    double h = Bottom - Top;
                    return $"{path},{x:0.#},{y:0.#},{w:0.#},{h:0.#},{Class}";
                default:
                    return "";
            }
        }
    }

    public class ImageRecord : BindableBase, IEquatable<ImageRecord>, IComparable<ImageRecord>, IComparable {
        #region 프로퍼티
        public string FullPath { get; }
        private string _CommonPath;
        public string CommonPath {
            get => _CommonPath;
            set {
                if (SetProperty(ref _CommonPath, value)) {
                    RaisePropertyChanged(nameof(DisplayFilename));
                }
            }
        }
        #endregion

        #region 바인딩 전용 프로퍼티
        public string DisplayFilename {
            get {
                try {
                    return Extensions.GetRelativePath(_CommonPath, FullPath);
                } catch (ArgumentException) {
                    return FullPath;
                }
            }
        }
        #endregion

        public ImageRecord(string FullPath) {
            this.FullPath = FullPath;
            _CommonPath = "";
        }

        #region 동일성
        public override bool Equals(object? obj) {
            return obj switch
            {
                ImageRecord obj_r => Equals(obj_r),
                _ => false
            };
        }
        public bool Equals(ImageRecord? other) {
            return other switch
            {
                ImageRecord _ => FullPath.Equals(other.FullPath),
                _ => false
            };
        }
        public override int GetHashCode() => FullPath.GetHashCode();
        public static bool operator ==(ImageRecord record1, ImageRecord record2) => record1.Equals(record2);
        public static bool operator !=(ImageRecord record1, ImageRecord record2) => !record1.Equals(record2);
        #endregion

        #region 비교
        public int CompareTo(object? obj) {
            return obj switch
            {
                ImageRecord obj_r => CompareTo(obj_r),
                null => 1,
                _ => throw new ArgumentException()
            };
        }
        public int CompareTo(ImageRecord? other) {
            return other switch
            {
                ImageRecord _ => FullPath.CompareTo(other.FullPath),
                null => 1
            };
        }
        public static bool operator <(ImageRecord record1, ImageRecord record2) => record1.CompareTo(record2) < 0;
        public static bool operator <=(ImageRecord record1, ImageRecord record2) => record1.CompareTo(record2) <= 0;
        public static bool operator >(ImageRecord record1, ImageRecord record2) => record1.CompareTo(record2) > 0;
        public static bool operator >=(ImageRecord record1, ImageRecord record2) => record1.CompareTo(record2) >= 0;
        #endregion

        public override string ToString() => FullPath;

        public string SerializeAsNegative(string basePath) => $"{Extensions.GetRelativePath(basePath, FullPath)},,,,,";
    }

    public class ClassRecord : BindableBase, IEquatable<ClassRecord>, IComparable, IComparable<ClassRecord> {
        #region 필드와 프로퍼티
        public bool All { get; }
        public string Name { get; }
        private SolidColorBrush _ColorBrush;
        public SolidColorBrush ColorBrush {
            get => _ColorBrush;
            set => SetProperty(ref _ColorBrush, value);
        }
        #endregion

        #region 동일성
        public override bool Equals(object? obj) {
            return obj switch
            {
                ClassRecord obj_r => Equals(obj_r),
                _ => false
            };
        }
        public bool Equals(ClassRecord? other) {
            return other switch
            {
                ClassRecord _ when All => other.All,
                ClassRecord _ => Name.Equals(other.Name),
                _ => false
            };
        }
        public override int GetHashCode() {
            return All switch
            {
                true => true.GetHashCode(),
                false => Name.GetHashCode()
            };
        }
        public static bool operator ==(ClassRecord record1, ClassRecord record2) => record1.Equals(record2);
        public static bool operator !=(ClassRecord record1, ClassRecord record2) => !record1.Equals(record2);
        #endregion

        #region 비교
        public int CompareTo(object? obj) {
            return obj switch
            {
                ImageRecord obj_r => CompareTo(obj_r),
                null => 1,
                _ => throw new ArgumentException()
            };
        }
        public int CompareTo(ClassRecord? other) {
            return other switch
            {
                ClassRecord _ when All && other.All => 0,
                ClassRecord _ when All && !other.All => -1,
                ClassRecord _ when !All && other.All => 1,
                ClassRecord _ => Name.CompareTo(other.Name),
                null => 1
            };
        }
        public static bool operator <(ClassRecord record1, ClassRecord record2) => record1.CompareTo(record2) < 0;
        public static bool operator <=(ClassRecord record1, ClassRecord record2) => record1.CompareTo(record2) <= 0;
        public static bool operator >(ClassRecord record1, ClassRecord record2) => record1.CompareTo(record2) > 0;
        public static bool operator >=(ClassRecord record1, ClassRecord record2) => record1.CompareTo(record2) >= 0;
        #endregion

        private ClassRecord(string Name, SolidColorBrush ColorBrush, bool All) {
            this.Name = Name;
            _ColorBrush = ColorBrush;
            this.All = All;
        }
        public override string ToString() => Name;

        public static ClassRecord FromName(string Name) => new ClassRecord(Name, Brushes.Black, false);
        public static ClassRecord AllLabel() => new ClassRecord("(전체)", Brushes.Black, true);
    }
}
