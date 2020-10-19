using Prism.Mvvm;
using System;
using System.Windows.Media;

namespace LabelAnnotator.Records {

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
