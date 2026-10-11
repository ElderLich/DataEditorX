using FastColoredTextBoxNS;
using System.Text.RegularExpressions;

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
        readonly HashSet<(string Key, string Value)> shortTooltipEntries = new();
        readonly HashSet<(string Key, string Value)> longTooltipEntries = new();
        static readonly Regex FunctionSignature = new(
            @"^●.+?\s+([A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)\s*\(");
        static readonly Regex ConstantAssignment = new(
            @"^\s*([A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)\s*=\s*(.+)$");
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

        public void AddStringHint(string key, string text)
        {
            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(text))
            {
                AddToolIipDic(key.Trim(), text.Trim());
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
                    Match signature = FunctionSignature.Match(line);
                    isFind = signature.Success;
                    name = isFind ? signature.Groups[1].Value : string.Empty;
                    desc = isFind ? line : string.Empty;
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

            foreach (string line in File.ReadLines(conlua))
            {
                Match match = ConstantAssignment.Match(line);
                if (!match.Success) continue;
                AddToolIipDic(match.Groups[1].Value,
                    match.Groups[2].Value.Replace("--", Environment.NewLine));
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
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(val)) return;
            key = key.Trim();
            val = val.Trim();
            string skey = GetShortName(key);
            string canonicalKey = key.StartsWith("RD.", StringComparison.Ordinal)
                ? "RushDuel." + key[3..]
                : key.StartsWith("aux.", StringComparison.Ordinal) ? "Auxiliary." + key[4..] : key;
            string canonicalValue = val.Replace(" " + key + "(", " " + canonicalKey + "(", StringComparison.Ordinal);
            // Keep both qualified aliases in completion, but show their shared
            // documentation once in the unqualified method tooltip.
            if (shortTooltipEntries.Add((skey, canonicalValue)))
                AppendTooltip(tooltipDic, skey, val);
            if (longTooltipEntries.Add((key, val)))
                AppendTooltip(longTooltipDic, key, val);
        }

        static void AppendTooltip(SortedList<string, string> dictionary, string key, string value)
        {
            if (dictionary.TryGetValue(key, out string existing))
                dictionary[key] = existing + Environment.NewLine + Environment.NewLine + value;
            else
                dictionary.Add(key, value);
        }
        #endregion
    }
}
