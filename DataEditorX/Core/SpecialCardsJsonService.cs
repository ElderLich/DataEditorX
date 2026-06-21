using DataEditorX.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DataEditorX.Core
{
    internal static class SpecialCardsJsonService
    {
        public const long FlagDarkSynchro = 0x200;
        public const long FlagObelisk = 0x40000;
        public const long FlagRa = 0x80000;
        public const long FlagSlifer = 0x100000;
        public const long FlagRaviel = 0x400000;
        public const long FlagUria = 0x1000000;
        public const long FlagHamon = 0x2000000;

        private static readonly (long Flag, string Property)[] FrameFlags =
        {
            (FlagObelisk, "FrameObelisk"),
            (FlagRa, "FrameRa"),
            (FlagSlifer, "FrameSlifer"),
            (FlagRaviel, "FrameRaviel"),
            (FlagUria, "FrameUria"),
            (FlagHamon, "FrameHamon"),
            (FlagDarkSynchro, "FrameDarkSynchro")
        };

        private static long AllFrameFlags => FrameFlags.Aggregate(0L, (value, item) => value | item.Flag);

        public static bool Sync(Card card, uint previousId, out string message)
        {
            message = string.Empty;
            if (card.id == 0 || card.omega == null || card.omega.Length < 2 || card.omega[0] <= 0)
            {
                return true;
            }

            long flags = card.omega[1];
            bool hasFrameFlag = (flags & AllFrameFlags) != 0;
            string dataDirectory = DEXConfig.ReadString(DEXConfig.TAG_PROJECT_MANAGER_MDPRO3_DATA_DIR);
            if (string.IsNullOrWhiteSpace(dataDirectory))
            {
                if (hasFrameFlag)
                {
                    message = "MDPro3 Data Directory is not configured in Project Manager, so SpecialCards.json was not updated.";
                    return false;
                }

                return true;
            }

            string jsonFile = Path.Combine(dataDirectory, "SpecialCards.json");
            JObject root;
            if (File.Exists(jsonFile))
            {
                try
                {
                    root = JObject.Parse(File.ReadAllText(jsonFile));
                }
                catch (Exception ex)
                {
                    message = $"SpecialCards.json could not be read: {ex.Message}";
                    return false;
                }
            }
            else
            {
                root = new JObject();
            }

            bool changed = false;
            foreach ((_, string property) in FrameFlags)
            {
                JArray array = EnsureArray(root, property);
                changed |= RemoveCode(array, card.id);
                if (previousId != 0 && previousId != card.id)
                {
                    changed |= RemoveCode(array, previousId);
                }
            }

            foreach ((long flag, string property) in FrameFlags)
            {
                if ((flags & flag) == 0)
                {
                    continue;
                }

                JArray array = EnsureArray(root, property);
                if (!ContainsCode(array, card.id))
                {
                    array.Add((long)card.id);
                    changed = true;
                }
            }

            if (!changed)
            {
                return true;
            }

            try
            {
                Directory.CreateDirectory(dataDirectory);
                File.WriteAllText(jsonFile, root.ToString(Formatting.Indented));
                return true;
            }
            catch (Exception ex)
            {
                message = $"SpecialCards.json could not be written: {ex.Message}";
                return false;
            }
        }

        private static JArray EnsureArray(JObject root, string property)
        {
            if (root[property] is JArray array)
            {
                return array;
            }

            array = new JArray();
            root[property] = array;
            return array;
        }

        private static bool ContainsCode(JArray array, uint code)
        {
            return array.Any(token => token.Type == JTokenType.Integer && token.Value<long>() == code);
        }

        private static bool RemoveCode(JArray array, uint code)
        {
            bool removed = false;
            for (int i = array.Count - 1; i >= 0; i--)
            {
                JToken token = array[i];
                if (token.Type != JTokenType.Integer || token.Value<long>() != code)
                {
                    continue;
                }

                token.Remove();
                removed = true;
            }

            return removed;
        }
    }
}
