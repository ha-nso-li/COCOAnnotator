using System.Text.Json.Serialization;

namespace COCOAnnotator.Records.COCO {
    public sealed record ImageCOCO(
        [property: JsonPropertyName("id")] int ID,
        [property: JsonPropertyName("file_name")] string FileName,
        [property: JsonPropertyName("width")] int Width,
        [property: JsonPropertyName("height")] int Height
    ) { }
}
