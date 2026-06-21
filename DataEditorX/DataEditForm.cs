/*
 * Created with SharpDevelop.
 * User: Acer
 * Date: May 18, Sunday
 * Time: 20:22
 * 
 */
using DataEditorX.Common;
using DataEditorX.Config;
using DataEditorX.Core;
using DataEditorX.Core.Mse;
using DataEditorX.Language;
using System.Globalization;
using WeifenLuo.WinFormsUI.Docking;
using Microsoft.Data.Sqlite;
using System.Text.RegularExpressions;
using System.Text;

namespace DataEditorX
{
    public partial class DataEditForm : DockContent, IDataForm
    {
        private string defaultScriptName;

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string DefaultScriptName
        {
            get
            {
                if (!string.IsNullOrEmpty(defaultScriptName))
                {
                    return defaultScriptName;
                }
                else
                {
                    string cdbName = Path.GetFileNameWithoutExtension(nowCdbFile) ?? "";
                    if (cdbName.Length > 0 && File.Exists(GetPath().GetModuleScript(cdbName)))
                    {
                        return cdbName;
                    }
                }
                return "";
            }
            set
            {
                defaultScriptName = value?.Trim() ?? "";
            }
        }

        #region Fields and constructors
        TaskHelper tasker = null;
        string taskname;
        //Directory
        YgoPath ygopath;
        /// <summary>Current card</summary>
        Card oldCard = new(0);
        /// <summary>Search filter</summary>
        Card srcCard = new(0);
        //Card editing
        CardEdit cardedit;
        string[] strs = null;
        /// <summary>
        /// Compared ID list
        /// </summary>
        List<string> tmpCodes;
        //Initial title
        string title;
        string nowCdbFile = "";
        int maxRow = 37;
        int page = 1, pageNum = 1;
        /// <summary>
        /// Total card count
        /// </summary>
        int cardcount;

        /// <summary>
        /// Search results
        /// </summary>
        readonly List<Card> cardlist = new();

        //Setcode input is being edited
        readonly bool[] setcodeIsedit = new bool[5];
        readonly CommandManager cmdManager = new();

        Image cover;
        MSEConfig msecfg;

        string datapath, confcover;

        public DataEditForm(string datapath, string cdbfile, DataConfig datacfg = null)
        {
            Initialize(datapath);
            if (datacfg != null && File.Exists(cdbfile) && !cdbfile.EndsWith(".cdb", StringComparison.OrdinalIgnoreCase))
            {
                Dictionary<long, string> d = datacfg.dicSetnames;
                if (!d.ContainsKey(0)) d.Add(0L, "Archetype");
                using SqliteConnection con = new(@"Data Source=" + cdbfile);
                con.Open();
                using SqliteCommand cmd = con.CreateCommand();
                using SqliteDataReader reader = cmd.ExecuteReader();
                try
                {
                    while (reader.Read())
                    {
                        int c = reader.GetInt32(reader.GetOrdinal("officialcode"));
                        if (c == 0)
                            c = reader.GetInt32(reader.GetOrdinal("betacode"));
                        string n = reader.GetString(reader.GetOrdinal("name"));
                        if (c > 0 && d.ContainsKey(c))
                            d[c] = n;
                        else d.Add(c, n);
                    }
                }
                catch { }
                con.Close();
                SqliteConnection.ClearAllPools();
            }
            nowCdbFile = cdbfile;
        }

        public DataEditForm(string datapath)
        {
            Initialize(datapath);
        }
        public DataEditForm()
        {//Default startup
            string dir = DEXConfig.ReadString(DEXConfig.TAG_DATA);
            if (string.IsNullOrEmpty(dir))
            {
                Application.Exit();
            }
            datapath = MyPath.Combine(Application.StartupPath, dir);

            Initialize(datapath);
        }
        void Initialize(string datapath)
        {
            cardedit = new CardEdit(this);
            tmpCodes = new List<string>();
            ygopath = new YgoPath(Application.StartupPath);
            InitPath(datapath);
            InitializeComponent();
            title = Text;
            nowCdbFile = "";
            cmdManager.UndoStateChanged += delegate (bool val)
            {
                if (val)
                {
                    btn_undo.Enabled = true;
                }
                else
                {
                    btn_undo.Enabled = false;
                }
            };
        }

        #endregion

        #region Interfaces
        public void SetActived()
        {
            Activate();
        }
        public string GetOpenFile()
        {
            return nowCdbFile;
        }
        public bool CanOpen(string file)
        {
            return YGOUtil.IsDataBase(file);
        }
        public bool Create(string file)
        {
            return Open(file);
        }
        public bool Save(bool shift)
        {
            if (shift)
                using (SaveFileDialog dlg = new())
                {
                    dlg.Title = LanguageHelper.GetMsg(LMSG.NewFile);
                    try
                    {
                        dlg.Filter = LanguageHelper.GetMsg(LMSG.CdbType);
                    }
                    catch { }
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        string file = dlg.FileName;
                        if (DataBase.Create(file))
                        {
                            Card[] cl = GetCardList(false);
                            SetCDB(file);
                            SaveCards(cl);
                        }
                        else
                        {
                            File.Delete(file);
                            File.Delete(file + "-journal");
                            return false;
                        }
                    }
                    else return false;
                }
            return true;
        }
        #endregion

        #region Form lifecycle
        //First form load
        void DataEditFormLoad(object sender, EventArgs e)
        {
            //InitListRows();//Recalculate card list rows
            HideMenu();//Hide the embedded menu when needed
            SetTitle();//Update the title
                            //Load data
            msecfg = new MSEConfig(datapath);
            tasker = new TaskHelper(datapath, bgWorker1, msecfg);
            //Reset to an empty card
            oldCard = new Card(0);
            SetCard(oldCard);
            //Load related-file deletion setting
            menuitem_operacardsfile.Checked = DEXConfig.ReadBoolean(DEXConfig.TAG_DELETE_WITH);
            //Load script-in-editor setting
            menuitem_openfileinthis.Checked = DEXConfig.ReadBoolean(DEXConfig.TAG_OPEN_IN_THIS);
            //Load auto-update setting
            menuitem_autocheckupdate.Checked = DEXConfig.ReadBoolean(DEXConfig.TAG_AUTO_CHECK_UPDATE);
            //Default script opened when no card ID is selected
            DefaultScriptName = DEXConfig.ReadString(DEXConfig.TAG_DEFAULT_SCRIPT_NAME);
            if (nowCdbFile != null && File.Exists(nowCdbFile))
            {
                _ = Open(nowCdbFile);
            }
            //Build the MSE config menu
            AddMenuItemFormMSE();
            //
            GetLanguageItem();
            ApplyTheme();
            //   CheckUpdate(false);//Check for updates
        }
        //Form closing
        void DataEditFormFormClosing(object sender, FormClosingEventArgs e)
        {
            //Ask before closing while a task is running
            if (tasker != null && tasker.IsRuning())
            {
                if (!CancelTask())
                {
                    e.Cancel = true;
                    return;
                }
            }
        }
        //Form activated
        void DataEditFormEnter(object sender, EventArgs e)
        {
            SetTitle();
        }
        #endregion

        #region Initialization
        //Hide menu
        void HideMenu()
        {
            if (MdiParent == null)
            {
                return;
            }

            mainMenu.Visible = false;
            //this.SuspendLayout();
            ResumeLayout(true);
            foreach (Control c in Controls)
            {
                if (c.GetType() == typeof(MenuStrip))
                {
                    continue;
                }

                Point p = c.Location;
                c.Location = new Point(p.X, p.Y - 25);
            }
            ResumeLayout(false);
            //this.PerformLayout();
        }

        //Remove task suffix
        static string RemoveTag(string text)
        {
            int t = text.LastIndexOf(" (");
            if (t > 0)
            {
                return text[..t];
            }
            return text;
        }
        //Update the title
        void SetTitle()
        {
            string str = title;
            string str2 = RemoveTag(title);
            if (!string.IsNullOrEmpty(nowCdbFile))
            {
                str = nowCdbFile + "-" + str;
                str2 = Path.GetFileName(nowCdbFile);
            }
            if (MdiParent != null) //Running inside the main MDI container
            {
                Text = str2;
                if (tasker != null && tasker.IsRuning())
                {
                    if (DockPanel.ActiveContent == this)
                    {
                        MdiParent.Text = str;
                    }
                }
                else
                {
                    MdiParent.Text = str;
                }
            }
            else
            {
                Text = str;
            }
        }
        //Update paths from the current database
        void SetCDB(string cdb)
        {
            nowCdbFile = cdb;
            SetTitle();
            string path = Application.StartupPath;
            if (cdb.Length > 0)
            {
                path = Path.GetDirectoryName(cdb);
            }
            ygopath.SetPath(path);
        }
        //Initialize file paths
        void InitPath(string datapath)
        {
            this.datapath = datapath;
            confcover = MyPath.FindFile(datapath, "cover.jpg", "assets");
            if (File.Exists(confcover))
            {
                cover = MyBitmap.ReadImage(confcover);
            }
            else
            {
                cover = null;
            }
        }
        #endregion

        #region UI controls
        //Initialize controls
        public void InitControl(DataConfig datacfg)
        {
            if (datacfg == null)
            {
                return;
            }

            List<long> setcodes = DataManager.GetKeys(datacfg.dicSetnames);
            string[] setnames = DataManager.GetValues(datacfg.dicSetnames);
            try
            {
                InitComboBox(cb_cardrace, datacfg.dicCardRaces);
                InitComboBox(cb_cardattribute, datacfg.dicCardAttributes);
                InitComboBox(cb_cardrule, datacfg.dicCardRules);
                InitComboBox(cb_cardlevel, datacfg.dicCardLevels);
                InitCheckPanel(pl_cardtype, datacfg.dicCardTypes);
                InitCheckPanel(pl_markers, datacfg.dicLinkMarkers);
                InitCheckPanel(pl_category, datacfg.dicCardcategorys);
                InitCheckPanel(pl_flags, datacfg.dicCardflags);
                InitComboBox(cb_setname1, setcodes, setnames);
                InitComboBox(cb_setname2, setcodes, setnames);
                InitComboBox(cb_setname3, setcodes, setnames);
                InitComboBox(cb_setname4, setcodes, setnames);
            }
            catch (Exception ex)
            {
                _ = MessageBox.Show(ex.ToString(), "启动错误");
            }
        }

        //Initialize FlowLayoutPanel
        static void InitCheckPanel(FlowLayoutPanel fpanel, Dictionary<long, string> dic)
        {
            fpanel.SuspendLayout();
            fpanel.Controls.Clear();
            foreach (long key in dic.Keys)
            {
                string value = dic[key];
                if (value != null && value.StartsWith("NULL"))
                {
                    Label lab = new();
                    string[] sizes = value.Split(',');
                    if (sizes.Length >= 3)
                    {
                        lab.Size = new Size(int.Parse(sizes[1]), int.Parse(sizes[2]));
                    }
                    lab.AutoSize = false;
                    lab.Margin = fpanel.Margin;
                    fpanel.Controls.Add(lab);
                }
                else
                {
                    CheckBox _cbox = new()
                    {
                        //_cbox.Name = fpanel.Name + key.ToString("x");
                        Tag = key,//Bind value
                        Text = value,
                        AutoSize = true,
                        Margin = fpanel.Margin
                    };
                    //_cbox.Click += PanelOnCheckClick;
                    fpanel.Controls.Add(_cbox);
                }
            }
            fpanel.ResumeLayout(false);
            fpanel.PerformLayout();
        }

        //Initialize ComboBox
        void InitComboBox(ComboBox cb, Dictionary<long, string> tempdic)
        {
            InitComboBox(cb, DataManager.GetKeys(tempdic),
                         DataManager.GetValues(tempdic));
        }
        //Initialize ComboBox
        void InitComboBox(ComboBox cb, List<long> keys, string[] values)
        {
            cb.Items.Clear();
            cb.Tag = keys;
            cb.Items.AddRange(values);
            if (cb.Items.Count > 1 && (cb == cb_setname1
                || cb == cb_setname2 || cb == cb_setname3
                || cb == cb_setname4))
                cb.SelectedIndex = 1;
            else if (cb.Items.Count > 0)
                cb.SelectedIndex = 0;
        }
        //Calculate the maximum visible list rows
        void InitListRows()
        {
            bool addTest = lv_cardlist.Items.Count == 0;
            if (addTest)
            {
                ListViewItem item = new()
                {
                    Text = "Test"
                };
                _ = lv_cardlist.Items.Add(item);
            }
            int headH = lv_cardlist.Items[0].GetBounds(ItemBoundsPortion.ItemOnly).Y;
            int itemH = lv_cardlist.Items[0].GetBounds(ItemBoundsPortion.ItemOnly).Height;
            if (itemH > 0)
            {
                int n = (lv_cardlist.Height - headH) / itemH;
                if (n > 0)
                {
                    maxRow = n;
                }
                //MessageBox.Show("height="+lv_cardlist.Height+",item="+itemH+",head="+headH+",max="+MaxRow);
            }
            if (addTest)
            {
                lv_cardlist.Items.Clear();
            }
            if (maxRow < 10)
            {
                maxRow = 20;
            }
        }

        //Set checkboxes
        static void SetCheck(FlowLayoutPanel fpl, long number)
        {
            long temp;
            //string strType = "";
            foreach (Control c in fpl.Controls)
            {
                if (c is CheckBox cbox)
                {
                    if (cbox.Tag == null)
                    {
                        temp = 0;
                    }
                    else
                    {
                        temp = (long)cbox.Tag;
                    }

                    if ((temp & number) == temp && temp != 0)
                    {
                        cbox.Checked = true;
                        //strType += "/" + c.Text;
                    }
                    else
                    {
                        cbox.Checked = false;
                    }
                }
            }
            //return strType;
        }

        static void SetEnabled(FlowLayoutPanel fpl, bool set)
        {
            foreach (Control c in fpl.Controls)
            {
                if (c is CheckBox cbox)
                {
                    cbox.Enabled = set;
                }
            }
        }

        //Set combo box selection
        static void SetSelect(ComboBox cb, long k)
        {
            if (cb.Tag == null)
            {
                cb.SelectedIndex = 0;
                return;
            }
            List<long> keys = (List<long>)cb.Tag;
            int index = keys.IndexOf(k);
            if (index >= 0 && index < cb.Items.Count)
            {
                cb.SelectedIndex = index;
            }
        }

        //Get selected value
        static long GetSelect(ComboBox cb)
        {
            if (cb.Tag == null)
            {
                return 0;
            }
            List<long> keys = (List<long>)cb.Tag;
            int index = cb.SelectedIndex;
            if (index >= keys.Count || index < 0)
            {
                return 0;
            }
            else
            {
                return keys[index];
            }
        }

        //Read combined checkbox flags
        static long GetCheck(FlowLayoutPanel fpl)
        {
            long number = 0;
            long temp;
            foreach (Control c in fpl.Controls)
            {
                if (c is CheckBox cbox)
                {
                    if (cbox.Tag == null)
                    {
                        temp = 0;
                    }
                    else
                    {
                        temp = (long)cbox.Tag;
                    }

                    if (cbox.Checked)
                    {
                        number += temp;
                    }
                }
            }
            return number;
        }
        private void UpdateCardListPaging()
        {
            cardcount = cardlist.Count;
            int rowsPerPage = Math.Max(maxRow, 1);
            pageNum = Math.Max(1, (cardcount + rowsPerPage - 1) / rowsPerPage);
            if (page > pageNum)
            {
                page = pageNum;
            }
            tb_pagenum.Text = pageNum.ToString();
        }

        // Populate the visible card rows for the current page.
        void AddListView(int p)
        {
            int i, j, istart, iend;

            if (p <= 0)
            {
                p = 1;
            }
            else if (p >= pageNum)
            {
                p = pageNum;
            }

            istart = (p - 1) * maxRow;
            iend = p * maxRow;
            if (iend > cardcount)
            {
                iend = cardcount;
            }

            page = p;
            lv_cardlist.BeginUpdate();
            lv_cardlist.Items.Clear();
            if ((iend - istart) > 0)
            {
                ListViewItem[] items = new ListViewItem[iend - istart];
                Card mcard;
                for (i = istart, j = 0; i < iend; i++, j++)
                {
                    mcard = cardlist[i];
                    items[j] = new ListViewItem
                    {
                        Tag = i,
                        Text = mcard.id.ToString()
                    };
                    if (mcard.id == oldCard.id)
                    {
                        items[j].Checked = true;
                    }

                    _ = items[j].SubItems.Add(mcard.name);
                    ThemeManager.ApplyListViewItem(items[j]);
                }
                lv_cardlist.Items.AddRange(items);
            }
            lv_cardlist.EndUpdate();
            tb_page.Text = page.ToString();

        }
        #endregion

        #region Bind card to UI
        public YgoPath GetPath()
        {
            return ygopath;
        }
        public Card GetOldCard()
        {
            return oldCard;
        }

        private void SetLinkMarks(long mark, bool setCheck = false)
        {
            if (setCheck)
            {
                SetCheck(pl_markers, mark);
            }
            tb_link.Text = Convert.ToString(mark, 2).PadLeft(9, '0');
        }

        public void SetCard(Card c)
        {
            oldCard = c;

            tb_cardname.Text = c.name;
            tb_cardtext.Text = c.desc;

            strs = new string[c.Str.Length];
            Array.Copy(c.Str, strs, Card.STR_MAX);
            lb_scripttext.Items.Clear();
            lb_scripttext.Items.AddRange(c.Str);
            tb_edittext.Text = "";
            //data
            SetSelect(cb_cardrule, c.ot);
            SetSelect(cb_cardattribute, c.attribute);
            SetSelect(cb_cardlevel, c.level & 0xff);
            SetSelect(cb_cardrace, c.race);
            //setcode
            long[] setcodes = c.GetSetCode();
            tb_setcode1.Text = setcodes[0].ToString("x");
            tb_setcode2.Text = setcodes[1].ToString("x");
            tb_setcode3.Text = setcodes[2].ToString("x");
            tb_setcode4.Text = setcodes[3].ToString("x");
            //type,category
            SetCheck(pl_cardtype, c.type);
            if (c.IsType(Core.Info.CardType.TYPE_LINK))
            {
                SetLinkMarks(c.def, true);
            }
            else
            {
                tb_link.Text = "";
                SetCheck(pl_markers, 0);
            }
            SetCheck(pl_category, c.category);
            //Omega-exclusive
            if (!GetOpenFile().EndsWith(".cdb", StringComparison.OrdinalIgnoreCase))
            {
                SetCheck(pl_flags, c.omega[1]);
                tb_support.Text = c.omega[2].ToString("x");
            }
            else
            {
                SetCheck(pl_flags, 0);
                tb_support.Text = "0";
            }
            //Pendulum
            tb_pleft.Text = ((c.level >> 24) & 0xff).ToString();
            tb_pright.Text = ((c.level >> 16) & 0xff).ToString();
            // ATK/DEF
            tb_atk.Text = c.atk == -1 ? "" : c.atk < 0 ? "?" : c.atk.ToString();
            if (c.IsType(Core.Info.CardType.TYPE_LINK))
            {
                tb_def.Text = "0";
            }
            else
            {
                tb_def.Text = c.def == -1 ? "" : c.def < 0 ? "?" : c.def.ToString();
            }

            tb_cardcode.Text = c.id.ToString();
            tb_cardalias.Text = c.alias.ToString();
            SetImage(c.id.ToString());
        }
        #endregion

        #region Build card from UI
        public Card GetCard()
        {
            Card c = new(0)
            {
                name = tb_cardname.Text,
                desc = tb_cardtext.Text
            };

            Array.Copy(strs, c.Str, Card.STR_MAX);

            c.ot = (int)GetSelect(cb_cardrule);
            c.attribute = (int)GetSelect(cb_cardattribute);
            c.level = (int)GetSelect(cb_cardlevel);
            c.race = (int)GetSelect(cb_cardrace);
            //Archetype/setcode
            c.SetSetCode(
                tb_setcode1.Text,
                tb_setcode2.Text,
                tb_setcode3.Text,
                tb_setcode4.Text);

            c.type = GetCheck(pl_cardtype);
            c.category = GetCheck(pl_category);

            c.omega = new long[5];
            if (!GetOpenFile().EndsWith(".cdb", StringComparison.OrdinalIgnoreCase)) {
                c.script = oldCard.script;
                c.omega[0] = 1L;
                c.omega[1] = GetCheck(pl_flags);
                c.SetSupport(tb_support.Text);
            } else for (byte i = 0; i < 3; i++) c.omega[i] = 0L;

            _ = int.TryParse(tb_pleft.Text, out int temp);
            c.level += temp << 24;
            _ = int.TryParse(tb_pright.Text, out temp);
            c.level += temp << 16;
            string atkText = tb_atk.Text.Trim();
            if (string.IsNullOrEmpty(atkText) || atkText == ".")
            {
                c.atk = -1;
            }
            else if (atkText == "?" || atkText == "？")
            {
                c.atk = -2;
            }
            else
            {
                _ = int.TryParse(atkText, out c.atk);
            }

            if (c.IsType(Core.Info.CardType.TYPE_LINK))
            {
                c.def = (int)GetCheck(pl_markers);
            }
            else
            {
                string defText = tb_def.Text.Trim();
                if (string.IsNullOrEmpty(defText) || defText == ".")
                {
                    c.def = -1;
                }
                else if (defText == "?" || defText == "？")
                {
                    c.def = -2;
                }
                else
                {
                    _ = int.TryParse(defText, out c.def);
                }
            }
            _ = uint.TryParse(tb_cardcode.Text, out c.id);
            _ = uint.TryParse(tb_cardalias.Text, out c.alias);

            return c;
        }
        #endregion

        #region Card list
        //List selection
        void Lv_cardlistSelectedIndexChanged(object sender, EventArgs e)
        {
            if (lv_cardlist.SelectedItems.Count > 0)
            {
                int sel = lv_cardlist.SelectedItems[0].Index;
                int index = (page - 1) * maxRow + sel;
                if (index < cardlist.Count)
                {
                    Card c = cardlist[index];
                    SetCard(c);
                }
            }
        }
        //List keyboard handling
        void Lv_cardlistKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Delete:
                    cmdManager.ExcuteCommand(cardedit.delCard, menuitem_operacardsfile.Checked);
                    break;
                case Keys.Right:
                    Btn_PageDownClick(null, null);
                    break;
                case Keys.Left:
                    Btn_PageUpClick(null, null);
                    break;
            }
        }
        //Previous page
        void Btn_PageUpClick(object sender, EventArgs e)
        {
            if (!CheckOpen())
            {
                return;
            }

            page--;
            AddListView(page);
        }
        //Next page
        void Btn_PageDownClick(object sender, EventArgs e)
        {
            if (!CheckOpen())
            {
                return;
            }

            page++;
            AddListView(page);
        }
        //Jump to page
        void Tb_pageKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                _ = int.TryParse(tb_page.Text, out int p);
                if (p > 0)
                {
                    AddListView(p);
                }
            }
        }
        #endregion

        #region Card search and open
        //Check whether a database is open
        public bool CheckOpen()
        {
            if (File.Exists(nowCdbFile))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        //Open database
        public bool Open(string file, string name = "")
        {
            SetCDB(file);
            if (!File.Exists(file))
            {
                MyMsg.Error(LMSG.FileIsNotExists);
                return false;
            }
            //Clear current state
            tmpCodes.Clear();
            cardlist.Clear();
            //Ensure required tables exist
            _ = DataBase.CheckTable(file);
            srcCard = new Card();
            SetCards(DataBase.Read(file, true, ""), false);
            return true;
        }
        //Setcode filtering
        public static bool CardFilter(Card c, Card sc)
        {
            bool res = true;
            if (sc.setcode != 0)
            {
                res &= c.IsSetCode(sc.setcode & 0xffff);
            }

            return res;
        }
        // Replace the current search results and redraw the list.
        public void SetCards(Card[] cards, bool isfresh)
        {
            if (cards != null)
            {
                cardlist.Clear();
                foreach (Card c in cards)
                {
                    if (CardFilter(c, srcCard))
                    {
                        cardlist.Add(c);
                    }
                }
                UpdateCardListPaging();

                if (isfresh)
                {
                    AddListView(page);
                }
                else
                {
                    AddListView(1);
                }
            }
            else
            {
                page = 1;
                cardlist.Clear();
                UpdateCardListPaging();
                tb_page.Text = page.ToString();
                lv_cardlist.Items.Clear();
            }
        }
        //Search cards
        public void Search(bool isfresh)
        {
            Search(srcCard, isfresh);
        }
        void Search(Card c, bool isfresh)
        {
            if (!CheckOpen())
            {
                return;
            }
            //Reuse temporary comparison/filter results when present
            if (tmpCodes.Count > 0)
            {
                _ = DataBase.Read(nowCdbFile,
                                              true, tmpCodes.ToArray());
                SetCards(GetCompCards(), true);
            }
            else
            {
                srcCard = c;
                string sql = c.omega != null && c.omega[0] > 0 || !nowCdbFile.EndsWith(".cdb")
                    ? DataBase.OmegaGetSelectSQL(c) : DataBase.GetSelectSQL(c);
                SetCards(DataBase.Read(nowCdbFile, true, sql), isfresh);
            }
            if (lv_cardlist.Items.Count > 0)
            {
                lv_cardlist.SelectedIndices.Clear();
                _ = lv_cardlist.SelectedIndices.Add(0);
            }
        }
        //Reset temporary card
        public void Reset()
        {
            oldCard = new Card(0);
            SetCard(oldCard);
        }
        #endregion

        #region Buttons
        //Search cards
        void Btn_serachClick(object sender, EventArgs e)
        {
            tmpCodes.Clear();//Clear temporary results
            Search(GetCard(), false);
        }
        //Reset card
        void Btn_resetClick(object sender, EventArgs e)
        {
            Reset();
        }
        //Add
        void Btn_addClick(object sender, EventArgs e)
        {
            if (cardedit != null)
            {
                cmdManager.ExcuteCommand(cardedit.addCard);
            }
        }
        //Modify
        void Btn_modClick(object sender, EventArgs e)
        {
            if (cardedit != null)
            {
                cmdManager.ExcuteCommand(cardedit.modCard, menuitem_operacardsfile.Checked);
            }
        }
        //Open script
        void Btn_luaClick(object sender, EventArgs e)
        {
            if (cardedit != null)
            {
                _ = cardedit.OpenScript(menuitem_openfileinthis.Checked, DefaultScriptName);
            }
        }
        //Delete
        void Btn_delClick(object sender, EventArgs e)
        {
            if (cardedit != null)
            {
                cmdManager.ExcuteCommand(cardedit.delCard, menuitem_operacardsfile.Checked);
            }
        }
        //Undo
        void Btn_undoClick(object sender, EventArgs e)
        {
            if (!MyMsg.Question(LMSG.UndoConfirm))
            {
                return;
            }
            if (cardedit != null)
            {
                cmdManager.Undo();
                Search(true);
            }
        }
        //Import card art
        void Btn_imgClick(object sender, EventArgs e)
        {
            ImportImageFromSelect();
        }
        #endregion

        #region Text boxes
        //Card ID search
        void Tb_cardcodeKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                Card c = new(0);
                _ = uint.TryParse(tb_cardcode.Text, out c.id);
                if (c.id > 0)
                {
                    tmpCodes.Clear();//Clear temporary results
                    Search(c, false);
                }
            }
        }
        //Card name search/edit
        void Tb_cardnameKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                Card c = new(0)
                {
                    name = tb_cardname.Text
                };
                if (c.name.Length > 0)
                {
                    tmpCodes.Clear();//Clear temporary results
                    Search(c, false);
                }
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.R && e.Control)
            {
                Btn_resetClick(null, null);
                e.SuppressKeyPress = true;
            }
        }
        //Card description edit
        void Setscripttext(string str)
        {
            int index;
            try
            {
                index = lb_scripttext.SelectedIndex;
            }
            catch
            {
                index = -1;
                MyMsg.Error(LMSG.NotSelectScriptText);
            }
            if (index >= 0)
            {
                strs[index] = str;

                lb_scripttext.Items.Clear();
                lb_scripttext.Items.AddRange(strs);
                lb_scripttext.SelectedIndex = index;
            }
        }

        string Getscripttext()
        {
            int index;
            try
            {
                index = lb_scripttext.SelectedIndex;
            }
            catch
            {
                index = -1;
                MyMsg.Error(LMSG.NotSelectScriptText);
            }
            if (index >= 0)
            {
                return strs[index];
            }
            else
            {
                return "";
            }
        }
        //Script text
        void Lb_scripttextSelectedIndexChanged(object sender, EventArgs e)
        {
            tb_edittext.Text = Getscripttext();
        }

        //Script text
        void Tb_edittextTextChanged(object sender, EventArgs e)
        {
            Setscripttext(tb_edittext.Text);
        }
        #endregion

        #region Help menu
        void Menuitem_aboutClick(object sender, EventArgs e)
        {
            AboutForm.ShowVersionInfo(this, () => CheckUpdate(true));
        }

        void Menuitem_checkupdateClick(object sender, EventArgs e)
        {
            CheckUpdate(true);
        }
        public void CheckUpdate(bool showNew)
        {
            if (showNew)
            {
                UpdateProgressForm.CheckForUpdates(this, true);
                return;
            }

            if (!IsRun())
            {
                tasker.SetTask(MyTask.CheckUpdate, null, showNew.ToString());
                Run(LanguageHelper.GetMsg(LMSG.checkUpdate));
            }
        }
        bool CancelTask()
        {
            bool bl = false;
            if (tasker != null && tasker.IsRuning())
            {
                bl = MyMsg.Question(LMSG.IfCancelTask);
                if (bl)
                {
                    if (tasker != null)
                    {
                        tasker.Cancel();
                    }

                    if (bgWorker1.IsBusy)
                    {
                        bgWorker1.CancelAsync();
                    }
                }
            }
            return bl;
        }
        void Menuitem_cancelTaskClick(object sender, EventArgs e)
        {
            _ = CancelTask();
        }
        void Menuitem_githubClick(object sender, EventArgs e)
        {
            string url = DEXConfig.ReadString(DEXConfig.TAG_SOURCE_URL);
            if (!MyUtils.OpenShellTarget(url))
            {
                MyMsg.Error($"Unable to open source URL:\n{url}");
            }
        }
        #endregion

        #region File menu
        //Open file
        void Menuitem_openClick(object sender, EventArgs e)
        {
            using OpenFileDialog dlg = new();
            dlg.Title = LanguageHelper.GetMsg(LMSG.SelectDataBasePath);
            try
            {
                dlg.Filter = LanguageHelper.GetMsg(LMSG.CdbType);
            }
            catch { }
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _ = Open(dlg.FileName);
            }
        }
        //New file
        void Menuitem_newClick(object sender, EventArgs e)
        {
            using SaveFileDialog dlg = new();
            dlg.Title = LanguageHelper.GetMsg(LMSG.SelectDataBasePath);
            try
            {
                dlg.Filter = LanguageHelper.GetMsg(LMSG.CdbType);
            }
            catch { }
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                if (DataBase.Create(dlg.FileName))
                {
                    if (MyMsg.Question(LMSG.IfOpenDataBase))
                    {
                        _ = Open(dlg.FileName);
                    }
                }
            }
        }
        //Read YDK
        void Menuitem_readydkClick(object sender, EventArgs e)
        {
            if (!CheckOpen())
            {
                return;
            }

            using OpenFileDialog dlg = new();
            dlg.Title = LanguageHelper.GetMsg(LMSG.SelectYdkPath);
            try
            {
                dlg.Filter = LanguageHelper.GetMsg(LMSG.ydkType);
            }
            catch { }
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                tmpCodes.Clear();
                string[] ids = YGOUtil.ReadYDK(dlg.FileName);
                if (MessageBox.Show("Show cards outside of this YDK?", null, MessageBoxButtons.YesNo, MessageBoxIcon.None, MessageBoxDefaultButton.Button2) == DialogResult.Yes) {
                    SetCards(DataBase.Read(nowCdbFile, true, "SELECT datas.*,texts.* FROM datas,texts WHERE datas.id=texts.id and datas.id not in ("
                        + string.Join(",", ids) + ");"), false);
                    foreach (Card c in cardlist)
                        tmpCodes.Add(c.id.ToString());
                } else {
                    tmpCodes.AddRange(ids);
                    SetCards(DataBase.Read(nowCdbFile, true,
                                           ids), false);
                }
            }
        }
        void Menuitem_readlistClick(object sender, EventArgs e)
        {
            if (!CheckOpen())
            {
                return;
            }

            using OpenFileDialog dlg = new();
            dlg.Title = "Select decklist file";
            dlg.Filter = "Plain text files (*.txt)|*.txt|all files(*.*)|*.*";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                tmpCodes.Clear();
                List<string> IDs = new();
                string ydkfile = dlg.FileName;
                string str;
                if (File.Exists(ydkfile))
                {
                    using FileStream f = new(ydkfile, FileMode.Open, FileAccess.Read);
                    StreamReader sr = new(f, Encoding.Default);
                    str = sr.ReadLine();
                    while (str != null)
                    {
                        if (str.Length > 0)
                            if (IDs.IndexOf(str) < 0)
                                IDs.Add(str.Replace("\"", "\"\"").ToLowerInvariant());
                        str = sr.ReadLine();
                    }
                    sr.Close();
                    f.Close();
                }
                if (IDs.Count == 0)
                    return;
                string[] names = IDs.ToArray();
                if (MessageBox.Show("Show cards outside of this YDK?", null, MessageBoxButtons.YesNo, MessageBoxIcon.None, MessageBoxDefaultButton.Button2) == DialogResult.Yes) {
                    SetCards(DataBase.Read(nowCdbFile, true, "SELECT datas.*,texts.* FROM datas,texts WHERE datas.id=texts.id and lower(name) not in (\"" + string.Join("\",\"", names) + "\");"), false);
                } else {
                    SetCards(DataBase.Read(nowCdbFile, true, "SELECT datas.*,texts.* FROM datas,texts WHERE datas.id=texts.id and lower(name) in (\"" + string.Join("\",\"", names) + "\");"), false);
                }
                foreach (Card c in cardlist)
                    tmpCodes.Add(c.id.ToString());
            }
        }
        //Read from image folder
        void Menuitem_readimagesClick(object sender, EventArgs e)
        {
            if (!CheckOpen())
            {
                return;
            }

            using FolderBrowserDialog fdlg = new();
            fdlg.Description = LanguageHelper.GetMsg(LMSG.SelectImagePath);
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                tmpCodes.Clear();
                string[] ids = YGOUtil.ReadImage(fdlg.SelectedPath);
                if (MessageBox.Show("Show cards without an image?", null, MessageBoxButtons.YesNo, MessageBoxIcon.None, MessageBoxDefaultButton.Button2) == DialogResult.Yes) {
                    SetCards(DataBase.Read(nowCdbFile, true, "SELECT datas.*,texts.* FROM datas,texts WHERE datas.id=texts.id and alias=0 and datas.id not in (" + string.Join(",", ids) + ");"), false);
                    foreach(Card c in cardlist)
                        tmpCodes.Add(c.id.ToString());
                } else {
                    tmpCodes.AddRange(ids);
                    SetCards(DataBase.Read(nowCdbFile, true,
                                           ids.OrderBy(int.Parse).ToArray()), false);
                }
            }
        }
        void Menuitem_readscriptsClick(object sender, EventArgs e)
        {
            if (!CheckOpen())
            {
                return;
            }

            using FolderBrowserDialog fdlg = new();
            fdlg.Description = "Select script folder";
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                tmpCodes.Clear();
                string[] ids = YGOUtil.ReadScript(fdlg.SelectedPath);
                if (MessageBox.Show("Show cards without a script?", null,
                    MessageBoxButtons.YesNo, MessageBoxIcon.None, MessageBoxDefaultButton.Button2) == DialogResult.Yes) {
                    SetCards(DataBase.Read(nowCdbFile, true, "SELECT datas.*,texts.* FROM datas,texts WHERE datas.id=texts.id and type & 17 != 17 and alias = 0 and datas.id not in (4005,4006,4007,4010,4011,4012,4017,4018,4019,10000050,10000060,10000070," + string.Join(",", ids) + ");"), false);
                    foreach (Card c in cardlist)
                        tmpCodes.Add(c.id.ToString());
                } else {
                    tmpCodes.AddRange(ids);
                    SetCards(DataBase.Read(nowCdbFile, true,
                                       ids.OrderBy(int.Parse).ToArray()), false);
                }
            }
        }
        //Close
        void Menuitem_quitClick(object sender, EventArgs e)
        {
            Close();
        }
        #endregion

        #region Worker task
        //Check whether a task is running
        bool IsRun()
        {
            if (tasker != null && tasker.IsRuning())
            {
                MyMsg.Warning(LMSG.RunError);
                return true;
            }
            return false;
        }
        //Run task
        void Run(string name)
        {
            if (IsRun())
            {
                return;
            }

            taskname = name;
            title = title + " (" + taskname + ")";
            SetTitle();
            bgWorker1.RunWorkerAsync();
        }
        //Worker task body
        void BgWorker1DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {
            tasker.Run();
        }
        void BgWorker1ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            title = string.Format("{0} ({1}-{2})",
                                  RemoveTag(title),
                                  taskname,
                                  // e.ProgressPercentage,
                                  e.UserState);
            SetTitle();
        }
        //Task completed
        void BgWorker1RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
        {
            //Restore the original title
            int t = title.LastIndexOf(" (");
            if (t > 0)
            {
                title = title[..t];
                SetTitle();
            }
            if (e.Error != null)
            {//Error
                if (tasker != null)
                {
                    tasker.Cancel();
                }

                if (bgWorker1.IsBusy)
                {
                    bgWorker1.CancelAsync();
                }

                MyMsg.Show(LanguageHelper.GetMsg(LMSG.TaskError) + "\n" + e.Error);
            }
            else if (tasker.IsCancel() || e.Cancelled)
            {//Task cancelled
                MyMsg.Show(LMSG.CancelTask);
            }
            else
            {
                MyTask mt = tasker.GetLastTask();
                switch (mt)
                {
                    case MyTask.CheckUpdate:
                        break;
                    case MyTask.ExportData:
                        MyMsg.Show(LMSG.ExportDataOK);
                        break;
                    case MyTask.CutImages:
                        MyMsg.Show(LMSG.CutImageOK);
                        break;
                    case MyTask.SaveAsMSE:
                        MyMsg.Show(LMSG.SaveMseOK);
                        break;
                    case MyTask.ConvertImages:
                        MyMsg.Show(LMSG.ConvertImageOK);
                        break;
                    case MyTask.ReadMSE:
                        //Save imported cards
                        SaveCards(tasker.CardList);
                        MyMsg.Show(LMSG.ReadMSEisOK);
                        break;
                }
            }
        }
        #endregion

        #region Copy cards
        //Get all cards or selected cards
        public Card[] GetCardList(bool onlyselect)
        {
            if (!CheckOpen())
            {
                return null;
            }

            List<Card> cards = new();
            if (onlyselect)
            {
                foreach (ListViewItem lvitem in lv_cardlist.SelectedItems)
                {
                    int index;
                    if (lvitem.Tag != null)
                    {
                        index = (int)lvitem.Tag;
                    }
                    else
                    {
                        index = lvitem.Index + (page - 1) * maxRow;
                    }

                    if (index >= 0 && index < cardlist.Count)
                    {
                        cards.Add(cardlist[index]);
                    }
                }
            }
            else
            {
                cards.AddRange(cardlist.ToArray());
            }

            if (cards.Count == 0)
            {
                //MyMsg.Show(LMSG.NoSelectCard);
            }
            return cards.ToArray();
        }
        void Menuitem_copytoClick(object sender, EventArgs e)
        {
            if (!CheckOpen())
            {
                return;
            }

            CopyTo(GetCardList(false));
        }

        void Menuitem_copyselecttoClick(object sender, EventArgs e)
        {
            if (!CheckOpen())
            {
                return;
            }

            CopyTo(GetCardList(true));
        }
        //Save cards to the current database
        public void SaveCards(Card[] cards)
        {
            cmdManager.ExcuteCommand(cardedit.copyCard, cards);
            Search(srcCard, true);
        }

        //Save/copy cards to another database
        static void CopyTo(Card[] cards)
        {
            if (cards == null || cards.Length == 0)
            {
                return;
            }
            //select file
            bool replace = false;
            string filename = null;
            using (OpenFileDialog dlg = new())
            {
                dlg.Title = LanguageHelper.GetMsg(LMSG.SelectDataBasePath);
                try
                {
                    dlg.Filter = LanguageHelper.GetMsg(LMSG.CdbType);
                }
                catch { }
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    filename = dlg.FileName;
                    replace = MyMsg.Question(LMSG.IfReplaceExistingCard);
                }
            }
            if (!string.IsNullOrEmpty(filename))
            {
                _ = DataBase.CopyDB(filename, !replace, cards);
                MyMsg.Show(LMSG.CopyCardsToDBIsOK);
            }

        }
        #endregion

        #region MSE files and image cropping
        //Crop images
        void Menuitem_cutimagesClick(object sender, EventArgs e)
        {
            if (!CheckOpen())
            {
                return;
            }

            if (IsRun())
            {
                return;
            }

            bool isreplace = MyMsg.Question(LMSG.IfReplaceExistingImage);
            tasker.SetTask(MyTask.CutImages, cardlist.ToArray(),
                           ygopath.picpath, isreplace.ToString());
            Run(LanguageHelper.GetMsg(LMSG.CutImage));
        }
        void Menuitem_saveasmse_selectClick(object sender, EventArgs e)
        {
            //Selected cards
            SaveAsMSE(true);
        }

        void Menuitem_saveasmseClick(object sender, EventArgs e)
        {
            //All cards
            SaveAsMSE(false);
        }
        void SaveAsMSE(bool onlyselect)
        {
            if (!CheckOpen())
            {
                return;
            }

            if (IsRun())
            {
                return;
            }

            Card[] cards = GetCardList(onlyselect);
            if (cards == null)
            {
                return;
            }
            //select save mse-set
            using SaveFileDialog dlg = new();
            dlg.Title = LanguageHelper.GetMsg(LMSG.selectMseset);
            try
            {
                dlg.Filter = LanguageHelper.GetMsg(LMSG.MseType);
            }
            catch { }
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                tasker.SetTask(MyTask.SaveAsMSE, cards, dlg.FileName, "");
                Run(LanguageHelper.GetMsg(LMSG.SaveMse));
            }
        }
        #endregion

        #region Import card art
        void ImportImageFromSelect()
        {
            string tid = tb_cardcode.Text;
            if (tid == "0" || tid.Length == 0)
            {
                return;
            }

            using OpenFileDialog dlg = new();
            dlg.Title = LanguageHelper.GetMsg(LMSG.SelectImage) + "-" + tb_cardname.Text;
            try
            {
                dlg.Filter = LanguageHelper.GetMsg(LMSG.ImageType);
            }
            catch { }
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                //dlg.FileName;
                ImportImage(dlg.FileName, tid);
            }
        }
        private void Pl_image_DoubleClick(object sender, EventArgs e)
        {
            ImportImageFromSelect();
        }
        void Pl_imageDragDrop(object sender, DragEventArgs e)
        {
            string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (File.Exists(files[0]))
            {
                ImportImage(files[0], tb_cardcode.Text);
            }
        }

        void Pl_imageDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Link; //Mark drag data as a link, such as a file path
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }
        private void Menuitem_importmseimg_Click(object sender, EventArgs e)
        {
            string tid = tb_cardcode.Text;
            menuitem_importmseimg.Checked = !menuitem_importmseimg.Checked;
            SetImage(tid);
        }
        void ImportImage(string file, string tid)
        {
            string f;
            if (pl_image.BackgroundImage != null
                && pl_image.BackgroundImage != cover)
            {//Release image resources
                pl_image.BackgroundImage.Dispose();
                pl_image.BackgroundImage = cover;
            }
            if (menuitem_importmseimg.Checked)
            {
                if (!Directory.Exists(tasker.MSEImagePath))
                {
                    _ = Directory.CreateDirectory(tasker.MSEImagePath);
                }

                f = MyPath.Combine(tasker.MSEImagePath, tid + ".jpg");
                File.Copy(file, f, true);
            }
            else
            {
                //	tasker.ToImg(file, ygopath.GetImage(tid),
                //				 ygopath.GetImageThum(tid));
                tasker.ToImg(file, ygopath.GetImage(tid));
            }
            SetImage(tid);
        }
        public void SetImage(string id)
        {
            _ = long.TryParse(id, out long t);
            SetImage(t);
        }
        public void SetImage(long id)
        {
            string pic = ygopath.GetImage(id);
            if (menuitem_importmseimg.Checked)//Show MSE image
            {
                string msepic = MseMaker.GetCardImagePath(tasker.MSEImagePath, oldCard);
                if (File.Exists(msepic))
                {
                    pl_image.BackgroundImage = MyBitmap.ReadImage(msepic);
                }
            }
            else if (File.Exists(pic))
            {
                pl_image.BackgroundImage = MyBitmap.ReadImage(pic);
            }
            else
            {
                pl_image.BackgroundImage = cover;
            }
        }
        void Menuitem_convertimageClick(object sender, EventArgs e)
        {
            if (!CheckOpen())
            {
                return;
            }

            if (IsRun())
            {
                return;
            }

            using FolderBrowserDialog fdlg = new();
            fdlg.Description = LanguageHelper.GetMsg(LMSG.SelectImagePath);
            if (fdlg.ShowDialog() == DialogResult.OK)
            {
                bool isreplace = MyMsg.Question(LMSG.IfReplaceExistingImage);
                tasker.SetTask(MyTask.ConvertImages, null,
                               fdlg.SelectedPath, ygopath.gamepath, isreplace.ToString());
                Run(LanguageHelper.GetMsg(LMSG.ConvertImage));
            }
        }
        #endregion

        #region Export data package
        void Menuitem_exportdataClick(object sender, EventArgs e)
        {
            if (!CheckOpen())
            {
                return;
            }

            if (IsRun())
            {
                return;
            }

            using SaveFileDialog dlg = new();
            dlg.InitialDirectory = ygopath.gamepath;
            try
            {
                dlg.Filter = "Zip|(*.zip|All Files(*.*)|*.*";
            }
            catch { }
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                tasker.SetTask(MyTask.ExportData,
                               GetCardList(false),
                               ygopath.gamepath,
                               dlg.FileName,
                               GetOpenFile(),
                               DefaultScriptName);
                Run(LanguageHelper.GetMsg(LMSG.ExportData));
            }

        }
        #endregion

        #region Compare data
        /// <summary>
        /// Returns true for matching data; false for missing or different data.
        /// </summary>
        static bool CheckCard(Card[] cards, Card card, bool checkinfo)
        {
            foreach (Card c in cards)
            {
                if (c.id != card.id)
                {
                    continue;
                }
                //Card data differs
                if (checkinfo)
                {
                    return card.EqualsData(c);
                }
                else
                {
                    return true;
                }
            }
            return false;
        }
        //Read data to compare
        Card[] GetCompCards()
        {
            if (tmpCodes.Count == 0)
            {
                return null;
            }

            if (!CheckOpen())
            {
                return null;
            }

            return DataBase.Read(nowCdbFile, true, tmpCodes.ToArray());
        }
        public void CompareCards(string cdbfile, bool checktext)
        {
            if (!CheckOpen())
            {
                return;
            }

            tmpCodes.Clear();
            srcCard = new Card();
            Card[] mcards = DataBase.Read(nowCdbFile, true, "");
            Card[] cards = DataBase.Read(cdbfile, true, "");
            foreach (Card card in mcards)
            {
                if (!CheckCard(cards, card, checktext))//Add to ID set
                {
                    tmpCodes.Add(card.id.ToString());
                }
            }
            if (tmpCodes.Count == 0)
            {
                SetCards(null, false);
                return;
            }
            SetCards(GetCompCards(), false);
        }
        #endregion

        #region MSE config menu
        //Add config files to the menu
        void AddMenuItemFormMSE()
        {
            if (!Directory.Exists(datapath))
            {
                return;
            }

            menuitem_mseconfig.DropDownItems.Clear();//Clear current state
            string[] files = MyPath.FindFiles(datapath, MyPath.GetFileName(MSEConfig.TAG, "*"), "mse");
            foreach (string file in files)
            {
                string name = MyPath.GetFullFileName(MSEConfig.TAG, file);
                //Check for MSE config files
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }
                //Use the language name as menu text
                ToolStripMenuItem tsmi = new(name)
                {
                    ToolTipText = file//Store the real path in the tooltip
                };
                tsmi.Click += SetMseConfig_Click;
                if (msecfg.configName.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    tsmi.Checked = true;//Check the current config
                }

                _ = menuitem_mseconfig.DropDownItems.Add(tsmi);
            }
        }
        void SetMseConfig_Click(object sender, EventArgs e)
        {
            if (IsRun())//Task is running
            {
                return;
            }

            if (sender is ToolStripMenuItem tsmi)
            {
                //Load selected config
                msecfg.SetConfig(tsmi.ToolTipText, datapath);
                //Refresh menu
                AddMenuItemFormMSE();
                //Save config
                XMLReader.Save(DEXConfig.TAG_MSE, tsmi.Text);
            }
        }
        #endregion

        #region Find Lua functions
        private void Menuitem_findluafunc_Click(object sender, EventArgs e)
        {
            string funtxt = DEXConfig.GetFunctionFile(datapath);
            using FolderBrowserDialog fd = new();
            fd.Description = "Folder Name: ocgcore";
            if (fd.ShowDialog() == DialogResult.OK)
            {
                LuaFunction.Read(funtxt);//Load the previous function list first
                _ = LuaFunction.Find(fd.SelectedPath);//Find new functions and save them
                _ = MessageBox.Show("OK");
            }
        }

        #endregion

        #region Setcode text boxes
        //On setcode text input
        void SetCode_InputText(int index, ComboBox cb, TextBox tb)
        {
            if (index >= 0 && index < setcodeIsedit.Length)
            {
                if (setcodeIsedit[index])//Ignore recursive edit updates
                {
                    return;
                }

                setcodeIsedit[index] = true;
                _ = int.TryParse(tb.Text, NumberStyles.HexNumber, null, out int temp);
                //tb.Text = temp.ToString("x");
                if (temp == 0 && (tb.Text != "0" || tb.Text.Length == 0))
                {
                    temp = -1;
                }

                SetSelect(cb, temp);
                setcodeIsedit[index] = false;
            }
        }
        private void Tb_setcode1_TextChanged(object sender, EventArgs e)
        {
            SetCode_InputText(1, cb_setname1, tb_setcode1);
        }

        private void Tb_setcode2_TextChanged(object sender, EventArgs e)
        {
            SetCode_InputText(2, cb_setname2, tb_setcode2);
        }

        private void Tb_setcode3_TextChanged(object sender, EventArgs e)
        {
            SetCode_InputText(3, cb_setname3, tb_setcode3);
        }

        private void Tb_setcode4_TextChanged(object sender, EventArgs e)
        {
            SetCode_InputText(4, cb_setname4, tb_setcode4);
        }
        #endregion

        #region Setcode combo boxes
        //On setcode combo selection
        void SetCode_Selected(int index, ComboBox cb, TextBox tb)
        {
            if (index >= 0 && index < setcodeIsedit.Length)
            {
                if (setcodeIsedit[index])//Ignore recursive edit updates
                {
                    return;
                }

                setcodeIsedit[index] = true;
                long tmp = GetSelect(cb);
                tb.Text = tmp.ToString("x");
                setcodeIsedit[index] = false;
            }
        }
        private void Cb_setname1_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetCode_Selected(1, cb_setname1, tb_setcode1);
        }

        private void Cb_setname2_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetCode_Selected(2, cb_setname2, tb_setcode2);
        }

        private void Cb_setname3_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetCode_Selected(3, cb_setname3, tb_setcode3);
        }

        private void Cb_setname4_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetCode_Selected(4, cb_setname4, tb_setcode4);
        }
        #endregion

        #region Read MSE file
        private void Menuitem_readmse_Click(object sender, EventArgs e)
        {
            if (!CheckOpen())
            {
                return;
            }

            if (IsRun())
            {
                return;
            }
            //select open mse-set
            using OpenFileDialog dlg = new();
            dlg.Title = LanguageHelper.GetMsg(LMSG.selectMseset);
            try
            {
                dlg.Filter = LanguageHelper.GetMsg(LMSG.MseType);
            }
            catch { }
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                bool isUpdate = MyMsg.Question(LMSG.IfReplaceExistingImage);
                tasker.SetTask(MyTask.ReadMSE, null,
                               dlg.FileName, isUpdate.ToString());
                Run(LanguageHelper.GetMsg(LMSG.ReadMSE));
            }
        }
        #endregion

        #region Compact database
        private void Menuitem_compdb_Click(object sender, EventArgs e)
        {
            if (!CheckOpen())
            {
                return;
            }

            DataBase.Compression(nowCdbFile);
            MyMsg.Show(LMSG.CompDBOK);
        }
        #endregion

        #region Settings
        //Delete image/script files when deleting cards
        private void Menuitem_deletecardsfile_Click(object sender, EventArgs e)
        {
            menuitem_operacardsfile.Checked = !menuitem_operacardsfile.Checked;
            XMLReader.Save(DEXConfig.TAG_DELETE_WITH, menuitem_operacardsfile.Checked.ToString().ToLower());
        }
        //Open Lua scripts in the code editor
        private void Menuitem_openfileinthis_Click(object sender, EventArgs e)
        {
            menuitem_openfileinthis.Checked = !menuitem_openfileinthis.Checked;
            XMLReader.Save(DEXConfig.TAG_OPEN_IN_THIS, menuitem_openfileinthis.Checked.ToString().ToLower());
        }
        //Load auto-update setting
        private void Menuitem_autocheckupdate_Click(object sender, EventArgs e)
        {
            menuitem_autocheckupdate.Checked = !menuitem_autocheckupdate.Checked;
            XMLReader.Save(DEXConfig.TAG_AUTO_CHECK_UPDATE, menuitem_autocheckupdate.Checked.ToString().ToLower());
        }
        //Set the default script opened when no card ID is selected
        private void Menuitem_default_script_Click(object sender, EventArgs e)
        {
            DefaultScriptName = Microsoft.VisualBasic.Interaction.InputBox(
                "Set default script name (without extension).\n\nPress \"Cancel\" to remove the default script.",
                "",
                DefaultScriptName);
            XMLReader.Save(DEXConfig.TAG_DEFAULT_SCRIPT_NAME, DefaultScriptName);
        }
        #endregion

        #region Language menu
        void GetLanguageItem()
        {
            if (!Directory.Exists(datapath))
            {
                return;
            }

            menuitem_language.DropDownItems.Clear();
            TextInfo txinfo = new CultureInfo(CultureInfo.InstalledUICulture.Name).TextInfo;
            foreach (string name in DEXConfig.GetAvailableLanguageNames(datapath))
            {
                ToolStripMenuItem tsmi = new(txinfo.ToTitleCase(name))
                {
                    Tag = name
                };
                tsmi.Click += SetLanguage_Click;
                if (DEXConfig.ReadString(DEXConfig.TAG_LANGUAGE).Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    tsmi.Checked = true;
                }

                _ = menuitem_language.DropDownItems.Add(tsmi);
            }
        }
        void SetLanguage_Click(object sender, EventArgs e)
        {
            if (IsRun())
            {
                return;
            }

            if (sender is ToolStripMenuItem tsmi)
            {
                string language = tsmi.Tag?.ToString() ?? tsmi.Text;
                XMLReader.Save(DEXConfig.TAG_LANGUAGE, language);
                GetLanguageItem();
                MyMsg.Show(LMSG.PlzRestart);
            }
        }
        #endregion

        //Export MSE set as images
        void Menuitem_exportMSEimageClick(object sender, EventArgs e)
        {
            if (IsRun())
            {
                return;
            }

            string msepath = MyPath.GetRealPath(DEXConfig.ReadString(DEXConfig.TAG_MSE_PATH));
            if (!File.Exists(msepath))
            {
                MyMsg.Error(LMSG.exportMseImagesErr);
                menuitem_exportMSEimage.Checked = false;
                return;
            }
            else
            {
                if (MseMaker.MseIsRunning())
                {
                    MseMaker.MseStop();
                    menuitem_exportMSEimage.Checked = false;
                    return;
                }
                else
                {

                }
            }
            //select open mse-set
            using OpenFileDialog dlg = new();
            dlg.Title = LanguageHelper.GetMsg(LMSG.selectMseset);
            try
            {
                dlg.Filter = LanguageHelper.GetMsg(LMSG.MseType);
            }
            catch { }
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                string mseset = dlg.FileName;
                string exportpath = MyPath.GetRealPath(DEXConfig.ReadString(DEXConfig.TAG_MSE_EXPORT));
                MseMaker.ExportSet(msepath, mseset, exportpath, delegate
                {
                    menuitem_exportMSEimage.Checked = false;
                });
                menuitem_exportMSEimage.Checked = true;
            }
            else
            {
                menuitem_exportMSEimage.Checked = false;
            }
        }
        void Menuitem_testPendulumTextClick(object sender, EventArgs e)
        {
            Card c = GetCard();
            if (c != null)
            {
                tasker.TestPendulumText(c.desc);
            }
        }
        void Menuitem_export_select_sqlClick(object sender, EventArgs e)
        {
            using SaveFileDialog dlg = new();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                DataBase.ExportSql(dlg.FileName, GetCardList(true));
                MyMsg.Show("OK");
            }
        }
        void Menuitem_export_all_sqlClick(object sender, EventArgs e)
        {
            using SaveFileDialog dlg = new();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                DataBase.ExportSql(dlg.FileName, GetCardList(false));
                MyMsg.Show("OK");
            }
        }
        void Menuitem_autoreturnClick(object sender, EventArgs e)
        {
            if (!CheckOpen())
            {
                return;
            }

            using SaveFileDialog dlg = new();
            dlg.Title = LanguageHelper.GetMsg(LMSG.SelectDataBasePath);
            try
            {
                dlg.Filter = LanguageHelper.GetMsg(LMSG.CdbType);
            }
            catch { }
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                Card[] cards = DataBase.Read(nowCdbFile, true, "");
                int count = cards.Length;
                if (cards == null || cards.Length == 0)
                {
                    return;
                }

                if (DataBase.Create(dlg.FileName))
                {
                    //
                    int len = DEXConfig.ReadInteger(DEXConfig.TAG_AUTO_LEN, 30);
                    for (int i = 0; i < count; i++)
                    {
                        if (cards[i].desc != null)
                        {
                            cards[i].desc = StrUtil.AutoEnter(cards[i].desc, len, ' ');
                        }
                    }
                    _ = DataBase.CopyDB(dlg.FileName, false, cards);
                    MyMsg.Show(LMSG.CopyCardsToDBIsOK);
                }
            }
        }

        void Menuitem_replaceClick(object sender, EventArgs e)
        {
            if (!CheckOpen())
            {
                return;
            }

            using SaveFileDialog dlg = new();
            dlg.Title = LanguageHelper.GetMsg(LMSG.SelectDataBasePath);
            try
            {
                dlg.Filter = LanguageHelper.GetMsg(LMSG.CdbType);
            }
            catch { }
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                Card[] cards = DataBase.Read(nowCdbFile, true, "");
                int count = cards.Length;
                if (cards == null || cards.Length == 0)
                {
                    return;
                }

                if (DataBase.Create(dlg.FileName))
                {
                    //
                    _ = DEXConfig.ReadInteger(DEXConfig.TAG_AUTO_LEN, 30);
                    _ = DataBase.CopyDB(dlg.FileName, false, cards);
                    MyMsg.Show(LMSG.CopyCardsToDBIsOK);
                }
            }
        }

        private void Text2LinkMarks(string text)
        {
            try
            {
                long mark = Convert.ToInt64(text, 2);
                SetLinkMarks(mark, true);
            }
            catch
            {
                //
            }
        }

        void Tb_linkTextChanged(object sender, EventArgs e)
        {
            Text2LinkMarks(tb_link.Text);
        }

        private void DataEditForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.F)
            {
                _ = tb_cardname.Focus();
                tb_cardname.SelectAll();
            }
        }

        private void Tb_cardtext_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Modifiers == Keys.Control && e.KeyCode == Keys.R)
            {
                Btn_resetClick(null, null);
            }
            else if (e.Modifiers == Keys.Control && e.KeyCode == Keys.F)
            {
                _ = tb_cardname.Focus();
            }
        }

        private void OnDragDrop(object sender, DragEventArgs e)
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
                (DockPanel.Parent as MainForm).Open(file);
            }
        }
        private static void GetTexts(string txt, out string[] tmp)
        {
            tmp = new string[2];
            bool mf = false;
            foreach (string l in txt.Split("\n", StringSplitOptions.RemoveEmptyEntries))
            {
                string n = l.Replace("\r", "");
                if (n.StartsWith("[") || n.StartsWith("---")) continue;
                if (n.Contains('●') || (!string.IsNullOrEmpty(tmp[1])
                    && tmp[1].Contains("FLIP:", StringComparison.OrdinalIgnoreCase)))
                    tmp[mf ? 1 : 0] += n;
                else
                {
                    if (mf)
                    {
                        if (string.IsNullOrEmpty(tmp[1]))
                            tmp[1] = n;
                        else
                        {
                            tmp[0] += n;
                            tmp[1] = tmp[1].Replace(n, "");
                        }
                    }
                    else
                    {
                        tmp[mf ? 1 : 0] = n;
                        mf = true;
                    }
                }
            }
        }
        void TextToPendulum(object sender, EventArgs e)
        {
            string txt = tb_cardtext.Text;
            if ((GetCheck(pl_cardtype) & 0x1000000) > 0)
            {
                string[] template = msecfg.temp_text.Replace("\n", Environment.NewLine).Split("%");
                GetTexts(txt, out string[] tmp);
                if (!txt.StartsWith(template[0]))
                    tb_cardtext.Text = template.Length > 5 ? template[0] + tb_pleft.Text
                        + (tb_pleft.Text != tb_pright.Text ? "/" + tb_pright.Text : "") + template[2]
                        + ((GetCheck(pl_flags) & 0x800000) > 0 ? "" : tmp[0]) + template[4] + tmp[1] + template[6]
                        : template[0] + ((GetCheck(pl_flags) & 0x800000) > 0 ? "" : tmp[0]) + template[2] + tmp[1]
                        + template[4];
            }
            else
            {
                txt = Regex.Replace(txt, msecfg.regx_pendulum, "$1");
                txt = Regex.Replace(txt, "(\\r?\\n)*---*\\r?\\n.*", "");
                txt = Regex.Replace(txt, msecfg.regx_monster, "$1");
                txt = Regex.Replace(txt, "^(\\r?\\n)*", "", RegexOptions.Multiline);
                txt = Regex.Replace(txt, "(?:\\r)?\\n", Environment.NewLine);
                tb_cardtext.Text = txt;
            }
        }
        private void OnDragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        void Tb_linkKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != '0' && e.KeyChar != '1' && e.KeyChar != 1 && e.KeyChar != 22 && e.KeyChar != 3 && e.KeyChar != 8)
            {
                //				MessageBox.Show("key="+(int)e.KeyChar);
                e.Handled = true;
            }
            else
            {
                Text2LinkMarks(tb_link.Text);
            }
        }
        void DataEditFormSizeChanged(object sender, EventArgs e)
        {
            InitListRows();
            UpdateCardListPaging();
            AddListView(page);
        }
        private void AddArchetypeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainForm mf;
            try
            {
                mf = DockPanel.Parent as MainForm;
            }
            catch
            {
                return;
            }
            DataConfig datacfg = mf.GetDataConfig();
            Dictionary<long, string> d = datacfg.dicSetnames;
            AddArchetypeForm form = new(d);
            if (form.ShowDialog() == DialogResult.OK)
            {
                int setcode = form.code;
                string setname = form.name;
                if (!d.ContainsKey(setcode)) d.Add(setcode, setname);
                mf.GetCodeConfig().SetNames(d);
                InitControl(datacfg);
            }
        }
        private void Pl_categoryScroll(object sender, MouseEventArgs e)
        {
            int d = e.Delta / 6;
            int c = pl_category.VerticalScroll.Value;
            pl_category.VerticalScroll.Value = Math.Max(0, c + d);
        }
        private void Pl_flagsScroll(object sender, MouseEventArgs e)
        {
            int d = e.Delta / 6;
            int c = pl_flags.VerticalScroll.Value;
            pl_flags.VerticalScroll.Value = Math.Max(0, c + d);
        }

        public void ApplyTheme()
        {
            ThemeManager.ThemePalette palette = ThemeManager.CurrentPalette;
            ThemeManager.ApplyControlTree(this);
            pl_image.BackColor = palette.UsesOriginalColors ? SystemColors.ButtonHighlight : palette.InputBackColor;
            ThemeManager.ApplyListViewItems(lv_cardlist);
        }
    }
}
