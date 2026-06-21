/*
 * Created with SharpDevelop.
 * User: Acer
 * Date: 2014-10-15
 * Time: 15:47
 * 
 */
using DataEditorX.Common;
using DataEditorX.Config;
using System.Text;

namespace DataEditorX.Core.Mse
{
    /// <summary>
    /// Description of MSEConfig.
    /// </summary>
    public class MSEConfig
    {
        #region  Constants
        public const string TAG = "mse";
        /// <summary>Save file header</summary>
        public const string TAG_HEAD = "head";
        /// <summary>Save file footer</summary>
        public const string TAG_END = "end";
        /// <summary>Simplified-to-Traditional conversion</summary>
        public const string TAG_CN2TW = "cn2tw";
        /// <summary>Spell symbol format</summary>
        public const string TAG_SPELL = "spell";
        /// <summary>Trap symbol format</summary>
        public const string TAG_TRAP = "trap";
        public const string TAG_REG_PENDULUM = "pendulum-text";
        public const string TAG_REG_MONSTER = "monster-text";
        public const string TAG_REG_RUSH = "rush-text";
        public const string TAG_MAXCOUNT = "maxcount";
        public const string TAG_RACE = "race";
        public const string TAG_TYPE = "type";
        public const string TAG_WIDTH = "width";
        public const string TAG_HEIGHT = "height";

        public const string TAG_REIMAGE = "reimage";
        public const string TAG_PEND_WIDTH = "pwidth";
        public const string TAG_PEND_HEIGHT = "pheight";

        public const string TAG_IMAGE = "imagepath";
        public const string TAG_REPALCE = "replace";
        public const string TAG_TEXT = "text";

        public const string TAG_NO_TEN = "no10";

        public const string TAG_NO_START_CARDS = "no_star_cards";

        public const string TAG_REP = "%%";
        public const string SEP_LINE = " ";
        //Default configuration
        public const string FILE_CONFIG_NAME = "Chinese-Simplified";
        public const string PATH_IMAGE = "Images";
        public string configName = FILE_CONFIG_NAME;
        #endregion
        public MSEConfig(string path)
        {
            Init(path);
        }
        public void SetConfig(string config, string path)
        {
            if (!File.Exists(config))
            {
                return;
            }

            regx_monster = "(\\s\\S*?)";
            regx_pendulum = "(\\s\\S*?)";
            regx_rush = "(\\s\\S*?)";
            //Set file name
            configName = MyPath.GetFullFileName(TAG, config);

            replaces = new SortedList<string, string>();

            typeDic = new SortedList<long, string>();
            raceDic = new SortedList<long, string>();
            string[] lines = File.ReadAllLines(config, Encoding.UTF8);
            foreach (string line in lines)
            {
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                {
                    continue;
                }

                if (line.StartsWith(TAG_CN2TW))
                {
                    Iscn2tw = ConfHelper.GetBooleanValue(line);
                }
                else if (line.StartsWith(TAG_SPELL))
                {
                    str_spell = ConfHelper.GetValue(line);
                }
                else if (line.StartsWith(TAG_HEAD))
                {
                    head = ConfHelper.GetMultLineValue(line);
                }
                else if (line.StartsWith(TAG_END))
                {
                    end = ConfHelper.GetMultLineValue(line);
                }
                else if (line.StartsWith(TAG_TEXT))
                {
                    temp_text = ConfHelper.GetMultLineValue(line);
                }
                else if (line.StartsWith(TAG_TRAP))
                {
                    str_trap = ConfHelper.GetValue(line);
                }
                else if (line.StartsWith(TAG_REG_PENDULUM))
                {
                    regx_pendulum = RegXPendulum = ConfHelper.GetValue(line);
                }
                else if (line.StartsWith(TAG_REG_MONSTER))
                {
                    regx_monster = RegXMonster = ConfHelper.GetValue(line);
                }
                else if (line.StartsWith(TAG_REG_RUSH))
                {
                    regx_rush = RegXRush = ConfHelper.GetValue(line);
                }
                else if (line.StartsWith(TAG_MAXCOUNT))
                {
                    maxcount = ConfHelper.GetIntegerValue(line, 0);
                }
                else if (line.StartsWith(TAG_WIDTH))
                {
                    width = ConfHelper.GetIntegerValue(line, 0);
                }
                else if (line.StartsWith(TAG_HEIGHT))
                {
                    height = ConfHelper.GetIntegerValue(line, 0);
                }
                else if (line.StartsWith(TAG_PEND_WIDTH))
                {
                    pwidth = ConfHelper.GetIntegerValue(line, 0);
                }
                else if (line.StartsWith(TAG_PEND_HEIGHT))
                {
                    pheight = ConfHelper.GetIntegerValue(line, 0);
                }
                else if (line.StartsWith(TAG_NO_TEN))
                {
                    no10 = ConfHelper.GetBooleanValue(line);
                }
                else if (line.StartsWith(TAG_NO_START_CARDS))
                {
                    string val = ConfHelper.GetValue(line);
                    string[] cs = val.Split(',');
                    noStartCards = new long[cs.Length];
                    int i = 0;
                    foreach (string str in cs)
                    {
                        _ = long.TryParse(str, out long l);
                        noStartCards[i++] = l;
                    }
                }
                else if (line.StartsWith(TAG_IMAGE))
                {
                    //Use fallback path when configured path is invalid
                    imagepath = MyPath.CheckDir(ConfHelper.GetValue(line), MyPath.Combine(path, PATH_IMAGE));
                    //Image cache directory
                    imagecache = MyPath.Combine(imagepath, "cache");
                    MyPath.CreateDir(imagecache);
                }
                else if (line.StartsWith(TAG_REPALCE))
                {//Special character replacements
                    string word = ConfHelper.GetValue(line);
                    string p = ConfHelper.GetRegex(ConfHelper.GetValue1(word));
                    string r = ConfHelper.GetRegex(ConfHelper.GetValue2(word));
                    if (!string.IsNullOrEmpty(p))
                    {
                        replaces.Add(p, r);
                    }
                }
                else if (line.StartsWith(TAG_RACE))
                {//Race
                    ConfHelper.DicAdd(raceDic, line);
                }
                else if (line.StartsWith(TAG_TYPE))
                {//Types
                    ConfHelper.DicAdd(typeDic, line);
                }
                else if (line.StartsWith(TAG_REIMAGE))
                {
                    reimage = ConfHelper.GetBooleanValue(line);
                }
            }
        }
        public void Init(string path)
        {
            Iscn2tw = false;

            //Read configuration
            string tmp = MyPath.FindFile(path, MyPath.GetFileName(TAG, DEXConfig.ReadString(DEXConfig.TAG_MSE)), "mse");

            if (!File.Exists(tmp))
            {
                tmp = MyPath.FindFile(path, MyPath.GetFileName(TAG, FILE_CONFIG_NAME), "mse");
                if (!File.Exists(tmp))
                {
                    return;//Return when even the default config is missing
                }
            }
            SetConfig(tmp, path);
        }
        /// <summary>
        /// Whether to resize images
        /// </summary>
        public bool reimage;
        /// <summary>
        /// Middle image width
        /// </summary>
        public int width;
        /// <summary>
        /// Middle image height
        /// </summary>
        public int height;

        public int pwidth;
        public int pheight;

        //Cards without stars
        public long[] noStartCards;
        //Series 10 layout
        public bool no10;
        //Maximum cards per save file
        public int maxcount;
        //Image path
        public string imagepath;
        /// <summary>
        /// Image cache path
        /// </summary>
        public string imagecache;
        //Spell symbol
        public string str_spell;
        //Trap symbol
        public string str_trap;
        //Effect text format
        public string temp_text;
        // Simplified-to-Traditional conversion flag.
        public bool Iscn2tw;
        //Special character replacements
        public SortedList<string, string> replaces;
        //Effect text extraction regex
        public string regx_pendulum;
        public string regx_monster;
        public string regx_rush;
        public static string RegXPendulum { get; set; }
        public static string RegXMonster { get; set; }
        public static string RegXRush { get; set; }
        //Save file header
        public string head;
        //Save file footer
        public string end;
        public SortedList<long, string> typeDic;
        public SortedList<long, string> raceDic;
    }
}
