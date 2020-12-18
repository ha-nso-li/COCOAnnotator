using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LabelAnnotator.Records.COCO {
    public class COCODataset {
        [JsonPropertyName("images")]
        public List<ImageCOCO> Images { get; set; } = new List<ImageCOCO>();

        [JsonPropertyName("annotations")]
        public List<AnnotationCOCO> Annotations { get; set; } = new List<AnnotationCOCO>();

        [JsonPropertyName("categories")]
        public List<CategoryCOCO> Categories { get; set; } = new List<CategoryCOCO>();
    }
}
