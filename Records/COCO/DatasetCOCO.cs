using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace COCOAnnotator.Records.COCO {
    public sealed record DatasetCOCO(
        [property: JsonPropertyName("images")] List<ImageCOCO> Images,
        [property: JsonPropertyName("annotations")] List<AnnotationCOCO> Annotations,
        [property: JsonPropertyName("categories")] List<CategoryCOCO> Categories
    ) {
        public DatasetCOCO() : this([], [], []) { }
    }
}
