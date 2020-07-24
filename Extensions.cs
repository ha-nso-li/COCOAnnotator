using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Media;

namespace LabelAnnotator {
    public static class Extensions {
        /// <summary>
        /// <paramref name="fromPath"/>에서 <paramref name="toPath"/>로 가는 유닉스 호환 상대 경로를 찾습니다.
        /// </summary>
        public static string GetRelativePath(string fromPath, string toPath) {
            // Windows 경로로 변경
            fromPath = fromPath.Replace("/", "\\");
            toPath = toPath.Replace("/", "\\");

            int fromAttr = GetPathAttribute(fromPath);
            int toAttr = GetPathAttribute(toPath);

            StringBuilder path = new StringBuilder(260); // MAX_PATH
            if (PathRelativePathTo(path, fromPath, fromAttr, toPath, toAttr) == 0) {
                throw new ArgumentException("Paths must have a common prefix");
            }
            // 유닉스 호환 상대 경로로 변경
            if (path.ToString(0, 2) == ".\\") path.Remove(0, 2);
            path.Replace("\\", "/");
            return path.ToString();
        }

        private const int FILE_ATTRIBUTE_DIRECTORY = 0x10;
        private const int FILE_ATTRIBUTE_NORMAL = 0x80;

        private static int GetPathAttribute(string path) {
            if (Directory.Exists(path)) return FILE_ATTRIBUTE_DIRECTORY;
            else return FILE_ATTRIBUTE_NORMAL;
        }

        [DllImport("shlwapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern int PathRelativePathTo(StringBuilder pszPath, string pszFrom, int dwAttrFrom, string pszTo, int dwAttrTo);

        /// <summary>
        /// 컬렉션에 포함된 모든 경로의 공통 부모 폴더의 경로를 찾습니다.
        /// </summary>
        public static string GetCommonParentPath(IEnumerable<string> source) {
            using IEnumerator<string> etor = source.GetEnumerator();
            if (!etor.MoveNext()) return "";
            string first = etor.Current;
            int len = first.Length;
            while (etor.MoveNext()) {
                string current = etor.Current;
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
        /// 주어진 로컬 파일 경로를 이스케이프를 고려하여 URI로 변환합니다.
        /// </summary>
        public static Uri FilePathToUri(string filePath) {
            StringBuilder uri = new StringBuilder();
            foreach (char v in filePath) {
                if ((v >= 'a' && v <= 'z') || (v >= 'A' && v <= 'Z') || (v >= '0' && v <= '9') || v == '+' || v == '/' || v == ':' || v == '.' || v == '-' || v == '_' || v == '~' || v > '\xFF') {
                    uri.Append(v);
                } else if (v == Path.DirectorySeparatorChar || v == Path.AltDirectorySeparatorChar) {
                    uri.Append('/');
                } else {
                    uri.Append($"%{(int)v:X2}");
                }
            }
            if (uri.Length >= 2 && uri[0] == '/' && uri[1] == '/') // UNC path
                uri.Insert(0, "file:");
            else
                uri.Insert(0, "file:///");
            return new Uri(uri.ToString());
        }
        /// <summary>
        /// 이 애플리케이션에서 이미지로서 허용하는 확장자의 집합을 제공합니다.
        /// </summary>
        public static ISet<string> ApprovedImageExtension => new SortedSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".png", ".jpeg", ".tif" };

        /// <summary>
        /// 기본 경로와 레이블 파일의 한 행 내의 문자열을 이용해 이미지와 레이블 레코드를 역직렬화합니다.
        /// </summary>
        /// <returns>
        /// <list type="bullet">
        /// <item><description>둘다 <see langword="null"/>이면 역직렬화 실패를 의미합니다.</description></item>
        /// <item><description><seealso cref="LabelRecord"/>만 <see langword="null"/>이면 음성 샘플임을 의미합니다.</description></item>
        /// </list>
        /// </returns>
        public static (ImageRecord?, LabelRecord?) DeserializeRecords(string BasePath, string Text) {
            string[] split = Text.Split(',');
            if (split.Length < 6) return (null, null);
            string path = Path.Combine(BasePath, split[0]);
            path = path.Replace('/', '\\');
            ImageRecord img = new ImageRecord(path);
            string classname = split[5];
            if (string.IsNullOrWhiteSpace(classname)) return (img, null);
            bool success = double.TryParse(split[1], out double num1);
            success &= double.TryParse(split[2], out double num2);
            success &= double.TryParse(split[3], out double num3);
            success &= double.TryParse(split[4], out double num4);
            if (!success) return (null, null);
            switch (SettingManager.Format) {
                case "LTRB":
                    return (img, new LabelRecord(img, num1, num2, num3, num4, ClassRecord.FromName(classname)));
                case "XYWH":
                    // num1 = x, num2 = y, num3 = w, num4 = h
                    double left = num1 - num3 / 2;
                    double right = num1 + num3 / 2;
                    double top = num2 - num4 / 2;
                    double bottom = num2 + num4 / 2;
                    return (img, new LabelRecord(img, left, top, right, bottom, ClassRecord.FromName(classname)));
                default:
                    return (null, null);
            }
        }

        public static double GetColorDistance(this Color color1, Color color2) {
            double rmean = (color1.R + color2.R) / 2.0;
            double rdelta = Math.Pow(color1.R - color2.R, 2);
            double gdelta = Math.Pow(color1.G - color2.G, 2);
            double bdelta = Math.Pow(color1.B - color2.B, 2);
            return Math.Sqrt((512 + rmean) * rdelta / 256 + 4 * gdelta + (767 - rmean) * bdelta / 256);
        }
    }
}