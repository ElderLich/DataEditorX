namespace FastColoredTextBoxNS
{
    public class FastColoredTextBoxEx : FastColoredTextBox
    {
        public Label lbTooltip;
        private Panel tooltipPanel;
        Point lastMouseCoord;

        public FastColoredTextBoxEx() : base()
        {
            SyntaxHighlighter = new MySyntaxHighlighter(this);
            // Folding walks the whole document; wait until typing pauses.
            TextChangedDelayed += FctbTextChanged;
            TextChanged += (_, _) => HideTooltip();
            ToolTipDelay = 350;
            DelayedEventsInterval = 150;
            DelayedTextChangedInterval = 250;
            InitializeTooltip();
        }

        public new event EventHandler<ToolTipNeededEventArgs> ToolTipNeeded;

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            lastMouseCoord = e.Location;
        }

        protected override void OnToolTip()
        {
            if (ToolTip == null || ToolTipNeeded == null) return;
            Place place = PointToPlace(lastMouseCoord);
            Point p = PlaceToPoint(place);
            if (Math.Abs(p.X - lastMouseCoord.X) > CharWidth * 2 ||
                Math.Abs(p.Y - lastMouseCoord.Y) > CharHeight * 2)
            {
                HideTooltip();
                return;
            }
            var range = new Range(this, place, place);
            string hoveredWord = range.GetFragment("[a-zA-Z0-9_]").Text;
            var args = new ToolTipNeededEventArgs(place, hoveredWord);
            ToolTipNeeded(this, args);
            if (!string.IsNullOrWhiteSpace(args.ToolTipText))
                ShowTooltipWithLabel(args.ToolTipTitle, args.ToolTipText);
            else
                HideTooltip();
        }

        public void ShowTooltipWithLabel(AutocompleteItem item)
            => ShowTooltipWithLabel(item.ToolTipTitle, item.ToolTipText);

        public void ShowTooltipWithLabel(string title, string text)
            => ShowTooltipWithLabel(title, text, lastMouseCoord.Y + CharHeight);

        public void ShowTooltipWithLabel(string title, string text, int height)
        {
            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(text))
            {
                HideTooltip();
                return;
            }
            int width = Math.Max(1, Math.Min(520, ClientSize.Width - 16));
            int maxHeight = Math.Max(1, Math.Min(320, ClientSize.Height - 16));
            tooltipPanel.AutoScrollPosition = Point.Empty;
            lbTooltip.MaximumSize = new Size(Math.Max(1, width - SystemInformation.VerticalScrollBarWidth - 16), 0);
            lbTooltip.Text = $"{title}\r\n\r\n{text}";
            tooltipPanel.BackColor = lbTooltip.BackColor;
            tooltipPanel.Size = new Size(width, Math.Min(maxHeight, lbTooltip.PreferredHeight + 16));
            int y = height + tooltipPanel.Height <= ClientSize.Height - 8
                ? height : height - tooltipPanel.Height - CharHeight;
            tooltipPanel.Location = new Point(Math.Max(0, ClientSize.Width - width - 8),
                Math.Clamp(y, 0, Math.Max(0, ClientSize.Height - tooltipPanel.Height)));
            tooltipPanel.Visible = true;
            tooltipPanel.BringToFront();
        }

        public void HideTooltip()
        {
            if (tooltipPanel != null) tooltipPanel.Visible = false;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) HideTooltip();
            base.OnKeyDown(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (tooltipPanel != null && !tooltipPanel.RectangleToScreen(tooltipPanel.ClientRectangle).Contains(Cursor.Position))
                HideTooltip();
            base.OnMouseLeave(e);
        }

        void FctbTextChanged(object sender, TextChangedEventArgs e)
        {
            Range.ClearFoldingMarkers();
            int currentIndent = 0;
            int lastNonEmptyLine = 0;
            for (int i = 0; i < LinesCount; i++)
            {
                var line = this[i];
                int spacesCount = line.StartSpacesCount;
                if (spacesCount == line.Count) continue;
                if (currentIndent < spacesCount)
                    this[lastNonEmptyLine].FoldingStartMarker = "m" + currentIndent;
                else if (currentIndent > spacesCount)
                    this[lastNonEmptyLine].FoldingEndMarker = "m" + spacesCount;
                currentIndent = spacesCount;
                lastNonEmptyLine = i;
            }
        }

        private void InitializeTooltip()
        {
            lbTooltip = new Label
            {
                AutoSize = true,
                BackColor = SystemColors.Info,
                ForeColor = SystemColors.InfoText,
                Font = SystemFonts.MessageBoxFont,
                Location = new Point(6, 6),
                MaximumSize = new Size(480, 0),
                Name = "lbTooltip"
            };
            tooltipPanel = new Panel
            {
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false,
                Name = "functionHelp",
                Padding = new Padding(6)
            };
            tooltipPanel.Controls.Add(lbTooltip);
            Controls.Add(tooltipPanel);
            SizeChanged += (_, _) => HideTooltip();
            Scroll += (_, _) => HideTooltip();
        }
    }
}
