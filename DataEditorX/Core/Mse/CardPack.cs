/*
 * Created with SharpDevelop.
 * User: Hasee
 * Date: 2016/2/27
 * Time: 7:55
 * 
 * Legacy SharpDevelop template note.
 */

namespace DataEditorX.Core
{
    /// <summary>
    /// Description of CardPack.
    /// </summary>
    public class CardPack
    {
        public CardPack(long id)
        {
            CardId = id;
        }

        public long CardId
        {
            get;
            private set;
        }
        public string pack_id;
        public string pack_name;
        public string rarity;
        public string date;

        public string GetMseRarity()
        {
            if (this.rarity == null)
            {
                return "common";
            }

            string rarity = this.rarity.Trim().ToLower();
            if (rarity.Equals("common") || rarity.Equals("short print"))
            {
                return "common";
            }
            if (rarity.Equals("rare") || rarity.Equals("normal rare"))
            {
                return "rare";
            }
            else if (rarity.Contains("parallel") || rarity.Contains("duel terminal"))
            {
                return "parallel rare";
            }
            else if (rarity.Contains("super") || rarity.Contains("holofoil"))
            {
                return "super rare";
            }
            else if (rarity.Contains("ultra"))
            {
                return "ultra rare";
            }
            else if (rarity.Contains("secret"))
            {
                return "secret rare";
            }
            else if (rarity.Contains("ultimate"))
            {
                return "ultimate rare";
            }
            else if (rarity.Contains("prismatic"))
            {
                return "prismatic rare";
            }
            else if (rarity.Contains("star"))
            {
                return "star rare";
            }
            else if (rarity.Contains("mosaic"))
            {
                return "mosaic rare";
            }
            else if (rarity.Contains("platinum"))
            {
                return "platinum rare";
            }
            else if (rarity.Contains("ghost") || rarity.Contains("holographic"))
            {
                return "ghost rare";
            }
            else if (rarity.Contains("millenium"))
            {
                return "millenium rare";
            }
            else if (rarity.Contains("Kaiba"))
            {
                return "Kaiba Corporation Rare";
            }
            if (this.rarity.Contains('/'))
            {
                return this.rarity.Split('/')[0];
            }
            return this.rarity;
        }
    }
}
