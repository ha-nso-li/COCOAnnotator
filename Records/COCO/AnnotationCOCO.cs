using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace COCOAnnotator.Records.COCO {
    public class AnnotationCOCO {
        [JsonPropertyName("id")]
        public int ID { get; set; }

        [JsonPropertyName("category_id")]
        public int CategoryID { get; set; }

        [JsonPropertyName("image_id")]
        public int ImageID { get; set; }

        [JsonPropertyName("iscrowd")]
        public int IsCrowd { get; set; }

        [JsonPropertyName("bbox")]
        public List<float> BoundaryBox { get; set; } = new List<float>();

        [JsonPropertyName("area")]
        public float Area { get; set; }
    }
}
