using COCOAnnotator.Records;
using COCOAnnotator.Records.COCO;
using COCOAnnotator.Records.Enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace COCOAnnotator.Services.Utilities {
    public static class SerializationService {
        /// <summary>주어진 데이터셋을 UTF-8 COCO JSON으로 직렬화하여 파일로 출력합니다.</summary>
        /// <returns>직렬화된 JSON 파일의 경로입니다.</returns>
        public static async Task<string> SerializeAsync(DatasetRecord Dataset) {
            string InstanceName = Path.GetFileName(Dataset.BasePath);
            string JsonPath = Path.GetFullPath($@"..\annotations\instances_{InstanceName}.json", Dataset.BasePath);
            DatasetCOCO datasetcoco = new();
            foreach (CategoryRecord i in Dataset.Categories.Where(s => !s.All)) {
                int id = datasetcoco.Categories.Count + 1;
                datasetcoco.Categories.Add(new(id, i.Name, i.Name));
            }
            foreach (ImageRecord i in Dataset.Images) {
                int image_id = datasetcoco.Images.Count;
                datasetcoco.Images.Add(new(image_id, i.Path, i.Width, i.Height));
                foreach (AnnotationRecord j in i.Annotations) {
                    int? category_id = datasetcoco.Categories.FirstOrDefault(s => s.Name == j.Category.Name)?.ID;
                    if (category_id is null) continue;
                    int annotation_id = datasetcoco.Annotations.Count;
                    datasetcoco.Annotations.Add(new(annotation_id, category_id.Value, image_id, 0, new() { j.Left, j.Top, j.Width, j.Height }, j.Area));
                }
            }
            using FileStream fileStream = File.Create(JsonPath);
            await JsonSerializer.SerializeAsync(fileStream, datasetcoco).ConfigureAwait(false);
            return JsonPath;
        }

        /// <summary>주어진 파일을 COCO JSON으로 간주하여 역직렬화합니다.</summary>
        /// <param name="JsonPath">역직렬화할 JSON 파일이 존재하는 경로입니다.</param>
        public static async Task<DatasetRecord> DeserializeAsync(string JsonPath) {
            string JsonFileName = Path.GetFileNameWithoutExtension(JsonPath);
            string InstanceName = JsonFileName[(JsonFileName.IndexOf('_')+1)..];
            string BasePath = Path.GetFullPath($@"..\..\{InstanceName}", JsonPath);
            DatasetCOCO datasetcoco = await DeserializeRawAsync(JsonPath).ConfigureAwait(false);
            SortedDictionary<int, ImageRecord> images = new();
            SortedDictionary<int, CategoryRecord> categories = new();
            foreach (ImageCOCO i in datasetcoco.Images) {
                if (!images.ContainsKey(i.ID)) images.Add(i.ID, new(i.FileName.Replace('/', '\\'), i.Width, i.Height));
            }
            foreach (CategoryCOCO i in datasetcoco.Categories) {
                if (!categories.ContainsKey(i.ID)) categories.Add(i.ID, CategoryRecord.FromName(i.Name));
            }
            foreach (AnnotationCOCO i in datasetcoco.Annotations) {
                if (categories.TryGetValue(i.CategoryID, out CategoryRecord? category) && images.TryGetValue(i.ImageID, out ImageRecord? image) && i.BoundaryBoxes.Count >= 4) {
                    image.Annotations.Add(new(image, i.BoundaryBoxes[0], i.BoundaryBoxes[1], i.BoundaryBoxes[2], i.BoundaryBoxes[3], category));
                }
            }
            return new(BasePath, images.Values, categories.Values);
        }

        /// <summary>주어진 파일을 COCO JSON으로 간주하여 역직렬화합니다. .</summary>
        public static async Task<DatasetCOCO> DeserializeRawAsync(string JsonPath) {
            using FileStream fileStream = File.OpenRead(JsonPath);
            return await JsonSerializer.DeserializeAsync<DatasetCOCO>(fileStream).ConfigureAwait(false) ?? new();
        }

        /// <summary>주어진 데이터셋을 CSV로 직렬화하여 파일로 출력합니다.</summary>
        public static async Task<string> SerializeCSVAsync(DatasetRecord Dataset, CSVFormat CSVFormat) {
            string CSVPath = Path.Combine(Dataset.BasePath, "instances.csv");
            using StreamWriter csv = File.CreateText(CSVPath);
            foreach (ImageRecord image in Dataset.Images) {
                if (image.Annotations.Count == 0) {
                    await csv.WriteLineAsync($"{image.Path},,,,,").ConfigureAwait(false);
                } else {
                    foreach (AnnotationRecord annotation in image.Annotations) {
                        await csv.WriteLineAsync(CSVFormat switch {
                            CSVFormat.LTRB => $"{image.Path},{annotation.Left:0.#},{annotation.Top:0.#},{annotation.Left + annotation.Width:0.#}," +
                                $"{annotation.Top + annotation.Height:0.#},{annotation.Category}",
                            CSVFormat.CXCYWH => $"{image.Path},{annotation.Left + annotation.Width / 2:0.#},{annotation.Top + annotation.Height / 2:0.#},{annotation.Width:0.#}," +
                                $"{annotation.Height:0.#},{annotation.Category}",
                            CSVFormat.LTWH => $"{image.Path},{annotation.Left:0.#},{annotation.Top:0.#},{annotation.Width:0.#},{annotation.Height:0.#},{annotation.Category}",
                            _ => throw new ArgumentException(null, nameof(CSVFormat)),
                        }).ConfigureAwait(false);
                    }
                }
            }
            return CSVPath;
        }

        public static async Task<DatasetRecord> DeserializeCSVAsync(string CSVPath, CSVFormat CSVFormat) {
            using StreamReader csv = File.OpenText(CSVPath);
            SortedSet<ImageRecord> images = new();
            SortedSet<CategoryRecord> categories = new();
            string? line;
            while ((line = await csv.ReadLineAsync().ConfigureAwait(false)) is not null) {
                string[] split = line.Split(',');
                if (split.Length < 6) continue;
                ImageRecord image = new(Path.GetFullPath(split[0], Path.GetDirectoryName(CSVPath) ?? "").Replace('/', '\\'));
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
                        CSVFormat.LTRB => new(image, num1, num2, num3 - num1, num4 - num2, category),
                        CSVFormat.CXCYWH => new(image, num1 - num3 / 2, num2 - num4 / 2, num3, num4, category),
                        CSVFormat.LTWH => new(image, num1, num2, num3, num4, category),
                        _ => throw new ArgumentException(null, nameof(CSVFormat)),
                    });
                }
            }
            return new(images.GetCommonParentPath(), images, categories);
        }

        public static bool IsJsonPathValid(string JsonPath) {
            FileInfo JsonFileInfo = new(JsonPath);
            DirectoryInfo? JsonDirInfo = JsonFileInfo.Directory;
            return JsonFileInfo.Extension.Equals(".json", StringComparison.OrdinalIgnoreCase) && JsonFileInfo.Name.StartsWith("instances_", StringComparison.OrdinalIgnoreCase)
                && JsonDirInfo is not null && JsonDirInfo.Parent is not null;
        }
    }
}
