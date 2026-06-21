/*
 * Created with SharpDevelop.
 * User: Acer
 * Date: May 12, Monday
 * Time: 12:00
 * 
 */
using DataEditorX.Config;
using DataEditorX.Language;
using System.Text;


namespace DataEditorX
{
    internal sealed class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            string arg = (args.Length > 0) ? args[0] : "";
            if (arg == DEXConfig.TAG_SAVE_LAGN || arg == DEXConfig.TAG_SAVE_LAGN2)
            {
                // Save generated language data.
                SaveLanguage();
                _ = MessageBox.Show("Save Language OK.");
                Environment.Exit(1);
            }
            if (DEXConfig.OpenOnExistForm(arg))// Open the file in an existing window.
            {
                Environment.Exit(1);
            }
            else// Create a new window.
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                MainForm mainForm = new();
                // Set pending file to open.
                mainForm.SetOpenFile(arg);
                // Set data directory.
                mainForm.SetDataPath(MyPath.Combine(Application.StartupPath, DEXConfig.TAG_DATA));

                Application.Run(mainForm);

                Dictionary<long, string> dic = mainForm.GetDataConfig().dicSetnames;
                Dictionary<long, string> old = mainForm.GetDataConfig(true).dicSetnames;
                foreach (long setcode in dic.Keys)
                {
                    if (old.ContainsKey(setcode))
                        continue;
                    string cardinfo = DEXConfig.GetCardInfoFile(MyPath.Combine(Application.StartupPath,
                        DEXConfig.TAG_DATA));
                    if (File.Exists(cardinfo))
                        using (FileStream cStream = new(cardinfo, FileMode.Open, FileAccess.ReadWrite))
                        {
                            try
                            {
                                byte[] content = Encoding.UTF8.GetBytes($"\n0x{setcode:x}\t{dic[setcode]}\n#end");
                                _ = cStream.Seek(-5, SeekOrigin.End);
                                cStream.Write(content, 0, content.Length);
                            }
                            catch { }
                            finally
                            {
                                cStream.Close();
                            }
                        }
                    string file = MyPath.FindFile(MyPath.Combine(Application.StartupPath, DEXConfig.TAG_DATA), DEXConfig.FILE_STRINGS, "lua");
                    if (!string.IsNullOrEmpty(file) && File.Exists(file))
                    {
                        using FileStream sStream = new(file, FileMode.Open, FileAccess.Write);
                        try
                        {
                            byte[] content = Encoding.UTF8.GetBytes($"!setname 0x{setcode:x} {dic[setcode]}\n");
                            _ = sStream.Seek(0, SeekOrigin.End);
                            sStream.Write(content, 0, content.Length);
                        }
                        catch { }
                        finally
                        {
                            sStream.Close();
                        }
                    }
                }
            }
        }
        static void SaveLanguage()
        {
            string datapath = MyPath.Combine(Application.StartupPath, DEXConfig.TAG_DATA);
            string conflang = DEXConfig.GetLanguageFile(datapath);
            LanguageHelper.LoadFormLabels(conflang);
            LanguageHelper langhelper = new();
            MainForm form1 = new();
            LanguageHelper.SetFormLabel(form1);
            langhelper.GetFormLabel(form1);
            DataEditForm form2 = new();
            LanguageHelper.SetFormLabel(form2);
            langhelper.GetFormLabel(form2);
            CodeEditForm form3 = new();
            LanguageHelper.SetFormLabel(form3);
            langhelper.GetFormLabel(form3);
            // LANG.GetFormLabel(this);
            // Read form text.
            _ = langhelper.SaveLanguage(conflang + ".bak");
        }

    }
}
