using Prism.Mvvm;
using System;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media;

namespace COCOAnnotator.Records {
    public sealed class ImageRecord : BindableBase, IEquatable<ImageRecord>, IComparable<ImageRecord>, IComparable {
        #region 프로퍼티
        public ObservableCollection<AnnotationRecord> Annotations { get; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Path { get; }
        public SolidColorBrush ColorBrush => Annotations.Count == 0 ? Brushes.DarkGray : Brushes.Black;
        #endregion

        public ImageRecord() : this("", 0, 0) { }

        public ImageRecord(string Path) : this(Path, 0, 0) { }

        public ImageRecord(string Path, int Width, int Height) {
            this.Path = Path;
            Annotations = new();
            Annotations.CollectionChanged += AnnotationCollectionChanged;
            this.Width = Width;
            this.Height = Height;
        }

        private void AnnotationCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
            if (e.Action is NotifyCollectionChangedAction.Replace or NotifyCollectionChangedAction.Move) return;
            RaisePropertyChanged(nameof(ColorBrush));
        }

        #region 동일성
        public override bool Equals([NotNullWhen(true)] object? obj) {
            return obj switch {
                ImageRecord obj_r => Equals(obj_r),
                _ => false
            };
        }
        public bool Equals([NotNullWhen(true)] ImageRecord? other) {
            return other switch {
                ImageRecord => Path.Equals(other.Path, StringComparison.Ordinal),
                _ => false
            };
        }
        public override int GetHashCode() => Path.GetHashCode();
        public static bool operator ==(ImageRecord record1, ImageRecord record2) => record1.Equals(record2);
        public static bool operator !=(ImageRecord record1, ImageRecord record2) => !record1.Equals(record2);
        #endregion

        #region 비교
        int IComparable.CompareTo(object? obj) {
            return obj switch {
                ImageRecord obj_r => CompareTo(obj_r),
                null => 1,
                _ => throw new ArgumentException(null, nameof(obj))
            };
        }
        public int CompareTo(ImageRecord? other) {
            return other switch {
                ImageRecord => string.Compare(Path, other.Path, StringComparison.Ordinal),
                _ => 1
            };
        }
        public static bool operator <(ImageRecord record1, ImageRecord record2) => record1.CompareTo(record2) < 0;
        public static bool operator <=(ImageRecord record1, ImageRecord record2) => record1.CompareTo(record2) <= 0;
        public static bool operator >(ImageRecord record1, ImageRecord record2) => record1.CompareTo(record2) > 0;
        public static bool operator >=(ImageRecord record1, ImageRecord record2) => record1.CompareTo(record2) >= 0;
        #endregion

        public override string ToString() => Path;
    }
}
