/*
 * Created with SharpDevelop.
 * User: Acer
 * Date: May 18, Sunday
 * Time: 18:08
 * 
 */
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DataEditorX.Config
{
    public class DataManager
    {
        /// <summary>
        /// Content start tag
        /// </summary>
        public const string TAG_START = "##";
        /// <summary>
        /// Content end tag
        /// </summary>
        public const string TAG_END = "#";
        /// <summary>
        /// Line separator
        /// </summary>
        public const char SEP_LINE = '\t';

        #region Read content by tag
        static string reReturn(string content)
        {
            string text = content.Replace("\r\n", "\n");
            text = text.Replace("\r", "\n");
            return text;
        }
        public static string SubString(string content, string tag)
        {
            Regex reg = new(string.Format(@"{0}{1}\n([\S\s]*?)\n{2}", TAG_START, tag, TAG_END), RegexOptions.Multiline);
            Match mac = reg.Match(reReturn(content));
            if (mac.Success)//Extract matching content
            {
                return mac.Groups[1].Value.Replace("\n", Environment.NewLine);
            }
            return "";
        }
        #endregion

        #region Read
        /// <summary>
        /// Split string content by tag and read it
        /// </summary>
        /// <param name="content">String</param>
        /// <param name="tag">Start tag</param>
        /// <returns></returns>
        public static Dictionary<long, string> Read(string content, string tag)
        {
            return Read(SubString(content, tag));
        }
        /// <summary>
        /// Read content from a file line by line
        /// </summary>
        /// <param name="strFile"></param>
        /// <param name="encode"></param>
        /// <returns></returns>
        public static Dictionary<long, string> Read(string strFile, Encoding encode)
        {
            return Read(File.ReadAllLines(strFile, encode));
        }
        /// <summary>
        /// Read line-based content from a string
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static Dictionary<long, string> Read(string content)
        {
            string text = reReturn(content);
            text = text.Replace("\r", "\n");
            text = text.Replace("\n\n", "\n"); //Linux and macOS compatibility, 2019-03-24 by JoyJ
            return Read(text.Split('\n'));
        }
        /// <summary>
        /// Read content from a line
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        public static Dictionary<long, string> Read(string[] lines)
        {
            Dictionary<long, string> tempDic = new();
            long lkey;
            foreach (string line in lines)
            {
                if (line.StartsWith("#"))
                {
                    continue;
                }

                string[] words = line.Split(SEP_LINE);
                if (line.StartsWith("!setname ")) words = line.Split(" ")[1..];
                if (words.Length < 2)
                {
                    continue;
                }

                if (words[0].StartsWith("0x"))
                {
                    _ = long.TryParse(words[0].Replace("0x", ""), NumberStyles.HexNumber, null, out lkey);
                }
                else
                {
                    _ = long.TryParse(words[0], out lkey);
                }
                // Hide N/A data
                if (!tempDic.ContainsKey(lkey) && words[1] != "N/A")
                {
                    tempDic.Add(lkey, string.Join(' ', words[1..]));
                }
            }
            return tempDic;
        }

        #endregion

        #region Find
        public static List<long> GetKeys(Dictionary<long, string> dic)
        {
            List<long> list = new();
            foreach (long l in dic.Keys)
            {
                list.Add(l);
            }
            return list;
        }
        public static string[] GetValues(Dictionary<long, string> dic)
        {
            List<string> list = new();
            foreach (long l in dic.Keys)
            {
                list.Add(dic[l]);
            }
            return list.ToArray();
        }
        /// <summary>
        /// Get value
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetValue(Dictionary<long, string> dic, long key)
        {
            if (dic.ContainsKey(key))
            {
                return dic[key].Trim();
            }

            return key.ToString("x");
        }
        #endregion
    }
}
