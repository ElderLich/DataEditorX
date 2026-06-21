using FastColoredTextBoxNS;
using System.Runtime.CompilerServices;
using WeifenLuo.WinFormsUI.Docking;

namespace DataEditorX.Config
{
    public static class ThemeManager
    {
        private static readonly Color FormBackColor = Color.FromArgb(30, 30, 30);
        private static readonly Color SurfaceBackColor = Color.FromArgb(37, 37, 38);
        private static readonly Color InputBackColor = Color.FromArgb(45, 45, 48);
        private static readonly Color MenuBackColor = Color.FromArgb(45, 45, 48);
        private static readonly Color ButtonBackColor = Color.FromArgb(62, 62, 66);
        private static readonly Color HeaderBackColor = Color.FromArgb(63, 63, 70);
        private static readonly Color TextColor = Color.FromArgb(241, 241, 241);
        private static readonly Color MutedTextColor = Color.FromArgb(200, 200, 200);
        private static readonly Color BorderColor = Color.FromArgb(80, 80, 84);
        private static readonly Color AccentColor = Color.FromArgb(0, 122, 204);
        private static readonly Color LightHeaderBackColor = Color.FromArgb(192, 192, 255);

        private static readonly ConditionalWeakTable<Control, ControlThemeState> ControlStates = new();
        private static readonly ConditionalWeakTable<ToolStrip, ToolStripThemeState> ToolStripStates = new();
        private static readonly ConditionalWeakTable<ToolStripItem, ToolStripItemThemeState> ToolStripItemStates = new();
        private static readonly ConditionalWeakTable<FastColoredTextBox, FastTextBoxThemeState> FastTextBoxStates = new();

        public static bool IsDarkTheme
        {
            get
            {
                try
                {
                    return DEXConfig.ReadBoolean(DEXConfig.TAG_DARK_THEME);
                }
                catch
                {
                    return false;
                }
            }
        }

        public static Color ListItemBackColor(int index)
        {
            if (!IsDarkTheme)
            {
                return index % 2 == 0 ? Color.GhostWhite : Color.White;
            }

            return index % 2 == 0 ? InputBackColor : SurfaceBackColor;
        }

        public static Color CurrentTextColor => IsDarkTheme ? TextColor : SystemColors.WindowText;

        public static Color CurrentInputBackColor => IsDarkTheme ? InputBackColor : SystemColors.Window;

        public static void ApplyDockPanel(DockPanel dockPanel)
        {
            if (dockPanel == null)
            {
                return;
            }

            if (dockPanel.Contents.Count == 0)
            {
                try
                {
                    dockPanel.Theme = IsDarkTheme ? new VS2015DarkTheme() : new VS2015LightTheme();
                }
                catch (InvalidOperationException)
                {
                }
            }

            dockPanel.DockBackColor = IsDarkTheme ? FormBackColor : Color.FromArgb(238, 238, 242);
            dockPanel.BackColor = dockPanel.DockBackColor;
        }

        public static void ApplyControlTree(Control control)
        {
            ApplyControlTree(control, IsDarkTheme);
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
            ApplyToolStripItem(item, IsDarkTheme);
        }

        private static void ApplyControlTree(Control control, bool isDarkTheme)
        {
            if (control == null)
            {
                return;
            }

            ControlThemeState state = ControlStates.GetValue(control, CreateControlState);
            if (isDarkTheme)
            {
                ApplyDarkControl(control, state);
            }
            else
            {
                RestoreControl(control, state);
            }

            if (control is ToolStrip toolStrip)
            {
                ApplyToolStrip(toolStrip, isDarkTheme);
            }

            if (control.ContextMenuStrip != null)
            {
                ApplyControlTree(control.ContextMenuStrip, isDarkTheme);
            }

            foreach (Control child in control.Controls)
            {
                ApplyControlTree(child, isDarkTheme);
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

        private static void ApplyDarkControl(Control control, ControlThemeState state)
        {
            control.ForeColor = TextColor;

            switch (control)
            {
                case Form:
                    control.BackColor = FormBackColor;
                    break;
                case MenuStrip:
                case ContextMenuStrip:
                case ToolStrip:
                    control.BackColor = MenuBackColor;
                    break;
                case FastColoredTextBox textBox:
                    ApplyFastTextBox(textBox, true);
                    break;
                case TextBoxBase:
                case ComboBox:
                case ListBox:
                case ListView:
                    control.BackColor = InputBackColor;
                    break;
                case CheckBox:
                case RadioButton:
                    control.BackColor = ParentBackColor(control);
                    if (control is ButtonBase optionButton)
                    {
                        optionButton.UseVisualStyleBackColor = false;
                    }
                    break;
                case ButtonBase buttonBase:
                    buttonBase.UseVisualStyleBackColor = false;
                    control.BackColor = ButtonBackColor;
                    break;
                case Label label:
                    control.BackColor = state.BackColor == LightHeaderBackColor ? HeaderBackColor : ParentBackColor(control);
                    label.ForeColor = TextColor;
                    break;
                case SplitContainer splitContainer:
                    splitContainer.BackColor = BorderColor;
                    break;
                case Panel:
                    control.BackColor = SurfaceBackColor;
                    break;
                default:
                    control.BackColor = ParentBackColor(control);
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
                ApplyFastTextBox(textBox, false);
            }
        }

        private static void ApplyFastTextBox(FastColoredTextBox textBox, bool isDarkTheme)
        {
            FastTextBoxThemeState state = FastTextBoxStates.GetValue(textBox, CreateFastTextBoxState);
            if (isDarkTheme)
            {
                textBox.BackColor = Color.FromArgb(30, 30, 30);
                textBox.ForeColor = TextColor;
                textBox.IndentBackColor = Color.FromArgb(30, 30, 30);
                textBox.LineNumberColor = MutedTextColor;
                textBox.SelectionColor = Color.FromArgb(80, AccentColor);
                textBox.DisabledColor = Color.FromArgb(100, 180, 180, 180);
            }
            else
            {
                textBox.BackColor = state.BackColor;
                textBox.ForeColor = state.ForeColor;
                textBox.IndentBackColor = state.IndentBackColor;
                textBox.LineNumberColor = state.LineNumberColor;
                textBox.SelectionColor = state.SelectionColor;
                textBox.DisabledColor = state.DisabledColor;
            }
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

        private static void ApplyToolStrip(ToolStrip toolStrip, bool isDarkTheme)
        {
            ToolStripThemeState state = ToolStripStates.GetValue(toolStrip, CreateToolStripState);
            if (isDarkTheme)
            {
                toolStrip.RenderMode = ToolStripRenderMode.Professional;
                toolStrip.Renderer = new ToolStripProfessionalRenderer(new DarkColorTable());
            }
            else
            {
                toolStrip.Renderer = state.Renderer;
                toolStrip.RenderMode = state.RenderMode;
            }

            foreach (ToolStripItem item in toolStrip.Items)
            {
                ApplyToolStripItem(item, isDarkTheme);
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

        private static void ApplyToolStripItem(ToolStripItem item, bool isDarkTheme)
        {
            ToolStripItemThemeState state = ToolStripItemStates.GetValue(item, CreateToolStripItemState);
            if (isDarkTheme)
            {
                item.BackColor = MenuBackColor;
                item.ForeColor = TextColor;
            }
            else
            {
                item.BackColor = state.BackColor;
                item.ForeColor = state.ForeColor;
            }

            if (item is ToolStripMenuItem menuItem)
            {
                foreach (ToolStripItem child in menuItem.DropDownItems)
                {
                    ApplyToolStripItem(child, isDarkTheme);
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

        private static Color ParentBackColor(Control control)
        {
            return control.Parent?.BackColor ?? SurfaceBackColor;
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

        private sealed class DarkColorTable : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground => MenuBackColor;
            public override Color MenuBorder => BorderColor;
            public override Color MenuItemBorder => AccentColor;
            public override Color MenuItemSelected => HeaderBackColor;
            public override Color MenuItemSelectedGradientBegin => HeaderBackColor;
            public override Color MenuItemSelectedGradientEnd => HeaderBackColor;
            public override Color MenuItemPressedGradientBegin => HeaderBackColor;
            public override Color MenuItemPressedGradientMiddle => HeaderBackColor;
            public override Color MenuItemPressedGradientEnd => HeaderBackColor;
            public override Color ImageMarginGradientBegin => MenuBackColor;
            public override Color ImageMarginGradientMiddle => MenuBackColor;
            public override Color ImageMarginGradientEnd => MenuBackColor;
            public override Color CheckBackground => HeaderBackColor;
            public override Color CheckSelectedBackground => HeaderBackColor;
            public override Color CheckPressedBackground => HeaderBackColor;
            public override Color SeparatorDark => BorderColor;
            public override Color SeparatorLight => BorderColor;
            public override Color ToolStripBorder => BorderColor;
            public override Color ToolStripGradientBegin => MenuBackColor;
            public override Color ToolStripGradientMiddle => MenuBackColor;
            public override Color ToolStripGradientEnd => MenuBackColor;
        }
    }
}
