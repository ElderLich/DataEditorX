/*
 * 由SharpDevelop创建。
 * 用户： Acer
 * 日期: 2014-10-26
 * 时间: 10:26
 * 
 */
using System.Text;

namespace System.IO
{
    /// <summary>
    /// 路径处理
    /// </summary>
    public class MyPath
    {
        /// <summary>
        /// 从相对路径获取真实路径
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
        /// 合并路径
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
        /// 检查目录是否合法
        /// </summary>
        /// <param name="dir">目录</param>
        /// <param name="defalut">不合法时，采取的目录</param>
        /// <returns></returns>
        public static string CheckDir(string dir, string defalut)
        {
            DirectoryInfo fo;
            try
            {
                fo = new DirectoryInfo(GetRealPath(dir));
            }
            catch
            {
                //路径不合法
                fo = new DirectoryInfo(defalut);
            }
            if (!fo.Exists)
            {
                fo.Create();
            }

            dir = fo.FullName;
            return dir;
        }
        /// <summary>
        /// 根据tag获取文件名
        /// tag_lang.txt
        /// </summary>
        /// <param name="tag">前面</param>
        /// <param name="lang"></param>
        /// <returns></returns>
        public static string GetFileName(string tag, string lang)
        {
            return tag + "_" + lang + ".txt";
        }
        /// <summary>
        /// 由tag和lang获取文件名
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

            string rootFile = Combine(dir, fileName);
            if (File.Exists(rootFile))
            {
                return rootFile;
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

            if (Directory.Exists(dir))
            {
                try
                {
                    string found = Directory.GetFiles(dir, fileName, SearchOption.AllDirectories).FirstOrDefault();
                    if (!string.IsNullOrEmpty(found))
                    {
                        return found;
                    }
                }
                catch
                {
                }
            }

            return preferredSubdirs.Length > 0 && !string.IsNullOrEmpty(preferredSubdirs[0])
                ? Combine(dir, preferredSubdirs[0], fileName)
                : rootFile;
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

            AddFiles(dir, SearchOption.TopDirectoryOnly);
            foreach (string subdir in preferredSubdirs)
            {
                if (!string.IsNullOrEmpty(subdir))
                {
                    AddFiles(Combine(dir, subdir), SearchOption.TopDirectoryOnly);
                }
            }
            AddFiles(dir, SearchOption.AllDirectories);

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
