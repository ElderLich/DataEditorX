/*
 * Created with SharpDevelop.
 * User: Acer
 * Date: 2014-10-26
 * Time: 10:26
 * 
 */
using System.Text;

namespace System.IO
{
    /// <summary>
    /// Path helpers
    /// </summary>
    public class MyPath
    {
        /// <summary>
        /// Resolve a relative path
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static string GetRealPath(string dir)
        {
            string path = Application.StartupPath;
            if (dir.StartsWith("."))
            {
                dir = Combine(path, dir[2..]);
            }
            return dir;
        }
        /// <summary>
        /// Combine paths
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        public static string Combine(params string[] paths)
        {
            if (paths.Length == 0)
            {
                throw new ArgumentException("please input path");
            }
            else
            {
                StringBuilder builder = new();
                string spliter = Path.DirectorySeparatorChar.ToString();
                string firstPath = paths[0];
                if (firstPath.StartsWith("HTTP", StringComparison.OrdinalIgnoreCase))
                {
                    spliter = "/";
                }
                if (!firstPath.EndsWith(spliter))
                {
                    firstPath += spliter;
                }
                _ = builder.Append(firstPath);
                for (int i = 1; i < paths.Length; i++)
                {
                    string nextPath = paths[i];
                    if (nextPath.StartsWith("/") || nextPath.StartsWith("\\"))
                    {
                        nextPath = nextPath[1..];
                    }
                    if (i != paths.Length - 1)//not the last one
                    {
                        if (nextPath.EndsWith("/") || nextPath.EndsWith("\\"))
                        {
                            nextPath = nextPath[..^1] + spliter;
                        }
                        else
                        {
                            nextPath += spliter;
                        }
                    }
                    _ = builder.Append(nextPath);
                }
                return builder.ToString();
            }
        }
        /// <summary>
        /// Validate directory
        /// </summary>
        /// <param name="dir">Directory</param>
        /// <param name="fallbackPath">Fallback directory when invalid</param>
        /// <returns></returns>
        public static string CheckDir(string dir, string fallbackPath)
        {
            try
            {
                DirectoryInfo info = Directory.CreateDirectory(GetRealPath(dir));
                return info.FullName;
            }
            catch
            {
                DirectoryInfo fallbackInfo = Directory.CreateDirectory(fallbackPath);
                return fallbackInfo.FullName;
            }
        }
        /// <summary>
        /// Get file name from tag
        /// tag_lang.txt
        /// </summary>
        /// <param name="tag">Prefix</param>
        /// <param name="lang"></param>
        /// <returns></returns>
        public static string GetFileName(string tag, string lang)
        {
            return tag + "_" + lang + ".txt";
        }
        /// <summary>
        /// Get file name from tag and language
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public static string GetFullFileName(string tag, string file)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            if (!name.StartsWith(tag + "_"))
            {
                return "";
            }
            else
            {
                return name.Replace(tag + "_", "");
            }
        }

        public static string FindFile(string dir, string fileName, params string[] preferredSubdirs)
        {
            if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(fileName))
            {
                return fileName;
            }

            foreach (string subdir in preferredSubdirs)
            {
                if (string.IsNullOrEmpty(subdir))
                {
                    continue;
                }

                string file = Combine(dir, subdir, fileName);
                if (File.Exists(file))
                {
                    return file;
                }
            }

            return preferredSubdirs.Length > 0 && !string.IsNullOrEmpty(preferredSubdirs[0])
                ? Combine(dir, preferredSubdirs[0], fileName)
                : Combine(dir, fileName);
        }

        public static string[] FindFiles(string dir, string searchPattern, params string[] preferredSubdirs)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
            {
                return [];
            }

            List<string> files = new();
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

            void AddFiles(string path, SearchOption option)
            {
                if (!Directory.Exists(path))
                {
                    return;
                }

                foreach (string file in Directory.GetFiles(path, searchPattern, option))
                {
                    if (seen.Add(Path.GetFileName(file)))
                    {
                        files.Add(file);
                    }
                }
            }

            if (preferredSubdirs.Length == 0)
            {
                AddFiles(dir, SearchOption.TopDirectoryOnly);
            }
            else
            {
                foreach (string subdir in preferredSubdirs)
                {
                    if (!string.IsNullOrEmpty(subdir))
                    {
                        AddFiles(Combine(dir, subdir), SearchOption.TopDirectoryOnly);
                    }
                }
            }

            return files
                .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(file => file, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public static void CreateDir(string dir)
        {
            if (!Directory.Exists(dir))
            {
                _ = Directory.CreateDirectory(dir);
            }
        }
        public static void CreateDirByFile(string file)
        {
            string dir = Path.GetDirectoryName(file);
            if (!Directory.Exists(dir))
            {
                _ = Directory.CreateDirectory(dir);
            }
        }
    }
}
