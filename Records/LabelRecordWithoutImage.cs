namespace LabelAnnotator.Records {
    public class LabelRecordWithoutImage {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public ClassRecord Class { get; set; }

        public LabelRecordWithoutImage(double Left, double Top, double Width, double Height, ClassRecord Classname) {
            this.Left = Left;
            this.Top = Top;
            this.Width = Width;
            this.Height = Height;
            Class = Classname;
        }

        public LabelRecord WithImage(ImageRecord ImageRecord) {
            return new LabelRecord(ImageRecord, Left, Top, Width, Height, Class);
        }

        public override string ToString() => $"{Class} ({Left},{Top},{Width},{Height})";
    }
}
