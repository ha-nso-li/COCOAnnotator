using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace COCOAnnotator.Records.COCO {
    public record AnnotationCOCO(
        [property: JsonPropertyName("id")] int ID,
        [property: JsonPropertyName("category_id")] int CategoryID,
        [property: JsonPropertyName("image_id")] int ImageID,
        [property: JsonPropertyName("iscrowd")] int IsCrowd,
        [property: JsonPropertyName("bbox")] List<float> BoundaryBoxes,
        [property: JsonPropertyName("area")] float Area
    ) { }
}
