using System.Text.Json.Serialization;

namespace COCOAnnotator.Records.COCO {
    public class CategoryCOCO {
        [JsonPropertyName("id")]
        public int ID { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("super_category")]
        public string SuperCategory { get; set; } = "";
    }
}
