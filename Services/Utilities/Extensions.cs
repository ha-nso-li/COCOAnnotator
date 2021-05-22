using COCOAnnotator.Records;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace COCOAnnotator.Services.Utilities {
    /// <summary>
    /// 각종 확장 메서드를 포함하는 유틸 클래스입니다.
    /// </summary>
    public static class Extensions {
        /// <summary>
        /// 주어진 로컬 파일 경로를 이스케이프를 고려하여 URI로 변환합니다.
        /// </summary>
        public static Uri ToUri(this string filePath) {
            StringBuilder uri = new();
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
            return new(uri.ToString());
        }

        public static bool LoadSize(this ImageRecord Image, string BasePath) {
            using FileStream stream = new(Path.Combine(BasePath, Image.Path), FileMode.Open, FileAccess.Read, FileShare.Read);
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
    }
}
