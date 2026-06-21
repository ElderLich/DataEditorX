/*
 * Created with SharpDevelop.
 * User: Acer
 * Date: 2014-10-23
 * Time: 7:54
 * 
 */

namespace DataEditorX.Config
{
    /// <summary>
    /// DataEditor data
    /// </summary>
    public class DataConfig
    {
        public DataConfig()
        {
            InitMember(MyPath.Combine(Application.StartupPath, DEXConfig.TAG_CARDINFO + ".txt"));
        }
        public DataConfig(string conf)
        {
            InitMember(conf);
        }
        /// <summary>
        /// Initialize members
        /// </summary>
        /// <param name="conf"></param>
        public void InitMember(string conf)
        {
            //conf = MyPath.Combine(datapath, MyConfig.FILE_INFO);
            if (!File.Exists(conf))
            {
                dicCardRules = new Dictionary<long, string>();
                dicSetnames = new Dictionary<long, string>();
                dicCardTypes = new Dictionary<long, string>();
                dicLinkMarkers = new Dictionary<long, string>();
                dicCardcategorys = new Dictionary<long, string>();
                dicCardflags = new Dictionary<long, string>();
                dicCardAttributes = new Dictionary<long, string>();
                dicCardRaces = new Dictionary<long, string>();
                dicCardLevels = new Dictionary<long, string>();
                return;
            }
            //Extract content
            string text = File.ReadAllText(conf);
            dicCardRules = DataManager.Read(text, DEXConfig.TAG_RULE);
            dicSetnames = DataManager.Read(text, DEXConfig.TAG_SETNAME);
            dicCardTypes = DataManager.Read(text, DEXConfig.TAG_TYPE);
            dicLinkMarkers = DataManager.Read(text, DEXConfig.TAG_MARKER);
            dicCardcategorys = DataManager.Read(text, DEXConfig.TAG_CATEGORY);
            dicCardflags = DataManager.Read(text, DEXConfig.TAG_FLAGS);
            dicCardAttributes = DataManager.Read(text, DEXConfig.TAG_ATTRIBUTE);
            dicCardRaces = DataManager.Read(text, DEXConfig.TAG_RACE);
            dicCardLevels = DataManager.Read(text, DEXConfig.TAG_LEVEL);

        }
        /// <summary>
        /// Rule
        /// </summary>
        public Dictionary<long, string> dicCardRules = null;
        /// <summary>
        /// Attribute
        /// </summary>
        public Dictionary<long, string> dicCardAttributes = null;
        /// <summary>
        /// Race
        /// </summary>
        public Dictionary<long, string> dicCardRaces = null;
        /// <summary>
        /// Level
        /// </summary>
        public Dictionary<long, string> dicCardLevels = null;
        /// <summary>
        /// Archetype/setcode names
        /// </summary>
        public Dictionary<long, string> dicSetnames = null;
        /// <summary>
        /// Card types
        /// </summary>
        public Dictionary<long, string> dicCardTypes = null;
        /// <summary>
        /// Link markers
        /// </summary>
        public Dictionary<long, string> dicLinkMarkers = null;
        /// <summary>
        /// Effect categories
        /// </summary>
        public Dictionary<long, string> dicCardcategorys = null;
        /// <summary>
        /// Omega-exclusive
        /// </summary>
        public Dictionary<long, string> dicCardflags = null;
    }
}
