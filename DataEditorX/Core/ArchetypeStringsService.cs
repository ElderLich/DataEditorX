using DataEditorX.Config;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DataEditorX.Core
{
    internal sealed record ArchetypeWriteResult(string CustomStringsFile, string MdPro3StringsFile, bool SyncedToMdPro3);

    internal static class ArchetypeStringsService
    {
        private static readonly Regex SetnamePattern = new(
            @"^\s*([#!]?)setname\s+(0x[0-9a-fA-F]+|[0-9a-fA-F]+)\s+(.+?)\s*$",
            RegexOptions.Compiled);

        public static bool TryParseSetcode(string text, out long setcode)
        {
            setcode = 0;
            text = (text ?? string.Empty).Trim();
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                text = text[2..];
            }

            return text.Length > 0
                && long.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out setcode)
                && setcode > 0;
        }

        public static string FormatSetcode(long setcode)
        {
            return "0x" + setcode.ToString("x", CultureInfo.InvariantCulture);
        }

        public static Dictionary<long, string> BuildKnownSetnames(Dictionary<long, string> baseSetnames, string dataPath)
        {
            Dictionary<long, string> setnames = new();
            if (baseSetnames != null)
            {
                foreach ((long key, string value) in baseSetnames)
                {
                    TryAdd(setnames, key, value);
                }
            }

            MergeSetnames(setnames, MyPath.FindFile(dataPath, DEXConfig.FILE_STRINGS, "lua"), includeCommented: true);
            MergeSetnames(setnames, GetCustomStringsFile(), includeCommented: true);
            MergeSetnames(setnames, GetMdPro3StringsFile(), includeCommented: true);
            return setnames;
        }

        public static void MergeSetnames(Dictionary<long, string> setnames, string stringsFile, bool includeCommented = false)
        {
            if (setnames == null || string.IsNullOrWhiteSpace(stringsFile) || !File.Exists(stringsFile))
            {
                return;
            }

            foreach ((long setcode, string name) in ReadSetnames(stringsFile, includeCommented))
            {
                TryAdd(setnames, setcode, name);
            }
        }

        public static ArchetypeWriteResult AddArchetype(long setcode, string name, Dictionary<long, string> knownSetnames)
        {
            name = (name ?? string.Empty).Trim();
            if (setcode <= 0)
            {
                throw new InvalidOperationException("Setcode ID must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Archetype name cannot be empty.");
            }

            Dictionary<long, string> allSetnames = new();
            if (knownSetnames != null)
            {
                foreach ((long key, string value) in knownSetnames)
                {
                    TryAdd(allSetnames, key, value);
                }
            }

            string customStringsFile = GetCustomStringsFile();
            if (string.IsNullOrWhiteSpace(customStringsFile))
            {
                throw new InvalidOperationException("Custom Project Directory is not configured in Project Manager.");
            }

            MergeSetnames(allSetnames, customStringsFile, includeCommented: true);
            string mdPro3StringsFile = GetMdPro3StringsFile();
            MergeSetnames(allSetnames, mdPro3StringsFile, includeCommented: true);
            if (allSetnames.TryGetValue(setcode, out string existingName))
            {
                throw new InvalidOperationException($"{FormatSetcode(setcode)} is already used by {existingName}.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(customStringsFile));
            AppendSetname(customStringsFile, setcode, name);

            bool synced = false;
            if (!string.IsNullOrWhiteSpace(mdPro3StringsFile))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(mdPro3StringsFile));
                File.Copy(customStringsFile, mdPro3StringsFile, overwrite: true);
                synced = true;
            }

            return new ArchetypeWriteResult(customStringsFile, mdPro3StringsFile, synced);
        }

        public static string GetCustomStringsFile()
        {
            string customProjectDirectory = DEXConfig.ReadString(DEXConfig.TAG_PROJECT_MANAGER_CUSTOM_PROJECT_DIR);
            return GetStringsFile(customProjectDirectory);
        }

        public static string GetMdPro3StringsFile()
        {
            string mdPro3Directory = DEXConfig.ReadString(DEXConfig.TAG_PROJECT_MANAGER_MDPRO3_DIR);
            return GetStringsFile(mdPro3Directory);
        }

        private static string GetStringsFile(string projectDirectory)
        {
            if (string.IsNullOrWhiteSpace(projectDirectory))
            {
                return string.Empty;
            }

            return Path.Combine(projectDirectory, "Expansions", DEXConfig.FILE_STRINGS);
        }

        private static List<(long Setcode, string Name)> ReadSetnames(string stringsFile, bool includeCommented)
        {
            List<(long Setcode, string Name)> setnames = new();
            foreach (string line in File.ReadLines(stringsFile))
            {
                Match match = SetnamePattern.Match(line);
                if (!match.Success)
                {
                    continue;
                }

                string prefix = match.Groups[1].Value;
                if (prefix == "#" && !includeCommented)
                {
                    continue;
                }

                if (!TryParseSetcode(match.Groups[2].Value, out long setcode))
                {
                    continue;
                }

                setnames.Add((setcode, match.Groups[3].Value.Trim()));
            }

            return setnames;
        }

        private static void AppendSetname(string stringsFile, long setcode, string name)
        {
            string line = $"!setname {FormatSetcode(setcode)} {name}";
            string prefix = string.Empty;
            if (File.Exists(stringsFile) && new FileInfo(stringsFile).Length > 0)
            {
                string text = File.ReadAllText(stringsFile);
                if (!text.EndsWith('\n') && !text.EndsWith('\r'))
                {
                    prefix = Environment.NewLine;
                }
            }

            File.AppendAllText(stringsFile, prefix + line + Environment.NewLine, new UTF8Encoding(false));
        }

        private static void TryAdd(Dictionary<long, string> setnames, long setcode, string name)
        {
            if (!setnames.ContainsKey(setcode) && !string.IsNullOrWhiteSpace(name))
            {
                setnames.Add(setcode, name.Trim());
            }
        }
    }
}
