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

        public ThemeSettingsForm()
        {
            Text = "Theme";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(420, 560);

            palette = CurrentPalette.Clone(CurrentProfileName);
            InitializeBindings();
            InitializeControls();
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
            Controls.Add(root);
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
            cbProfile.SelectedItem = ProfileCustom;
            changingProfile = false;
        }

        private void PickColor(ColorBinding binding)
        {
            EnsureCustomProfile();
            using ColorDialog dialog = new()
            {
                FullOpen = true,
                Color = binding.Get(palette)
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            binding.Set(palette, dialog.Color);
            RefreshButtons();
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
