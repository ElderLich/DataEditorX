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
        }
        finally { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); File.Delete(db); }
    }
    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
