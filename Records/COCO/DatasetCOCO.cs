using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace COCOAnnotator.Records.COCO {
    public class DatasetCOCO {
        [JsonPropertyName("images")]
        public List<ImageCOCO> Images { get; set; } = new List<ImageCOCO>();

        [JsonPropertyName("annotations")]
        public List<AnnotationCOCO> Annotations { get; set; } = new List<AnnotationCOCO>();

        [JsonPropertyName("categories")]
        public List<CategoryCOCO> Categories { get; set; } = new List<CategoryCOCO>();
    }
}
