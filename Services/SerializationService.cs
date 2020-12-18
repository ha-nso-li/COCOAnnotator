using LabelAnnotator.Records;
using LabelAnnotator.Records.COCO;
using LabelAnnotator.Records.Enums;
using LabelAnnotator.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace LabelAnnotator.Services {
    public class SerializationService {
        private readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true };

        /// <summary>주어진 이미지 및 분류를 UTF-8 COCO JSON으로 직렬화합니다.</summary>
        /// <param name="BasePath">레이블 파일이 위치한 경로입니다. 이미지의 절대 경로, 상대 경로 간 변환에 사용됩니다.</param>
        public byte[] Serialize(string BasePath, IEnumerable<ImageRecord> Images, IEnumerable<ClassRecord> Categories) {
            COCODataset cocodataset = new COCODataset();
            foreach (ClassRecord i in Categories) {
                int id = cocodataset.Categories.Count;
                cocodataset.Categories.Add(new CategoryCOCO {
                    ID = id,
                    Name = i.Name,
                    SuperCategory = i.Name,
                });
            }
            foreach (ImageRecord i in Images) {
                int image_id = cocodataset.Images.Count;
                cocodataset.Images.Add(new ImageCOCO {
                    ID = image_id,
                    FileName = Utils.GetRelativePath(BasePath, i.FullPath),
                    Width = i.Width,
                    Height = i.Height,
                });
                foreach (LabelRecord j in i.Annotations) {
                    int category_id = cocodataset.Categories.FindIndex(s => s.Name == j.Class.Name);
                    int annotation_id = cocodataset.Annotations.Count;
                    cocodataset.Annotations.Add(new AnnotationCOCO {
                        ID = annotation_id,
                        CategoryID = category_id,
                        ImageID = image_id,
                        IsCrowd = 0,
                        BoundaryBox = new List<double> { j.Left, j.Top, j.Width, j.Height },
                        Area = j.Area,
                    });
                }
            }
            return JsonSerializer.SerializeToUtf8Bytes(cocodataset, jsonSerializerOptions);
        }
        /// <summary>주어진 UTF-8 바이트 배열을 COCO JSON으로 간주하여 역직렬화합니다.</summary>
        /// <param name="BasePath">레이블 파일이 위치한 경로입니다. 이미지의 절대 경로, 상대 경로 간 변환에 사용됩니다.</param>
        public (ICollection<ImageRecord> Images, ICollection<ClassRecord> Categories) Deserialize(string BasePath, byte[] JsonContents) {
            ReadOnlySpan<byte> JsonSpan = new ReadOnlySpan<byte>(JsonContents);
            COCODataset cocodataset = JsonSerializer.Deserialize<COCODataset>(JsonSpan, jsonSerializerOptions);
            SortedDictionary<int, ImageRecord> images = new SortedDictionary<int, ImageRecord>();
            SortedDictionary<int, ClassRecord> categories = new SortedDictionary<int, ClassRecord>();
            foreach (ImageCOCO i in cocodataset.Images) {
                images.Add(i.ID, new ImageRecord(Path.Combine(BasePath, i.FileName), i.Width, i.Height));
            }
            foreach (CategoryCOCO i in cocodataset.Categories) {
                categories.Add(i.ID, ClassRecord.FromName(i.Name));
            }
            foreach (AnnotationCOCO i in cocodataset.Annotations) {
                if (categories.TryGetValue(i.CategoryID, out ClassRecord? category) && images.TryGetValue(i.ImageID, out ImageRecord? image) && i.BoundaryBox.Count >= 4) {
                    image.Annotations.Add(new LabelRecord(image, i.BoundaryBox[0], i.BoundaryBox[1], i.BoundaryBox[2], i.BoundaryBox[3], category));
                }
            }
            return (images.Values, categories.Values);
        }

        #region CSV 변환
        /// <summary>주어진 레이블을 CSV로 직렬화합니다.</summary>
        /// <param name="BasePath">레이블 파일이 위치한 경로입니다. 이미지의 상대 경로 계산에 사용됩니다.</param>
        public string CSVSerializeAsPositive(string BasePath, LabelRecord Label, SettingFormats Format) {
            string path = Utils.GetRelativePath(BasePath, Label.Image.FullPath);
            return Format switch {
                SettingFormats.LTRB => $"{path},{Label.Left:0.#},{Label.Top:0.#},{Label.Left + Label.Width:0.#},{Label.Top + Label.Height:0.#},{Label.Class:0.#}",
                SettingFormats.CXCYWH => $"{path},{Label.Left + Label.Width / 2:0.#},{Label.Top + Label.Height / 2:0.#},{Label.Width:0.#},{Label.Height:0.#},{Label.Class:0.#}",
                SettingFormats.LTWH => $"{path},{Label.Left:0.#},{Label.Top:0.#},{Label.Width:0.#},{Label.Height:0.#},{Label.Class:0.#}",
                _ => ""
            };
        }
        /// <summary>주어진 이미지를 음성 샘플로 간주하여 CSV로 직렬화합니다.</summary>
        /// <param name="BasePath">레이블 파일이 위치한 경로입니다. 이미지의 상대 경로 계산에 사용됩니다.</param>
        public string CSVSerializeAsNegative(string BasePath, ImageRecord Image) => $"{Utils.GetRelativePath(BasePath, Image.FullPath)},,,,,";
        /// <summary>
        /// 기본 경로와 CSV 레이블 파일의 한 행 내의 문자열을 이용해 이미지와 레이블 레코드를 역직렬화합니다.
        /// </summary>
        /// <returns>
        /// <list type="bullet">
        /// <item><description>둘다 <see langword="null"/>이면 역직렬화 실패를 의미합니다.</description></item>
        /// <item><description><seealso cref="LabelRecord"/>만 <see langword="null"/>이면 음성 샘플임을 의미합니다.</description></item>
        /// </list>
        /// </returns>
        public (ImageRecord?, LabelRecord?) CSVDeserialize(string BasePath, string Text, SettingFormats Format) {
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
        #endregion
    }
}
