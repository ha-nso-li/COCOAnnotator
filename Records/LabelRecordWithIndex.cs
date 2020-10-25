namespace LabelAnnotator.Records {
    public class LabelRecordWithIndex {
        public LabelRecordWithIndex(int index, LabelRecord label) {
            Index = index;
            Label = label;
        }

        public int Index { get; }
        public LabelRecord Label { get; }

        public void Deconstruct(out int Index, out LabelRecord Label) {
            Index = this.Index;
            Label = this.Label;
        }
    }
}
