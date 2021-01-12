using COCOAnnotator.Records;
using COCOAnnotator.Records.COCO;
using COCOAnnotator.Records.Enums;
using System;
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
            DatasetCOCO datasetcoco = new DatasetCOCO();
            foreach (CategoryRecord i in Categories) {
                int id = datasetcoco.Categories.Count + 1;
                datasetcoco.Categories.Add(new CategoryCOCO {
                    ID = id,
                    Name = i.Name,
                    SuperCategory = i.Name,
                });
            }
            foreach (ImageRecord i in Images) {
                int image_id = datasetcoco.Images.Count;
                datasetcoco.Images.Add(new ImageCOCO {
                    ID = image_id,
                    FileName = Path.GetRelativePath(basePath, i.FullPath).Replace('\\', '/'),
                    Width = i.Width,
                    Height = i.Height,
                });
                foreach (AnnotationRecord j in i.Annotations) {
                    int? category_id = datasetcoco.Categories.Find(s => s.Name == j.Category.Name)?.ID;
                    if (category_id is null) continue;
                    int annotation_id = datasetcoco.Annotations.Count;
                    datasetcoco.Annotations.Add(new AnnotationCOCO {
                        ID = annotation_id,
                        CategoryID = category_id.Value,
                        ImageID = image_id,
                        IsCrowd = 0,
                        BoundaryBox = new List<float> { j.Left, j.Top, j.Width, j.Height },
                        Area = j.Area,
                    });
                }
            }
            using FileStream fileStream = File.Create(JsonPath);
            await JsonSerializer.SerializeAsync(fileStream, datasetcoco).ConfigureAwait(false);
        }

        /// <summary>주어진 UTF-8 바이트 배열을 COCO JSON으로 간주하여 역직렬화합니다.</summary>
        /// <param name="JsonPath">역직렬화할 JSON 파일이 존재하는 경로입니다.</param>
        public static async Task<DatasetRecord> DeserializeAsync(string JsonPath) {
            DatasetCOCO datasetcoco = await DeserializeRawAsync(JsonPath).ConfigureAwait(false);
            SortedDictionary<int, ImageRecord> images = new SortedDictionary<int, ImageRecord>();
            SortedDictionary<int, CategoryRecord> categories = new SortedDictionary<int, CategoryRecord>();
            foreach (ImageCOCO i in datasetcoco.Images) {
                if (!images.ContainsKey(i.ID)) images.Add(i.ID, new ImageRecord(Path.GetFullPath(i.FileName, Path.GetDirectoryName(JsonPath) ?? "").Replace('/', '\\'), i.Width, i.Height));
            }
            foreach (CategoryCOCO i in datasetcoco.Categories) {
                if (!categories.ContainsKey(i.ID)) categories.Add(i.ID, CategoryRecord.FromName(i.Name));
            }
            foreach (AnnotationCOCO i in datasetcoco.Annotations) {
                if (categories.TryGetValue(i.CategoryID, out CategoryRecord? category) && images.TryGetValue(i.ImageID, out ImageRecord? image) && i.BoundaryBox.Count >= 4) {
                    image.Annotations.Add(new AnnotationRecord(image, i.BoundaryBox[0], i.BoundaryBox[1], i.BoundaryBox[2], i.BoundaryBox[3], category));
                }
            }
            return new DatasetRecord(images.Values, categories.Values);
        }

        public static async Task<DatasetCOCO> DeserializeRawAsync(string JsonPath) {
            using FileStream fileStream = File.OpenRead(JsonPath);
            return await JsonSerializer.DeserializeAsync<DatasetCOCO>(fileStream).ConfigureAwait(false) ?? new DatasetCOCO();
        }

        public static async Task SerializeCSVAsync(string CSVPath, IEnumerable<ImageRecord> Images, CSVFormat CSVFormat) {
            using StreamWriter csv = File.CreateText(CSVPath);
            foreach (ImageRecord image in Images) {
                string imagePath = Path.GetRelativePath(Path.GetDirectoryName(CSVPath) ?? "", image.FullPath).Replace('\\', '/');
                if (image.Annotations.Count == 0) {
                    await csv.WriteLineAsync($"{imagePath},,,,,").ConfigureAwait(false);
                } else {
                    foreach (AnnotationRecord annotation in image.Annotations) {
                        await csv.WriteLineAsync(CSVFormat switch {
                            CSVFormat.LTRB => $"{imagePath},{annotation.Left:0.#},{annotation.Top:0.#},{annotation.Left + annotation.Width:0.#}," +
                                $"{annotation.Top + annotation.Height:0.#},{annotation.Category}",
                            CSVFormat.CXCYWH => $"{imagePath},{annotation.Left + annotation.Width / 2:0.#},{annotation.Top + annotation.Height / 2:0.#},{annotation.Width:0.#}," +
                                $"{annotation.Height:0.#},{annotation.Category}",
                            CSVFormat.LTWH => $"{imagePath},{annotation.Left:0.#},{annotation.Top:0.#},{annotation.Width:0.#},{annotation.Height:0.#},{annotation.Category}",
                            _ => throw new ArgumentException(null, nameof(CSVFormat)),
                        }).ConfigureAwait(false);
                    }
                }
            }
        }

        public static async Task<DatasetRecord> DeserializeCSVAsync(string CSVPath, CSVFormat CSVFormat) {
            using StreamReader csv = File.OpenText(CSVPath);
            SortedSet<ImageRecord> images = new SortedSet<ImageRecord>();
            SortedSet<CategoryRecord> categories = new SortedSet<CategoryRecord>();
            string? line;
            while ((line = await csv.ReadLineAsync().ConfigureAwait(false)) is not null) {
                string[] split = line.Split(',');
                if (split.Length < 6) continue;
                ImageRecord image = new ImageRecord(Path.GetFullPath(split[0], Path.GetDirectoryName(CSVPath) ?? "").Replace('/', '\\'));
                string categoryName = split[5];
                if (string.IsNullOrWhiteSpace(categoryName)) {
                    // Negative
                    if (!images.Contains(image)) images.Add(image);
                } else {
                    // Positive
                    if (!float.TryParse(split[1], out float num1)) continue;
                    if (!float.TryParse(split[2], out float num2)) continue;
                    if (!float.TryParse(split[3], out float num3)) continue;
                    if (!float.TryParse(split[4], out float num4)) continue;
                    if (images.TryGetValue(image, out ImageRecord? realImage)) image = realImage;
                    else images.Add(image);
                    CategoryRecord category = CategoryRecord.FromName(categoryName);
                    if (categories.TryGetValue(category, out CategoryRecord? realCategory)) category = realCategory;
                    else categories.Add(category);
                    image.Annotations.Add(CSVFormat switch {
                        CSVFormat.LTRB => new AnnotationRecord(image, num1, num2, num3 - num1, num4 - num2, category),
                        CSVFormat.CXCYWH => new AnnotationRecord(image, num1 - num3 / 2, num2 - num4 / 2, num3, num4, category),
                        CSVFormat.LTWH => new AnnotationRecord(image, num1, num2, num3, num4, category),
                        _ => throw new ArgumentException(null, nameof(CSVFormat)),
                    });
                }
            }
            return new DatasetRecord(images, categories);
        }
    }
}
