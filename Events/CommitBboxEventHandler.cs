using System;
using System.Collections.Generic;

namespace LabelAnnotator.Events {
    public delegate void CommitBboxEventHandler(object sender, CommitBboxEventArgs e);
    public class CommitBboxEventArgs : EventArgs {
        public CommitBboxEventArgs(IEnumerable<Records.LabelRecordWithoutImage> added, IEnumerable<Records.LabelRecordWithIndex> changed, IEnumerable<Records.LabelRecordWithIndex> deleted) {
            Added = added;
            Changed = changed;
            Deleted = deleted;
        }

        public IEnumerable<Records.LabelRecordWithoutImage> Added { get; }
        public IEnumerable<Records.LabelRecordWithIndex> Changed { get; }
        public IEnumerable<Records.LabelRecordWithIndex> Deleted { get; }
    }
}
