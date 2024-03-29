using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;

namespace COCOAnnotator.Services.Utilities {
    /// <summary>사소한 정적 유틸 메서드를 포함하는 서비스입니다.</summary>
    public static class Miscellaneous {
        /// <summary>주어진 두 값을 비교하고 작아야 하는 값이 더 크면 두 값을 교환합니다.</summary>
        /// <param name="ShouldSmaller">교환 후에 다른 한 쪽에 비해 작거나 같아야 할 값입니다.</param>
        /// <param name="ShouldBigger">교환 후에 다른 한 쪽에 비해 크거나 같아야 할 값입니다.</param>
        public static void SortTwoValues<T>(ref T ShouldSmaller, ref T ShouldBigger) where T : IComparable<T> {
            if (ShouldSmaller.CompareTo(ShouldBigger) > 0) (ShouldSmaller, ShouldBigger) = (ShouldBigger, ShouldSmaller);
        }

        /// <summary>미리 정의된 HSV 기반의 색깔 생성 방법에 따라 주어진 개수 만큼의 색을 생성합니다.</summary>
        public static IEnumerable<Color> GenerateFixedColor(int ColorCount) {
            int TotalValue = ColorCount <= 20 ? 1 : 2;
            int TotalHue = (int)Math.Ceiling((double)ColorCount / TotalValue);
            double StepHue = 360d / TotalHue;
            int CurrentCount = 0;
            for (int h = 0; h < TotalHue; h++) {
                for (int v = 0; v < TotalValue; v++) {
                    yield return ColorFromHSV(StepHue * h, 1, 1.0 - 0.3 * v);
                    CurrentCount++;
                    if (CurrentCount >= ColorCount) yield break;
                }
            }
        }

        /// <summary>HSV 색상을 RGB 색상으로 변환합니다.</summary>
        /// <param name="hue">색상입니다. 0에서 360까지의 범위를 갖습니다.</param>
        /// <param name="saturation">채도입니다. 0에서 1까지의 범위를 갖습니다.</param>
        /// <param name="value">명도입니다. 0에서 1까지의 범위를 갖습니다.</param>
        private static Color ColorFromHSV(double hue, double saturation, double value) {
            int hi = (int)Math.Floor(hue / 60) % 6;
            double f = hue / 60 - Math.Truncate(hue / 60);

            byte v = (byte)Math.Round(value * 255);
            byte p = (byte)Math.Round(value * 255 * (1 - saturation));
            byte q = (byte)Math.Round(value * 255 * (1 - f * saturation));
            byte t = (byte)Math.Round(value * 255 * (1 - (1 - f) * saturation));

            return hi switch {
                0 => Color.FromRgb(v, t, p),
                1 => Color.FromRgb(q, v, p),
                2 => Color.FromRgb(p, v, t),
                3 => Color.FromRgb(p, q, v),
                4 => Color.FromRgb(t, p, v),
                _ => Color.FromRgb(v, p, q),
            };
        }

        /// <summary>주어진 모든 색과의 색차가 주어진 값보다 같거나 큰 새로운 색 하나를 무작위로 생성합니다.</summary>
        /// <returns>100회 시도 내에 색의 생성에 실패한 경우 <see langword="null"/>을 반환합니다.</returns>
        public static Color? GenerateRandomColor(IEnumerable<Color> ExistingColors, double ColorDifferenceThreshold) {
            Span<byte> buffer = stackalloc byte[3];
            Color newColor;
            for (int i = 0; i < 100; i++) {
                Random.Shared.NextBytes(buffer);
                newColor = Color.FromRgb(buffer[0], buffer[1], buffer[2]);
                if (ExistingColors.All(s => GetColorDifference(newColor, s) >= ColorDifferenceThreshold)) return newColor;
            }
            return null;
        }

        /// <summary>주어진 두 색의 색차를 구합니다.</summary>
        private static double GetColorDifference(Color color1, Color color2) {
            double rmean = (color1.R + color2.R) / 2.0;
            double rdelta = Math.Pow(color1.R - color2.R, 2);
            double gdelta = Math.Pow(color1.G - color2.G, 2);
            double bdelta = Math.Pow(color1.B - color2.B, 2);
            return Math.Sqrt((2 + rmean / 256) * rdelta + 4 * gdelta + (2 + (255 - rmean) / 256) * bdelta);
        }

        /// <summary>파일을 지정한 경로로 복사합니다. 복사할 경로의 폴더가 존재하지 않으면 생성합니다.</summary>
        public static void CopyFile(string FromPath, string ToPath) {
            string? ToDirectory = Path.GetDirectoryName(ToPath);
            if (ToDirectory is not null) Directory.CreateDirectory(ToDirectory);
            File.Copy(FromPath, ToPath);
        }
    }
}
