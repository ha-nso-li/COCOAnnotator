using LabelAnnotator.Records;
using Prism.Events;

namespace LabelAnnotator.Events {
    public class ScrollTxtLogVerifyDataset : PubSubEvent { }
    public class ScrollTxtLogUndupeLabel : PubSubEvent { }
    public class ScrollViewCategoriesList : PubSubEvent<CategoryRecord> { }
    public class ScrollViewImagesList : PubSubEvent<ImageRecord> { }
    public class TryCommitBbox : PubSubEvent { }
}
