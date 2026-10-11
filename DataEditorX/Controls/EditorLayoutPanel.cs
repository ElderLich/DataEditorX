namespace DataEditorX
{
    // A presentation-only wrapper. LanguageHelper keeps the original child keys.
    internal sealed class EditorLayoutPanel : Panel
    {
        public EditorLayoutPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }
    }
}
