using DataEditorX.Config;
using static DataEditorX.Config.ThemeManager;

namespace DataEditorX
{
    public sealed class ThemeSettingsForm : Form
    {
        private readonly ComboBox cbProfile = new();
        private readonly CheckBox chkDarkStyle = new();
        private readonly List<ColorBinding> bindings = new();
        private ThemePalette palette;
        private bool changingProfile;
        private readonly Panel preview = new() { Dock = DockStyle.Fill, Padding = new Padding(12) };
        private readonly Label previewTitle = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        private readonly ListView previewCards = new() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HideSelection = false };
        private readonly FastColoredTextBoxNS.FastColoredTextBoxEx previewCode = new()
        {
            Dock = DockStyle.Fill, ReadOnly = true, Language = FastColoredTextBoxNS.Language.Lua,
            BackColor = Color.FromArgb(64, 64, 64), ForeColor = Color.GhostWhite,
            ShowLineNumbers = true, AccessibleName = "Lua theme preview"
        };

        public ThemeSettingsForm()
        {
            Text = "Theme";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(900, 620);
            MinimumSize = new Size(800, 520);

            palette = CurrentPalette.Clone(CurrentProfileName);
            InitializeBindings();
            InitializeControls();
            ThemeManager.ApplyControlTree(this);
            LoadProfile(CurrentProfileName);
        }

        private void InitializeControls()
        {
            TableLayoutPanel root = new()
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(12)
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));

            cbProfile.AccessibleName = "Theme preset";
            cbProfile.DropDownStyle = ComboBoxStyle.DropDownList;
            cbProfile.Dock = DockStyle.Fill;
            cbProfile.Items.AddRange(ProfileNames.Select(name => (object)name).ToArray());
            cbProfile.SelectedIndexChanged += CbProfileSelectedIndexChanged;
            root.Controls.Add(cbProfile, 0, 0);

            chkDarkStyle.Text = "Use dark window chrome";
            chkDarkStyle.Dock = DockStyle.Fill;
            chkDarkStyle.CheckedChanged += (_, _) =>
            {
                if (!changingProfile)
                {
                    EnsureCustomProfile();
                    palette.IsDark = chkDarkStyle.Checked;
                    RefreshPreview();
                }
            };
            root.Controls.Add(chkDarkStyle, 0, 1);

            TableLayoutPanel colorGrid = new()
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                ColumnCount = 2,
                Padding = new Padding(0, 4, 0, 4)
            };
            colorGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            colorGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));

            foreach (ColorBinding binding in bindings)
            {
                int row = colorGrid.RowCount++;
                colorGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

                Label label = new()
                {
                    Text = binding.Label,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Dock = DockStyle.Fill
                };
                Button button = new()
                {
                    Dock = DockStyle.Fill,
                    FlatStyle = FlatStyle.Flat
                };
                button.Click += (_, _) => PickColor(binding);
                binding.Button = button;

                colorGrid.Controls.Add(label, 0, row);
                colorGrid.Controls.Add(button, 1, row);
            }
            root.Controls.Add(colorGrid, 0, 2);

            FlowLayoutPanel buttons = new()
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 8, 0, 0)
            };

            Button ok = new()
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Width = 88
            };
            ok.Click += (_, _) => SaveTheme();

            Button cancel = new()
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Width = 88
            };

            Button reset = new()
            {
                Text = "Reset",
                Width = 88
            };
            reset.Click += (_, _) => LoadProfile((string)cbProfile.SelectedItem);

            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(reset);
            root.Controls.Add(buttons, 0, 3);

            AcceptButton = ok;
            CancelButton = cancel;
            TableLayoutPanel shell = new() { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 370));
            shell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            shell.Controls.Add(root, 0, 0);
            shell.Controls.Add(preview, 1, 0);
            InitializePreview();
            Controls.Add(shell);
        }

        private void InitializePreview()
        {
            TableLayoutPanel samples = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5 };
            samples.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            samples.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            samples.RowStyles.Add(new RowStyle(SizeType.Absolute, 125));
            samples.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            samples.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            previewTitle.Font = new Font(Font, FontStyle.Bold);
            samples.Controls.Add(previewTitle, 0, 0);
            samples.Controls.Add(new TextBox { Text = "Search cards: Spellcaster", ReadOnly = true, Dock = DockStyle.Fill, AccessibleName = "Card search preview" }, 0, 1);
            previewCards.Columns.Add("Card", 190);
            previewCards.Columns.Add("ATK", 65);
            previewCards.Columns.Add("DEF", 65);
            previewCards.Items.Add(new ListViewItem(new[] { "Dark Magician", "2500", "2100" }));
            previewCards.Items.Add(new ListViewItem(new[] { "Mystic Dealer", "1000", "0" }));
            previewCards.Items.Add(new ListViewItem(new[] { "Sevens Road Magician", "2100", "1500" }));
            samples.Controls.Add(previewCards, 0, 2);
            ((FastColoredTextBoxNS.MySyntaxHighlighter)previewCode.SyntaxHighlighter)
                .SetKnownFunctions(new[] { "Duel.Draw", "RD.Draw" });
            previewCode.Text = "-- Lua colors and line numbers\nlocal s,id=GetID()\nlocal title=\"Draw a card\"\n\nfunction s.operation(e,tp)\n    Duel.Draw(tp,1,REASON_EFFECT)\nend\n\n-- Rush helper reference\n-- RD.Draw()";
            samples.Controls.Add(previewCode, 0, 3);
            samples.Controls.Add(new Label
            {
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                Text = "Preview only. Choose OK to save; Cancel keeps your current theme."
            }, 0, 4);
            preview.Controls.Add(samples);
        }

        private void RefreshPreview()
        {
            ThemeManager.ApplyControlTree(preview, palette);
            previewTitle.Text = palette.Name + " — Preview";
            previewTitle.BackColor = palette.HeaderBackColor;
            foreach (ListViewItem item in previewCards.Items)
            {
                item.BackColor = item.Index % 2 == 0 ? palette.ListEvenBackColor : palette.ListOddBackColor;
                item.ForeColor = palette.TextColor;
            }
            preview.Invalidate(true);
        }

        private void InitializeBindings()
        {
            bindings.Add(new("Form background", p => p.FormBackColor, (p, c) => p.FormBackColor = c));
            bindings.Add(new("Surface background", p => p.SurfaceBackColor, (p, c) => p.SurfaceBackColor = c));
            bindings.Add(new("Input background", p => p.InputBackColor, (p, c) => p.InputBackColor = c));
            bindings.Add(new("Menu background", p => p.MenuBackColor, (p, c) => p.MenuBackColor = c));
            bindings.Add(new("Button background", p => p.ButtonBackColor, (p, c) => p.ButtonBackColor = c));
            bindings.Add(new("Header background", p => p.HeaderBackColor, (p, c) => p.HeaderBackColor = c));
            bindings.Add(new("Text", p => p.TextColor, (p, c) => p.TextColor = c));
            bindings.Add(new("Muted text", p => p.MutedTextColor, (p, c) => p.MutedTextColor = c));
            bindings.Add(new("Border", p => p.BorderColor, (p, c) => p.BorderColor = c));
            bindings.Add(new("Accent", p => p.AccentColor, (p, c) => p.AccentColor = c));
            bindings.Add(new("List row", p => p.ListEvenBackColor, (p, c) => p.ListEvenBackColor = c));
            bindings.Add(new("List alternate row", p => p.ListOddBackColor, (p, c) => p.ListOddBackColor = c));
            bindings.Add(new("Dock background", p => p.DockBackColor, (p, c) => p.DockBackColor = c));
            bindings.Add(new("Code background", p => p.CodeBackColor, (p, c) => p.CodeBackColor = c));
            bindings.Add(new("Code indent", p => p.CodeIndentBackColor, (p, c) => p.CodeIndentBackColor = c));
        }

        private void CbProfileSelectedIndexChanged(object sender, EventArgs e)
        {
            if (changingProfile || cbProfile.SelectedItem is not string profile)
            {
                return;
            }

            LoadProfile(profile);
        }

        private void LoadProfile(string profile)
        {
            changingProfile = true;
            palette = ThemeManager.GetPalette(profile).Clone(profile);
            if (!ProfileNames.Contains(profile))
            {
                profile = ProfileLight;
            }
            cbProfile.SelectedItem = profile;
            chkDarkStyle.Checked = palette.IsDark;
            RefreshButtons();
            RefreshPreview();
            changingProfile = false;
        }

        private void EnsureCustomProfile()
        {
            if (cbProfile.SelectedItem is string profile && profile == ProfileCustom)
            {
                return;
            }

            changingProfile = true;
            palette = palette.Clone(ProfileCustom);
            palette.UsesOriginalColors = false;
            cbProfile.SelectedItem = ProfileCustom;
            changingProfile = false;
        }

        private void PickColor(ColorBinding binding)
        {
            using ColorDialog dialog = new()
            {
                FullOpen = true,
                Color = binding.Get(palette)
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            EnsureCustomProfile();
            binding.Set(palette, dialog.Color);
            RefreshButtons();
            RefreshPreview();
        }

        private void RefreshButtons()
        {
            foreach (ColorBinding binding in bindings)
            {
                Color color = binding.Get(palette);
                binding.Button.BackColor = color;
                binding.Button.Text = $"{color.R}, {color.G}, {color.B}";
                binding.Button.ForeColor = GetReadableTextColor(color);
            }
        }

        private void SaveTheme()
        {
            string profile = cbProfile.SelectedItem as string ?? ProfileLight;
            if (profile == ProfileCustom)
            {
                palette.IsDark = chkDarkStyle.Checked;
                ThemeManager.SaveCustomTheme(palette);
                return;
            }

            ThemeManager.SaveThemeProfile(profile);
        }

        private static Color GetReadableTextColor(Color backColor)
        {
            int brightness = (backColor.R * 299) + (backColor.G * 587) + (backColor.B * 114);
            return brightness > 140000 ? Color.Black : Color.White;
        }

        private sealed class ColorBinding
        {
            public ColorBinding(string label, Func<ThemePalette, Color> get, Action<ThemePalette, Color> set)
            {
                Label = label;
                Get = get;
                Set = set;
            }

            public string Label { get; }
            public Func<ThemePalette, Color> Get { get; }
            public Action<ThemePalette, Color> Set { get; }
            public Button Button { get; set; }
        }
    }
}
