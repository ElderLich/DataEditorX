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
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../DataEditorX/data"));
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
            VerifyReferenceCompletions(root);
            VerifyOmegaMetadataWithoutRushCheckbox(root);
            VerifyThemeReadability();
            VerifyThemePreview();
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); File.Delete(db); }
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

    private static void VerifyOmegaMetadataWithoutRushCheckbox(string root)
    {
        foreach (string file in Directory.GetFiles(Path.Combine(root, "languages"), "cardinfo_*.txt"))
            Assert(new DataConfig(file).dicCardflags.Count == 0, $"Bundled flag controls must be empty after removing Rush: {file}");
        string database = Path.Combine(Path.GetTempPath(), $"dataeditor-omega-{Guid.NewGuid():N}.bytes");
        try
        {
            Assert(DataBase.Create(database), "Temporary Omega database must be created");
            Card legacy = new(42) { name = "Legacy Omega card", ot = 4, omega = [1L, 0x40100L, 0L] };
            Assert(DataBase.Command(database, DataBase.OmegaGetInsertSQL(legacy, true)) == 2, "Temporary Omega card must be inserted");
            var config = new DataConfig(Path.Combine(root, "languages/cardinfo_english.txt"));
            using var form = new DataEditForm(root);
            form.InitControl(config);
            form.Open(database);
            form.SetCard(DataBase.Read(database, true, "").Single());
            Assert(Control<FlowLayoutPanel>(form, "pl_flags").Controls.Count == 0, "The obsolete Rush checkbox must not be created");
            Card edited = form.GetCard();
            Assert(edited.omega[1] == legacy.omega[1], "Editing with no flag controls must preserve every stored Omega metadata bit");
            Assert(DataBase.Command(database, DataBase.OmegaGetUpdateSQL(edited)) > 0, "Edited Omega card must save");
            Assert(DataBase.Read(database, true, "").Single().omega[1] == legacy.omega[1], "Saving must retain legacy Rush and unrelated Omega flags");
            config.dicCardflags.Add(0x40000L, "Custom metadata");
            form.InitControl(config);
            form.SetCard(legacy);
            Control<FlowLayoutPanel>(form, "pl_flags").Controls.OfType<CheckBox>().Single().Checked = false;
            Assert(form.GetCard().omega[1] == 0x100L, "Custom flag controls must edit only their own bits and preserve hidden metadata");
            Console.WriteLine("PASS: removed Rush checkbox in every locale; Omega metadata survives edit/save and custom flag controls remain scoped.");
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
