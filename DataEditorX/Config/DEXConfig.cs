using DataEditorX.Common;
using System.Diagnostics;
using System.Globalization;

namespace DataEditorX.Config
{
    /// <summary>
    /// Configuration
    /// </summary>
    public class DEXConfig : XMLReader
    {
        #region Constants
        public const string TAG_SAVE_LAGN = "-savelanguage";
        public const string TAG_SAVE_LAGN2 = "-sl";
        /// <summary>
        /// Window message for opening files
        /// </summary>
        public const int WM_OPEN = 0x0401;
        /// <summary>
        /// Maximum history entries
        /// </summary>
        public const int MAX_HISTORY = 0x10;
        /// <summary>
        /// Data directory
        /// </summary>
        public const string TAG_DATA = "data";
        /// <summary>
        /// Pending open file
        /// </summary>
        //public const string TAG_OPEN = "open";
        /// <summary>
        /// <summary>
        /// Card info
        /// </summary>
        public const string TAG_CARDINFO = "cardinfo";
        /// <summary>
        /// Language
        /// </summary>
        public const string TAG_LANGUAGE = "language";
        /// <summary>
        /// Temporary file
        /// </summary>
        public const string FILE_TEMP = "open.tmp";
        /// <summary>
        /// History file
        /// </summary>
        public const string FILE_HISTORY = "history.txt";
        /// <summary>
        /// Functions
        /// </summary>
        public const string FILE_FUNCTION = "_functions.txt";
        public const string FILE_FUNCTION_ENGLISH = "_function_english.txt";
        /// <summary>
        /// Constants
        /// </summary>
        public const string FILE_CONSTANT = "constant.lua";
        /// <summary>
        /// Counters and victory messages
        /// </summary>
        public const string FILE_STRINGS = "strings.conf";
        /// <summary>
        /// Source URL
        /// </summary>
        public const string TAG_SOURCE_URL = "sourceURL";
        /// <summary>
        /// Update URL
        /// </summary>
        public const string TAG_UPDATE_URL = "updateURL";
        /// <summary>
        /// Delete image and script files when deleting a card
        /// </summary>
        public const string TAG_DELETE_WITH = "opera_with_cards_file";
        /// <summary>
        /// Load data asynchronously
        /// </summary>
        public const string TAG_ASYNC = "async";
        /// <summary>
        /// Open files in this program
        /// </summary>
        public const string TAG_OPEN_IN_THIS = "open_file_in_this";
        /// <summary>
        /// Load auto-update setting
        /// </summary>
        public const string TAG_AUTO_CHECK_UPDATE = "auto_check_update";
        /// <summary>
        /// Dark theme
        /// </summary>
        public const string TAG_DARK_THEME = "dark_theme";
        /// <summary>
        /// Selected theme profile
        /// </summary>
        public const string TAG_THEME_PROFILE = "theme_profile";
        /// <summary>
        /// Custom theme palette
        /// </summary>
        public const string TAG_THEME_CUSTOM_PALETTE = "theme_custom_palette";
        public const string TAG_PROJECT_MANAGER_MDPRO3_DIR = "project_manager_mdpro3_dir";
        public const string TAG_PROJECT_MANAGER_MDPRO3_DATA_DIR = "project_manager_mdpro3_data_dir";
        public const string TAG_PROJECT_MANAGER_CUSTOM_PROJECT_DIR = "project_manager_custom_project_dir";
        public const string TAG_PROJECT_MANAGER_VOICE_PACK_DIR = "project_manager_voice_pack_dir";
        /// <summary>
        /// Default script name
        /// </summary>
        public const string TAG_DEFAULT_SCRIPT_NAME = "default_script_name";
        /// <summary>
        /// Check system language
        /// </summary>
        public const string TAG_CHECK_SYSLANG = "check_system_language";
        /// Image dimensions: thumbnail w/h and full-size W/H
        /// </summary>
        public const string TAG_IMAGE_SIZE = "image";
        /// <summary>
        /// ImageQuality
        /// </summary>
        public const string TAG_IMAGE_QUALITY = "image_quality";
        //CodeEditor
        /// <summary>
        /// Font name
        /// </summary>
        public const string TAG_FONT_NAME = "fontname";
        /// <summary>
        /// Selected editor
        /// </summary>
        public const string USE_EDITOR = "editor";
        /// <summary>
        /// Font size
        /// </summary>
        public const string TAG_FONT_SIZE = "fontsize";
        /// <summary>
        /// Enable Chinese IME
        /// </summary>
        public const string TAG_IME = "IME";
        /// <summary>
        /// Word wrap
        /// </summary>
        public const string TAG_WORDWRAP = "wordwrap";
        /// <summary>
        /// Replace tabs with spaces
        /// </summary>
        public const string TAG_TAB2SPACES = "tabisspace";
        /// <summary>
        /// </summary>
        public const string TAG_SAVE2DB = "save_to_db";
        /// <summary>
        /// Rule
        /// </summary>
        public const string TAG_RULE = "rule";
        /// <summary>
        /// Race
        /// </summary>
        public const string TAG_RACE = "race";
        /// <summary>
        /// Attribute
        /// </summary>
        public const string TAG_ATTRIBUTE = "attribute";
        /// <summary>
        /// Level
        /// </summary>
        public const string TAG_LEVEL = "level";
        /// <summary>
        /// Effect categories
        /// </summary>
        public const string TAG_CATEGORY = "category \\(genre\\)";
        /// <summary>
        /// Omega-exclusive
        /// </summary>
        public const string TAG_FLAGS = "flags \\(category\\)";
        /// <summary>
        /// Types
        /// </summary>
        public const string TAG_TYPE = "type";
        /// <summary>
        /// Archetype/setcode names
        /// </summary>
        public const string TAG_SETNAME = "setname";
        /// <summary>
        /// Link markers
        /// </summary>
        public const string TAG_MARKER = "link marker";
        /// <summary>
        /// Temporary file
        /// </summary>
        public const string TOOLTIP_FONT = "tooltip_font";
        #endregion

        #region Read config content
        /// <summary>
        /// Read string value
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string ReadString(string key)
        {
            return GetAppConfig(key);
        }
        /// <summary>
        /// Read integer value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="def"></param>
        /// <returns></returns>
        public static int ReadInteger(string key, int def)
        {
            if (int.TryParse(ReadString(key), out int i))
            {
                return i;
            }

            return def;
        }
        /// <summary>
        /// Read float value
        /// </summary>
        /// <param name="key"></param>
        /// <param name="def"></param>
        /// <returns></returns>
        public static float ReadFloat(string key, float def)
        {
            if (float.TryParse(ReadString(key), out float i))
            {
                return i;
            }

            return def;
        }
        /// <summary>
        /// Read integer array
        /// </summary>
        /// <param name="key"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static int[] ReadIntegers(string key, int length)
        {
            string temp = ReadString(key);
            int[] ints = new int[length];
            string[] ws = string.IsNullOrEmpty(temp) ? null : temp.Split(',');

            if (ws != null && ws.Length > 0 && ws.Length <= length)
            {
                for (int i = 0; i < ws.Length; i++)
                {
                    _ = int.TryParse(ws[i], out ints[i]);
                }
            }
            return ints;
        }
        /// <summary>
        /// Read rectangle area
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static Area ReadArea(string key)
        {
            int[] ints = ReadIntegers(key, 4);
            Area a = new();
            if (ints != null)
            {
                a.left = ints[0];
                a.top = ints[1];
                a.width = ints[2];
                a.height = ints[3];
            }
            return a;
        }
        /// <summary>
        /// Read boolean value
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool ReadBoolean(string key, bool def = false)
        {
            string val = ReadString(key);
            if ("true".Equals(val, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            if ("false".Equals(val, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return def;
        }
        #endregion


        /// <summary>
        /// Language configuration file name
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetLanguageFile(string path)
        {
            if (ReadBoolean(TAG_CHECK_SYSLANG) && Directory.Exists(path))
            {
                Save(TAG_CHECK_SYSLANG, "false");
                string[] words = CultureInfo.InstalledUICulture.EnglishName.Split(' ');
                string syslang = words[0];
                foreach (string name in GetAvailableLanguageNames(path))
                {
                    if (syslang.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        Save(TAG_LANGUAGE, syslang);
                        break;
                    }
                }
            }
            string languageFile = MyPath.FindFile(path, MyPath.GetFileName(TAG_LANGUAGE, GetAppConfig(TAG_LANGUAGE)), "languages");
            if (File.Exists(languageFile))
            {
                return languageFile;
            }

            string englishFile = MyPath.FindFile(path, MyPath.GetFileName(TAG_LANGUAGE, "english"), "languages");
            return File.Exists(englishFile) ? englishFile : languageFile;
        }
        /// <summary>
        /// Card info configuration file name
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetCardInfoFile(string path)
        {
            string cardInfoFile = MyPath.FindFile(path, MyPath.GetFileName(TAG_CARDINFO, GetAppConfig(TAG_LANGUAGE)), "languages");
            if (File.Exists(cardInfoFile))
            {
                return cardInfoFile;
            }

            string englishFile = MyPath.FindFile(path, MyPath.GetFileName(TAG_CARDINFO, "english"), "languages");
            return File.Exists(englishFile) ? englishFile : cardInfoFile;
        }

        public static string GetFunctionFile(string path)
        {
            string fileName = IsChineseLanguage(GetAppConfig(TAG_LANGUAGE))
                ? FILE_FUNCTION
                : FILE_FUNCTION_ENGLISH;

            string functionFile = MyPath.FindFile(path, fileName, "lua");
            if (File.Exists(functionFile))
            {
                return functionFile;
            }

            string fallbackFile = MyPath.FindFile(
                path,
                fileName.Equals(FILE_FUNCTION, StringComparison.OrdinalIgnoreCase)
                    ? FILE_FUNCTION_ENGLISH
                    : FILE_FUNCTION,
                "lua");
            return File.Exists(fallbackFile) ? fallbackFile : functionFile;
        }

        static bool IsChineseLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                return false;
            }

            string normalized = language.Replace('_', '-');
            return normalized.Contains("chinese", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("zh", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("zh-cn", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("zh-tw", StringComparison.OrdinalIgnoreCase);
        }

        public static string[] GetAvailableLanguageNames(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                return [];
            }

            SortedSet<string> names = new(StringComparer.OrdinalIgnoreCase);
            foreach (string file in MyPath.FindFiles(path, MyPath.GetFileName(TAG_LANGUAGE, "*"), "languages"))
            {
                string name = MyPath.GetFullFileName(TAG_LANGUAGE, file);
                if (!string.IsNullOrEmpty(name))
                {
                    _ = names.Add(name);
                }
            }

            foreach (string file in MyPath.FindFiles(path, MyPath.GetFileName(TAG_CARDINFO, "*"), "languages"))
            {
                string name = MyPath.GetFullFileName(TAG_CARDINFO, file);
                if (!string.IsNullOrEmpty(name))
                {
                    _ = names.Add(name);
                }
            }

            return names.ToArray();
        }
        /// <summary>
        /// Send open-file message
        /// </summary>
        /// <param name="file"></param>
        public static bool OpenOnExistForm(string file)
        {
            Process instance = RunningInstance(Application.ExecutablePath.
                        Replace('/', Path.DirectorySeparatorChar));
            if (instance == null)
            {
                return false;
            }
            else
            {
                //Write the pending file path to the temp file
                string tmpfile = Path.Combine(Application.StartupPath, FILE_TEMP);
                File.WriteAllText(tmpfile, file);
                //Send message
                _ = User32.SendMessage(instance.MainWindowHandle, WM_OPEN, 0, 0);
                return true;
            }
        }
        public static void OpenFileInThis(string file)
        {
            //Write the pending file path to the temp file
            string tmpfile = Path.Combine(Application.StartupPath, FILE_TEMP);
            File.WriteAllText(tmpfile, file);
            //Send message
            _ = User32.SendMessage(Process.GetCurrentProcess().MainWindowHandle, WM_OPEN, 0, 0);
            File.Delete(tmpfile);
        }
        public static Process RunningInstance(string filename)
        {
            Process current = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(current.ProcessName);
            //Scan processes with the same process name
            foreach (Process process in processes)
            {
                //Ignore the current process when another instance exists
                if (process.Id != current.Id)
                {
                    //Ensure both processes come from the same file path
                    if (filename == current.MainModule.FileName)
                    {
                        //Return the existing process
                        return process;
                    }
                }
            }
            return null;
        }
    }

}
