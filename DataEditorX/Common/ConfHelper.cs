using System.Globalization;
using System.Text;

namespace DataEditorX.Common
{
    public class ConfHelper
    {
        /// <summary>
        /// Content delimiter
        /// </summary>
        public const string SEP_LINE = " ";
        /// <summary>
        /// Read value from a line
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public static string GetValue(string line)
        {
            int t = line.IndexOf('=');
            if (t > 0)
            {
                return line[(t + 1)..].Trim();
            }

            return "";
        }
        /// <summary>
        /// Read the first value from tokens
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public static string GetValue1(string word)
        {
            int i = word.IndexOf(SEP_LINE);
            if (i > 0)
            {
                return word[..i];
            }

            return word;
        }
        /// <summary>
        /// Read the second value from tokens
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public static string GetValue2(string word)
        {
            int i = word.IndexOf(SEP_LINE);
            if (i > 0)
            {
                return word[(i + SEP_LINE.Length)..];
            }

            return "";
        }
        /// <summary>
        /// Read multiline value and unescape \n / \t
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public static string GetMultLineValue(string line)
        {
            return GetRegex(GetValue(line));
        }
        /// <summary>
        /// Replace escape sequences
        /// </summary>
        /// <param name="word"></param>
        /// <returns></returns>
        public static string GetRegex(string word)
        {
            StringBuilder sb = new(word);
            _ = sb.Replace("\\r", "\r");
            _ = sb.Replace("\\n", "\n");
            _ = sb.Replace("\\t", "\t");
            _ = sb.Replace("[:space:]", " ");
            return sb.ToString();
        }
        /// <summary>
        /// Read boolean value
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public static bool GetBooleanValue(string line)
        {
            if (GetValue(line).ToLower() == "true")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Read integer value
        /// </summary>
        /// <param name="line"></param>
        /// <param name="defalut">Fallback value</param>
        /// <returns></returns>
        public static int GetIntegerValue(string line, int defalut)
        {
            int i;
            try
            {
                i = int.Parse(GetValue(line));
                return i;
            }
            catch { }
            return defalut;
        }
        /// <summary>
        /// Read content from a line and add it to the dictionary
        /// race 0x1 xxx
        /// </summary>
        /// <param name="dic"></param>
        /// <param name="line"></param>
        public static void DicAdd(SortedList<long, string> dic, string line)
        {
            int i = line.IndexOf("0x");
            int j = (i > 0) ? line.IndexOf(SEP_LINE, i + 1) : -1;
            if (j > 0)
            {
                string strkey = line.Substring(i + 2, j - i - 1);
                string strval = line[(j + 1)..];
                _ = long.TryParse(strkey, NumberStyles.HexNumber, null, out long key);
                if (!dic.ContainsKey(key))
                {
                    dic.Add(key, strval.Trim());
                }
            }
        }
    }
}
