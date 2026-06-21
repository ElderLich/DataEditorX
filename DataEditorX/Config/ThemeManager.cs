using FastColoredTextBoxNS;
using System.Globalization;
using System.Runtime.CompilerServices;
using WeifenLuo.WinFormsUI.Docking;

namespace DataEditorX.Config
{
    public static class ThemeManager
    {
        public const string ProfileLight = "Light";
        public const string ProfileDark = "Dark";
        public const string ProfileAliceLight = "Alice Light";
        public const string ProfileCustom = "Custom";

        private static readonly Color LightHeaderBackColor = Color.FromArgb(192, 192, 255);

        private static readonly ConditionalWeakTable<Control, ControlThemeState> ControlStates = new();
        private static readonly ConditionalWeakTable<ToolStrip, ToolStripThemeState> ToolStripStates = new();
        private static readonly ConditionalWeakTable<ToolStripItem, ToolStripItemThemeState> ToolStripItemStates = new();
        private static readonly ConditionalWeakTable<FastColoredTextBox, FastTextBoxThemeState> FastTextBoxStates = new();

        public static IReadOnlyList<string> ProfileNames { get; } =
            new[] { ProfileLight, ProfileDark, ProfileAliceLight, ProfileCustom };

        public static ThemePalette LightPalette { get; } = new()
        {
            Name = ProfileLight,
            UsesOriginalColors = true,
            FormBackColor = SystemColors.Control,
            SurfaceBackColor = SystemColors.Control,
            InputBackColor = SystemColors.Window,
            MenuBackColor = SystemColors.Menu,
            ButtonBackColor = SystemColors.Control,
            HeaderBackColor = LightHeaderBackColor,
            TextColor = SystemColors.WindowText,
            MutedTextColor = SystemColors.GrayText,
            BorderColor = SystemColors.ControlDark,
            AccentColor = Color.FromArgb(0, 122, 204),
            ListEvenBackColor = Color.GhostWhite,
            ListOddBackColor = Color.White,
            DockBackColor = Color.FromArgb(238, 238, 242),
            CodeBackColor = Color.White,
            CodeIndentBackColor = Color.White,
            CodeLineNumberColor = Color.Maroon,
            IsDark = false
        };

        public static ThemePalette DarkPalette { get; } = new()
        {
            Name = ProfileDark,
            FormBackColor = Color.FromArgb(30, 30, 30),
            SurfaceBackColor = Color.FromArgb(37, 37, 38),
            InputBackColor = Color.FromArgb(45, 45, 48),
            MenuBackColor = Color.FromArgb(45, 45, 48),
            ButtonBackColor = Color.FromArgb(62, 62, 66),
            HeaderBackColor = Color.FromArgb(63, 63, 70),
            TextColor = Color.FromArgb(241, 241, 241),
            MutedTextColor = Color.FromArgb(200, 200, 200),
            BorderColor = Color.FromArgb(80, 80, 84),
            AccentColor = Color.FromArgb(0, 122, 204),
            ListEvenBackColor = Color.FromArgb(45, 45, 48),
            ListOddBackColor = Color.FromArgb(37, 37, 38),
            DockBackColor = Color.FromArgb(30, 30, 30),
            CodeBackColor = Color.FromArgb(30, 30, 30),
            CodeIndentBackColor = Color.FromArgb(30, 30, 30),
            CodeLineNumberColor = Color.FromArgb(200, 200, 200),
            IsDark = true
        };

        public static ThemePalette AliceLightPalette { get; } = new()
        {
            Name = ProfileAliceLight,
            FormBackColor = Color.FromArgb(246, 250, 255),
            SurfaceBackColor = Color.FromArgb(239, 247, 255),
            InputBackColor = Color.White,
            MenuBackColor = Color.FromArgb(231, 241, 252),
            ButtonBackColor = Color.FromArgb(231, 241, 252),
            HeaderBackColor = Color.FromArgb(201, 226, 255),
            TextColor = Color.FromArgb(24, 34, 45),
            MutedTextColor = Color.FromArgb(90, 103, 117),
            BorderColor = Color.FromArgb(162, 190, 220),
            AccentColor = Color.FromArgb(39, 126, 201),
            ListEvenBackColor = Color.AliceBlue,
            ListOddBackColor = Color.White,
            DockBackColor = Color.FromArgb(238, 246, 255),
            CodeBackColor = Color.FromArgb(252, 254, 255),
            CodeIndentBackColor = Color.FromArgb(239, 247, 255),
            CodeLineNumberColor = Color.FromArgb(64, 97, 126),
            IsDark = false
        };

        public static bool IsDarkTheme => CurrentPalette.IsDark;

        public static ThemePalette CurrentPalette => GetPalette(CurrentProfileName);

        public static string CurrentProfileName
        {
            get
            {
                string profile = NormalizeProfileName(DEXConfig.ReadString(DEXConfig.TAG_THEME_PROFILE));
                if (!string.IsNullOrEmpty(profile))
                {
                    return profile;
                }

                return DEXConfig.ReadBoolean(DEXConfig.TAG_DARK_THEME) ? ProfileDark : ProfileLight;
            }
        }

        public static Color CurrentTextColor => CurrentPalette.TextColor;

        public static Color CurrentInputBackColor => CurrentPalette.InputBackColor;

        public static ThemePalette GetPalette(string profileName)
        {
            return NormalizeProfileName(profileName) switch
            {
                ProfileDark => DarkPalette.Clone(),
                ProfileAliceLight => AliceLightPalette.Clone(),
                ProfileCustom => LoadCustomPalette(),
                _ => LightPalette.Clone()
            };
        }

        public static void SaveThemeProfile(string profileName)
        {
            string normalized = NormalizeProfileName(profileName);
            if (string.IsNullOrEmpty(normalized))
            {
                normalized = ProfileLight;
            }

            DEXConfig.Save(DEXConfig.TAG_THEME_PROFILE, normalized);
            DEXConfig.Save(DEXConfig.TAG_DARK_THEME, GetPalette(normalized).IsDark.ToString().ToLowerInvariant());
        }

        public static void SaveCustomTheme(ThemePalette palette)
        {
            ThemePalette custom = palette.Clone(ProfileCustom);
            DEXConfig.Save(DEXConfig.TAG_THEME_CUSTOM_PALETTE, SerializePalette(custom));
            DEXConfig.Save(DEXConfig.TAG_THEME_PROFILE, ProfileCustom);
            DEXConfig.Save(DEXConfig.TAG_DARK_THEME, custom.IsDark.ToString().ToLowerInvariant());
        }

        public static Color ListItemBackColor(int index)
        {
            ThemePalette palette = CurrentPalette;
            return index % 2 == 0 ? palette.ListEvenBackColor : palette.ListOddBackColor;
        }

        public static void ApplyDockPanel(DockPanel dockPanel)
        {
            if (dockPanel == null)
            {
                return;
            }

            ThemePalette palette = CurrentPalette;
            if (dockPanel.Contents.Count == 0)
            {
                try
                {
                    dockPanel.Theme = palette.IsDark ? new VS2015DarkTheme() : new VS2015LightTheme();
                }
                catch (InvalidOperationException)
                {
                }
            }

            dockPanel.DockBackColor = palette.DockBackColor;
            dockPanel.BackColor = dockPanel.DockBackColor;
        }

        public static void ApplyControlTree(Control control)
        {
            ApplyControlTree(control, CurrentPalette);
        }

        public static void ApplyListViewItems(ListView listView)
        {
            if (listView == null)
            {
                return;
            }

            foreach (ListViewItem item in listView.Items)
            {
                ApplyListViewItem(item);
            }
        }

        public static void ApplyListViewItem(ListViewItem item)
        {
            if (item == null)
            {
                return;
            }

            int index = item.Tag is int sourceIndex ? sourceIndex : item.Index;
            Color backColor = ListItemBackColor(index);
            item.BackColor = backColor;
            item.ForeColor = CurrentTextColor;

            foreach (ListViewItem.ListViewSubItem subItem in item.SubItems)
            {
                subItem.BackColor = backColor;
                subItem.ForeColor = CurrentTextColor;
            }
        }

        public static void ApplyToolStripItem(ToolStripItem item)
        {
            ApplyToolStripItem(item, CurrentPalette);
        }

        private static void ApplyControlTree(Control control, ThemePalette palette)
        {
            if (control == null)
            {
                return;
            }

            ControlThemeState state = ControlStates.GetValue(control, CreateControlState);
            if (palette.UsesOriginalColors)
            {
                RestoreControl(control, state);
            }
            else
            {
                ApplyPaletteControl(control, state, palette);
            }

            if (control is ToolStrip toolStrip)
            {
                ApplyToolStrip(toolStrip, palette);
            }

            if (control.ContextMenuStrip != null)
            {
                ApplyControlTree(control.ContextMenuStrip, palette);
            }

            foreach (Control child in control.Controls)
            {
                ApplyControlTree(child, palette);
            }
        }

        private static ControlThemeState CreateControlState(Control control)
        {
            ControlThemeState state = new()
            {
                BackColor = control.BackColor,
                ForeColor = control.ForeColor
            };

            if (control is ButtonBase buttonBase)
            {
                state.UseVisualStyleBackColor = buttonBase.UseVisualStyleBackColor;
                state.HasUseVisualStyleBackColor = true;
            }

            return state;
        }

        private static void ApplyPaletteControl(Control control, ControlThemeState state, ThemePalette palette)
        {
            control.ForeColor = palette.TextColor;

            switch (control)
            {
                case Form:
                    control.BackColor = palette.FormBackColor;
                    break;
                case MenuStrip:
                case ContextMenuStrip:
                case ToolStrip:
                    control.BackColor = palette.MenuBackColor;
                    break;
                case FastColoredTextBox textBox:
                    ApplyFastTextBox(textBox, palette);
                    break;
                case TextBoxBase:
                case ComboBox:
                case ListBox:
                case ListView:
                    control.BackColor = palette.InputBackColor;
                    break;
                case CheckBox:
                case RadioButton:
                    control.BackColor = ParentBackColor(control, palette);
                    if (control is ButtonBase optionButton)
                    {
                        optionButton.UseVisualStyleBackColor = false;
                    }
                    break;
                case ButtonBase buttonBase:
                    buttonBase.UseVisualStyleBackColor = false;
                    control.BackColor = palette.ButtonBackColor;
                    break;
                case Label label:
                    control.BackColor = state.BackColor == LightHeaderBackColor
                        ? palette.HeaderBackColor
                        : ParentBackColor(control, palette);
                    label.ForeColor = palette.TextColor;
                    break;
                case SplitContainer splitContainer:
                    splitContainer.BackColor = palette.BorderColor;
                    break;
                case Panel:
                    control.BackColor = palette.SurfaceBackColor;
                    break;
                default:
                    control.BackColor = ParentBackColor(control, palette);
                    break;
            }
        }

        private static void RestoreControl(Control control, ControlThemeState state)
        {
            control.BackColor = state.BackColor;
            control.ForeColor = state.ForeColor;

            if (control is ButtonBase buttonBase && state.HasUseVisualStyleBackColor)
            {
                buttonBase.UseVisualStyleBackColor = state.UseVisualStyleBackColor;
            }

            if (control is FastColoredTextBox textBox)
            {
                RestoreFastTextBox(textBox);
            }
        }

        private static void ApplyFastTextBox(FastColoredTextBox textBox, ThemePalette palette)
        {
            _ = FastTextBoxStates.GetValue(textBox, CreateFastTextBoxState);
            textBox.BackColor = palette.CodeBackColor;
            textBox.ForeColor = palette.TextColor;
            textBox.IndentBackColor = palette.CodeIndentBackColor;
            textBox.LineNumberColor = palette.CodeLineNumberColor;
            textBox.SelectionColor = Color.FromArgb(80, palette.AccentColor);
            textBox.DisabledColor = Color.FromArgb(100, palette.MutedTextColor);
        }

        private static void RestoreFastTextBox(FastColoredTextBox textBox)
        {
            FastTextBoxThemeState state = FastTextBoxStates.GetValue(textBox, CreateFastTextBoxState);
            textBox.BackColor = state.BackColor;
            textBox.ForeColor = state.ForeColor;
            textBox.IndentBackColor = state.IndentBackColor;
            textBox.LineNumberColor = state.LineNumberColor;
            textBox.SelectionColor = state.SelectionColor;
            textBox.DisabledColor = state.DisabledColor;
        }

        private static FastTextBoxThemeState CreateFastTextBoxState(FastColoredTextBox textBox)
        {
            return new FastTextBoxThemeState
            {
                BackColor = textBox.BackColor,
                ForeColor = textBox.ForeColor,
                IndentBackColor = textBox.IndentBackColor,
                LineNumberColor = textBox.LineNumberColor,
                SelectionColor = textBox.SelectionColor,
                DisabledColor = textBox.DisabledColor
            };
        }

        private static void ApplyToolStrip(ToolStrip toolStrip, ThemePalette palette)
        {
            ToolStripThemeState state = ToolStripStates.GetValue(toolStrip, CreateToolStripState);
            if (palette.UsesOriginalColors)
            {
                toolStrip.Renderer = state.Renderer;
                toolStrip.RenderMode = state.RenderMode;
            }
            else
            {
                toolStrip.RenderMode = ToolStripRenderMode.Professional;
                toolStrip.Renderer = new ToolStripProfessionalRenderer(new ThemedColorTable(palette));
            }

            foreach (ToolStripItem item in toolStrip.Items)
            {
                ApplyToolStripItem(item, palette);
            }
        }

        private static ToolStripThemeState CreateToolStripState(ToolStrip toolStrip)
        {
            return new ToolStripThemeState
            {
                Renderer = toolStrip.Renderer,
                RenderMode = toolStrip.RenderMode
            };
        }

        private static void ApplyToolStripItem(ToolStripItem item, ThemePalette palette)
        {
            ToolStripItemThemeState state = ToolStripItemStates.GetValue(item, CreateToolStripItemState);
            if (palette.UsesOriginalColors)
            {
                item.BackColor = state.BackColor;
                item.ForeColor = state.ForeColor;
            }
            else
            {
                item.BackColor = palette.MenuBackColor;
                item.ForeColor = palette.TextColor;
            }

            if (item is ToolStripMenuItem menuItem)
            {
                foreach (ToolStripItem child in menuItem.DropDownItems)
                {
                    ApplyToolStripItem(child, palette);
                }
            }
        }

        private static ToolStripItemThemeState CreateToolStripItemState(ToolStripItem item)
        {
            return new ToolStripItemThemeState
            {
                BackColor = item.BackColor,
                ForeColor = item.ForeColor
            };
        }

        private static Color ParentBackColor(Control control, ThemePalette palette)
        {
            return control.Parent?.BackColor ?? palette.SurfaceBackColor;
        }

        private static string NormalizeProfileName(string profileName)
        {
            foreach (string candidate in ProfileNames)
            {
                if (candidate.Equals(profileName, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            return string.Empty;
        }

        private static ThemePalette LoadCustomPalette()
        {
            ThemePalette fallback = DarkPalette.Clone(ProfileCustom);
            string raw = DEXConfig.ReadString(DEXConfig.TAG_THEME_CUSTOM_PALETTE);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return fallback;
            }

            string[] sections = raw.Split('|', 2);
            if (sections.Length != 2)
            {
                return fallback;
            }

            string[] colors = sections[1].Split(';');
            if (colors.Length != 15)
            {
                return fallback;
            }

            ThemePalette palette = fallback.Clone(ProfileCustom);
            palette.IsDark = bool.TryParse(sections[0], out bool isDark) ? isDark : fallback.IsDark;
            palette.FormBackColor = ParseColor(colors[0], fallback.FormBackColor);
            palette.SurfaceBackColor = ParseColor(colors[1], fallback.SurfaceBackColor);
            palette.InputBackColor = ParseColor(colors[2], fallback.InputBackColor);
            palette.MenuBackColor = ParseColor(colors[3], fallback.MenuBackColor);
            palette.ButtonBackColor = ParseColor(colors[4], fallback.ButtonBackColor);
            palette.HeaderBackColor = ParseColor(colors[5], fallback.HeaderBackColor);
            palette.TextColor = ParseColor(colors[6], fallback.TextColor);
            palette.MutedTextColor = ParseColor(colors[7], fallback.MutedTextColor);
            palette.BorderColor = ParseColor(colors[8], fallback.BorderColor);
            palette.AccentColor = ParseColor(colors[9], fallback.AccentColor);
            palette.ListEvenBackColor = ParseColor(colors[10], fallback.ListEvenBackColor);
            palette.ListOddBackColor = ParseColor(colors[11], fallback.ListOddBackColor);
            palette.DockBackColor = ParseColor(colors[12], fallback.DockBackColor);
            palette.CodeBackColor = ParseColor(colors[13], fallback.CodeBackColor);
            palette.CodeIndentBackColor = ParseColor(colors[14], fallback.CodeIndentBackColor);
            palette.CodeLineNumberColor = palette.MutedTextColor;
            return palette;
        }

        private static string SerializePalette(ThemePalette palette)
        {
            Color[] colors =
            {
                palette.FormBackColor,
                palette.SurfaceBackColor,
                palette.InputBackColor,
                palette.MenuBackColor,
                palette.ButtonBackColor,
                palette.HeaderBackColor,
                palette.TextColor,
                palette.MutedTextColor,
                palette.BorderColor,
                palette.AccentColor,
                palette.ListEvenBackColor,
                palette.ListOddBackColor,
                palette.DockBackColor,
                palette.CodeBackColor,
                palette.CodeIndentBackColor
            };

            return palette.IsDark.ToString().ToLowerInvariant() + "|"
                + string.Join(";", colors.Select(FormatColor));
        }

        private static string FormatColor(Color color)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{color.R},{color.G},{color.B}");
        }

        private static Color ParseColor(string value, Color fallback)
        {
            string[] parts = value.Split(',');
            if (parts.Length != 3)
            {
                return fallback;
            }

            return byte.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte r)
                && byte.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte g)
                && byte.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out byte b)
                ? Color.FromArgb(r, g, b)
                : fallback;
        }

        public sealed class ThemePalette
        {
            public string Name { get; set; } = ProfileLight;
            public bool UsesOriginalColors { get; set; }
            public bool IsDark { get; set; }
            public Color FormBackColor { get; set; }
            public Color SurfaceBackColor { get; set; }
            public Color InputBackColor { get; set; }
            public Color MenuBackColor { get; set; }
            public Color ButtonBackColor { get; set; }
            public Color HeaderBackColor { get; set; }
            public Color TextColor { get; set; }
            public Color MutedTextColor { get; set; }
            public Color BorderColor { get; set; }
            public Color AccentColor { get; set; }
            public Color ListEvenBackColor { get; set; }
            public Color ListOddBackColor { get; set; }
            public Color DockBackColor { get; set; }
            public Color CodeBackColor { get; set; }
            public Color CodeIndentBackColor { get; set; }
            public Color CodeLineNumberColor { get; set; }

            public ThemePalette Clone(string name = null)
            {
                ThemePalette clone = (ThemePalette)MemberwiseClone();
                if (!string.IsNullOrEmpty(name))
                {
                    clone.Name = name;
                }

                return clone;
            }
        }

        private sealed class ControlThemeState
        {
            public Color BackColor { get; init; }
            public Color ForeColor { get; init; }
            public bool UseVisualStyleBackColor { get; set; }
            public bool HasUseVisualStyleBackColor { get; set; }
        }

        private sealed class ToolStripThemeState
        {
            public ToolStripRenderer Renderer { get; init; }
            public ToolStripRenderMode RenderMode { get; init; }
        }

        private sealed class ToolStripItemThemeState
        {
            public Color BackColor { get; init; }
            public Color ForeColor { get; init; }
        }

        private sealed class FastTextBoxThemeState
        {
            public Color BackColor { get; init; }
            public Color ForeColor { get; init; }
            public Color IndentBackColor { get; init; }
            public Color LineNumberColor { get; init; }
            public Color SelectionColor { get; init; }
            public Color DisabledColor { get; init; }
        }

        private sealed class ThemedColorTable : ProfessionalColorTable
        {
            private readonly ThemePalette palette;

            public ThemedColorTable(ThemePalette palette)
            {
                this.palette = palette;
            }

            public override Color ToolStripDropDownBackground => palette.MenuBackColor;
            public override Color MenuBorder => palette.BorderColor;
            public override Color MenuItemBorder => palette.AccentColor;
            public override Color MenuItemSelected => palette.HeaderBackColor;
            public override Color MenuItemSelectedGradientBegin => palette.HeaderBackColor;
            public override Color MenuItemSelectedGradientEnd => palette.HeaderBackColor;
            public override Color MenuItemPressedGradientBegin => palette.HeaderBackColor;
            public override Color MenuItemPressedGradientMiddle => palette.HeaderBackColor;
            public override Color MenuItemPressedGradientEnd => palette.HeaderBackColor;
            public override Color ImageMarginGradientBegin => palette.MenuBackColor;
            public override Color ImageMarginGradientMiddle => palette.MenuBackColor;
            public override Color ImageMarginGradientEnd => palette.MenuBackColor;
            public override Color CheckBackground => palette.HeaderBackColor;
            public override Color CheckSelectedBackground => palette.HeaderBackColor;
            public override Color CheckPressedBackground => palette.HeaderBackColor;
            public override Color SeparatorDark => palette.BorderColor;
            public override Color SeparatorLight => palette.BorderColor;
            public override Color ToolStripBorder => palette.BorderColor;
            public override Color ToolStripGradientBegin => palette.MenuBackColor;
            public override Color ToolStripGradientMiddle => palette.MenuBackColor;
            public override Color ToolStripGradientEnd => palette.MenuBackColor;
        }
    }
}
