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
            switch (Format) {
            case SettingFormats.LTRB: {
                int l = (int)Math.Floor(Label.Left);
                int t = (int)Math.Floor(Label.Top);
                int r = (int)Math.Ceiling(Label.Right);
                int b = (int)Math.Ceiling(Label.Bottom);
                return $"{path},{l},{t},{r},{b},{Label.Class}";
            }
            case SettingFormats.CXCYWH: {
                double x = (Label.Left + Label.Right) / 2;
                double y = (Label.Top + Label.Bottom) / 2;
                double w = Label.Right - Label.Left;
                double h = Label.Bottom - Label.Top;
                return $"{path},{x:0.#},{y:0.#},{w:0.#},{h:0.#},{Label.Class}";
            }
            case SettingFormats.LTWH: {
                int l = (int)Math.Floor(Label.Left);
                int t = (int)Math.Floor(Label.Top);
                int r = (int)Math.Ceiling(Label.Right);
                int b = (int)Math.Ceiling(Label.Bottom);
                int w = r - l;
                int h = b - t;
                return $"{path},{l},{t},{w},{h},{Label.Class}";
            }
            default:
                return "";
            }
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
            switch (Format) {
            case SettingFormats.LTRB:
                return (img, new LabelRecord(img, num1, num2, num3, num4, ClassRecord.FromName(classname)));
            case SettingFormats.CXCYWH: {
                double l = num1 - num3 / 2;
                double r = num1 + num3 / 2;
                double t = num2 - num4 / 2;
                double b = num2 + num4 / 2;
                return (img, new LabelRecord(img, l, t, r, b, ClassRecord.FromName(classname)));
            }
            case SettingFormats.LTWH: {
                double r = num1 + num3;
                double b = num2 + num4;
                return (img, new LabelRecord(img, num1, num2, r, b, ClassRecord.FromName(classname)));
            }
            default:
                return (null, null);
            }
        }
    }
}
