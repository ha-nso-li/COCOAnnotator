using COCOAnnotator.Records;
using System;
using System.Collections.Generic;

namespace COCOAnnotator.Events {
    public class CommitBboxEventArgs : EventArgs {
        public CommitBboxEventArgs(IEnumerable<AnnotationRecord> added, IEnumerable<AnnotationRecord> changed_old, IEnumerable<AnnotationRecord> changed_new, IEnumerable<AnnotationRecord> deleted) {
            Added = added;
            ChangedOldItems = changed_old;
            ChangedNewItems = changed_new;
            Deleted = deleted;
        }

        public IEnumerable<AnnotationRecord> Added { get; }
        public IEnumerable<AnnotationRecord> ChangedOldItems { get; }
        public IEnumerable<AnnotationRecord> ChangedNewItems { get; }
        public IEnumerable<AnnotationRecord> Deleted { get; }
    }
}
