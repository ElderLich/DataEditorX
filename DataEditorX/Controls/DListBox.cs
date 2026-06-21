namespace DataEditorX
{
    public class DListBox : ListBox
    {
        private const int WM_PAINT = 0x000F;

        public DListBox()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint,
                true);
            UpdateStyles();
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_PAINT)
            {
                DrawRowLines();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            Invalidate();
        }

        private void DrawRowLines()
        {
            if (!IsHandleCreated || ItemHeight <= 0 || ClientSize.Width <= 0 || ClientSize.Height <= 0)
            {
                return;
            }

            using Graphics graphics = Graphics.FromHwnd(Handle);
            using Pen pen = new(GetRowLineColor());
            int width = ClientSize.Width - 1;
            for (int y = ItemHeight - 1; y < ClientSize.Height; y += ItemHeight)
            {
                graphics.DrawLine(pen, 0, y, width, y);
            }
        }

        private Color GetRowLineColor()
        {
            Color back = BackColor;
            Color fore = ForeColor;
            const int backWeight = 5;
            const int foreWeight = 1;
            return Color.FromArgb(
                (back.R * backWeight + fore.R * foreWeight) / (backWeight + foreWeight),
                (back.G * backWeight + fore.G * foreWeight) / (backWeight + foreWeight),
                (back.B * backWeight + fore.B * foreWeight) / (backWeight + foreWeight));
        }
    }
}
