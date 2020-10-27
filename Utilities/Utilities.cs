using System;
using System.Collections.Generic;
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
        /// 주어진 개수 만큼의 서로 다른 색깔을 생성합니다.
        /// </summary>
        public static IEnumerable<Color> GenerateColor(int ColorCount) {
            int TotalSaturation = (int)Math.Ceiling(ColorCount / 60d);
            int TotalHue = ColorCount / TotalSaturation;
            double StepSaturation = 1d / TotalSaturation;
            double StepHue = 360d / TotalHue;

            for (int s = 0; s < TotalSaturation; s++) {
                for (int h = 0; h < TotalHue; h++) {
                    yield return ColorFromHSV(StepHue * h, 1d - StepSaturation * s, 1.0);
                }
            }
        }

        /// <summary>HSV 색상을 RGB 색상으로 변환합니다.</summary>
        /// <param name="hue">색상입니다. 0에서 360까지의 범위를 갖습니다.</param>
        /// <param name="saturation">채도입니다. 0에서 1까지의 범위를 갖습니다.</param>
        /// <param name="value">명도입니다. 0에서 1까지의 범위를 갖습니다.</param>
        /// <returns></returns>
        private static Color ColorFromHSV(double hue, double saturation, double value) {
            int hi = (int)Math.Floor(hue / 60) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            byte v = (byte)(value * 255);
            byte p = (byte)(v * (1 - saturation));
            byte q = (byte)(v * (1 - f * saturation));
            byte t = (byte)(v * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                return Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                return Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                return Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                return Color.FromArgb(255, t, p, v);
            else
                return Color.FromArgb(255, v, p, q);
        }
    }
}
