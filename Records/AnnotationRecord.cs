namespace COCOAnnotator.Records {
    public class AnnotationRecord {
        public ImageRecord Image { get; set; }

        public float Left { get; set; }
        public float Top { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public CategoryRecord Category { get; set; }

        public float Area => Width * Height;

        public AnnotationRecord(ImageRecord Image, float Left, float Top, float Width, float Height, CategoryRecord Category) {
            this.Image = Image;
            this.Left = Left;
            this.Top = Top;
            this.Width = Width;
            this.Height = Height;
            this.Category = Category;
        }

        public override string ToString() => $"{Category} ({Left:0.#},{Top:0.#},{Width:0.#},{Height:0.#})";
    }
}
