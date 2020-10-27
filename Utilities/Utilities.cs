using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace LabelAnnotator.Utilities {
    /// <summary>
    /// 별도의 서비스로 기술하기에는 미미한 수준의 편의성 메서드를 포함하는 정적 클래스입니다.
    /// </summary>
    public static class Miscellaneous {
        /// <summary>
        /// 주어진 두 값을 비교하고 작아야 하는 값이 더 크면 두 값을 교환합니다.
        /// </summary>
        public static void SortTwoValues<T>(ref T ShouldSmaller, ref T ShouldBigger) where T : IComparable<T> {
            Comparer<T> comparer = Comparer<T>.Default;
            if (comparer.Compare(ShouldSmaller, ShouldBigger) > 0) {
                T temp = ShouldBigger;
                ShouldBigger = ShouldSmaller;
                ShouldSmaller = temp;
            }
        }

        /// <summary>
        /// 주어진 모든 색과의 색차가 주어진 값보다 같거나 큰 새로운 색을 생성합니다.
        /// </summary>
        public static Color GenerateColor(IEnumerable<Color> ExistingColors, double ColorDifferenceThreshold) {
            Random random = new Random();
            while (true) {
                Color newColor = Color.FromRgb((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256));
                if (ExistingColors.All(s => GetColorDifference(newColor, s) >= ColorDifferenceThreshold)) {
                    return newColor;
                }
            }
        }

        private static double GetColorDifference(Color color1, Color color2) {
            double rmean = (color1.R + color2.R) / 2.0;
            double rdelta = Math.Pow(color1.R - color2.R, 2);
            double gdelta = Math.Pow(color1.G - color2.G, 2);
            double bdelta = Math.Pow(color1.B - color2.B, 2);
            return Math.Sqrt((512 + rmean) * rdelta / 256 + 4 * gdelta + (767 - rmean) * bdelta / 256);
        }
    }
}
