namespace LabelAnnotator.Records {
    public class AnnotationRecord {
        public ImageRecord Image { get; set; }

        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public CategoryRecord Class { get; set; }

        public double Area => Width * Height;

        public AnnotationRecord(ImageRecord Image, double Left, double Top, double Width, double Height, CategoryRecord Classname) {
            this.Image = Image;
            this.Left = Left;
            this.Top = Top;
            this.Width = Width;
            this.Height = Height;
            Class = Classname;
        }

        public override string ToString() => $"{Class} ({Left},{Top},{Width},{Height})";
    }
}
