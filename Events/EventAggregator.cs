using Prism.Events;

namespace LabelAnnotator.Events {
    public class ScrollTxtLogVerifyLabel : PubSubEvent { }
    public class ScrollTxtLogUndupeLabel : PubSubEvent { }
    public class ScrollViewCategoriesList : PubSubEvent<Records.ClassRecord> { }
    public class ScrollViewImagesList : PubSubEvent<Records.ImageRecord> { }
    public class TryCommitBbox : PubSubEvent { }
}
