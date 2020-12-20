using COCOAnnotator.Records;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace COCOAnnotator.Utilities {
    /// <summary>
    /// 각종 편의성 메서드를 포함하는 정적 클래스입니다.
    /// </summary>
    public static class Utils {
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
        /// 미리 정의된 HSV 기반의 색깔 생성 방법에 따라 주어진 개수 만큼의 색을 생성합니다.
        /// </summary>
        public static IEnumerable<Color> GenerateFixedColor(int ColorCount) {
            int TotalSaturation = (int)Math.Ceiling(ColorCount / 40d);
            int TotalHue, TotalValue;
            if ((double)ColorCount / TotalSaturation >= 10) {
                TotalHue = (int)Math.Ceiling((double)ColorCount / TotalSaturation / 2);
                TotalValue = 2;
            } else {
                TotalHue = (int)Math.Ceiling((double)ColorCount / TotalSaturation);
                TotalValue = 1;
            }
            double StepSaturation = 1d / TotalSaturation;
            double StepHue = 360d / TotalHue;
            int CurrentCount = 0;
            for (int h = 0; h < TotalHue; h++) {
                for (int s = 0; s < TotalSaturation; s++) {
                    for (int v = 0; v < TotalValue; v++) {
                        yield return ColorFromHSV(StepHue * h, 1d - StepSaturation * s, 1.0 - 0.3 * v);
                        CurrentCount++;
                        if (CurrentCount >= ColorCount) yield break;
                    }
                }
            }
        }

        /// <summary>HSV 색상을 RGB 색상으로 변환합니다.</summary>
        /// <param name="hue">색상입니다. 0에서 360까지의 범위를 갖습니다.</param>
        /// <param name="saturation">채도입니다. 0에서 1까지의 범위를 갖습니다.</param>
        /// <param name="value">명도입니다. 0에서 1까지의 범위를 갖습니다.</param>
        private static Color ColorFromHSV(double hue, double saturation, double value) {
            int hi = (int)Math.Floor(hue / 60) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            byte v = (byte)(value * 255);
            byte p = (byte)(v * (1 - saturation));
            byte q = (byte)(v * (1 - f * saturation));
            byte t = (byte)(v * (1 - (1 - f) * saturation));

            return hi switch {
                0 => Color.FromArgb(255, v, t, p),
                1 => Color.FromArgb(255, q, v, p),
                2 => Color.FromArgb(255, p, v, t),
                3 => Color.FromArgb(255, p, q, v),
                4 => Color.FromArgb(255, t, p, v),
                _ => Color.FromArgb(255, v, p, q)
            };
        }

        /// <summary>
        /// 주어진 모든 색과의 색차가 주어진 값보다 같거나 큰 새로운 색 하나를 생성합니다.
        /// </summary>
        public static Color GenerateRandomColor(IEnumerable<Color> ExistingColors, double ColorDifferenceThreshold) {
            Random random = new Random();
            while (true) {
                Color newColor = Color.FromRgb((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256));
                if (ExistingColors.All(s => GetColorDifference(newColor, s) >= ColorDifferenceThreshold)) return newColor;
            }
        }

        /// <summary>
        /// 주어진 두 색의 색차를 구합니다.
        /// </summary>
        private static double GetColorDifference(Color color1, Color color2) {
            double rmean = (color1.R + color2.R) / 2.0;
            double rdelta = Math.Pow(color1.R - color2.R, 2);
            double gdelta = Math.Pow(color1.G - color2.G, 2);
            double bdelta = Math.Pow(color1.B - color2.B, 2);
            return Math.Sqrt((512 + rmean) * rdelta / 256 + 4 * gdelta + (767 - rmean) * bdelta / 256);
        }

        /// <summary>
        /// <paramref name="fromPath"/>에서 <paramref name="toPath"/>로 가는 유닉스 호환 상대 경로를 찾습니다.
        /// </summary>
        public static string GetRelativePath(string fromPath, string toPath) {
            string result = Path.GetRelativePath(fromPath, toPath);
            // 유닉스 호환 상대 경로로 변경
            result = result.Replace('\\', '/');
            return result;
        }

        /// <summary>
        /// 컬렉션에 포함된 모든 이미지들이 위치한 경로의 공통 부모 폴더의 경로를 찾습니다.
        /// </summary>
        public static string GetCommonParentPath(IEnumerable<ImageRecord> source) {
            using IEnumerator<ImageRecord> etor = source.GetEnumerator();
            if (!etor.MoveNext()) return "";
            string first = etor.Current.FullPath;
            int len = first.Length;
            while (etor.MoveNext()) {
                string current = etor.Current.FullPath;
                len = Math.Min(len, current.Length);
                for (int i = 0; i < len; i++) {
                    if (current[i] != first[i]) {
                        len = i;
                        break;
                    }
                }
            }
            string prefix = first.Substring(0, len);
            return prefix.Substring(0, prefix.LastIndexOfAny(new char[] { '\\', '/' }));
        }

        /// <summary>
        /// 주어진 로컬 파일 경로를 이스케이프를 고려하여 URI로 변환합니다.
        /// </summary>
        public static Uri ToUri(this string filePath) {
            StringBuilder uri = new StringBuilder();
            foreach (char v in filePath) {
                if ((v >= 'a' && v <= 'z') || (v >= 'A' && v <= 'Z') || (v >= '0' && v <= '9') || v == '+' || v == '/' || v == ':' || v == '.' || v == '-' || v == '_' || v == '~' || v > '\xFF') {
                    uri.Append(v);
                } else if (v == Path.DirectorySeparatorChar || v == Path.AltDirectorySeparatorChar) {
                    uri.Append('/');
                } else {
                    uri.Append($"%{Convert.ToByte(v):X2}");
                }
            }
            if (uri.Length >= 2 && uri[0] == '/' && uri[1] == '/') uri.Insert(0, "file:");
            else uri.Insert(0, "file:///");
            return new Uri(uri.ToString());
        }

        /// <summary>
        /// 이 애플리케이션에서 이미지로서 허용하는 확장자의 집합을 제공합니다.
        /// </summary>
        public static ISet<string> ApprovedImageExtensions => new SortedSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".png", ".jpeg", ".tif" };

        public static (int Width, int Height) GetSizeOfImage(string ImagePath) {
            using FileStream stream = new FileStream(ImagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            BitmapFrame bitmap = BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
            return (bitmap.PixelWidth, bitmap.PixelHeight);
        }
    }
}
