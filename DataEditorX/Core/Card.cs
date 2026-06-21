/*
 * CreateDate :2014-02-07
 * desc: card model
 * ModiftyDate :2014-02-12
 */
using DataEditorX.Core.Info;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DataEditorX.Core
{
    public struct Card : IEquatable<Card>
    {
        public const int STR_MAX = 0x10;
        public const int SETCODE_MAX = 4;
        public static Card Empty => new(0);

        #region Constructors
        /// <summary>
        /// Card
        /// </summary>
        /// <param name="cardCode">Card ID</param>
        /// <param name="cardName">Name</param>
        public Card(uint cardCode)
        {
            id = cardCode;
            name = "";
            ot = 0;
            alias = 0;
            setcode = 0;
            type = 0;
            atk = -1;
            def = -1;
            level = 0;
            race = 0;
            attribute = 0;
            category = 0;
            omega = [0L, 0L, 0L];
            script = "";
            desc = "";
            str = new string[STR_MAX];
            for (int i = 0; i < STR_MAX; i++)
            {
                str[i] = "";
            }
        }
        #endregion

        #region Fields
        /// <summary>Card ID</summary>
        public uint id;
        /// <summary>Card rule</summary>
        public int ot;
        /// <summary>Card alias</summary>
        public uint alias;
        /// <summary>Card setcode</summary>
        public long setcode;
        /// <summary>Card type</summary>
        public long type;
        /// <summary>Attack</summary>
        public int atk;
        /// <summary>Defense</summary>
        public int def;
        /// <summary>Card level</summary>
        public long level;
        /// <summary>Card races</summary>
        public long race;
        /// <summary>Card attributes</summary>
        public int attribute;
        /// <summary>Effect category</summary>
        public long category;
        /// <summary>Omega-exclusive parameters</summary>
        public long[] omega;
        public string script;
        /// <summary>Card name</summary>
        public string name;
        /// <summary>Description text</summary>
        public string desc;
        string[] str;
        /// <summary>Script text entries</summary>
        public string[] Str
        {
            get
            {
                if (str == null)
                {
                    str = new string[STR_MAX];
                    for (int i = 0; i < STR_MAX; i++)
                    {
                        str[i] = "";
                    }
                }
                return str;
            }
            set { str = value; }
        }
        public long[] GetSetCode()
        {
            long[] setcodes = new long[SETCODE_MAX];
            for (int i = 0, k = 0; i < SETCODE_MAX; k += 0x10, i++)
            {
                setcodes[i] = (setcode >> k) & 0xffff;
            }
            return setcodes;
        }
        public void SetSetCode(params long[] setcodes)
        {
            int i = 0;
            setcode = 0;
            if (setcodes != null)
            {
                foreach (long sc in setcodes)
                {
                    setcode += sc << i;
                    i += 0x10;
                }
            }
        }
        public void SetSetCode(params string[] setcodes)
        {
            int i = 0;
            setcode = 0;
            if (setcodes != null)
            {
                foreach (string sc in setcodes)
                {
                    _ = long.TryParse(sc, NumberStyles.HexNumber, null, out long temp);
                    setcode += temp << i;
                    i += 0x10;
                }
            }
        }
        public void SetSupport(long setcodes)
        {
            omega[2] = setcodes;
        }
        public void SetSupport(string setcodes)
        {
            if (long.TryParse(setcodes, NumberStyles.HexNumber, null, out long temp))
                omega[2] = temp;
        }
        public long GetLeftScale()
        {
            return (level >> 24) & 0xff;
        }
        public long GetRightScale()
        {
            return (level >> 16) & 0xff;
        }
        #endregion

        #region Comparison, hash code, and operators
        /// <summary>
        /// Compare
        /// </summary>
        /// <param name="obj">Object</param>
        /// <returns>Result</returns>
        public override bool Equals(object obj)
        {
            if (obj is Card)
            {
                return Equals((Card)obj); // use Equals method below
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Compare cards excluding script hint text
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool EqualsData(Card other)
        {
            bool equalBool = true;
            if (id != other.id)
            {
                equalBool = false;
            }
            else if (ot != other.ot)
            {
                equalBool = false;
            }
            else if (alias != other.alias)
            {
                equalBool = false;
            }
            else if (setcode != other.setcode)
            {
                equalBool = false;
            }
            else if (type != other.type)
            {
                equalBool = false;
            }
            else if (atk != other.atk)
            {
                equalBool = false;
            }
            else if (def != other.def)
            {
                equalBool = false;
            }
            else if (level != other.level)
            {
                equalBool = false;
            }
            else if (race != other.race)
            {
                equalBool = false;
            }
            else if (attribute != other.attribute)
            {
                equalBool = false;
            }
            else if (category != other.category)
            {
                equalBool = false;
            }
            else if (!name.Equals(other.name))
            {
                equalBool = false;
            }
            else if (!desc.Equals(other.desc))
            {
                equalBool = false;
            }
            else if (omega[0] > 0 && other.omega[0] > 0)
                for (byte i = 1; i < 3; i++)
                {
                    if (omega[i] != other.omega[i])
                    {
                        equalBool = false;
                        break;
                    }
                }
            return equalBool;
        }
        /// <summary>
        /// Compare whether cards match
        /// </summary>
        /// <param name="other">Card to compare</param>
        /// <returns>Result</returns>
        public bool Equals(Card other)
        {
            bool equalBool = EqualsData(other);
            if (!equalBool)
            {
                return false;
            }
            else if (str.Length != other.str.Length)
            {
                equalBool = false;
            }
            else
            {
                int l = str.Length;
                for (int i = 0; i < l; i++)
                {
                    if (!str[i].Equals(other.str[i]))
                    {
                        equalBool = false;
                        break;
                    }
                }
            }
            return equalBool;

        }
        /// <summary>
        /// Get hash code
        /// </summary>
        public override int GetHashCode()
        {
            // combine the hash codes of all members here (e.g. with XOR operator ^)
            int hashCode = id.GetHashCode() + name.GetHashCode();
            return hashCode;//member.GetHashCode();
        }
        /// <summary>
        /// Compare card equality
        /// </summary>
        public static bool operator ==(Card left, Card right)
        {
            return left.Equals(right);
        }
        /// <summary>
        /// Check whether the card has a type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
		public bool IsType(CardType type)
        {
            if ((this.type & (long)type) == (long)type)
            {
                return true;
            }

            return false;
        }
        /// <summary>
        /// Check whether the card has a setcode
        /// </summary>
        /// <param name="sc"></param>
        /// <returns></returns>
		public bool IsSetCode(long sc)
        {
            long settype = sc & 0xfff;
            long setsubtype = sc & 0xf000;
            long setcode = this.setcode;
            while (setcode != 0)
            {
                if ((setcode & 0xfff) == settype && (setcode & 0xf000 & setsubtype) == setsubtype)
                {
                    return true;
                }

                setcode >>= 0x10;
            }
            return false;
        }
        /// <summary>
        /// Compare card inequality
        /// </summary>
        public static bool operator !=(Card left, Card right)
        {
            return !left.Equals(right);
        }
        #endregion

        #region Card text info
        /// <summary>
        /// Card ID string
        /// </summary>
        public string IdString
        {
            get { return id.ToString("00000000"); }
        }
        /// <summary>
        /// Format as string
        /// </summary>
        public override string ToString()
        {
            string str;
            if (IsType(CardType.TYPE_MONSTER))
            {
                str = name + "[" + IdString + "]\n["
                    + YGOUtil.GetTypeString(type) + "] "
                    + YGOUtil.GetRace(race) + "/" + YGOUtil.GetAttributeString(attribute)
                    + "\n" + levelString() + " " + atk + "/" + def + "\n" + redesc();
            }
            else
            {
                str = name + "[" + IdString + "]\n[" + YGOUtil.GetTypeString(type) + "]\n" + redesc();
            }

            return str;
        }
        public string ToShortString()
        {
            return name + " [" + IdString + "]";
        }
        public string ToLongString()
        {
            return ToString();
        }

        string levelString()
        {
            string star = "[";
            long j = level & 0xff;
            long i;
            for (i = 0; i < j; i++)
            {
                if (i > 0 && (i % 4) == 0)
                {
                    star += " ";
                }

                star += "★";
            }
            return star + "]";
        }
        string redesc()
        {
            string str = desc.Replace(Environment.NewLine, "\n");
            str = Regex.Replace(str, "([。|？|?])", "$1\n");
            str = str.Replace("\n\n", "\n");
            return str;
        }
        #endregion
    }

}
