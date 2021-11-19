using COCOAnnotator.Records;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace COCOAnnotator.Services.Utilities {
    /// <summary>각종 확장 메서드를 포함하는 유틸 클래스입니다.</summary>
    public static class Extensions {
        /// <summary>컬렉션에 포함된 모든 이미지들이 위치한 경로의 공통 부모 폴더의 경로를 찾습니다.</summary>
        public static string GetCommonParentPath(this IEnumerable<ImageRecord> source) {
            using IEnumerator<ImageRecord> etor = source.GetEnumerator();
            if (!etor.MoveNext()) return "";
            string first = etor.Current.Path;
            int len = first.Length;
            while (etor.MoveNext()) {
                string current = etor.Current.Path;
                len = Math.Min(len, current.Length);
                for (int i = 0; i < len; i++) {
                    if (current[i] != first[i]) {
                        len = i;
                        break;
                    }
                }
            }
            string prefix = first[..len];
            return prefix[..prefix.LastIndexOfAny(new[] { '\\', '/' })];
        }

        /// <summary>주어진 로컬 파일 경로를 이스케이프를 고려하여 URI로 변환합니다.</summary>
        public static Uri ToUri(this string filePath) {
            StringBuilder uri = new();
            foreach (char v in filePath) {
                if (v is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '+' or ':' or '.' or '-' or '_' or '~' or > '\xFF') {
                    uri.Append(v);
                } else if (v is '/' or '\\') {
                    uri.Append('/');
                } else {
                    uri.Append($"%{Convert.ToByte(v):X2}");
                }
            }
            if (uri.Length >= 2 && uri[0] == '/' && uri[1] == '/') uri.Insert(0, "file:");
            else uri.Insert(0, "file:///");
            return new(uri.ToString());
        }

        public static string NormalizePath(this string FilePath) => FilePath.Replace('\\', '/');

        /// <summary>이 <seealso cref="ImageRecord"/>에 해당하는 이미지의 실제 크기를 읽어와서 갱신합니다.</summary>
        /// <returns><seealso cref="ImageRecord"/>가 가지고 있었던 크기 값과 실제 크기값이 똑같았으면 <see langword="false"/>, 아니면 <see langword="true"/>입니다.</returns>
        /// <exception cref="NotSupportedException">이미지의 크기를 읽어오는데 실패한 경우 발생하는 예외입니다.</exception>
        public static bool LoadSize(this ImageRecord Image, string BasePath) {
            using FileStream stream = File.OpenRead(Path.Combine(BasePath, Image.Path));
            BitmapFrame bitmap = BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
            int oldWidth = Image.Width;
            int oldHeight = Image.Height;
            Image.Width = bitmap.PixelWidth;
            Image.Height = bitmap.PixelHeight;
            return oldWidth != Image.Width || oldHeight != Image.Height;
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source) {
            Random rng = new();
            T[] elements = source.ToArray();
            for (int i = elements.Length - 1; i >= 0; i--) {
                int j = rng.Next(i + 1);
                yield return elements[j];
                elements[j] = elements[i];
            }
        }

        public static IEnumerable<(int Index, T Element)> Enumerate<T>(this IEnumerable<T> source) => source.Select((s, idx) => (idx, s));
    }
}
