using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LabelAnnotator.Services {
    public class PathService {
        /// <summary>
        /// <paramref name="fromPath"/>에서 <paramref name="toPath"/>로 가는 유닉스 호환 상대 경로를 찾습니다.
        /// </summary>
        public string GetRelativePath(string fromPath, string toPath) {
            string result = Path.GetRelativePath(fromPath, toPath);
            // 유닉스 호환 상대 경로로 변경
            result = result.Replace('\\', '/');
            return result;
        }

        /// <summary>
        /// 컬렉션에 포함된 모든 경로의 공통 부모 폴더의 경로를 찾습니다.
        /// </summary>
        public string GetCommonParentPath(IEnumerable<string> source) {
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
        /// 주어진 로컬 파일 경로를 이스케이프를 고려하여 URI로 변환합니다.
        /// </summary>
        public Uri FilePathToUri(string filePath) {
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
        public ISet<string> ApprovedImageExtension => new SortedSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".png", ".jpeg", ".tif" };
    }
}
