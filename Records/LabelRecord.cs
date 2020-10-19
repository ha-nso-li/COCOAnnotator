using System;

namespace LabelAnnotator.Records {
    public class LabelRecord {
        public ImageRecord Image { get; set; }

        public double Left { get; set; }
        public double Top { get; set; }
        public double Right { get; set; }
        public double Bottom { get; set; }
        public ClassRecord Class { get; set; }

        public double Size => Math.Abs(Right - Left) * Math.Abs(Bottom - Top);

        public LabelRecord(ImageRecord Image, double Left, double Top, double Right, double Bottom, ClassRecord Classname) {
            this.Image = Image;
            this.Left = Left;
            this.Top = Top;
            this.Right = Right;
            this.Bottom = Bottom;
            Class = Classname;
        }

        public override string ToString() => $"{Class} ({Left},{Top},{Right},{Bottom})";
    }
}
