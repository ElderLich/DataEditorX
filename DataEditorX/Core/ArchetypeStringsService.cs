using DataEditorX.Config;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DataEditorX.Core
{
    internal sealed record ArchetypeWriteResult(string CustomStringsFile, string MdPro3StringsFile, bool SyncedToMdPro3);
    internal sealed record CounterWriteResult(string CustomStringsFile, string MdPro3StringsFile, bool SyncedToMdPro3);
    internal sealed record StringsEntryWriteResult(string CustomStringsFile, string MdPro3StringsFile, bool SyncedToMdPro3);

    internal static class ArchetypeStringsService
    {
        private static readonly Regex SetnamePattern = new(
            @"^\s*([#!]?)setname\s+(0x[0-9a-fA-F]+|[0-9a-fA-F]+)\s+(.+?)\s*$",
            RegexOptions.Compiled);

        private static readonly Regex CounterPattern = new(
            @"^\s*([#!]?)counter\s+(0x[0-9a-fA-F]+|[0-9a-fA-F]+)\s+(.+?)\s*$",
            RegexOptions.Compiled);

        public static bool TryParseSetcode(string text, out long setcode)
        {
            return TryParseHexId(text, out setcode);
        }

        public static bool TryParseHexId(string text, out long value)
        {
            value = 0;
            text = (text ?? string.Empty).Trim();
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                text = text[2..];
            }

            return text.Length > 0
                && long.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value)
                && value > 0;
        }

        public static string FormatSetcode(long setcode)
        {
            return FormatHexId(setcode);
        }

        public static string FormatHexId(long value)
        {
            return "0x" + value.ToString("x", CultureInfo.InvariantCulture);
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

        public static Dictionary<long, string> BuildKnownCounters(string dataPath)
        {
            Dictionary<long, string> counters = new();
            MergeCounters(counters, MyPath.FindFile(dataPath, DEXConfig.FILE_STRINGS, "lua"), includeCommented: true);
            MergeCounters(counters, GetCustomStringsFile(), includeCommented: true);
            MergeCounters(counters, GetMdPro3StringsFile(), includeCommented: true);
            return counters;
        }

        public static void MergeSetnames(Dictionary<long, string> setnames, string stringsFile, bool includeCommented = false)
        {
            if (setnames == null || string.IsNullOrWhiteSpace(stringsFile) || !File.Exists(stringsFile))
            {
                return;
            }

            foreach ((long setcode, string name) in ReadEntries(stringsFile, SetnamePattern, includeCommented))
            {
                TryAdd(setnames, setcode, name);
            }
        }

        public static void MergeCounters(Dictionary<long, string> counters, string stringsFile, bool includeCommented = false)
        {
            if (counters == null || string.IsNullOrWhiteSpace(stringsFile) || !File.Exists(stringsFile))
            {
                return;
            }

            foreach ((long counterId, string name) in ReadEntries(stringsFile, CounterPattern, includeCommented))
            {
                TryAdd(counters, counterId, name);
            }
        }

        public static ArchetypeWriteResult AddArchetype(long setcode, string name, Dictionary<long, string> knownSetnames)
        {
            StringsEntryWriteResult result = AddEntry(
                setcode,
                name,
                knownSetnames,
                "setname",
                "Setcode ID",
                "Archetype name",
                SetnamePattern);

            return new ArchetypeWriteResult(result.CustomStringsFile, result.MdPro3StringsFile, result.SyncedToMdPro3);
        }

        public static CounterWriteResult AddCounter(long counterId, string name, Dictionary<long, string> knownCounters)
        {
            StringsEntryWriteResult result = AddEntry(
                counterId,
                name,
                knownCounters,
                "counter",
                "Counter ID",
                "Counter name",
                CounterPattern);

            return new CounterWriteResult(result.CustomStringsFile, result.MdPro3StringsFile, result.SyncedToMdPro3);
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

        private static StringsEntryWriteResult AddEntry(
            long entryId,
            string name,
            Dictionary<long, string> knownEntries,
            string directive,
            string idLabel,
            string nameLabel,
            Regex entryPattern)
        {
            name = (name ?? string.Empty).Trim();
            if (entryId <= 0)
            {
                throw new InvalidOperationException($"{idLabel} must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException($"{nameLabel} cannot be empty.");
            }

            Dictionary<long, string> allEntries = new();
            if (knownEntries != null)
            {
                foreach ((long key, string value) in knownEntries)
                {
                    TryAdd(allEntries, key, value);
                }
            }

            string customStringsFile = GetCustomStringsFile();
            if (string.IsNullOrWhiteSpace(customStringsFile))
            {
                throw new InvalidOperationException("Custom Project Directory is not configured in Project Manager.");
            }

            MergeEntries(allEntries, customStringsFile, entryPattern, includeCommented: true);
            string mdPro3StringsFile = GetMdPro3StringsFile();
            MergeEntries(allEntries, mdPro3StringsFile, entryPattern, includeCommented: true);
            if (allEntries.TryGetValue(entryId, out string existingName))
            {
                throw new InvalidOperationException($"{FormatHexId(entryId)} is already used by {existingName}.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(customStringsFile));
            AppendEntry(customStringsFile, directive, entryId, name);

            bool synced = false;
            if (!string.IsNullOrWhiteSpace(mdPro3StringsFile))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(mdPro3StringsFile));
                File.Copy(customStringsFile, mdPro3StringsFile, overwrite: true);
                synced = true;
            }

            return new StringsEntryWriteResult(customStringsFile, mdPro3StringsFile, synced);
        }

        private static void MergeEntries(Dictionary<long, string> entries, string stringsFile, Regex entryPattern, bool includeCommented)
        {
            if (entries == null || string.IsNullOrWhiteSpace(stringsFile) || !File.Exists(stringsFile))
            {
                return;
            }

            foreach ((long entryId, string name) in ReadEntries(stringsFile, entryPattern, includeCommented))
            {
                TryAdd(entries, entryId, name);
            }
        }

        private static List<(long EntryId, string Name)> ReadEntries(string stringsFile, Regex entryPattern, bool includeCommented)
        {
            List<(long EntryId, string Name)> entries = new();
            foreach (string line in File.ReadLines(stringsFile))
            {
                Match match = entryPattern.Match(line);
                if (!match.Success)
                {
                    continue;
                }

                string prefix = match.Groups[1].Value;
                if (prefix == "#" && !includeCommented)
                {
                    continue;
                }

                if (!TryParseHexId(match.Groups[2].Value, out long entryId))
                {
                    continue;
                }

                entries.Add((entryId, match.Groups[3].Value.Trim()));
            }

            return entries;
        }

        private static void AppendEntry(string stringsFile, string directive, long entryId, string name)
        {
            string line = $"!{directive} {FormatHexId(entryId)} {name}";
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
