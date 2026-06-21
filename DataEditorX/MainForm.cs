/*
 * Created with SharpDevelop.
 * User: Acer
 * Date: 2014-10-20
 * Time: 9:19
 * 
 */
using DataEditorX.Config;
using DataEditorX.Controls;
using DataEditorX.Core;
using DataEditorX.Language;
using WeifenLuo.WinFormsUI.Docking;

namespace DataEditorX
{
    public partial class MainForm : Form, IMainForm
    {
        #region member
        // History.
        History history;
        // Data directory.
        string datapath;
        // Language configuration.
        string conflang;
        // Database comparison tabs.
        DataEditForm compare1, compare2;
        // Temporary card buffer.
        Card[] tCards;
        // Editor configuration.
        DataConfig datacfg = null;
        private DataConfig olddatacfg = null;
        CodeConfig codecfg = null;
        // Pending file to open.
        string openfile;
        readonly List<ToolStripMenuItem> themeProfileMenuItems = new();
        #endregion
        public DataConfig GetDataConfig(bool old = false) {
            return old ? olddatacfg : datacfg;
        }
        public CodeConfig GetCodeConfig() {
            return codecfg;
        }
        #region UI and language initialization
        public MainForm()
        {
            // Initialize controls.
            InitializeComponent();
            InitializeThemeMenu();
            ApplyTheme();
        }
        public void SetDataPath(string datapath)
        {
            // Validate the configured path.
            if (string.IsNullOrEmpty(datapath))
            {
                return;
            }

            tCards = null;
            // Data directory.
            this.datapath = datapath;
            if (DEXConfig.ReadBoolean(DEXConfig.TAG_ASYNC))
            {
                // Load data in the background.
                bgWorker1.RunWorkerAsync();
            }
            else
            {
                Init();
                InitForm();
            }
        }
        void CheckUpdate()
        {
            TaskHelper.CheckVersion(false);
        }
        public void SetOpenFile(string file)
        {
            openfile = file;
        }
        void Init()
        {
            // File paths.
            conflang = DEXConfig.GetLanguageFile(datapath);
            // Game data.
            olddatacfg = datacfg = new DataConfig(DEXConfig.GetCardInfoFile(datapath));
            string confstring = MyPath.FindFile(datapath, DEXConfig.FILE_STRINGS, "lua");
            if (File.Exists(confstring))
            {
                Dictionary<long, string> d = datacfg.dicSetnames;
                if (!d.ContainsKey(0)) d.Add(0L, "Archetype");
                ArchetypeStringsService.MergeSetnames(d, confstring);
                ArchetypeStringsService.MergeSetnames(d, ArchetypeStringsService.GetCustomStringsFile());
            }
            // Initialize YGOUtil data.
            YGOUtil.SetConfig(datacfg);

            // Code hints.
            string funtxt = DEXConfig.GetFunctionFile(datapath);
            string conlua = MyPath.FindFile(datapath, DEXConfig.FILE_CONSTANT, "lua");
            codecfg = new CodeConfig();
            // Add functions.
            codecfg.AddFunction(funtxt);
            // Add counters.
            codecfg.AddStrings(confstring);
            codecfg.AddStrings(ArchetypeStringsService.GetCustomStringsFile());
            // Add constants.
            codecfg.AddConstant(conlua);
            codecfg.SetNames(datacfg.dicSetnames);
            // Build menus.
            codecfg.InitAutoMenus();
            history = new History(this);
            // Read history.
            history.ReadHistory(MyPath.Combine(datapath, DEXConfig.FILE_HISTORY));
            // Load localization.
            LanguageHelper.LoadFormLabels(conflang);
        }
        void InitForm()
        {
            LanguageHelper.SetFormLabel(this);
            InitializeThemeMenu();

            // Apply language to all open windows.
            DockContentCollection contents = dockPanel.Contents;
            foreach (DockContent dc in contents.Cast<DockContent>())
            {
                if (dc is not null)
                {
                    LanguageHelper.SetFormLabel(dc);
                }
            }
            // Build history menus.
            history.MenuHistory();
            ApplyTheme();

            // Open an empty database tab when no file is pending.
            if (string.IsNullOrEmpty(openfile))
            {
                OpenDataBase(null);
            }
            else
            {
                Open(openfile);
            }
        }
        #endregion

        #region Open history
        // Clear CDB history.
        public void CdbMenuClear()
        {
            menuitem_history.DropDownItems.Clear();
        }
        // Clear Lua history.
        public void LuaMenuClear()
        {
            menuitem_shistory.DropDownItems.Clear();
        }
        // Add CDB history item.
        public void AddCdbMenu(ToolStripItem item)
        {
            _ = menuitem_history.DropDownItems.Add(item);
            ThemeManager.ApplyToolStripItem(item);
        }
        // Add Lua history item.
        public void AddLuaMenu(ToolStripItem item)
        {
            _ = menuitem_shistory.DropDownItems.Add(item);
            ThemeManager.ApplyToolStripItem(item);
        }
        #endregion

        #region Window messages
        protected override void DefWndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case DEXConfig.WM_OPEN:// Handle open-file message.
                    string file = MyPath.Combine(Application.StartupPath, DEXConfig.FILE_TEMP);
                    if (File.Exists(file))
                    {
                        Activate();
                        string openfile = File.ReadAllText(file);
                        // Read the file path to open.
                        Open(openfile);
                        //File.Delete(file);
                    }
                    break;
                default:
                    base.DefWndProc(ref m);
                    break;
            }
        }
        #endregion

        #region Open files
        // Open script.
        void OpenScript(string file)
        {
            CodeEditForm cf = new();
            // Apply UI language.
            LanguageHelper.SetFormLabel(cf);
            // Set CDB history list.
            cf.SetCDBList(history.GetcdbHistory());
            // Initialize function hints.
            cf.InitTooltip(codecfg);
            // Open file.
            DataEditForm df;
            try { df = (DataEditForm)dockPanel.ActiveContent; }
            catch { df = null; }
            if (df != null) cf.SetCardDB(df.GetOpenFile());
            if (!string.IsNullOrEmpty(file) && (file.IndexOf('\n') > -1 || file.IndexOf("function ") > -1))
            {
                if (long.TryParse(file.Split("```")[0], out long tmp)) cf.nowCode = tmp;
                cf.Controls["fctb"].Text = file.IndexOf("```") > -1 ? file.Split("```")[1] : file;
            }
            else _ = cf.Open(file, df == null ? "cards" : Path.GetFileNameWithoutExtension(df.GetOpenFile()));
            cf.ApplyTheme();
            cf.Show(dockPanel, DockState.Document);
        }
        // Open database.
        void OpenDataBase(string file)
        {
            DataEditForm def;
            if (string.IsNullOrEmpty(file) || !File.Exists(file))
            {
                def = new DataEditForm(datapath, "", datacfg);
            }
            else
            {
                def = new DataEditForm(datapath, file, datacfg);
            }
            // Apply language.
            LanguageHelper.SetFormLabel(def);
            // Initialize UI data.
            def.InitControl(datacfg);
            def.ApplyTheme();
            def.Show(dockPanel, DockState.Document);
        }
        // Open file.
        public void Open(string file)
        {
            if (file.IndexOf('\n') > -1)
            {
                OpenScript(file);
                return;
            }
            if (string.IsNullOrEmpty(file) || !File.Exists(file))
            {
                return;
            }
            // Add history.
            history.AddHistory(file);
            // Check whether the file is already open.
            if (FindEditForm(file, true))
            {
                return;
            }
            // Reuse a compatible blank editor when available.
            if (FindEditForm(file, false))
            {
                return;
            }

            if (YGOUtil.IsScript(file))
            {
                OpenScript(file);
            }
            else if (YGOUtil.IsDataBase(file))
            {
                OpenDataBase(file);
            }
        }
        // Check whether a matching editor is open.
        bool FindEditForm(string file, bool isOpen)
        {
            DockContentCollection contents = dockPanel.Contents;
            // Scan all tabs.
            foreach (DockContent dc in contents.Cast<DockContent>())
            {
                if (dc is not IEditForm edform)
                {
                    continue;
                }

                if (isOpen)// Check opened files.
                {
                    if (file != null && file.Equals(edform.GetOpenFile()))
                    {
                        edform.SetActived();
                        return true;
                    }
                }
                else// Use a blank compatible tab for the file.
                {
                    if (string.IsNullOrEmpty(edform.GetOpenFile()) && edform.CanOpen(file))
                    {
                        _ = edform.Open(file, Path.GetFileNameWithoutExtension(openfile));
                        edform.SetActived();
                        return true;
                    }
                }
            }
            return false;
        }
        #endregion

        #region Window management
        // Close current tab.
        void CloseToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (dockPanel.ActiveContent != null && dockPanel.ActiveContent.DockHandler != null)
            {
                dockPanel.ActiveContent.DockHandler.Close();
            }
        }
        // Open script editor.
        void Menuitem_codeeditorClick(object sender, EventArgs e)
        {
            OpenScript(null);
        }

        // Open project manager.
        void Menuitem_projectmanagerClick(object sender, EventArgs e)
        {
            OpenProjectManager();
        }

        void OpenProjectManager()
        {
            foreach (DockContent dc in dockPanel.Contents.Cast<DockContent>())
            {
                if (dc is ProjectManagerForm existing)
                {
                    existing.DockHandler.Activate();
                    return;
                }
            }

            ProjectManagerForm manager = new();
            LanguageHelper.SetFormLabel(manager);
            manager.ApplyTheme();
            manager.Show(dockPanel, DockState.Document);
        }

        // Create DataEditorX database.
        void DataEditorToolStripMenuItemClick(object sender, EventArgs e)
        {
            OpenDataBase(null);
        }
        // Close other or all tabs.
        void CloseMdi(bool isall)
        {
            DockContentCollection contents = dockPanel.Contents;
            int num = contents.Count - 1;
            try
            {
                while (num >= 0)
                {
                    if (contents[num].DockHandler.DockState == DockState.Document)
                    {
                        if (isall)
                        {
                            contents[num].DockHandler.Close();
                        }
                        else if (dockPanel.ActiveContent != contents[num])
                        {
                            contents[num].DockHandler.Close();
                        }
                    }
                    num--;
                }
            }
            catch { }
        }
        // Close other tabs.
        void CloseOtherToolStripMenuItemClick(object sender, EventArgs e)
        {
            CloseMdi(false);
        }
        // Close all tabs.
        void CloseAllToolStripMenuItemClick(object sender, EventArgs e)
        {
            CloseMdi(true);
        }
        #endregion

        #region File menu
        // Get the active data editor.
        DataEditForm GetActive()
        {
            DataEditForm df = dockPanel.ActiveContent as DataEditForm;
            return df;
        }
        // Open file.
        void Menuitem_openClick(object sender, EventArgs e)
        {
            using OpenFileDialog dlg = new();
            dlg.Title = LanguageHelper.GetMsg(LMSG.OpenFile);
            if (GetActive() != null || dockPanel.Contents.Count == 0)// Check whether the active tab is a data editor.
            {
                try
                {
                    dlg.Filter = LanguageHelper.GetMsg(LMSG.CdbType);
                }
                catch { }
            }
            else
            {
                try
                {
                    dlg.Filter = LanguageHelper.GetMsg(LMSG.ScriptFilter);
                }
                catch { }
            }

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                string file = dlg.FileName;
                Open(file);
            }
        }

        // Exit.
        void QuitToolStripMenuItemClick(object sender, EventArgs e)
        {
            Close();
        }
        // New file.
        void Menuitem_newClick(object sender, EventArgs e)
        {
            using SaveFileDialog dlg = new();
            dlg.Title = LanguageHelper.GetMsg(LMSG.NewFile);
            dlg.AddExtension = true;
            if (GetActive() != null || dockPanel.Contents.Count == 0)// Check whether the active tab is a data editor.
            {
                dlg.DefaultExt = "cdb";
                dlg.FilterIndex = 1;
                try
                {
                    dlg.Filter = LanguageHelper.GetMsg(LMSG.CdbType);
                }
                catch { }
            }
            else
            {
                dlg.DefaultExt = "lua";
                dlg.FilterIndex = 1;
                try
                {
                    dlg.Filter = LanguageHelper.GetMsg(LMSG.ScriptFilter);
                }
                catch { }
            }

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                string file = dlg.FileName;
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
                // Check whether the target is a database.
                if (YGOUtil.IsDataBase(file))
                {
                    if (DataBase.Create(file))// Check whether creation succeeded.
                    {
                        if (MyMsg.Question(LMSG.IfOpenDataBase))// Ask whether to open the new database.
                        {
                            Open(file);
                        }
                    }
                }
                else
                {
                    try
                    {
                        File.Create(file).Dispose();
                    }
                    catch { }
                    Open(file);
                }
            }
        }
        // Save file.
        void Menuitem_saveClick(object sender, EventArgs e)
        {
            if (dockPanel.ActiveContent is IEditForm cf)
            {
                if (cf.Save(false))// Check whether save succeeded.
                {
                    MyMsg.Show(LMSG.SaveFileOK);
                }
            }
        }
        void Menuitem_saveAsClick(object sender, EventArgs e)
        {
            if (dockPanel.ActiveContent is IEditForm cf)
            {
                if (cf.Save(true))// Check whether save succeeded.
                {
                    history.AddHistory(cf.GetOpenFile());
                    MyMsg.Show(LMSG.SaveFileOK);
                }
            }
        }
        #endregion

        #region Card copy and paste
        // Copy selected cards.
        void Menuitem_copyselecttoClick(object sender, EventArgs e)
        {
            DataEditForm df = GetActive();// Get the active data editor.
            if (df != null)
            {
                tCards = df.GetCardList(true); // Get selected cards.
                if (tCards != null)
                {
                    SetCopyNumber(tCards.Length);// Show copied card count.
                    MyMsg.Show(LMSG.CopyCards);
                }
            }
        }
        // Copy current result set.
        void Menuitem_copyallClick(object sender, EventArgs e)
        {
            DataEditForm df = GetActive();// Get the active data editor.
            if (df != null)
            {
                tCards = df.GetCardList(false);// Get all cards from the current results.
                if (tCards != null)
                {
                    SetCopyNumber(tCards.Length);// Show copied card count.
                    MyMsg.Show(LMSG.CopyCards);
                }
            }
        }
        // Show copied card count.
        void SetCopyNumber(int c)
        {
            string tmp = menuitem_pastecards.Text;
            int t = tmp.LastIndexOf(" (");
            if (t > 0)
            {
                tmp = tmp[..t];
            }

            tmp = tmp + " (" + c.ToString() + ")";
            menuitem_pastecards.Text = tmp;
        }
        // Paste cards.
        void Menuitem_pastecardsClick(object sender, EventArgs e)
        {
            if (tCards == null)
            {
                return;
            }

            DataEditForm df = GetActive();
            if (df == null)
            {
                return;
            }

            df.SaveCards(tCards);// Save cards.
            MyMsg.Show(LMSG.PasteCards);
        }

        #endregion

        #region Database comparison
        // Set first database.
        void Menuitem_comp1Click(object sender, EventArgs e)
        {
            compare1 = GetActive();
            if (compare1 != null && !string.IsNullOrEmpty(compare1.GetOpenFile()))
            {
                menuitem_comp2.Enabled = true;
                CompareDB();
            }
        }
        // Set second database.
        void Menuitem_comp2Click(object sender, EventArgs e)
        {
            compare2 = GetActive();
            if (compare2 != null && !string.IsNullOrEmpty(compare2.GetOpenFile()))
            {
                CompareDB();
            }
        }
        // Compare databases.
        void CompareDB()
        {
            if (compare1 == null || compare2 == null)
            {
                return;
            }

            string cdb1 = compare1.GetOpenFile();
            string cdb2 = compare2.GetOpenFile();
            if (string.IsNullOrEmpty(cdb1)
               || string.IsNullOrEmpty(cdb2)
               || cdb1 == cdb2)
            {
                return;
            }

            bool checktext = MyMsg.Question(LMSG.CheckText);
            // Compare both databases.
            compare1.CompareCards(cdb2, checktext);
            compare2.CompareCards(cdb1, checktext);
            MyMsg.Show(LMSG.CompareOK);
            menuitem_comp2.Enabled = false;
            compare1 = null;
            compare2 = null;
        }

        #endregion

        #region Background data loading
        private void BgWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            Init();
        }

        private void BgWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            // Update UI.
            InitForm();
        }
        #endregion

        private void DockPanel_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void DockPanel_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
            {
                foreach (string file in files)
                {
                    Open(file);
                }
            }
            else
            {
                string file = (string)e.Data.GetData(DataFormats.Text);
                if (file != null && File.Exists(file))
                {
                    Open(file);
                }
            }
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            string[] drops = (string[])e.Data.GetData(DataFormats.FileDrop);
            List<string> files = new();
            if (drops == null)
            {
                string file = (string)e.Data.GetData(DataFormats.Text);
                drops = [file];
            }
            foreach (string file in drops)
            {
                if (Directory.Exists(file))
                {
                    files.AddRange(Directory.EnumerateFiles(file, "*.cdb", SearchOption.AllDirectories));
                    files.AddRange(Directory.EnumerateFiles(file, "*.lua", SearchOption.AllDirectories));
                }
                else if (File.Exists(file))
                {
                    files.Add(file);
                }
            }
            if (files.Count > 5)
            {
                if (!MyMsg.Question(LMSG.IfOpenLotsOfFile))
                {
                    return;
                }
            }
            foreach (string file in files)
            {
                Open(file);
            }
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }
        private void Menuitem_darkthemeClick(object sender, EventArgs e)
        {
            ThemeManager.SaveThemeProfile(menuitem_darktheme.Checked ? ThemeManager.ProfileDark : ThemeManager.ProfileLight);
            ApplyTheme();
        }

        private void InitializeThemeMenu()
        {
            menuitem_darktheme.Click -= Menuitem_darkthemeClick;
            menuitem_darktheme.CheckOnClick = false;
            menuitem_darktheme.Text = "Theme";
            menuitem_darktheme.DropDownItems.Clear();
            themeProfileMenuItems.Clear();

            foreach (string profile in ThemeManager.ProfileNames)
            {
                ToolStripMenuItem item = new(profile)
                {
                    CheckOnClick = false,
                    Tag = profile
                };
                item.Click += Menuitem_themeProfileClick;
                themeProfileMenuItems.Add(item);
                _ = menuitem_darktheme.DropDownItems.Add(item);
            }

            _ = menuitem_darktheme.DropDownItems.Add(new ToolStripSeparator());
            ToolStripMenuItem customize = new("Customize...")
            {
                Name = "menuitem_theme_customize"
            };
            customize.Click += Menuitem_themeCustomizeClick;
            _ = menuitem_darktheme.DropDownItems.Add(customize);
        }

        private void Menuitem_themeProfileClick(object sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem item || item.Tag is not string profile)
            {
                return;
            }

            ThemeManager.SaveThemeProfile(profile);
            ApplyTheme();
        }

        private void Menuitem_themeCustomizeClick(object sender, EventArgs e)
        {
            using ThemeSettingsForm dialog = new();
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            ApplyTheme();
        }

        private void ApplyTheme()
        {
            menuitem_darktheme.Text = "Theme";
            foreach (ToolStripMenuItem item in themeProfileMenuItems)
            {
                item.Checked = item.Tag is string profile
                    && profile.Equals(ThemeManager.CurrentProfileName, StringComparison.OrdinalIgnoreCase);
            }

            ThemeManager.ApplyControlTree(this);
            ThemeManager.ApplyDockPanel(dockPanel);

            foreach (DockContent dc in dockPanel.Contents.Cast<DockContent>())
            {
                switch (dc)
                {
                    case DataEditForm dataEditForm:
                        dataEditForm.ApplyTheme();
                        break;
                    case CodeEditForm codeEditForm:
                        codeEditForm.ApplyTheme();
                        break;
                    case ProjectManagerForm projectManagerForm:
                        projectManagerForm.ApplyTheme();
                        break;
                    default:
                        ThemeManager.ApplyControlTree(dc);
                        break;
                }
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Check for updates.
            if (DEXConfig.ReadBoolean(DEXConfig.TAG_AUTO_CHECK_UPDATE))
            {
                Thread th = new(CheckUpdate)
                {
                    IsBackground = true// Stop the thread when the executable exits.
                };
                th.Start();
            }
        }
    }
}
