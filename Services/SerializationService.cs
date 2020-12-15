using LabelAnnotator.Records;
using LabelAnnotator.Records.Enums;
using LabelAnnotator.Utilities;
using System;
using System.IO;

namespace LabelAnnotator.Services {
    public class SerializationService {
        /// <summary>주어진 레이블을 직렬화합니다.</summary>
        /// <param name="BasePath">레이블 파일이 위치한 경로입니다. 이미지의 상대 경로 계산에 사용됩니다.</param>
        public string SerializeAsPositive(string BasePath, LabelRecord Label, SettingFormats Format) {
            string path = Utils.GetRelativePath(BasePath, Label.Image.FullPath);
            return Format switch {
                SettingFormats.LTRB => $"{path},{Label.Left:0.#},{Label.Top:0.#},{Label.Left + Label.Width:0.#},{Label.Top + Label.Height:0.#},{Label.Class:0.#}",
                SettingFormats.CXCYWH => $"{path},{Label.Left + Label.Width / 2:0.#},{Label.Top + Label.Height / 2:0.#},{Label.Width:0.#},{Label.Height:0.#},{Label.Class:0.#}",
                SettingFormats.LTWH => $"{path},{Label.Left:0.#},{Label.Top:0.#},{Label.Width:0.#},{Label.Height:0.#},{Label.Class:0.#}",
                _ => ""
            };
        }

        /// <summary>주어진 이미지를 음성 샘플로 간주하여 직렬화합니다.</summary>
        /// <param name="BasePath">레이블 파일이 위치한 경로입니다. 이미지의 상대 경로 계산에 사용됩니다.</param>
        public string SerializeAsNegative(string BasePath, ImageRecord Image) => $"{Utils.GetRelativePath(BasePath, Image.FullPath)},,,,,";

        /// <summary>
        /// 기본 경로와 레이블 파일의 한 행 내의 문자열을 이용해 이미지와 레이블 레코드를 역직렬화합니다.
        /// </summary>
        /// <returns>
        /// <list type="bullet">
        /// <item><description>둘다 <see langword="null"/>이면 역직렬화 실패를 의미합니다.</description></item>
        /// <item><description><seealso cref="LabelRecord"/>만 <see langword="null"/>이면 음성 샘플임을 의미합니다.</description></item>
        /// </list>
        /// </returns>
        public (ImageRecord?, LabelRecord?) Deserialize(string BasePath, string Text, SettingFormats Format) {
            string[] split = Text.Split(',');
            if (split.Length < 6) return (null, null);
            string path = Path.Combine(BasePath, split[0]);
            path = Path.GetFullPath(path).Replace('/', '\\');
            ImageRecord img = new ImageRecord(path);
            string classname = split[5];
            if (string.IsNullOrWhiteSpace(classname)) return (img, null);
            bool success = double.TryParse(split[1], out double num1);
            success &= double.TryParse(split[2], out double num2);
            success &= double.TryParse(split[3], out double num3);
            success &= double.TryParse(split[4], out double num4);
            if (!success) return (null, null);
            return Format switch {
                SettingFormats.LTRB => (img, new LabelRecord(img, num1, num2, num3 - num1, num4 - num2, ClassRecord.FromName(classname))),
                SettingFormats.CXCYWH => (img, new LabelRecord(img, num1 - num3 / 2, num2 - num4 / 2, num3, num4, ClassRecord.FromName(classname))),
                SettingFormats.LTWH => (img, new LabelRecord(img, num1, num2, num3, num4, ClassRecord.FromName(classname))),
                _ => (null, null)
            };
        }
    }
}
