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
            VerifyReferenceCompletions(root);
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); File.Delete(db); }
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
