/*
 * Created with SharpDevelop.
 * User: Acer
 * Date: 2014-10-22
 * Time: 19:16
 * 
 */
using DataEditorX.Common;
using DataEditorX.Config;
using DataEditorX.Controls;
using DataEditorX.Core;
using DataEditorX.Language;
using FastColoredTextBoxNS;
using Neo.IronLua;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;
using WeifenLuo.WinFormsUI.Docking;

namespace DataEditorX
{
    /// <summary>
    /// Description of CodeEditForm.
    /// </summary>
    public partial class CodeEditForm : DockContent, IEditForm
    {
        #region Style
        SortedDictionary<long, string> cardlist;
        readonly MarkerStyle sameWordsStyle = new(new SolidBrush(Color.FromArgb(40, Color.White)));
        #endregion

        #region init Function tooltip menu
        //Autocomplete
        AutocompleteMenu popupMenu;
        string nowFile;
        public long nowCode;
        string title;
        string oldtext;
        SortedList<string, string> tooltipDic;
        AutocompleteItem[] items;
        bool tabisspaces = false;
        string nowcdb;
        public CodeEditForm()
        {
            InitForm();
        }

        void InitForm()
        {
            cardlist = new SortedDictionary<long, string>();
            tooltipDic = new SortedList<string, string>();
            InitializeComponent();
            //Apply font name and size
            string fontname = DEXConfig.ReadString(DEXConfig.TAG_FONT_NAME);
            float fontsize = DEXConfig.ReadFloat(DEXConfig.TAG_FONT_SIZE, fctb.Font.Size);
            fctb.Font = new Font(fontname, fontsize);
            if (DEXConfig.ReadBoolean(DEXConfig.TAG_IME))
            {
                fctb.ImeMode = ImeMode.On;
            }

            if (DEXConfig.ReadBoolean(DEXConfig.TAG_WORDWRAP))
            {
                fctb.WordWrap = true;
            }
            else
            {
                fctb.WordWrap = false;
            }

            if (DEXConfig.ReadBoolean(DEXConfig.TAG_TAB2SPACES))
            {
                tabisspaces = true;
            }
            else
            {
                tabisspaces = false;
            }

            Font ft = new(fctb.Font.Name, fctb.Font.Size / 1.2f, FontStyle.Regular);
            popupMenu = new AutocompleteMenu(fctb)
            {
                MinFragmentLength = 2
            };
            fctb.TextChanged += Fctb_TextChanged;
            popupMenu.ToolTip.Popup += ToolTip_Popup;
            popupMenu.Items.Font = ft;
            popupMenu.AutoSize = true;
            popupMenu.MinimumSize = new Size(300, 0);
            popupMenu.BackColor = fctb.BackColor;
            popupMenu.ForeColor = fctb.ForeColor;
            popupMenu.Closed += new ToolStripDropDownClosedEventHandler(PopupMenu_Closed);
            popupMenu.SelectedColor = Color.LightGray;
            popupMenu.VisibleChanged += PopupMenu_VisibleChanged;
            popupMenu.Opened += PopupMenu_VisibleChanged;
            popupMenu.Items.FocussedItemIndexChanged += Items_FocussedItemIndexChanged;
            title = Text;
            ApplyTheme();
        }

        private void Fctb_TextChanged(object sender, TextChangedEventArgs e)
        {
            PopupMenu_VisibleChanged(null, null);
        }

        private void ToolTip_Popup(object sender, PopupEventArgs e)
        {
            e.Cancel = true;
        }

        private void PopupMenu_VisibleChanged(object sender, EventArgs e)
        {
            if (!popupMenu.Visible)
            {
                AdjustPopupMenuSize();
                return;
            }
            if (popupMenu.Items.FocussedItem == null)
            {
                if (popupMenu.Items.Count == 0)
                {
                    return;
                }
                popupMenu.Items.FocussedItemIndex = 0;
            }
            fctb.ShowTooltipWithLabel(popupMenu.Items.FocussedItem.ToolTipTitle,
                popupMenu.Items.FocussedItem.ToolTipText);
            AdjustPopupMenuSize();
        }
        private void AdjustPopupMenuSize()
        {
            if (!popupMenu.Visible || popupMenu.Items.FocussedItem == null)
            {
                popupMenu.Size = new Size(300, 0);
                popupMenu.MinimumSize = new Size(300, 0);
                return;
            }
            Size s = TextRenderer.MeasureText(popupMenu.Items.FocussedItem.ToolTipTitle,
                popupMenu.Items.Font, new Size(0, 0), TextFormatFlags.NoPadding);
            s = new Size(s.Width + 50, popupMenu.Size.Height);
            if (popupMenu.Size.Width < s.Width)
            {
                popupMenu.Size = s;
                popupMenu.MinimumSize = s;
            }
        }
        private void Items_FocussedItemIndexChanged(object sender, EventArgs e)
        {
            if (popupMenu.Items.FocussedItem == null)
            {
                return;
            }
            AdjustPopupMenuSize();
            fctb.ShowTooltipWithLabel(popupMenu.Items.FocussedItem.ToolTipTitle,
                popupMenu.Items.FocussedItem.ToolTipText);
        }

        void PopupMenu_Closed(object sender, ToolStripDropDownClosedEventArgs e)
        {
            popupMenu.Items.SetAutocompleteItems(items);
        }
        #endregion

        #region IEditFormInterfaces
        public void SetActived()
        {
            Activate();
        }
        public bool CanOpen(string file)
        {
            return YGOUtil.IsScript(file);
        }
        public string GetOpenFile()
        {
            return nowFile;
        }
        public bool Create(string file)
        {
            return Open(file);
        }
        public bool Save(bool shift)
        {
            return SaveFile(shift);
        }
        public bool Open(string file, string dbname = "cards")
        {
            if (!string.IsNullOrEmpty(file))
            {
                if (!File.Exists(file))
                {
                    FileStream fs = new(file, FileMode.Create);
                    fs.Close();
                }
                nowFile = file;
                FileInfo fi = new(file);
                if (fi.Name.ToUpper().EndsWith(".LUA"))
                {
                    (fctb.SyntaxHighlighter as MySyntaxHighlighter).cCode
                        = fi.Name[..^4];
                }
                string cdb = MyPath.Combine(
                    Path.GetDirectoryName(file), "../cards.cdb");
                if (!File.Exists(cdb)) cdb = MyPath.Combine(
                    Path.GetDirectoryName(file), "..", dbname + ".cdb");
                if (!File.Exists(cdb)) cdb = MyPath.Combine(
                      Path.GetDirectoryName(file), "../Database.bytes");
                if (!File.Exists(cdb)) cdb = MyPath.Combine(
                    Path.GetDirectoryName(file), "..", dbname + ".bytes");
                if (!File.Exists(cdb)) cdb = MyPath.Combine(
                    Path.GetDirectoryName(file), "..", dbname + ".db");
                if (!File.Exists(cdb)) cdb = MyPath.Combine(
                      Path.GetDirectoryName(file), "../Databases/Database.bytes");
                if (!File.Exists(cdb)) cdb = MyPath.Combine(
                    Path.GetDirectoryName(file), "../Databases", dbname + ".bytes");
                if (!File.Exists(cdb)) cdb = MyPath.Combine(
                    Path.GetDirectoryName(file), "../Databases", dbname + ".db");
                SetCardDB(cdb);//Load card data in the background
                fctb.OpenFile(nowFile, new UTF8Encoding(false));
                oldtext = fctb.Text;
                SetTitle();
                return true;
            }
            return false;
        }

        #endregion

        #region Document map view
        //Document map view
        void ShowMapToolStripMenuItemClick(object sender, EventArgs e)
        {
            if (menuitem_showmap.Checked)
            {
                documentMap1.Visible = false;
                menuitem_showmap.Checked = false;
                fctb.Width += documentMap1.Width;
            }
            else
            {
                documentMap1.Visible = true;
                menuitem_showmap.Checked = true;
                fctb.Width -= documentMap1.Width;
            }
        }
        #endregion

        #region Update the title
        void SetTitle()
        {
            string str;
            if (string.IsNullOrEmpty(nowFile))
            {
                str = title;
            }
            else
            {
                str = new FileInfo(nowFile).Name;
            }

            if (MdiParent != null)//Running inside the main MDI container
            {
                if (string.IsNullOrEmpty(nowFile))
                {
                    Text = title;
                    TabText = title;
                }
                else
                {
                    Text = Path.GetFileName(nowFile);
                }
                MdiParent.Text = str;
            }
            else
            {
                Text = str;
                TabText = str;
            }
        }

        void CodeEditFormEnter(object sender, EventArgs e)
        {
            SetTitle();
        }
        #endregion

        #region Autocomplete
        public void LoadXml(string xmlfile)
        {
            fctb.DescriptionFile = xmlfile;
        }
        public void InitTooltip(CodeConfig codeconfig)
        {
            tooltipDic = codeconfig.TooltipDic;
            items = codeconfig.Items;
            if (fctb.SyntaxHighlighter is MySyntaxHighlighter highlighter)
            {
                highlighter.SetKnownFunctions(codeconfig.LongTooltipDic.Keys);
            }
            popupMenu.Items.SetAutocompleteItems(items);
        }
        #endregion

        #region Hovered function description
        //Find function description
        string FindTooltip(string word)
        {
            string desc = "";
            foreach (string v in tooltipDic.Keys)
            {
                int t = v.IndexOf(".");
                string k = v;
                if (t > 0)
                {
                    k = v[(t + 1)..];
                }

                if (word == k)
                {
                    desc = tooltipDic[v];
                }
            }
            return desc;
        }

        //Hovered function description
        void FctbToolTipNeeded(object sender, ToolTipNeededEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.HoveredWord))
            {
                long tl = 0;
                string name = e.HoveredWord;
                string desc = "";
                if (!name.StartsWith("0x") && name.Length <= 9)
                {
                    name = name.Replace("c", "");
                    _ = long.TryParse(name, out tl);
                }

                if (tl > 0)
                {
                    //Get card info
                    if (cardlist.ContainsKey(tl))
                    {
                        desc = cardlist[tl];
                    }
                }
                else
                {
                    desc = FindTooltip(e.HoveredWord);
                }

                if (!string.IsNullOrEmpty(desc))
                {
                    e.ToolTipTitle = e.HoveredWord;
                    e.ToolTipText = desc;
                }
            }
        }
        #endregion

        #region Save file
        bool SaveFile(bool saveas)
        {
            string alltext = fctb.Text;
            if (!tabisspaces)
            {
                alltext = alltext.Replace("    ", "\t");
            }

            if (saveas || string.IsNullOrEmpty(nowFile) || !File.Exists(nowFile))
            {
                using SaveFileDialog sfdlg = new();
                sfdlg.FileName = string.IsNullOrEmpty(nowFile) ? "c" + nowCode.ToString() + ".lua" : nowFile;
                sfdlg.AddExtension = true;
                sfdlg.DefaultExt = "lua";
                sfdlg.FilterIndex = 1;
                try
                {
                    sfdlg.Filter = LanguageHelper.GetMsg(LMSG.ScriptFilter);
                }
                catch { }
                if (sfdlg.ShowDialog() == DialogResult.OK)
                {
                    nowFile = sfdlg.FileName;
                    SetTitle();
                }
                else
                {
                    return false;
                }
            }
            oldtext = fctb.Text;
            File.WriteAllText(nowFile, alltext, new UTF8Encoding(false));
            if (long.TryParse(new FileInfo(nowFile).Name.Replace("c", "").Replace(".lua", ""), out long tl) && tl > 0
                && File.Exists(nowcdb) && (DEXConfig.ReadBoolean(DEXConfig.TAG_SAVE2DB) || menuitem_save2database.Checked))
            {
                try
                {
                    _ = DataBase.Command(nowcdb, "update datas set script = '" + alltext.Replace("'", "''") + "' where id=" + tl);
                }
                catch { }
            }
            return true;
        }
        #endregion

        #region Menu
        //Show or hide the input box
        void Menuitem_showinputClick(object sender, EventArgs e)
        {
            if (menuitem_showinput.Checked)
            {
                menuitem_showinput.Checked = false;
                tb_input.Visible = false;
            }
            else
            {
                menuitem_showinput.Checked = true;
                tb_input.Visible = true;
            }
        }

        void CodeEditFormLoad(object sender, EventArgs e)
        {

        }
        void Menuitem_findClick(object sender, EventArgs e)
        {
            fctb.ShowFindDialog();
        }

        void Menuitem_replaceClick(object sender, EventArgs e)
        {
            fctb.ShowReplaceDialog();
        }

        void QuitToolStripMenuItemClick(object sender, EventArgs e)
        {
            Close();
        }

        void AboutToolStripMenuItemClick(object sender, EventArgs e)
        {
            AboutForm.ShowVersionInfo(this, () => UpdateProgressForm.CheckForUpdates(this, true));
        }

        void Menuitem_openClick(object sender, EventArgs e)
        {
            using OpenFileDialog sfdlg = new();
            try
            {
                sfdlg.Filter = LanguageHelper.GetMsg(LMSG.ScriptFilter);
            }
            catch { }
            if (sfdlg.ShowDialog() == DialogResult.OK)
            {
                nowFile = sfdlg.FileName;
                fctb.OpenFile(nowFile, new UTF8Encoding(false));
            }
        }

        #endregion

        #region Search functions
        //Search functions
        void Tb_inputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string key = tb_input.Text;
                List<AutocompleteItem> list = new();
                foreach (AutocompleteItem item in items)
                {
                    if (item.ToolTipText.Contains(key))
                    {
                        list.Add(item);
                    }
                }
                popupMenu.Items.SetAutocompleteItems(list.ToArray());
                popupMenu.Show(true);
            }
        }
        #endregion

        #region Save prompt
        void CodeEditFormFormClosing(object sender, FormClosingEventArgs e)
        {
            if (!string.IsNullOrEmpty(oldtext))
            {
                if (fctb.Text != oldtext)
                {
                    if (MyMsg.Question(LMSG.IfSaveScript))
                    {
                        _ = Save(false);
                    }
                }
            }
            else if (fctb.Text.Length > 0)
            {
                if (MyMsg.Question(LMSG.IfSaveScript))
                {
                    _ = Save(true);
                }
            }
        }
        #endregion

        #region Card tooltip
        public void SetCDBList(string[] cdbs)
        {
            if (cdbs == null)
            {
                return;
            }

            foreach (string cdb in cdbs)
            {
                ToolStripMenuItem tsmi = new(cdb);
                tsmi.Click += MenuItem_Click;
                _ = menuitem_setcard.DropDownItems.Add(tsmi);
            }
        }
        void MenuItem_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem tsmi)
            {
                string file = tsmi.Text;
                SetCardDB(file);
            }
        }
        public void SetCardDB(string name)
        {
            nowcdb = name;
            if (!backgroundWorker1.IsBusy)
            {
                backgroundWorker1.RunWorkerAsync();
            }
        }

        private void BackgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            if (nowcdb != null && File.Exists(nowcdb))
            {
                SetCards(DataBase.Read(nowcdb, true, ""));
            }
        }
        public void SetCards(Card[] cards)
        {
            if (cards == null)
            {
                return;
            }

            cardlist.Clear();
            foreach (Card c in cards)
            {
                cardlist.Add(c.id, c.ToString());
            }
        }
        #endregion

        #region Selection highlighting
        void FctbSelectionChangedDelayed(object sender, EventArgs e)
        {
            tb_input.Text = fctb.SelectedText;
            fctb.VisibleRange.ClearStyle(sameWordsStyle);
            if (!fctb.Selection.IsEmpty)
            {
                return;//user selected diapason
            }

            //get fragment around caret
            var fragment = fctb.Selection.GetFragment(@"\w");
            string text = fragment.Text;
            if (text.Length == 0)
            {
                return;
            }
            //highlight same words
            var ranges = fctb.Range.GetRanges("\\b" + text + "\\b");
            foreach (var r in ranges)
            {
                r.SetStyle(sameWordsStyle);
            }
        }
        #endregion

        #region Jump to function
        void FctbMouseClick(object sender, MouseEventArgs e)
        {
            var fragment = fctb.Selection.GetFragment(@"\w");
            string text = fragment.Text;
            if (text.Length == 0)
            {
                return;
            }

            if (e.Button == MouseButtons.Left && ModifierKeys == Keys.Control)
            {
                List<int> linenums = fctb.FindLines(@"function\s+?\S+?\." + text + @"\(", RegexOptions.Singleline);
                if (linenums.Count > 0)
                {
                    fctb.Navigate(linenums[0]);
                }
            }
        }
        #endregion

        private void Menuitem_testlua_Click(object sender, EventArgs e)
        {
            if (nowFile == null) return;
            FileInfo fi = new(nowFile);
            string fn = fi.Name;
            if (!fn.ToUpper().EndsWith(".LUA"))
            {
                return;
            }
            string cCode = fn[..^4];
            bool error = false;
            try
            {
                Directory.SetCurrentDirectory(fi.DirectoryName);
                Lua lua = new();
                var env = lua.CreateEnvironment();
                string pre = "Duel={} Effect={} Card={} aux={} Auxiliary={} " + cCode + "={} Duel.LoadScript=function(str) end GetID=function() return "
                    + cCode + "," + cCode[1..] + "," + (long.Parse(cCode[1..]) < 100000000 ? "1" : "100") + " end";
                _ = env.DoChunk(pre + fctb.Text, "test.lua");
            }
            catch (LuaException ex)
            {
                _ = MessageBox.Show($"LINE{ex.Line} - {ex.Message}");
                error = true;
            }
            if (!error)
            {
                MyMsg.Show(LMSG.syntaxCheckPassed);
            }
        }

        private void OnDragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void OnDragDrop(object sender, DragEventArgs e)
        {
            string[] drops = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (drops == null)
            {
                return;
            }
            List<string> files = new();
            foreach (string file in drops)
            {
                if (Directory.Exists(file))
                {
                    files.AddRange(Directory.EnumerateFiles(file, "*.cdb", SearchOption.AllDirectories));
                    files.AddRange(Directory.EnumerateFiles(file, "*.lua", SearchOption.AllDirectories));
                }
                files.Add(file);
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
                (DockPanel.Parent as MainForm).Open(file);
            }
        }
        private void Menuitem_fixCardCode_Click(object sender, EventArgs e)
        {
            string text = fctb.Text;
            Regex regex = new(@"(c[0-9]{4,9})");
            var matches = regex.Matches(text);
            string cName;
            if (nowFile != null && regex.IsMatch(nowFile))
            {
                cName = regex.Match(nowFile).Groups[1].Value;
            }
            else
            {
                MyMsg.Show(LMSG.InvalidFileName);
                return;
            }
            HashSet<string> hs = new();
            foreach (Match match in matches.Cast<Match>())
            {
                _ = hs.Add(match.Groups[1].Value);
            }
            foreach (string str in hs)
            {
                text = text.Replace(str, cName);
                text = text.Replace(str[1..], cName[1..]);
            }
            fctb.Text = text;
        }
        private void Menuitem_tooltipFont_Click(object sender, EventArgs e)
        {
            FontDialog fd = new();
            string fontJson = DEXConfig.ReadString(DEXConfig.TOOLTIP_FONT);
            Font? f = null;
            try
            {
                f = JsonConvert.DeserializeObject<Font>(fontJson) ?? new Font("Microsoft YaHei", 10);
            }
            catch { }
            if (f == null) return;
            fd.Font = f;
            if (fd.ShowDialog() == DialogResult.OK)
            {
                XMLReader.Save(DEXConfig.TOOLTIP_FONT, JsonConvert.SerializeObject(fd.Font));
                fctb.lbTooltip.Font = fd.Font;
            }
        }
        private void Menuitem_save2database_Click(object sender, EventArgs e)
        {
            menuitem_save2database.Checked = !menuitem_save2database.Checked;
            XMLReader.Save(DEXConfig.TAG_SAVE2DB, menuitem_save2database.Checked.ToString().ToLower());

        }

        public void ApplyTheme()
        {
            ThemeManager.ThemePalette palette = ThemeManager.CurrentPalette;
            ThemeManager.ApplyControlTree(this);
            popupMenu.BackColor = palette.UsesOriginalColors ? fctb.BackColor : palette.InputBackColor;
            popupMenu.ForeColor = palette.UsesOriginalColors ? fctb.ForeColor : palette.TextColor;
            popupMenu.SelectedColor = palette.UsesOriginalColors ? Color.LightGray : palette.HeaderBackColor;
            documentMap1.BackColor = palette.UsesOriginalColors ? Color.DimGray : palette.SurfaceBackColor;
            documentMap1.ForeColor = palette.UsesOriginalColors ? Color.Maroon : palette.TextColor;
            fctb.Invalidate();
        }
    }
}
