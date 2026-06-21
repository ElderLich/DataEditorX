using FastColoredTextBoxNS;

namespace DataEditorX.Config
{
    /// <summary>
    /// Code editor configuration
    /// </summary>
    public class CodeConfig
    {

        #region Fields
        public CodeConfig()
        {
            tooltipDic = new SortedList<string, string>();
            longTooltipDic = new SortedList<string, string>();
            items = new List<AutocompleteItem>();
        }

        //Function hints
        readonly SortedList<string, string> tooltipDic;
        readonly SortedList<string, string> longTooltipDic;
        readonly List<AutocompleteItem> items;
        /// <summary>
        /// Input hints
        /// </summary>
        public SortedList<string, string> TooltipDic
        {
            get { return tooltipDic; }
        }
        public SortedList<string, string> LongTooltipDic
        {
            get { return longTooltipDic; }
        }
        public AutocompleteItem[] Items
        {
            get { return items.ToArray(); }
        }
        #endregion

        #region Set names and counters
        /// <summary>
        /// Set archetype/setcode names
        /// </summary>
        /// <param name="dic"></param>
        public void SetNames(Dictionary<long, string> dic)
        {
            foreach (long k in dic.Keys)
            {
                string key = "0x" + k.ToString("x");
                if (!tooltipDic.ContainsKey(key))
                {
                    AddToolIipDic(key, dic[k]);
                }
            }
        }
        /// <summary>
        /// Read counters
        /// </summary>
        /// <param name="file"></param>
        public void AddStrings(string file)
        {
            if (File.Exists(file))
            {
                string[] lines = File.ReadAllLines(file);
                foreach (string line in lines)
                {
                    //Special victories and counters
                    if (line.StartsWith("!victory")
                       || line.StartsWith("!counter"))
                    {
                        string[] ws = line.Split([' '], 3);
                        if (ws.Length > 2)
                        {
                            AddToolIipDic(ws[1], ws[2]);
                        }
                    }
                }
            }
        }

        #endregion

        #region function
        public void AddFunction(string funtxt)
        {
            if (!File.Exists(funtxt))
            {
                return;
            }

            string[] lines = File.ReadAllLines(funtxt);
            bool isFind = false;
            string name = "";
            string desc = "";
            foreach (string line in lines)
            {
                if (string.IsNullOrEmpty(line)
                   || line.StartsWith("==")
                   || line.StartsWith("#"))
                {
                    continue;
                }

                if (line.StartsWith("●"))
                {
                    //add
                    AddToolIipDic(name, desc);
                    int w = line.IndexOf("(");
                    int t = line.IndexOf(" ");

                    if (t < w && t > 0)
                    {
                        //Found function
                        name = line.Substring(t + 1, w - t - 1);
                        isFind = true;
                        desc = line;
                    }
                }
                else if (isFind)
                {
                    desc += Environment.NewLine + line;
                }
            }
            AddToolIipDic(name, desc);
        }
        #endregion

        #region Constants
        public void AddConstant(string conlua)
        {
            //conList.Add("con");
            if (!File.Exists(conlua))
            {
                return;
            }

            string[] lines = File.ReadAllLines(conlua);
            foreach (string line in lines)
            {
                if (line.StartsWith("--"))
                {
                    continue;
                }

                int t = line.IndexOf("=");
                _ = line.IndexOf("--");
                //Constants = 0x1 ---Comment
                string k = (t > 0) ? line[..t].TrimEnd([' ', '\t'])
                    : line;
                string desc = (t > 0) ? line[(t + 1)..].Replace("--", "\n")
    : line;
                AddToolIipDic(k, desc);
            }
        }
        #endregion

        #region Processing
        public void InitAutoMenus()
        {
            items.Clear();
            foreach (string k in tooltipDic.Keys)
            {
                AutocompleteItem item = new(k)
                {
                    ToolTipTitle = k,
                    ToolTipText = tooltipDic[k]
                };
                items.Add(item);
            }
            foreach (string k in longTooltipDic.Keys)
            {
                if (tooltipDic.ContainsKey(k))
                {
                    continue;
                }

                AutocompleteItem item = new(k)
                {
                    ToolTipTitle = k,
                    ToolTipText = longTooltipDic[k]
                };
                items.Add(item);
            }
        }

        static string GetShortName(string name)
        {
            int t = name.IndexOf(".");
            if (t > 0)
            {
                return name[(t + 1)..];
            }
            else
            {
                return name;
            }
        }
        void AddToolIipDic(string key, string val)
        {
            string skey = GetShortName(key);
            if (tooltipDic.ContainsKey(skey))//Exists
            {
                string nval = tooltipDic[skey];
                if (!nval.EndsWith(Environment.NewLine))
                {
                    nval += Environment.NewLine;
                }

                nval += Environment.NewLine + val;
                tooltipDic[skey] = nval;
            }
            else
            {
                tooltipDic.Add(skey, val);
            }
            //
            AddLongToolIipDic(key, val);
        }
        void AddLongToolIipDic(string key, string val)
        {
            if (longTooltipDic.ContainsKey(key))//Exists
            {
                string nval = longTooltipDic[key];
                if (!nval.EndsWith(Environment.NewLine))
                {
                    nval += Environment.NewLine;
                }

                nval += Environment.NewLine + val;
                longTooltipDic[key] = nval;
            }
            else
            {
                longTooltipDic.Add(key, val);
            }
        }
        #endregion
    }
}
