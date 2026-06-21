/*
 * Created with SharpDevelop.
 * User: Acer
 * Date: 2015-5-24
 * Time: 10:55
 * 
 * Legacy SharpDevelop template note.
 */
using System.Diagnostics;
using System.Text;

namespace DataEditorX.Common
{
    /// <summary>
    /// Description of MyUtils.
    /// </summary>
    public class MyUtils
    {
        /// <summary>
        /// Calculate file MD5
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetMD5HashFromFile(string fileName)
        {
            try
            {
                FileStream file = new(fileName, FileMode.Open);
                System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
                byte[] retVal = md5.ComputeHash(file);
                file.Close();

                StringBuilder sb = new();
                for (int i = 0; i < retVal.Length; i++)
                {
                    _ = sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch
            {

            }
            return "";
        }

        public static bool Md5isEmpty(string md5)
        {
            return md5 == null || md5.Length < 16;
        }

        public static bool OpenShellTarget(string target)
        {
            if (string.IsNullOrWhiteSpace(target))
            {
                return false;
            }

            try
            {
                ProcessStartInfo info = new()
                {
                    FileName = target,
                    UseShellExecute = true
                };
                if (File.Exists(target))
                {
                    info.WorkingDirectory = Path.GetDirectoryName(target);
                }

                return Process.Start(info) != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
