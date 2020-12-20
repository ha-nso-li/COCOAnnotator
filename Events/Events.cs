using COCOAnnotator.Records;
using Prism.Events;

namespace COCOAnnotator.Events {
    public class ScrollTxtLogVerifyDataset : PubSubEvent { }
    public class ScrollTxtLogUndupeLabel : PubSubEvent { }
    public class ScrollViewCategoriesList : PubSubEvent<CategoryRecord> { }
    public class ScrollViewImagesList : PubSubEvent<ImageRecord> { }
    public class TryCommitBbox : PubSubEvent { }
}
