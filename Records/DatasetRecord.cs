using Prism.Mvvm;
using System.Collections.Generic;

namespace COCOAnnotator.Records {
    public class DatasetRecord : BindableBase {
        private string _BasePath;
        public string BasePath {
            get => _BasePath;
            set => SetProperty(ref _BasePath, value);
        }

        public ObservableCollection<ImageRecord> Images { get; }
        public ObservableCollection<CategoryRecord> Categories { get; }

        public DatasetRecord() {
            _BasePath = "";
            Images = new ObservableCollection<ImageRecord>();
            Categories = new ObservableCollection<CategoryRecord>();
        }

        public DatasetRecord(string BasePath, IEnumerable<ImageRecord> Images, IEnumerable<CategoryRecord> Categories) {
            _BasePath = BasePath;
            this.Images = new ObservableCollection<ImageRecord>(Images);
            this.Categories = new ObservableCollection<CategoryRecord>(Categories);
        }
    }
}
