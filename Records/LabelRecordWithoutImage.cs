using System;

namespace LabelAnnotator.Records {
    public class LabelRecordWithoutImage {
        public double Left { get; set; }
        public double Top { get; set; }
        public double Right { get; set; }
        public double Bottom { get; set; }
        public ClassRecord Class { get; set; }

        public double Size => Math.Abs(Right - Left) * Math.Abs(Bottom - Top);

        public LabelRecordWithoutImage(double Left, double Top, double Right, double Bottom, ClassRecord Classname) {
            this.Left = Left;
            this.Top = Top;
            this.Right = Right;
            this.Bottom = Bottom;
            Class = Classname;
        }

        public LabelRecord WithImage(ImageRecord ImageRecord) {
            return new LabelRecord(ImageRecord, Left, Top, Right, Bottom, Class);
        }

        public override string ToString() => $"{Class} ({Left},{Top},{Right},{Bottom})";
    }
}
