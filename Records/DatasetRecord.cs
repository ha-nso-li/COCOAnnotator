using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace COCOAnnotator.Records {
    public class DatasetRecord {
        public string BasePath { get; set; }

        public FastObservableCollection<ImageRecord> Images { get; }
        public ObservableCollection<CategoryRecord> Categories { get; }

        public void Deconstruct(out ICollection<ImageRecord> Images, out ICollection<CategoryRecord> Categories) {
            Images = this.Images;
            Categories = this.Categories;
        }

        public DatasetRecord() {
            BasePath = "";
            Images = new FastObservableCollection<ImageRecord>();
            Categories = new ObservableCollection<CategoryRecord>();
        }

        public DatasetRecord(string BasePath, ICollection<ImageRecord> Images, ICollection<CategoryRecord> Categories) {
            this.BasePath = BasePath;
            this.Images = new FastObservableCollection<ImageRecord>(Images);
            this.Categories = new ObservableCollection<CategoryRecord>(Categories);
        }
    }
}
