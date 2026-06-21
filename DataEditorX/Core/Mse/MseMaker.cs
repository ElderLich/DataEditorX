/*
 * Created with SharpDevelop.
 * User: Acer
 * Date: 2014-10-12
 * Time: 12:48
 * 
 */
using DataEditorX.Common;
using DataEditorX.Core.Info;
using DataEditorX.Language;
using Microsoft.VisualBasic;
using System.Text;
using System.Text.RegularExpressions;

namespace DataEditorX.Core.Mse
{
    /// <summary>
    /// MSE generation
    /// </summary>
    public class MseMaker
    {
        #region Constants
        public const string TAG_CARD = "card";
        public const string TAG_CARDTYPE = "card type";
        public const string TAG_NAME = "name";
        public const string TAG_ATTRIBUTE = "attribute";
        public const string TAG_LEVEL = "level";
        public const string TAG_IMAGE = "image";
        /// <summary>Race</summary>
        public const string TAG_TYPE1 = "type 1";
        /// <summary>Effect1</summary>
        public const string TAG_TYPE2 = "type 2";
        /// <summary>Effect2/summary>
        public const string TAG_TYPE3 = "type 3";
        /// <summary>Effect3</summary>
        public const string TAG_TYPE4 = "type 4";
        public const string TAG_TYPE5 = "type 5";
        public const string TAG_TEXT = "rule text";
        public const string TAG_ATK = "attack";
        public const string TAG_DEF = "defense";
        public const string TAG_NUMBER = "number";
        public const string TAG_RARITY = "rarity";
        public const string TAG_PENDULUM = "pendulum";
        public const string TAG_PSCALE1 = "pendulum scale 1";
        public const string TAG_PSCALE2 = "pendulum scale 2";
        public const string TAG_PEND_TEXT = "pendulum text";
        public const string TAG_CODE = "gamecode";
        public const string UNKNOWN_ATKDEF = "?";
        public const int UNKNOWN_ATKDEF_VALUE = -2;
        public const string TAG_REP_TEXT = "%text%";
        public const string TAG_REP_PTEXT = "%ptext%";

        public const string TAG_Link_Marker_Up = "Link Marker Up";
        public const string TAG_Link_Marker_UL = "Link Marker UL";
        public const string TAG_Link_Marker_UR = "Link Marker UR";
        public const string TAG_Link_Marker_Down = "Link Marker Down";
        public const string TAG_Link_Marker_DL = "Link Marker DL";
        public const string TAG_Link_Marker_DR = "Link Marker DR";
        public const string TAG_Link_Marker_Left = "Link Marker Left";
        public const string TAG_Link_Marker_Right = "Link Marker Right";
        public const string TAG_Link_Number = "link number";
        #endregion

        #region Fields and initialization
        MSEConfig cfg;
        public int MaxNum
        {
            get { return cfg.maxcount; }
        }

        public string ImagePath
        {
            get { return cfg.imagepath; }
        }

        public MseMaker(MSEConfig mcfg)
        {
            SetConfig(mcfg);
        }
        public void SetConfig(MSEConfig mcfg)
        {
            cfg = mcfg;
        }
        public MSEConfig GetConfig()
        {
            return cfg;
        }
        #endregion

        #region Data processing
        //Merge
        public static string GetLine(string key, string word)
        {
            return "	" + key + ": " + word;
        }
        //Special character replacements
        public string ReItalic(string str)
        {
            str = CN2TW(str);
            foreach (string rs in cfg.replaces.Keys)
            {
                str = Regex.Replace(str, rs, cfg.replaces[rs]);
            }
            return str;
        }
        //Simplified-to-Traditional conversion
        public string CN2TW(string str)
        {
            if (cfg.Iscn2tw)
            {
                str = Strings.StrConv(str, VbStrConv.TraditionalChinese, 0);
                str = str.Replace("巖", "岩");
            }
            return str;
        }
        //Get the spell/trap type symbol
        public string GetSpellTrapSymbol(Card c, bool isSpell)
        {
            string level;
            if (c.IsType(CardType.TYPE_EQUIP))
            {
                level = MseSpellTrap.EQUIP;
            }
            else if (c.IsType(CardType.TYPE_QUICKPLAY))
            {
                level = MseSpellTrap.QUICKPLAY;
            }
            else if (c.IsType(CardType.TYPE_FIELD))
            {
                level = MseSpellTrap.FIELD;
            }
            else if (c.IsType(CardType.TYPE_CONTINUOUS))
            {
                level = MseSpellTrap.CONTINUOUS;
            }
            else if (c.IsType(CardType.TYPE_RITUAL))
            {
                level = MseSpellTrap.RITUAL;
            }
            else if (c.IsType(CardType.TYPE_COUNTER))
            {
                level = MseSpellTrap.COUNTER;
            }
            else if (cfg.str_spell == MSEConfig.TAG_REP && cfg.str_trap == MSEConfig.TAG_REP)
            {
                level = MseSpellTrap.NORMAL;//Image with text
            }
            else
            {
                level = "";
            }

            if (isSpell)
            {
                level = cfg.str_spell.Replace(MSEConfig.TAG_REP, level);
            }
            else
            {
                level = cfg.str_trap.Replace(MSEConfig.TAG_REP, level);
            }

            return level;
        }
        //Get image path
        public static string GetCardImagePath(string picpath, Card c)
        {
            //Card ID, zero-padded ID, and card name
            string jpg = MyPath.Combine(picpath, c.id + ".jpg");
            string jpg2 = MyPath.Combine(picpath, c.IdString + ".jpg");
            string jpg3 = MyPath.Combine(picpath, c.name + ".jpg");
            string png = MyPath.Combine(picpath, c.id + ".png");
            string png2 = MyPath.Combine(picpath, c.IdString + ".png");
            string png3 = MyPath.Combine(picpath, c.name + ".png");
            if (File.Exists(jpg))
            {
                return jpg;
            }
            else if (File.Exists(jpg2))
            {
                return jpg2;
            }
            else if (File.Exists(jpg3))
            {
                File.Copy(jpg3, jpg, true);
                if (File.Exists(jpg))
                {//Copy failed
                    return jpg;
                }
            }
            else if (File.Exists(png))
            {
                return png;
            }
            else if (File.Exists(png2))
            {
                return png2;
            }
            else if (File.Exists(png3))
            {
                File.Copy(png3, png, true);
                if (File.Exists(png))
                {//Copy failed
                    return png;
                }
            }
            return "";
        }
        //Get attribute
        public static string GetAttribute(int attr)
        {
            CardAttribute cattr = (CardAttribute)attr;
            string sattr = MseAttribute.NONE;
            switch (cattr)
            {
                case CardAttribute.ATTRIBUTE_DARK:
                    sattr = MseAttribute.DARK;
                    break;
                case CardAttribute.ATTRIBUTE_DEVINE:
                    sattr = MseAttribute.DIVINE;
                    break;
                case CardAttribute.ATTRIBUTE_EARTH:
                    sattr = MseAttribute.EARTH;
                    break;
                case CardAttribute.ATTRIBUTE_FIRE:
                    sattr = MseAttribute.FIRE;
                    break;
                case CardAttribute.ATTRIBUTE_LIGHT:
                    sattr = MseAttribute.LIGHT;
                    break;
                case CardAttribute.ATTRIBUTE_WATER:
                    sattr = MseAttribute.WATER;
                    break;
                case CardAttribute.ATTRIBUTE_WIND:
                    sattr = MseAttribute.WIND;
                    break;
            }
            return sattr;
        }
        //Get effect text
        public static string GetDesc(string cdesc, string regx)
        {
            string desc = cdesc.Replace("\r\n", "\n").Replace("\r", "\n");
            Regex regex = new(regx, RegexOptions.Multiline);
            Match mc = regex.Match(desc);
            if (mc.Success)
            {
                return (mc.Groups.Count > 1) ?
                        mc.Groups[1].Value : mc.Groups[0].Value;
            }

            return "";
        }

        public static string ReText(string text)
        {
            StringBuilder sb = new(text);
            _ = sb.Replace("\r\n", "\n");
            _ = sb.Replace("\r", "");
            _ = sb.Replace("\n\n", "\n");
            _ = sb.Replace("\n", "\n\t\t");
            return Regex.Replace(sb.ToString().Trim('\n'), "</?[bi]>", "");
        }
        //Get level/rank stars
        public static string GetStar(long level)
        {
            long j = level & 0xff;
            string star = "";
            for (int i = 0; i < j; i++)
            {
                star += "*";
            }
            return star;
        }
        //Get race
        public string GetRace(long race)
        {
            if (cfg.raceDic.ContainsKey(race))
            {
                return cfg.raceDic[race].Trim();
            }

            return race.ToString("x");
        }
        //Get type text
        public string GetType(CardType ctype)
        {
            long type = (long)ctype;
            if (cfg.typeDic.ContainsKey(type))
            {
                return cfg.typeDic[type].Trim();
            }

            return type.ToString("x");
        }

        //Read card type
        public string[] GetTypes(Card c)
        {
            // Card type, effect 1, effect 2, effect 3
            int MAX_TYPE = 5;
            var types = new string[MAX_TYPE + 1];
            types[0] = MseCardType.CARD_NORMAL;
            for (int i = 1; i < types.Length; i++)
            {
                types[i] = "";
            }
            if (c.IsType(CardType.TYPE_MONSTER))
            {
                CardType[] cardTypes = CardTypes.GetMonsterTypes(c.type, cfg.no10);
                int count = cardTypes.Length;
                for (int i = 0; i < count && i < MAX_TYPE; i++)
                {
                    types[i + 1] = GetType(cardTypes[i]);
                }
                if (cardTypes.Length > 0)
                {
                    if (c.IsType(CardType.TYPE_LINK))
                    {
                        types[0] = MseCardType.CARD_LINK;
                    }
                    else if (c.IsType(CardType.TYPE_TOKEN))
                    {
                        types[0] = (c.race == 0) ?
                            MseCardType.CARD_TOKEN2
                            : MseCardType.CARD_TOKEN;
                    }
                    else if (c.IsType(CardType.TYPE_RITUAL))
                    {
                        types[0] = MseCardType.CARD_RITUAL;
                    }
                    else if (c.IsType(CardType.TYPE_FUSION))
                    {
                        types[0] = MseCardType.CARD_FUSION;
                    }
                    else if (c.IsType(CardType.TYPE_SYNCHRO))
                    {
                        types[0] = MseCardType.CARD_SYNCHRO;
                    }
                    else if (c.IsType(CardType.TYPE_XYZ))
                    {
                        types[0] = MseCardType.CARD_XYZ;
                    }
                    else if (c.IsType(CardType.TYPE_EFFECT))
                    {
                        types[0] = MseCardType.CARD_EFFECT;
                    }
                    else
                    {
                        types[0] = MseCardType.CARD_NORMAL;
                        if (cardTypes.Length == 1)
                        {
                            //xxx/Normal
                        }
                    }
                }
            }
            if (c.race == 0)//When race is missing
            {
                types[1] = "";
                types[2] = "";
                types[3] = "";
                types[4] = "";
            }
            return types;
        }
        #endregion

        #region Write save file
        //Write save file
        public Dictionary<Card, string> WriteSet(string file, Card[] cards, string cardpack_db, bool rarity = true)
        {
            Dictionary<Card, string> list = new();
            string pic = cfg.imagepath;
            using (FileStream fs = new(file,
                                                  FileMode.Create, FileAccess.Write))
            {
                StreamWriter sw = new(fs, Encoding.UTF8);
                sw.WriteLine(cfg.head);
                foreach (Card c in cards)
                {
                    string jpg = GetCardImagePath(pic, c);
                    if (!string.IsNullOrEmpty(jpg))
                    {
                        list.Add(c, jpg);
                        jpg = Path.GetFileName(jpg);
                    }
                    CardPack cardpack = DataBase.FindPack(cardpack_db, c.id);
                    if (c.IsType(CardType.TYPE_SPELL) || c.IsType(CardType.TYPE_TRAP))
                    {
                        sw.WriteLine(GetSpellTrap(c, jpg, c.IsType(CardType.TYPE_SPELL), cardpack, rarity));
                    }
                    else if(c.type != (long)CardType.TYPE_TOKEN && c.type != (long)(CardType.TYPE_MONSTER | CardType.TYPE_TOKEN))
                    {
                         sw.WriteLine(GetMonster(c, jpg), cardpack, rarity);
                    }
                }
                sw.WriteLine(cfg.end);
                sw.Close();
            }

            return list;
        }
        static int GetLinkNumber(long link)
        {
            string str = Convert.ToString(link, 2);
            char[] cs = str.ToCharArray();
            int i = 0;
            foreach (char c in cs)
            {
                if (c == '1')
                {
                    i++;
                }
            }
            return i;
        }
        //Monster and Pendulum monster
        string GetMonster(Card c, string img, CardPack cardpack = null, bool rarity = true)
        {
            StringBuilder sb = new();
            string[] types = GetTypes(c);
            string race = GetRace(c.race);
            _ = sb.AppendLine(TAG_CARD + ":");
            _ = sb.AppendLine(GetLine(TAG_CARDTYPE, types[0]));
            _ = sb.AppendLine(GetLine(TAG_NAME, ReItalic(c.name)));
            _ = sb.AppendLine(GetLine(TAG_ATTRIBUTE, GetAttribute(c.attribute)));
            bool noStar = false;
            if (cfg.noStartCards != null)
            {
                foreach (long id in cfg.noStartCards)
                {
                    if (c.alias == id || c.id == id)
                    {
                        noStar = true;
                        break;
                    }
                }
            }
            if (!noStar)
            {
                _ = sb.AppendLine(GetLine(TAG_LEVEL, GetStar(c.level)));
            }
            _ = sb.AppendLine(GetLine(TAG_IMAGE, img));
            _ = sb.AppendLine(GetLine(TAG_TYPE1, CN2TW(race)));
            _ = sb.AppendLine(GetLine(TAG_TYPE2, CN2TW(types[1])));
            _ = sb.AppendLine(GetLine(TAG_TYPE3, CN2TW(types[2])));
            _ = sb.AppendLine(GetLine(TAG_TYPE4, CN2TW(types[3])));
            _ = sb.AppendLine(GetLine(TAG_TYPE5, CN2TW(types[4])));
            if (cardpack != null)
            {
                _ = sb.AppendLine(GetLine(TAG_NUMBER, cardpack.pack_id));
                if (rarity)
                {
                    _ = sb.AppendLine(GetLine(TAG_RARITY, cardpack.GetMseRarity()));
                }
            }
            string txt = Regex.Replace(c.desc, "\\[/?b\\]", "");
            string ptx = "";
            if (!string.IsNullOrEmpty(txt))
            {
                string[] t0 = Regex.Split(txt, "\r?\n\r?\n");
                if (t0.Length > 2)
                {
                    txt = txt[..(txt.Length - t0[2].Length - 1)];
                }
                List<string> t1 = GetMPText(txt);
                ptx = t1[1]; txt = t1[0];
            }
            if (c.IsType(CardType.TYPE_LINK))
            {
                if (CardLink.IsLink(c.def, CardLink.DownLeft))
                {
                    _ = sb.AppendLine(GetLine(TAG_Link_Marker_DL, "yes"));
                }
                if (CardLink.IsLink(c.def, CardLink.Down))
                {
                    _ = sb.AppendLine(GetLine(TAG_Link_Marker_Down, "yes"));
                }
                if (CardLink.IsLink(c.def, CardLink.DownRight))
                {
                    _ = sb.AppendLine(GetLine(TAG_Link_Marker_DR, "yes"));
                }
                if (CardLink.IsLink(c.def, CardLink.UpLeft))
                {
                    _ = sb.AppendLine(GetLine(TAG_Link_Marker_UL, "yes"));
                }
                if (CardLink.IsLink(c.def, CardLink.Up))
                {
                    _ = sb.AppendLine(GetLine(TAG_Link_Marker_Up, "yes"));
                }
                if (CardLink.IsLink(c.def, CardLink.UpRight))
                {
                    _ = sb.AppendLine(GetLine(TAG_Link_Marker_UR, "yes"));
                }
                if (CardLink.IsLink(c.def, CardLink.Left))
                {
                    _ = sb.AppendLine(GetLine(TAG_Link_Marker_Left, "yes"));
                }
                if (CardLink.IsLink(c.def, CardLink.Right))
                {
                    _ = sb.AppendLine(GetLine(TAG_Link_Marker_Right, "yes"));
                }
                _ = sb.AppendLine(GetLine(TAG_Link_Number, "" + GetLinkNumber(c.def)));
                _ = sb.AppendLine("	" + TAG_TEXT + ":");
                _ = sb.AppendLine("		" + ReText(ReItalic(txt)));
            }
            else
            {
                if (c.IsType(CardType.TYPE_PENDULUM))//Pendulum monster
                {
                    _ = sb.AppendLine("	" + TAG_TEXT + ":");
                    //sb.AppendLine(cfg.regx_monster + ":" + cfg.regx_pendulum);
                    _ = sb.AppendLine("		" + ReText(ReItalic(txt)));
                    _ = sb.AppendLine(GetLine(TAG_PENDULUM, "medium"));
                    _ = sb.AppendLine(GetLine(TAG_PSCALE1, ((c.level >> 0x18) & 0xff).ToString()));
                    _ = sb.AppendLine(GetLine(TAG_PSCALE2, ((c.level >> 0x10) & 0xff).ToString()));
                    _ = sb.AppendLine("	" + TAG_PEND_TEXT + ":");
                    _ = sb.AppendLine("		" + ReText(ReItalic(ptx)));
                }
                else//Regular monster
                {
                    _ = sb.AppendLine("	" + TAG_TEXT + ":");
                    _ = sb.AppendLine("		" + ReText(ReItalic(txt)));
                }
                _ = sb.AppendLine(GetLine(TAG_DEF, (c.def < 0) ? UNKNOWN_ATKDEF : c.def.ToString()));
            }
            _ = sb.AppendLine(GetLine(TAG_ATK, (c.atk < 0) ? UNKNOWN_ATKDEF : c.atk.ToString()));

            _ = sb.AppendLine(GetLine(TAG_CODE, c.IdString));
            return sb.ToString();
        }
        //Spell/trap
        string GetSpellTrap(Card c, string img, bool isSpell, CardPack cardpack = null, bool rarity = true)
        {
            StringBuilder sb = new();
            _ = sb.AppendLine(TAG_CARD + ":");
            _ = sb.AppendLine(GetLine(TAG_CARDTYPE, isSpell ? "spell card" : "trap card"));
            _ = sb.AppendLine(GetLine(TAG_NAME, ReItalic(c.name)));
            _ = sb.AppendLine(GetLine(TAG_ATTRIBUTE, isSpell ? "spell" : "trap"));
            _ = sb.AppendLine(GetLine(TAG_LEVEL, GetSpellTrapSymbol(c, isSpell)));
            _ = sb.AppendLine(GetLine(TAG_IMAGE, img));
            if (cardpack != null)
            {
                _ = sb.AppendLine(GetLine(TAG_NUMBER, cardpack.pack_id));
                if (rarity)
                {
                    _ = sb.AppendLine(GetLine(TAG_RARITY, cardpack.GetMseRarity()));
                }
            }
            string txt = Regex.Replace(c.desc, "\\[/?b\\]", "");
            if (!string.IsNullOrEmpty(txt)) txt = Regex.Split(txt, "\r?\n\r?\n")[0];
            _ = sb.AppendLine("	" + TAG_TEXT + ":");
            _ = sb.AppendLine("		" + ReText(ReItalic(txt)));
            _ = sb.AppendLine(GetLine(TAG_CODE, c.IdString));
            return sb.ToString();
        }
        #endregion

        #region Read save file
        public static int GetAttributeInt(string cattr)
        {
            int iattr = 0;
            switch (cattr)
            {
                case MseAttribute.DARK:
                    iattr = (int)CardAttribute.ATTRIBUTE_DARK;
                    break;
                case MseAttribute.DIVINE:
                    iattr = (int)CardAttribute.ATTRIBUTE_DEVINE;
                    break;
                case MseAttribute.EARTH:
                    iattr = (int)CardAttribute.ATTRIBUTE_EARTH;
                    break;
                case MseAttribute.FIRE:
                    iattr = (int)CardAttribute.ATTRIBUTE_FIRE;
                    break;
                case MseAttribute.LIGHT:
                    iattr = (int)CardAttribute.ATTRIBUTE_LIGHT;
                    break;
                case MseAttribute.WATER:
                    iattr = (int)CardAttribute.ATTRIBUTE_WATER;
                    break;
                case MseAttribute.WIND:
                    iattr = (int)CardAttribute.ATTRIBUTE_WIND;
                    break;
            }
            return iattr;
        }
        long GetRaceInt(string race)
        {
            if (!string.IsNullOrEmpty(race))
            {
                foreach (long key in cfg.raceDic.Keys)
                {
                    if (race.Equals(cfg.raceDic[key]))
                    {
                        return key;
                    }
                }
            }
            return (long)CardRace.RACE_NONE;
        }
        long GetTypeInt(string type)
        {
            if (!string.IsNullOrEmpty(type))
            {
                foreach (long key in cfg.typeDic.Keys)
                {
                    if (type.Equals(cfg.typeDic[key]))
                    {
                        return key;
                    }
                }
            }
            return 0;
        }
        static string GetValue(string content, string tag)
        {
            Regex regx = new(@"^[\t]+?" + tag + @":([\s\S]*?)$", RegexOptions.Multiline);
            Match m = regx.Match(content);
            if (m.Success)
            {
                if (m.Groups.Count >= 2)
                {
                    return RemoveTag(m.Groups[1].Value);
                }
            }
            return "";
        }
        //Multiline text
        static string GetMultiValue(string content, string tag)
        {
            //TODO
            content = content.Replace("\t\t", "");
            Regex regx = new(@"^[\t]+?" + tag + @":([\S\s]*?)^\t[\S\s]+?:", RegexOptions.Multiline);
            Match m = regx.Match(content);
            if (m.Success)
            {
                if (m.Groups.Count >= 2)
                {
                    string word = m.Groups[1].Value;
                    return RemoveTag(word).Replace("^", "").Replace("\t", "");
                }
            }
            return "";
        }

        static long GetSpellTrapType(string level)
        {
            long type = 0;
            //Spell/trap
            if (level.Contains(MseSpellTrap.EQUIP))
            {
                type = (long)CardType.TYPE_EQUIP;
            }

            if (level.Contains(MseSpellTrap.QUICKPLAY))
            {
                type = (long)CardType.TYPE_QUICKPLAY;
            }

            if (level.Contains(MseSpellTrap.FIELD))
            {
                type = (long)CardType.TYPE_FIELD;
            }

            if (level.Contains(MseSpellTrap.CONTINUOUS))
            {
                type = (long)CardType.TYPE_CONTINUOUS;
            }

            if (level.Contains(MseSpellTrap.RITUAL))
            {
                type = (long)CardType.TYPE_RITUAL;
            }

            if (level.Contains(MseSpellTrap.COUNTER))
            {
                type = (long)CardType.TYPE_COUNTER;
            }

            return type;
        }

        static long GetMonsterType(string cardtype)
        {
            long type;
            if (cardtype.Equals(MseCardType.CARD_SPELL))
            {
                type = (long)CardType.TYPE_SPELL;
            }
            else if (cardtype.Equals(MseCardType.CARD_TRAP))
            {
                type = (long)CardType.TYPE_TRAP;
            }
            else
            {
                type = (long)CardType.TYPE_MONSTER;
                type |= cardtype switch
                {
                    MseCardType.CARD_NORMAL => (long)CardType.TYPE_NORMAL,
                    MseCardType.CARD_EFFECT => (long)CardType.TYPE_EFFECT,
                    MseCardType.CARD_XYZ => (long)CardType.TYPE_XYZ,
                    MseCardType.CARD_RITUAL => (long)CardType.TYPE_RITUAL,
                    MseCardType.CARD_FUSION => (long)CardType.TYPE_FUSION,
                    MseCardType.CARD_TOKEN or MseCardType.CARD_TOKEN2 => (long)CardType.TYPE_TOKEN,
                    MseCardType.CARD_SYNCHRO => (long)CardType.TYPE_SYNCHRO,
                    _ => (long)CardType.TYPE_NORMAL,
                };
            }
            return type;
        }
        //Card types
        long GetCardType(string cardtype, string level, params string[] types)
        {
            long type = 0;
            //Spell/trap
            type |= GetSpellTrapType(level);
            //Monster
            type |= GetMonsterType(cardtype);
            //types identifies monster effect types
            foreach (string typ in types)
            {
                type |= GetTypeInt(typ);
            }

            return type;
        }

        static string RemoveTag(string word)
        {
            //Remove angle-bracket tags
            word = Regex.Replace(word, "<[^>]+?>", "");
            return word.Trim().Replace("\t", "");
        }
        //Parse card
        public Card ReadCard(string content, out string img)
        {
            string tmp;
            Card c = new()
            {
                ot = (int)CardRule.OCGTCG,
                //Card name
                name = GetValue(content, TAG_NAME)
            };
            tmp = GetValue(content, TAG_LEVEL);
            //Card races
            c.race = GetRaceInt(GetValue(content, TAG_TYPE1));
            //Card types
            c.type = GetCardType(GetValue(content, TAG_CARDTYPE), tmp,
                                 GetValue(content, TAG_TYPE2),
                                 GetValue(content, TAG_TYPE3),
                                 GetValue(content, TAG_TYPE4),
                                 GetValue(content, TAG_TYPE5));
            long t = GetSpellTrapType(GetValue(content, TAG_LEVEL));
            //Stars for non-spell/trap cards
            if (!(c.IsType(CardType.TYPE_SPELL)
                  || c.IsType(CardType.TYPE_TRAP)) && t == 0)
            {
                c.level = GetValue(content, TAG_LEVEL).Length;
            }

            //Attribute
            c.attribute = GetAttributeInt(GetValue(content, TAG_ATTRIBUTE));
            //Card ID
            _ = uint.TryParse(GetValue(content, TAG_CODE), out c.id);
            //ATK
            tmp = GetValue(content, TAG_ATK);
            if (tmp == UNKNOWN_ATKDEF)
            {
                c.atk = UNKNOWN_ATKDEF_VALUE;
            }
            else
            {
                _ = int.TryParse(tmp, out c.atk);
            }
            //DEF
            tmp = GetValue(content, TAG_DEF);
            if (tmp == UNKNOWN_ATKDEF)
            {
                c.def = UNKNOWN_ATKDEF_VALUE;
            }
            else
            {
                _ = int.TryParse(tmp, out c.def);
            }
            //Image
            img = GetValue(content, TAG_IMAGE);
            //Pendulum
            if (c.IsType(CardType.TYPE_PENDULUM))
            {//Replace content using the configured template
                tmp = cfg.temp_text.Replace(TAG_REP_TEXT,
                                            GetMultiValue(content, TAG_TEXT));
                tmp = tmp.Replace(TAG_REP_PTEXT,
                                  GetMultiValue(content, TAG_PEND_TEXT));
                c.desc = tmp;
            }
            else
            {
                c.desc = GetMultiValue(content, TAG_TEXT);
            }
            //Pendulum scales
            _ = int.TryParse(GetValue(content, TAG_PSCALE1), out int itmp);
            c.level += itmp << 0x18;
            _ = int.TryParse(GetValue(content, TAG_PSCALE2), out itmp);
            c.level += itmp << 0x10;
            return c;
        }
        //Read all cards
        public Card[] ReadCards(string set, bool repalceOld)
        {
            List<Card> cards = new();
            if (!File.Exists(set))
            {
                return null;
            }

            string allcontent = File.ReadAllText(set, Encoding.UTF8);

            Regex regx = new(@"^card:[\S\s]+?gamecode:[\S\s]+?$",
                                   RegexOptions.Multiline);
            MatchCollection matchs = regx.Matches(allcontent);
            uint i = 0;

            foreach (Match match in matchs.Cast<Match>())
            {
                string content = match.Groups[0].Value;
                i++;
                Card c = ReadCard(content, out string img);
                if (c.id <= 0)
                {
                    c.id = i;
                }
                //Add card
                cards.Add(c);
                //Image already extracted from the archive
                string saveimg = MyPath.Combine(cfg.imagepath, img);
                if (!File.Exists(saveimg))//Matching image was not extracted
                {
                    continue;
                }
                //Renamed image
                img = MyPath.Combine(cfg.imagepath, c.IdString + ".jpg");
                if (img == saveimg)//File names are the same
                {
                    continue;
                }

                if (File.Exists(img))
                {
                    if (repalceOld)//Back up the existing image when present
                    {
                        File.Delete(img + ".bak");// Delete old backup.
                        File.Move(img, img + ".bak");//Back up existing file
                        File.Move(saveimg, img);//Rename extracted file
                    }
                }
                else
                {
                    File.Move(saveimg, img);
                }
            }
            File.Delete(set);
            return cards.ToArray();
        }
        #endregion

        #region images
        /// <summary>
        /// Image cache
        /// </summary>
        /// <param name="img"></param>
        /// <param name="card"></param>
        /// <returns></returns>
        public string GetImageCache(string img, Card card)
        {
            if (!cfg.reimage)
            {
                //No resize needed
                return img;
            }
            bool isPendulum = card.IsType(CardType.TYPE_PENDULUM);
            if (isPendulum)
            {
                if (cfg.pwidth <= 0 && cfg.pheight <= 0)
                {
                    return img;
                }
            }
            else
            {
                if (cfg.width <= 0 && cfg.height <= 0)
                {
                    return img;
                }
            }
            string md5 = MyUtils.GetMD5HashFromFile(img);
            if (MyUtils.Md5isEmpty(md5) || cfg.imagecache == null)
            {
                //MD5 is empty
                return img;
            }
            string file = MyPath.Combine(cfg.imagecache, md5);
            if (!File.Exists(file))
            {
                //Create cache
                Bitmap bmp = MyBitmap.ReadImage(img);
                //Resize
                if (isPendulum)
                {
                    bmp = MyBitmap.Zoom(bmp, cfg.pwidth, cfg.pheight);
                }
                else
                {
                    bmp = MyBitmap.Zoom(bmp, cfg.width, cfg.height);
                }
                //Save file
                _ = MyBitmap.SaveAsJPEG(bmp, file, 100);
            }
            return file;
        }
        #endregion

        #region export
        static System.Diagnostics.Process _mseProcess;
        static EventHandler _exitHandler;
        private static void ExportSetThread(object obj)
        {
            string[] args = (string[])obj;
            if (args == null || args.Length < 3)
            {
                _ = MessageBox.Show(LanguageHelper.GetMsg(LMSG.exportMseImagesErr));
                return;
            }
            string mse_path = args[0];
            string setfile = args[1];
            string path = args[2];
            if (string.IsNullOrEmpty(mse_path) || string.IsNullOrEmpty(setfile))
            {
                _ = MessageBox.Show(LanguageHelper.GetMsg(LMSG.exportMseImagesErr));
                return;
            }
            else
            {
                string cmd = " --export " + setfile.Replace("\\\\", "\\").Replace("\\", "/") + " {card.gamecode}.png";
                _mseProcess = new System.Diagnostics.Process();
                _mseProcess.StartInfo.FileName = mse_path;
                _mseProcess.StartInfo.Arguments = cmd;
                _mseProcess.StartInfo.WorkingDirectory = path;
                _mseProcess.EnableRaisingEvents = true;
                MyPath.CreateDir(path);
                try
                {
                    _ = _mseProcess.Start();
                    //Wait for completion; this method should run on a worker thread
                    _mseProcess.WaitForExit();
                    _mseProcess.Exited += new EventHandler(_exitHandler);
                    _mseProcess.Close();
                    _mseProcess = null;
                    _ = MessageBox.Show(LanguageHelper.GetMsg(LMSG.exportMseImages));
                }
                catch
                {

                }
            }
        }

        public static bool MseIsRunning()
        {
            return _mseProcess != null;
        }
        public static void MseStop()
        {
            try
            {
                _mseProcess.Kill();
                _mseProcess.Close();
            }
            catch { }
        }
        public static void ExportSet(string mse_path, string setfile, string path, EventHandler handler)
        {
            if (string.IsNullOrEmpty(mse_path) || setfile == null || setfile.Length == 0)
            {
                return;
            }
            ParameterizedThreadStart ParStart = new(ExportSetThread);
            Thread myThread = new(ParStart)
            {
                IsBackground = true
            };
            myThread.Start(new string[] { mse_path, setfile, path });
            _exitHandler = handler;
        }
        #endregion

        public void TestPendulum(string desc)
        {
            List<string> table = GetMPText(Regex.Replace(desc, "\\[/?b\\]", ""));
            if (table == null && table.Count != 2)
            {
                _ = MessageBox.Show("desc is null", "info");
            }
            else
            {
                _ = MessageBox.Show(ReItalic(table[0]), "Monster Effect");
                _ = MessageBox.Show(ReItalic(table[1]), "Pendulum Effect");
            }
        }

        public static List<string> GetMPText(string desc)
        {
            if (string.IsNullOrEmpty(desc))
            {
                _ = MessageBox.Show("desc is null", "info");
                return null;
            }
            else
            {
                string ptext;
                string text;
                if (Regex.IsMatch(desc, MSEConfig.RegXPendulum))
                {
                    ptext = GetDesc(desc, MSEConfig.RegXPendulum);
                    text = GetDesc(desc, MSEConfig.RegXMonster);
                }
                else if(Regex.IsMatch(desc, MSEConfig.RegXRush))
                {
                    ptext = text = "";
                    Regex regex = new(MSEConfig.RegXRush, RegexOptions.Multiline);
                    Match mc = regex.Match(desc);
                    if (mc.Success) {
                        text = "<b>[Requirement]</b> " + mc.Groups[3].Value
                            + "\n<b>[Effect]</b> " + mc.Groups[4].Value;
                        if(!string.IsNullOrEmpty(mc.Groups[2].Value)) text = mc.Groups[2].Value + "\n" + text;
                        if(!string.IsNullOrEmpty(mc.Groups[1].Value)) text = mc.Groups[1].Value + "\n" + text;
                    }
                }
                else
                {
                    ptext = text = desc;
                }
                if (string.IsNullOrEmpty(text))
                {
                    text = desc;
                }
                else
                {
                    if (ptext == text) ptext = "";
                    text = Regex.Split(text, "\r?\n-*?\r?\n")[0];
                }

                List<string> val = new()
                {
                    text,
                    ptext
                };
                return val;
            }
        }
    }
}
