using System.Collections.Generic;

namespace COCOAnnotator.Records {
    public class DatasetRecord {
        public string BasePath { get; }

        public ICollection<ImageRecord> Images { get; }
        public ICollection<CategoryRecord> Categories { get; }

        public void Deconstruct(out ICollection<ImageRecord> Images, out ICollection<CategoryRecord> Categories) {
            Images = this.Images;
            Categories = this.Categories;
        }

        public DatasetRecord(string BasePath, ICollection<ImageRecord> Images, ICollection<CategoryRecord> Categories) {
            this.BasePath = BasePath;
            this.Images = Images;
            this.Categories = Categories;
        }
    }
}
