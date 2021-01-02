using COCOAnnotator.Records;
using COCOAnnotator.Records.COCO;
using COCOAnnotator.Records.Enums;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace COCOAnnotator.Services.Utilities {
    public static class SerializationService {
        /// <summary>주어진 이미지 및 분류를 UTF-8 COCO JSON으로 직렬화합니다.</summary>
        /// <param name="JsonPath">직렬화된 JSON 파일이 쓰일 경로입니다.</param>
        public static async Task SerializeAsync(string JsonPath, IEnumerable<ImageRecord> Images, IEnumerable<CategoryRecord> Categories) {
            string basePath = Path.GetDirectoryName(JsonPath) ?? "";
            COCODataset cocodataset = new COCODataset();
            foreach (CategoryRecord i in Categories) {
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
                    FileName = Miscellaneous.GetRelativePath(basePath, i.FullPath),
                    Width = i.Width,
                    Height = i.Height,
                });
                foreach (AnnotationRecord j in i.Annotations) {
                    int category_id = cocodataset.Categories.FindIndex(s => s.Name == j.Category.Name);
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
            using FileStream fileStream = File.Create(JsonPath);
            await JsonSerializer.SerializeAsync(fileStream, cocodataset).ConfigureAwait(false);
        }

        /// <summary>주어진 UTF-8 바이트 배열을 COCO JSON으로 간주하여 역직렬화합니다.</summary>
        /// <param name="BasePath">레이블 파일이 위치한 경로입니다. 이미지의 절대 경로, 상대 경로 간 변환에 사용됩니다.</param>
        public static async Task<(ICollection<ImageRecord> Images, ICollection<CategoryRecord> Categories)> DeserializeAsync(string JsonPath) {
            COCODataset cocodataset = await DeserializeRawAsync(JsonPath);
            string basePath = Path.GetDirectoryName(JsonPath) ?? "";
            SortedDictionary<int, ImageRecord> images = new SortedDictionary<int, ImageRecord>();
            SortedDictionary<int, CategoryRecord> categories = new SortedDictionary<int, CategoryRecord>();
            foreach (ImageCOCO i in cocodataset.Images) {
                if (!images.ContainsKey(i.ID)) images.Add(i.ID, new ImageRecord(Path.Combine(basePath, i.FileName), i.Width, i.Height));
            }
            foreach (CategoryCOCO i in cocodataset.Categories) {
                if (!categories.ContainsKey(i.ID)) categories.Add(i.ID, CategoryRecord.FromName(i.Name));
            }
            foreach (AnnotationCOCO i in cocodataset.Annotations) {
                if (categories.TryGetValue(i.CategoryID, out CategoryRecord? category) && images.TryGetValue(i.ImageID, out ImageRecord? image) && i.BoundaryBox.Count >= 4) {
                    image.Annotations.Add(new AnnotationRecord(image, i.BoundaryBox[0], i.BoundaryBox[1], i.BoundaryBox[2], i.BoundaryBox[3], category));
                }
            }
            return (images.Values, categories.Values);
        }

        public static async Task<COCODataset> DeserializeRawAsync(string JsonPath) {
            using FileStream fileStream = File.OpenRead(JsonPath);
            return await JsonSerializer.DeserializeAsync<COCODataset>(fileStream).ConfigureAwait(false) ?? new COCODataset();
        }

        #region CSV 변환
        /// <summary>주어진 레이블을 CSV로 직렬화합니다.</summary>
        /// <param name="BasePath">레이블 파일이 위치한 경로입니다. 이미지의 상대 경로 계산에 사용됩니다.</param>
        public static string CSVSerializeAsPositive(string BasePath, AnnotationRecord Label, CSVFormat Format) {
            string path = Miscellaneous.GetRelativePath(BasePath, Label.Image.FullPath);
            return Format switch {
                CSVFormat.LTRB => $"{path},{Label.Left:0.#},{Label.Top:0.#},{Label.Left + Label.Width:0.#},{Label.Top + Label.Height:0.#},{Label.Category}",
                CSVFormat.CXCYWH => $"{path},{Label.Left + Label.Width / 2:0.#},{Label.Top + Label.Height / 2:0.#},{Label.Width:0.#},{Label.Height:0.#},{Label.Category}",
                CSVFormat.LTWH => $"{path},{Label.Left:0.#},{Label.Top:0.#},{Label.Width:0.#},{Label.Height:0.#},{Label.Category}",
                _ => ""
            };
        }
        /// <summary>주어진 이미지를 음성 샘플로 간주하여 CSV로 직렬화합니다.</summary>
        /// <param name="BasePath">레이블 파일이 위치한 경로입니다. 이미지의 상대 경로 계산에 사용됩니다.</param>
        public static string CSVSerializeAsNegative(string BasePath, ImageRecord Image) => $"{Miscellaneous.GetRelativePath(BasePath, Image.FullPath)},,,,,";
        /// <summary>
        /// 기본 경로와 CSV 레이블 파일의 한 행 내의 문자열을 이용해 이미지와 레이블 레코드를 역직렬화합니다.
        /// </summary>
        /// <returns>
        /// <list type="bullet">
        /// <item><description>둘다 <see langword="null"/>이면 역직렬화 실패를 의미합니다.</description></item>
        /// <item><description><seealso cref="AnnotationRecord"/>만 <see langword="null"/>이면 음성 샘플임을 의미합니다.</description></item>
        /// </list>
        /// </returns>
        public static (ImageRecord?, AnnotationRecord?) CSVDeserialize(string BasePath, string Text, CSVFormat Format) {
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
                CSVFormat.LTRB => (img, new AnnotationRecord(img, num1, num2, num3 - num1, num4 - num2, CategoryRecord.FromName(classname))),
                CSVFormat.CXCYWH => (img, new AnnotationRecord(img, num1 - num3 / 2, num2 - num4 / 2, num3, num4, CategoryRecord.FromName(classname))),
                CSVFormat.LTWH => (img, new AnnotationRecord(img, num1, num2, num3, num4, CategoryRecord.FromName(classname))),
                _ => (null, null)
            };
        }
        #endregion
    }
}