using LabelAnnotator.Records;
using System;
using System.Collections.Generic;

namespace LabelAnnotator.Events {
    public delegate void CommitBboxEventHandler(object sender, CommitBboxEventArgs e);
    public class CommitBboxEventArgs : EventArgs {
        public CommitBboxEventArgs(IEnumerable<LabelRecordWithoutImage> added, IEnumerable<LabelRecord> changed_old, IEnumerable<LabelRecord> changed_new, IEnumerable<LabelRecord> deleted) {
            Added = added;
            ChangedOldItems = changed_old;
            ChangedNewItems = changed_new;
            Deleted = deleted;
        }

        public IEnumerable<LabelRecordWithoutImage> Added { get; }
        public IEnumerable<LabelRecord> ChangedOldItems { get; }
        public IEnumerable<LabelRecord> ChangedNewItems { get; }
        public IEnumerable<LabelRecord> Deleted { get; }
    }
}
