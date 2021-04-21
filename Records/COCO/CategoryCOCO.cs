using System.Text.Json.Serialization;

namespace COCOAnnotator.Records.COCO {
    public record CategoryCOCO(
        [property: JsonPropertyName("id")] int ID,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("super_category")] string SuperCategory
    ) { }
}
