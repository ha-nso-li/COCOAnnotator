using LabelAnnotator.Records;
using System;
using System.Collections.Generic;

namespace LabelAnnotator.Events {
    public delegate void CommitBboxEventHandler(object sender, CommitBboxEventArgs e);
    public class CommitBboxEventArgs : EventArgs {
        public CommitBboxEventArgs(IEnumerable<LabelRecordWithoutImage> added, IEnumerable<LabelRecordWithIndex> changed, IEnumerable<LabelRecordWithIndex> deleted) {
            Added = added;
            Changed = changed;
            Deleted = deleted;
        }

        public IEnumerable<LabelRecordWithoutImage> Added { get; }
        public IEnumerable<LabelRecordWithIndex> Changed { get; }
        public IEnumerable<LabelRecordWithIndex> Deleted { get; }
    }
}
