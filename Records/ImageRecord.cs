using Prism.Mvvm;
using System;

namespace LabelAnnotator.Records {
    public class ImageRecord : BindableBase, IEquatable<ImageRecord>, IComparable<ImageRecord>, IComparable {
        #region 프로퍼티
        public string FullPath { get; }
        #endregion

        #region 바인딩 전용 프로퍼티
        private string _DisplayFilename;
        public string DisplayFilename {
            get => _DisplayFilename;
            set => SetProperty(ref _DisplayFilename, value);
        }
        #endregion

        public ImageRecord(string FullPath) {
            this.FullPath = FullPath;
            _DisplayFilename = "";
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
                _ => throw new ArgumentException()
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
    }
}
