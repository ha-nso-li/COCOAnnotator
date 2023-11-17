namespace COCOAnnotator.Records {
    public sealed class AnnotationRecord(ImageRecord Image, float Left, float Top, float Width, float Height, CategoryRecord Category) {
        public ImageRecord Image { get; set; } = Image;

        public float Left { get; set; } = Left;
        public float Top { get; set; } = Top;
        public float Width { get; set; } = Width;
        public float Height { get; set; } = Height;
        public CategoryRecord Category { get; set; } = Category;

        public float Area => Width * Height;

        public override string ToString() => $"{Category} ({Left:0.#},{Top:0.#},{Width:0.#},{Height:0.#})";
    }
}
