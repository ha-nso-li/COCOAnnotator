using Prism.Mvvm;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media;

namespace COCOAnnotator.Records {
    public class CategoryRecord : BindableBase, IEquatable<CategoryRecord>, IComparable, IComparable<CategoryRecord> {
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
        public override bool Equals([NotNullWhen(true)] object? obj) {
            return obj switch {
                CategoryRecord obj_r => Equals(obj_r),
                _ => false
            };
        }
        public bool Equals([NotNullWhen(true)] CategoryRecord? other) {
            return other switch {
                CategoryRecord _ when All => other.All,
                CategoryRecord _ => Name.Equals(other.Name),
                _ => false
            };
        }
        public override int GetHashCode() {
            return All switch {
                true => true.GetHashCode(),
                false => Name.GetHashCode()
            };
        }
        public static bool operator ==(CategoryRecord record1, CategoryRecord record2) => record1.Equals(record2);
        public static bool operator !=(CategoryRecord record1, CategoryRecord record2) => !record1.Equals(record2);
        #endregion

        #region 비교
        int IComparable.CompareTo(object? obj) {
            return obj switch {
                CategoryRecord obj_r => CompareTo(obj_r),
                null => 1,
                _ => throw new ArgumentException(null, nameof(obj)),
            };
        }
        public int CompareTo(CategoryRecord? other) {
            return other switch {
                CategoryRecord _ when All && other.All => 0,
                CategoryRecord _ when All && !other.All => -1,
                CategoryRecord _ when !All && other.All => 1,
                CategoryRecord _ => Name.CompareTo(other.Name),
                null => 1
            };
        }
        public static bool operator <(CategoryRecord record1, CategoryRecord record2) => record1.CompareTo(record2) < 0;
        public static bool operator <=(CategoryRecord record1, CategoryRecord record2) => record1.CompareTo(record2) <= 0;
        public static bool operator >(CategoryRecord record1, CategoryRecord record2) => record1.CompareTo(record2) > 0;
        public static bool operator >=(CategoryRecord record1, CategoryRecord record2) => record1.CompareTo(record2) >= 0;
        #endregion

        private CategoryRecord(string Name, SolidColorBrush ColorBrush, bool All) {
            this.Name = Name;
            _ColorBrush = ColorBrush;
            this.All = All;
        }
        public override string ToString() => Name;

        public static CategoryRecord FromName(string Name) => new(Name, Brushes.Transparent, false);
        public static CategoryRecord AsAll() => new("(전체)", Brushes.Black, true);
    }
}
