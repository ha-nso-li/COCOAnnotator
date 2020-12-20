namespace COCOAnnotator.Records {
    public class AnnotationRecord {
        public ImageRecord Image { get; set; }

        public double Left { get; set; }
        public double Top { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public CategoryRecord Category { get; set; }

        public double Area => Width * Height;

        public AnnotationRecord(ImageRecord Image, double Left, double Top, double Width, double Height, CategoryRecord Category) {
            this.Image = Image;
            this.Left = Left;
            this.Top = Top;
            this.Width = Width;
            this.Height = Height;
            this.Category = Category;
        }

        public override string ToString() => $"{Category} ({Left},{Top},{Width},{Height})";
    }
}
