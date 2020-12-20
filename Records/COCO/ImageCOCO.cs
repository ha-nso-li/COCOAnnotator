using System.Text.Json.Serialization;

namespace COCOAnnotator.Records.COCO {
    public class ImageCOCO {
        [JsonPropertyName("id")]
        public int ID { get; set; }

        [JsonPropertyName("file_name")]
        public string FileName { get; set; } = "";

        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }
    }
}
