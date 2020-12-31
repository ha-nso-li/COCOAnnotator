using COCOAnnotator.Utilities;
using Prism.Mvvm;
using System;
using System.Collections.Specialized;
using System.Windows.Media;

namespace COCOAnnotator.Records {
    public class ImageRecord : BindableBase, IEquatable<ImageRecord>, IComparable<ImageRecord>, IComparable {
        #region 프로퍼티
        public string FullPath { get; }
        public FastObservableCollection<AnnotationRecord> Annotations { get; }
        public int Width { get; set; }
        public int Height { get; set; }
        #endregion

        #region 바인딩 전용 프로퍼티
        private string _DisplayFilename;
        public string DisplayFilename {
            get => _DisplayFilename;
            set => SetProperty(ref _DisplayFilename, value);
        }
        public SolidColorBrush ColorBrush => Annotations.Count == 0 ? Brushes.DarkGray : Brushes.Black;
        #endregion

        public ImageRecord(string FullPath) : this(FullPath, 0, 0) { }

        public ImageRecord(string FullPath, int Width, int Height) {
            this.FullPath = FullPath;
            _DisplayFilename = "";
            Annotations = new FastObservableCollection<AnnotationRecord>();
            Annotations.CollectionChanged += AnnotationCollectionChanged;
            this.Width = Width;
            this.Height = Height;
        }

        private void AnnotationCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
            if (e.Action == NotifyCollectionChangedAction.Replace || e.Action == NotifyCollectionChangedAction.Move) return;
            RaisePropertyChanged(nameof(ColorBrush));
        }

        #region 동일성
        public override bool Equals(object? obj) {
            return obj switch {
                ImageRecord obj_r => Equals(obj_r),
                _ => false
            };
        }
        public bool Equals(ImageRecord? other) {
            return other switch {
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
            return obj switch {
                ImageRecord obj_r => CompareTo(obj_r),
                null => 1,
                _ => throw new ArgumentException(null, nameof(obj))
            };
        }
        public int CompareTo(ImageRecord? other) {
            return other switch {
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

        public static ImageRecord Empty => new ImageRecord("");
    }
}
