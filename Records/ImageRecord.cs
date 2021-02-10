using Prism.Mvvm;
using System;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media;

namespace COCOAnnotator.Records {
    public class ImageRecord : BindableBase, IEquatable<ImageRecord>, IComparable<ImageRecord>, IComparable {
        #region 프로퍼티
        public ObservableCollection<AnnotationRecord> Annotations { get; }
        public int Width { get; set; }
        public int Height { get; set; }
        private string _Path;
        public string Path {
            get => _Path;
            set => SetProperty(ref _Path, value);
        }
        public SolidColorBrush ColorBrush => Annotations.Count == 0 ? Brushes.DarkGray : Brushes.Black;
        #endregion

        public ImageRecord(string Path) : this(Path, 0, 0) { }

        public ImageRecord(string Path, int Width, int Height) {
            _Path = Path;
            Annotations = new ObservableCollection<AnnotationRecord>();
            Annotations.CollectionChanged += AnnotationCollectionChanged;
            this.Width = Width;
            this.Height = Height;
        }

        private void AnnotationCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
            if (e.Action == NotifyCollectionChangedAction.Replace || e.Action == NotifyCollectionChangedAction.Move) return;
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
                ImageRecord _ => Path.Equals(other.Path),
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
                ImageRecord _ => Path.CompareTo(other.Path),
                null => 1
            };
        }
        public static bool operator <(ImageRecord record1, ImageRecord record2) => record1.CompareTo(record2) < 0;
        public static bool operator <=(ImageRecord record1, ImageRecord record2) => record1.CompareTo(record2) <= 0;
        public static bool operator >(ImageRecord record1, ImageRecord record2) => record1.CompareTo(record2) > 0;
        public static bool operator >=(ImageRecord record1, ImageRecord record2) => record1.CompareTo(record2) >= 0;
        #endregion

        public override string ToString() => Path;

        public static ImageRecord Empty => new ImageRecord("");
    }
}
