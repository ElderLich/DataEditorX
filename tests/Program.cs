using DataEditorX;
using DataEditorX.Config;
using DataEditorX.Core;
using System.Reflection;
using System.Windows.Forms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        string root = Environment.GetEnvironmentVariable("DATAEDITORX_TEST_DATA_DIR")
            ?? Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../DataEditorX/data"));
        string db = Path.Combine(Path.GetTempPath(), $"dataeditor-regression-{Guid.NewGuid():N}.cdb");
        try
        {
            DataBase.Create(db);
            Card first = new(1) { name = "First", atk = 1500, def = 1200 };
            Card second = new(2) { name = "Second", atk = 2000, def = 1000 };
            DataBase.Command(db, DataBase.GetInsertSQL(first, true));
            using var form = new DataEditForm(root);
            form.InitControl(new DataConfig(Path.Combine(root, "languages/cardinfo_english.txt")));
            form.Open(db);
            form.SetCard(second);
            DataBase.Command(db, DataBase.GetInsertSQL(second, true));
            form.RefreshAddedCard(second);
            Assert(form.GetCardList(false).Length == 2, "First add after opening must retain the existing card and display the new card");
            form.Search(true);
            Assert(form.GetCardList(false).Length == 2, "Refreshing the initial unfiltered list must not filter ATK/DEF to zero");
            Console.WriteLine("PASS: first add and initial search refresh retain all cards.");
            Card cyborg = new(120000001) { name = "Cyborg", race = 0x80000000L };
            form.SetCard(cyborg);
            Assert(form.GetCard().race == cyborg.race, "Cyborg race must not become negative when read from the UI");
            cyborg.race = int.MinValue;
            DataBase.Command(db, DataBase.GetInsertSQL(cyborg, true));
            Card raceFilter = new(0) { race = 0x80000000L };
            Card[] found = DataBase.Read(db, true, DataBase.GetSelectSQL(raceFilter));
            Assert(found.Length == 1 && found[0].race == 0x80000000L, "Legacy signed Cyborg rows must be found and normalized");
            Console.WriteLine("PASS: Cyborg UI round-trip and legacy signed-race search.");
            Card rush = new(120000010) { name = "Rush Legend", ot = 4, type = 0x29, desc = "RD/KP01-JP001\n[REQUIREMENT] None.\n[EFFECT] Draw 1 card." };
            form.SetCard(rush);
            Assert(Control<ComboBox>(form, "cb_cardrule").Text == "Rush", "Rush card IDs must display the Rush rule");
            Assert(form.GetCard().ot == 4 && form.GetCard().type == rush.type && form.GetCard().desc == rush.desc,
                "Rush editing must preserve OT, Legend bits and metadata text");
            DataBase.Command(db, DataBase.GetInsertSQL(rush, true));
            Card[] rushMatches = DataBase.Read(db, true, DataBase.GetSelectSQL(new Card(0) { ot = 0x100 }));
            Assert(rushMatches.Length == 2 && rushMatches.All(c => c.IsRushDuelCard), "Rush searches must use the ID range, including upstream OT=4 cards");
            Console.WriteLine("PASS: Rush selector, metadata round-trip and ID-range filtering.");
            using var editor = new FastColoredTextBoxNS.FastColoredTextBoxEx { Size = new System.Drawing.Size(280, 160) };
            editor.ShowTooltipWithLabel("Card.IsRace", string.Join("\n", Enumerable.Repeat("Function details and an example.", 40)), 150);
            var help = editor.Controls["functionHelp"]!;
            Assert(editor.ClientRectangle.Contains(help.Bounds), "Function help must stay inside a narrow editor viewport");
            Assert(((Panel)help).AutoScroll && editor.lbTooltip.Height > help.Height, "Long function help must remain scrollable");
            Assert(!editor.Selection.ColumnSelectionMode, "Normal text selection must be the default");
            Console.WriteLine("PASS: bounded scrollable code help and normal text selection.");
            VerifyCardTypeCoverage(root);
            VerifyGenreCoverage(root);
            VerifySelectionReusesMetadataLayout(root);
            VerifyMetadataLayout(root);
            VerifyResizePaging(root);
            VerifyDockedMetadataLayout(root);
            VerifyReferenceCompletions(root);
            VerifyLegacyMetadataWithoutExtraControls(root);
            VerifyThemeReadability();
            VerifyThemePreview();
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); File.Delete(db); }
    }
    private static void VerifySelectionReusesMetadataLayout(string root)
    {
        using var form = new DataEditForm(root) { Opacity = 0, ShowInTaskbar = false };
        var config = new DataConfig(Path.Combine(root, "languages/cardinfo_english.txt"));
        form.InitControl(config);
        form.Show();
        var first = new Card(10001) { name = "First selection", desc = "First text", ot = 1, type = 0x21, category = 1, atk = 1200, def = 800 };
        var second = new Card(10002) { name = "Second selection", desc = "Second text", ot = 4, type = 0x11, category = 0x800000000L, atk = 1800, def = 900 };
        first.Str[0] = "First prompt";
        second.Str[0] = "Second prompt";
        var rush = new Card(120000010) { name = "Rush selection", ot = 4, type = 0x8029, category = 0x100, atk = 2500, def = 1000 };
        form.SetCards([first, second, rush], false);
        var list = Control<ListView>(form, "lv_cardlist");
        var genres = Control<FlowLayoutPanel>(form, "pl_category");
        var types = Control<FlowLayoutPanel>(form, "pl_cardtype");
        list.Items[0].Selected = true;
        Application.DoEvents();
        genres.ScrollControlIntoView(genres.Controls[genres.Controls.Count - 1]);
        var scroll = genres.AutoScrollPosition;
        var bounds = genres.Bounds;
        var checkbox = types.Controls.OfType<CheckBox>().First();
        int layouts = 0;
        genres.Layout += (_, _) => layouts++;
        for (int i = 0; i < 12; i++)
        {
            list.SelectedIndices.Clear();
            int row = (i + 1) % 2;
            list.Items[row].Selected = true;
            var expected = row == 0 ? first : second;
            var actual = form.GetCard();
            Assert(actual.id == expected.id && actual.name == expected.name && actual.desc == expected.desc
                && actual.ot == expected.ot && actual.type == expected.type && actual.category == expected.category
                && actual.atk == expected.atk && actual.def == expected.def && actual.Str[0] == expected.Str[0],
                "Card selection must synchronously load all current card values");
        }
        Assert(layouts == 0, $"Selecting cards in the same format must not rebuild the metadata layout ({layouts} layout events)");
        Assert(genres.Bounds == bounds && genres.AutoScrollPosition == scroll && ReferenceEquals(checkbox, types.Controls[0]),
            "Card selection must preserve metadata scroll position, bounds and checkbox controls");
        list.SelectedIndices.Clear();
        list.Items[2].Selected = true;
        Assert(Control<Label>(form, "rushHelp").Visible && layouts > 0 && form.GetCard().type == rush.type,
            "Entering Rush must still arrange and display its help and type bits");
        layouts = 0;
        form.SetCard(rush);
        Assert(layouts == 0, "Selecting another Rush card must reuse the Rush layout");
        list.SelectedIndices.Clear();
        list.Items[1].Selected = true;
        Assert(!Control<Label>(form, "rushHelp").Visible && layouts > 0,
            "Leaving Rush must hide its help and reclaim the space");
        layouts = 0;
        form.InitControl(config);
        Assert(layouts > 0 && genres.Controls.Count == config.dicCardcategorys.Count,
            "Reloading language/configuration must still measure newly created checkboxes");
        form.Close();
        Console.WriteLine("PASS: card selection reuses stable metadata layout and scroll state; Rush transitions and configuration reloads still reflow.");
    }

    private static void VerifyDockedMetadataLayout(string root)
    {
        using var host = new MainForm { Opacity = 0, ShowInTaskbar = false };
        // Exercise the real MainForm/DockPanel geometry without loading user
        // databases, preferences or automatic startup actions in this regression.
        var startup = typeof(MainForm).GetMethod("MainForm_Load", BindingFlags.Instance | BindingFlags.NonPublic)!;
        host.Load -= (EventHandler)Delegate.CreateDelegate(typeof(EventHandler), host, startup);
        var dock = (WeifenLuo.WinFormsUI.Docking.DockPanel)typeof(MainForm)
            .GetProperty("dockPanel", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(host)!;
        using var form = new DataEditForm(root);
        form.InitControl(new DataConfig(Path.Combine(root, "languages/cardinfo_english.txt")));
        DataEditorX.Language.LanguageHelper.SetFormLabel(form);
        host.Show();
        form.Show(dock, WeifenLuo.WinFormsUI.Docking.DockState.Document);
        string output = Environment.GetEnvironmentVariable("DATAEDITORX_LAYOUT_PREVIEW_DIR");
        foreach (var scenario in new[] { ("DockedMinimum", 992, 697), ("DockedDefault", 992, 749),
            ("DockedWide", 1400, 749), ("DockedShrunk", 992, 697) })
        {
            host.ClientSize = new System.Drawing.Size(scenario.Item2, scenario.Item3);
            Application.DoEvents();
            ThemeManager.ApplyControlTree(host, ThemeManager.GetPalette(ThemeManager.ProfilePaper));
            VerifyCardLabels(form, scenario.Item1);
            var column = Control<Panel>(form, "metadataColumn");
            var genres = Control<FlowLayoutPanel>(form, "pl_category");
            var hostView = host.RectangleToScreen(host.ClientRectangle);
            Assert(hostView.Contains(column.RectangleToScreen(column.ClientRectangle)),
                scenario.Item1 + ": the entire metadata column and its right-hand scrollbars must fit in MainForm");
            var delete = Control<Button>(form, "btn_del");
            Assert(hostView.Contains(delete.RectangleToScreen(delete.ClientRectangle)),
                scenario.Item1 + ": the rightmost action button must stay on screen");
            var last = genres.Controls[genres.Controls.Count - 1];
            genres.ScrollControlIntoView(last);
            Application.DoEvents();
            Assert(last.Bottom <= genres.ClientSize.Height && last.Top >= 0,
                scenario.Item1 + ": the final Genre entry must remain reachable in the actual docked editor");
            if (!string.IsNullOrEmpty(output))
            {
                Directory.CreateDirectory(output);
                using var bitmap = new System.Drawing.Bitmap(host.Width, host.Height);
                host.DrawToBitmap(bitmap, new System.Drawing.Rectangle(0, 0, host.Width, host.Height));
                bitmap.Save(Path.Combine(output, scenario.Item1 + ".png"));
                using var editorBitmap = new System.Drawing.Bitmap(form.Width, form.Height);
                form.DrawToBitmap(editorBitmap, new System.Drawing.Rectangle(0, 0, form.Width, form.Height));
                editorBitmap.Save(Path.Combine(output, scenario.Item1.Replace("Docked", "DockedEditor") + ".png"));
            }
        }
        form.Show(dock, new System.Drawing.Rectangle(-20000, -20000, 600, 400));
        var floating = form.Pane.FloatWindow;
        floating.Opacity = 0;
        floating.Size = new System.Drawing.Size(600, 400);
        Application.DoEvents();
        Assert(floating.Width >= 1007 && floating.Height >= 640 && form.MinimumSize.IsEmpty,
            "Floating host keeps a usable minimum without enforcing an oversized docked child");
        form.DockState = WeifenLuo.WinFormsUI.Docking.DockState.Document;
        Application.DoEvents();
        Assert(host.RectangleToScreen(host.ClientRectangle).Contains(
            Control<Panel>(form, "metadataColumn").RectangleToScreen(Control<Panel>(form, "metadataColumn").ClientRectangle)),
            "Redocking after floating must still fit the actual host viewport");
        form.Close();
        host.Close();
        Console.WriteLine("PASS: actual MainForm docking keeps metadata scrollbars, final Genre row and rightmost actions visible at minimum/default/wide/shrunk sizes.");
    }

    private static void VerifyResizePaging(string root)
    {
        string database = Path.Combine(Path.GetTempPath(), $"dataeditor-resize-{Guid.NewGuid():N}.cdb");
        try
        {
            DataBase.Create(database);
            for (uint id = 1; id <= 80; id++)
                DataBase.Command(database, DataBase.GetInsertSQL(new Card(id) { name = "Card " + id }, true));
            using var form = new DataEditForm(root) { Opacity = 0, ShowInTaskbar = false };
            form.InitControl(new DataConfig(Path.Combine(root, "languages/cardinfo_english.txt")));
            form.Show();
            form.Open(database);
            Application.DoEvents();
            var list = Control<ListView>(form, "lv_cardlist");
            var name = Control<TextBox>(form, "tb_cardname");
            list.Items[10].Selected = true;
            Application.DoEvents();
            var selected = list.SelectedItems[0];
            uint cardId = form.GetOldCard().id;
            name.Text = "Unsaved resize draft";
            var size = form.ClientSize;
            for (int width = size.Width + 10; width <= size.Width + 200; width += 10)
                form.ClientSize = new System.Drawing.Size(width, size.Height);
            Application.DoEvents();
            Assert(ReferenceEquals(selected, list.SelectedItems[0]),
                "Width-only resizing must retain the selected ListViewItem without rebuilding rows");
            Assert(name.Text == "Unsaved resize draft" && form.GetOldCard().id == cardId,
                "Width-only resizing must preserve the active card and its unsaved draft");
            int oldCount = list.Items.Count;
            form.ClientSize = new System.Drawing.Size(form.ClientSize.Width, size.Height + 160);
            Application.DoEvents();
            Assert(list.Items.Count > oldCount, "A taller card list must recalculate rows after its viewport changes");
            Assert(list.SelectedItems.Count == 1 && list.SelectedItems[0].Text == cardId.ToString(),
                "Repaging after a height change must preserve the selected card");
            Assert(name.Text == "Unsaved resize draft", "Restoring resize selection must not reload the database card over a draft");
            form.ClientSize = size;
            Application.DoEvents();
            Assert(list.SelectedItems.Count == 1 && list.SelectedItems[0].Text == cardId.ToString()
                && name.Text == "Unsaved resize draft", "Shrinking must retain selection and draft too");
            form.Close();
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); File.Delete(database); }
        Console.WriteLine("PASS: width-only resize reuses rows; height repaging retains selection and unsaved edits.");
    }

    private static void VerifyMetadataLayout(string root)
    {
        using var form = new DataEditForm(root) { Opacity = 0, ShowInTaskbar = false };
        var config = new DataConfig(Path.Combine(root, "languages/cardinfo_english.txt"));
        form.InitControl(config);
        form.Show();
        ThemeManager.ApplyControlTree(form, ThemeManager.GetPalette(ThemeManager.ProfilePaper));
        string output = Environment.GetEnvironmentVariable("DATAEDITORX_LAYOUT_PREVIEW_DIR");
        foreach (string language in new[] { "chinese", "english" })
        {
            DataEditorX.Language.LanguageHelper.LoadFormLabels(Path.Combine(root, $"languages/language_{language}.txt"));
            DataEditorX.Language.LanguageHelper.SetFormLabel(form);
            Assert(Control<Label>(form, "lb_types").Text == (language == "chinese" ? "卡片类型" : "Card Type"),
                "Layout wrappers must preserve existing localized heading keys");
            Assert(Control<Label>(form, "lb_cardcode").Text == (language == "chinese" ? "卡片密码" : "Passcode"),
                "Layout wrappers must preserve existing localized field keys");
            Assert(Control<Label>(form, "lb_tiptexts").Text == (language == "chinese" ? "脚本提示文字" : "Strings"),
                "String-list localization must survive reparenting");
            VerifyCardLabels(form, language);
            var markerLabel = Control<Label>(form, "lb_markers");
            Console.WriteLine($"MARKER {language}: text={TextRenderer.MeasureText(markerLabel.Text, markerLabel.Font)}; available={markerLabel.ClientSize}");
            if (!string.IsNullOrEmpty(output))
            {
                Directory.CreateDirectory(output);
                using var bitmap = new System.Drawing.Bitmap(form.Width, form.Height);
                form.DrawToBitmap(bitmap, new System.Drawing.Rectangle(0, 0, form.Width, form.Height));
                bitmap.Save(Path.Combine(output, "Labels-" + language + ".png"));
            }
        }
        Application.DoEvents();
        var split = Control<SplitContainer>(form, "splitContainer");
        var column = Control<Panel>(form, "metadataColumn");
        var fields = Control<Panel>(form, "cardFields");
        var types = Control<FlowLayoutPanel>(form, "pl_cardtype");
        var genres = Control<FlowLayoutPanel>(form, "pl_category");
        var originalCheckboxes = types.Controls.Cast<System.Windows.Forms.Control>().ToArray();
        foreach (var scenario in new[] {
            ("Default", 991, 617, false), ("Narrow", 991, 601, false),
            ("Tall", 991, 900, false), ("Wide", 1400, 750, false),
            ("Shrunk", 991, 601, false), ("Regrown", 1400, 750, false),
            ("Rush", 991, 617, true), ("RushNarrow", 991, 601, true), ("RushWide", 1400, 750, true) })
        {
            form.ClientSize = new System.Drawing.Size(scenario.Item2, scenario.Item3);
            form.SetCard(new Card(scenario.Item4 ? 120000010u : 42u)
                { name = "Layout check", type = scenario.Item4 ? 0x8029 : 0x21, category = 0x800000001L });
            Application.DoEvents();
            form.PerformLayout();
            VerifyCardLabels(form, scenario.Item1);
            Assert(split.Panel2.ClientRectangle.Contains(column.Bounds), scenario.Item1 + ": metadata column must fit its pane");
            Assert(fields.Right < column.Left, scenario.Item1 + ": field and metadata columns must not overlap");
            Assert(types.Controls.Cast<System.Windows.Forms.Control>().SequenceEqual(originalCheckboxes),
                "Resizing must retain the same checkbox instances and handlers");
            var visibleSections = column.Controls.Cast<System.Windows.Forms.Control>()
                .Where(control => control.Visible).OrderBy(control => control.Top).ToArray();
            foreach (var section in visibleSections)
                Assert(column.ClientRectangle.Contains(section.Bounds), scenario.Item1 + ": section must remain visible: " + section.Name);
            for (int i = 1; i < visibleSections.Length; i++)
                Assert(visibleSections[i - 1].Bottom <= visibleSections[i].Top,
                    scenario.Item1 + ": metadata sections must not overlap");
            Assert(Control<ListBox>(form, "lb_scripttext").Height >= 48 && Control<TextBox>(form, "tb_edittext").Height >= 48,
                "String list and selected-string editor must keep usable space");
            Assert(form.GetCard().category == 0x800000001L, "High Genre bits must survive reflow");
            if (scenario.Item4)
            {
                var help = Control<Label>(form, "rushHelp");
                Assert(help.Height >= help.GetPreferredSize(new System.Drawing.Size(help.Width, 0)).Height,
                    "Rush help must fit completely in the reserved section");
            }
            if (scenario.Item1 == "Default")
                Assert(types.Height > 120 && genres.Height > 150,
                    "Default layout must reclaim the empty flag region for Card Type and Genre");
            if (scenario.Item1 is "Tall" or "Wide" or "Regrown" or "RushWide")
            {
                Assert(!types.VerticalScroll.Visible && !genres.VerticalScroll.Visible,
                    $"{scenario.Item1}: all checkbox rows should fit without nested scrolling: types={types.Size}/{types.VerticalScroll.Visible}/{types.DisplayRectangle}; genres={genres.Size}/{genres.VerticalScroll.Visible}/{genres.DisplayRectangle}; column={column.Size}");
            }
            if (!string.IsNullOrEmpty(output))
            {
                Directory.CreateDirectory(output);
                using var bitmap = new System.Drawing.Bitmap(form.Width, form.Height);
                form.DrawToBitmap(bitmap, new System.Drawing.Rectangle(0, 0, form.Width, form.Height));
                bitmap.Save(Path.Combine(output, scenario.Item1 + ".png"));
            }
            foreach (var checkPanel in new[] { types, genres })
            {
                var last = checkPanel.Controls[checkPanel.Controls.Count - 1];
                checkPanel.ScrollControlIntoView(last);
                Application.DoEvents();
                Assert(last.Bottom <= checkPanel.ClientSize.Height && last.Top >= 0,
                    $"{scenario.Item1}: {checkPanel.Name} final checkbox must be reachable: last={last.Bounds}, viewport={checkPanel.ClientSize}, scroll={checkPanel.AutoScrollPosition}, content={checkPanel.DisplayRectangle}");
                Assert(checkPanel.Controls.Cast<System.Windows.Forms.Control>().All(box => box.Right <= checkPanel.ClientSize.Width),
                    scenario.Item1 + ": rightmost checkbox labels must not be clipped");
            }
            Console.WriteLine($"LAYOUT {scenario.Item1}: column={column.Size}; types={types.Height}; genres={genres.Height}; scroll={types.VerticalScroll.Visible}/{genres.VerticalScroll.Visible}");
        }
        int wideColumn = column.Width;
        split.SplitterDistance = split.Width - split.SplitterWidth - split.Panel2MinSize;
        Application.DoEvents();
        Assert(column.Width < wideColumn && fields.Right < column.Left, "Dragging the splitter must reflow both editing columns");
        split.SplitterDistance = split.Panel1MinSize;
        Application.DoEvents();
        Assert(column.Width > wideColumn && !types.VerticalScroll.Visible && !genres.VerticalScroll.Visible,
            "Expanding the edit pane with the splitter must remove unnecessary checkbox scrollbars");
        foreach (var checkPanel in new[] { types, genres })
            Assert(checkPanel.Controls.Cast<System.Windows.Forms.Control>().All(box =>
                box.Bottom <= checkPanel.ClientSize.Height && box.Right <= checkPanel.ClientSize.Width),
                "Expanded splitter view must fit every checkbox and its label");
        form.Close();
        Console.WriteLine("PASS: responsive layout, scrolling, splitter changes and legacy localization work across nine size/format scenarios.");
    }

    private static void VerifyCardLabels(DataEditForm form, string scenario)
    {
        var label = Control<Label>(form, "lb_markers");
        var markers = Control<FlowLayoutPanel>(form, "pl_markers");
        var text = TextRenderer.MeasureText(label.Text, label.Font);
        Assert(text.Width <= label.ClientSize.Width - label.Padding.Horizontal
            && text.Height <= label.ClientSize.Height - label.Padding.Vertical,
            scenario + ": Link Markers must fit on one line without clipping");
        Assert(label.Right < markers.Left && Math.Abs(label.Top + label.Height / 2 - markers.Top - markers.Height / 2) <= 1,
            scenario + ": Link Markers must be centered beside its checkbox grid");
        Assert(!label.Bounds.IntersectsWith(Control<Label>(form, "lb_cardalias").Bounds),
            scenario + ": Link Markers must not overlap the Alias row");
        foreach (string field in new[] { "cardalias", "cardcode" })
        {
            var fieldLabel = Control<Label>(form, "lb_" + field);
            var input = Control<TextBox>(form, "tb_" + field);
            var fieldText = TextRenderer.MeasureText(fieldLabel.Text, fieldLabel.Font);
            Assert(fieldText.Width <= fieldLabel.Width && fieldText.Height <= fieldLabel.Height,
                scenario + ": " + fieldLabel.Text + " must fit on one line");
            Assert(fieldLabel.Right + 4 == input.Left && fieldLabel.Top == input.Top && fieldLabel.Height == input.Height,
                scenario + ": card identifier labels must align with their inputs with a consistent gap");
            Assert(!fieldLabel.Bounds.IntersectsWith(Control<Panel>(form, "pl_image").Bounds)
                && !fieldLabel.Bounds.IntersectsWith(markers.Bounds),
                scenario + ": card identifier labels must not overlap the cover or marker grid");
        }
    }

    private static void VerifyCardTypeCoverage(string root)
    {
        // MDPro3 CardType contains 25 standard bits; RDLegend/RDMaximum supply
        // the two otherwise-unused bits, covering every flag from 0x1 to 0x4000000.
        foreach (string file in Directory.GetFiles(Path.Combine(root, "languages"), "cardinfo_*.txt"))
        {
            var config = new DataConfig(file);
            long mask = config.dicCardTypes.Keys.Aggregate(0L, (combined, value) => combined | value);
            Assert(mask == 0x7ffffffL && config.dicCardTypes.Count == 27,
                $"All standard and Rush type flags must be available: {file}");

        }
        using var form = new DataEditForm(root);
        form.InitControl(new DataConfig(Path.Combine(root, "languages/cardinfo_english.txt")));
        const long unexposedType = 0x8000000L;
        form.SetCard(new Card(42) { type = unexposedType | 0x21 });
        var effect = Control<FlowLayoutPanel>(form, "pl_cardtype").Controls.OfType<CheckBox>()
            .Single(box => (long)box.Tag == 0x20);
        effect.Checked = false;
        Assert(form.GetCard().type == (unexposedType | 0x1),
            "Editing a known type must preserve noneditable type bits without restoring its checkbox");
        Console.WriteLine("PASS: all 27 standard/Rush types are present in every language; unexposed type bits survive edits.");
    }

    private static void VerifyGenreCoverage(string root)
    {
        foreach (string file in Directory.GetFiles(Path.Combine(root, "languages"), "cardinfo_*.txt"))
        {
            var config = new DataConfig(file);
            long genres = config.dicCardcategorys.Keys.Aggregate(0L, (combined, value) => combined | value);
            long races = config.dicCardRaces.Keys.Aggregate(0L, (combined, value) => combined | value);
            Assert(genres == 0xfffffffffL && config.dicCardcategorys.Count == 36,
                $"All 36 existing editor Genre flags must load, including its four extended flags: {file}");
            Assert(races == 0xffffffffL && config.dicCardRaces.Count == 33,
                $"All 32 standard/Rush race bits and the empty option must load: {file}");
        }
        using var form = new DataEditForm(root);
        form.InitControl(new DataConfig(Path.Combine(root, "languages/cardinfo_chinese.txt")));
        const long unexposedGenre = 0x10000000000L;
        form.SetCard(new Card(42) { category = unexposedGenre | 0x800000001L });
        var first = Control<FlowLayoutPanel>(form, "pl_category").Controls.OfType<CheckBox>()
            .Single(box => (long)box.Tag == 1L);
        first.Checked = false;
        Assert(form.GetCard().category == (unexposedGenre | 0x800000000L),
            "Genre editing must retain both existing extended and noneditable metadata bits");
        Console.WriteLine("PASS: every locale loads all 36 editor Genre flags and 32 races; hidden Genre metadata survives editing.");
    }

    private static void VerifyThemePreview()
    {
        string savedProfile = ThemeManager.CurrentProfileName;
        using var dialog = new ThemeSettingsForm();
        var profile = (ComboBox)typeof(ThemeSettingsForm).GetField("cbProfile", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(dialog)!;
        var code = (FastColoredTextBoxNS.FastColoredTextBoxEx)typeof(ThemeSettingsForm).GetField("previewCode", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(dialog)!;
        foreach (string name in new[] { ThemeManager.ProfileMidnight, ThemeManager.ProfilePaper, ThemeManager.ProfileForest })
        {
            profile.SelectedItem = name;
            Assert(code.BackColor == ThemeManager.GetPalette(name).CodeBackColor, "Preview must update immediately when a preset is selected");
            Assert(ThemeManager.CurrentProfileName == savedProfile, "Preview selection must not persist a new theme");
            string output = Environment.GetEnvironmentVariable("DATAEDITORX_THEME_PREVIEW_DIR");
            if (!string.IsNullOrEmpty(output))
            {
                Directory.CreateDirectory(output);
                dialog.Opacity = 0;
                dialog.Show();
                Application.DoEvents();
                dialog.PerformLayout();
                using var bitmap = new System.Drawing.Bitmap(dialog.Width, dialog.Height);
                dialog.DrawToBitmap(bitmap, new System.Drawing.Rectangle(0, 0, dialog.Width, dialog.Height));
                bitmap.Save(Path.Combine(output, name + ".png"));
            }
        }
        dialog.DialogResult = DialogResult.Cancel;
        Assert(ThemeManager.CurrentProfileName == savedProfile, "Cancel must leave the saved theme unchanged");
        Console.WriteLine("PASS: live theme previews update without saving; Cancel preserves the active theme.");
    }

    private static void VerifyThemeReadability()
    {
        using var editor = new FastColoredTextBoxNS.FastColoredTextBoxEx();
        var highlighter = (FastColoredTextBoxNS.MySyntaxHighlighter)editor.SyntaxHighlighter;
        highlighter.SetKnownFunctions(["RD.Draw", "RushDuel.Draw"]);
        editor.Text = "local text = \"Ready\"\n-- comment\nRD.Draw()\nRushDuel.Draw()\n123";
        highlighter.LuaSyntaxHighlight(editor.Range);
        foreach (string name in ThemeManager.ProfileNames.Where(n => n != ThemeManager.ProfileCustom))
        {
            var palette = ThemeManager.GetPalette(name);
            ThemeManager.ApplyControlTree(editor, palette);
            foreach ((int line, int column) in new[] { (0, 0), (0, 14), (1, 0), (2, 0), (3, 0), (4, 0) })
            {
                int styles = (int)editor[line][column].style;
                var ink = editor.Styles.Select((style, index) => (style, index))
                    .Where(pair => (styles & (1 << pair.index)) != 0)
                    .Select(pair => pair.style).OfType<FastColoredTextBoxNS.TextStyle>().FirstOrDefault();
                Assert(ink?.ForeBrush is System.Drawing.SolidBrush, $"{name}: Lua/Rush token must be highlighted");
                var foreground = ((System.Drawing.SolidBrush)ink!.ForeBrush).Color;
                Assert(Contrast(foreground, editor.BackColor) >= 4.5, $"{name}: syntax text must remain readable after switching palettes");
            }
            Assert(Contrast(editor.CaretColor, editor.BackColor) >= 4.5, $"{name}: caret must contrast with editor background");
        }
        Console.WriteLine("PASS: theme switching retains Lua/Rush highlighting, readable syntax and a visible caret without saving preferences.");
    }

    private static double Contrast(System.Drawing.Color a, System.Drawing.Color b)
    {
        double Luminance(System.Drawing.Color color)
        {
            double Linear(byte component) { double v = component / 255.0; return v <= .04045 ? v / 12.92 : Math.Pow((v + .055) / 1.055, 2.4); }
            return .2126 * Linear(color.R) + .7152 * Linear(color.G) + .0722 * Linear(color.B);
        }
        double x = Luminance(a), y = Luminance(b);
        return (Math.Max(x, y) + .05) / (Math.Min(x, y) + .05);
    }

    private static void VerifyLegacyMetadataWithoutExtraControls(string root)
    {
        foreach (string file in Directory.GetFiles(Path.Combine(root, "languages"), "cardinfo_*.txt"))
            Assert(!File.ReadAllText(file).Contains("##flags"), "Removed flag configuration must not remain in bundled languages");
        string database = Path.Combine(Path.GetTempPath(), $"dataeditor-legacy-{Guid.NewGuid():N}.bytes");
        try
        {
            Assert(DataBase.Create(database), "Temporary legacy database must be created");
            Card legacy = new(42) { name = "Legacy card", ot = 4, omega = [1L, 0x40100L, 0x11223344L], script = "-- preserved legacy script" };
            Assert(DataBase.Command(database, DataBase.OmegaGetInsertSQL(legacy, true)) == 2, "Legacy sample must be inserted");
            using var form = new DataEditForm(root);
            form.InitControl(new DataConfig(Path.Combine(root, "languages/cardinfo_english.txt")));
            form.Open(database);
            form.SetCard(DataBase.Read(database, true, "").Single());
            foreach (string name in new[] { "pl_flags", "lb_flags", "tb_support", "lb_support" })
                Assert(form.Controls.Find(name, true).Length == 0, "Removed simulator-specific control must not be constructed: " + name);
            Control<TextBox>(form, "tb_cardname").Text = "Edited common field";
            Card edited = form.GetCard();
            Assert(edited.omega.SequenceEqual(legacy.omega) && edited.script == legacy.script,
                "Editing common fields must retain all hidden legacy flags, support and script data");
            Assert(DataBase.Command(database, DataBase.OmegaGetUpdateSQL(edited)) > 0, "Edited legacy card must save");
            Card saved = DataBase.Read(database, true, "").Single();
            Assert(saved.name == "Edited common field" && saved.omega.SequenceEqual(legacy.omega) && saved.script == legacy.script,
                "Saving after UI removal must retain legacy metadata while applying the common-field edit");
            Console.WriteLine("PASS: simulator-specific controls/config are removed; legacy metadata and scripts survive common-field editing.");
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); File.Delete(database); }
    }

    private static void VerifyReferenceCompletions(string root)
    {
        var config = new CodeConfig();
        config.AddFunction(Path.Combine(root, "lua/_function_english.txt"));
        config.AddFunction(Path.Combine(root, "lua/_rush_function_english.txt"));
        config.AddConstant(Path.Combine(root, "lua/constant.lua"));
        config.AddConstant(Path.Combine(root, "lua/rush_constant.lua"));
        config.InitAutoMenus();
        string[] expected = ["Card.GetCardType", "Card.IsHasEffect", "RD.CostSendHandToGrave", "RushDuel.CostSendHandToGrave", "TYPE_MAXIMUM", "RACE_CYBORG"];
        foreach (string name in expected)
        {
            Assert(config.LongTooltipDic.ContainsKey(name), $"Reference must load {name}");
            Assert(config.Items.Count(item => item.ToolTipTitle == name) == 1, $"Completion must expose {name} exactly once");
        }
        Assert(config.LongTooltipDic["TYPE_MAXIMUM"].Contains("0x8000") && config.LongTooltipDic["RACE_CYBORG"].Contains("0x80000000"),
            "Rush constants must preserve the generated bit values");
        Assert(config.Items.All(item => !string.IsNullOrWhiteSpace(item.ToolTipTitle)), "Blank lines must not create empty completion items");
        Assert(config.Items.All(item => System.Text.RegularExpressions.Regex.IsMatch(item.ToolTipTitle, @"^[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*$")),
            "Invalid completion keys: " + string.Join("; ", config.Items.Where(item => !System.Text.RegularExpressions.Regex.IsMatch(item.ToolTipTitle, @"^[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*$")).Select(item => item.ToolTipTitle).Take(12)));
        Assert(config.TooltipDic["CostSendHandToGrave"].Split("Source: Rush/Utilities/RDCost.lua").Length == 2,
            "The RD alias must not duplicate the full documentation in the shared short-name tooltip");
        string costHelp = config.TooltipDic["CostSendHandToGrave"];
        config.AddFunction(Path.Combine(root, "lua/_rush_function_english.txt"));
        Assert(config.TooltipDic["CostSendHandToGrave"] == costHelp, "Reloading a reference must not multiply its documentation");
        string sample = Path.GetTempFileName();
        try
        {
            File.WriteAllText(sample, "\n   -- IGNORE_COMMENT = 1\nnot an assignment\nend\n  VALID_TEST = 0x20 -- A test constant\n");
            var parser = new CodeConfig();
            parser.AddConstant(sample);
            Assert(parser.LongTooltipDic.Count == 1 && parser.LongTooltipDic.ContainsKey("VALID_TEST"),
                "Constant loading must accept indented assignments and ignore blank lines, indented comments and statements");
        }
        finally { File.Delete(sample); }
        Console.WriteLine($"PASS: {config.Items.Length} real standard/Rush completions, aliases and constants load without bogus keys or repeated help.");
    }

    private static T Control<T>(DataEditForm form, string name) where T : System.Windows.Forms.Control
        => (T)typeof(DataEditForm).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(form)!;

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
