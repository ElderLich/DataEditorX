/*
 * Created with SharpDevelop.
 * User: Acer
 * Date: 2014-10-13
 * Time: 9:08
 * 
 */

namespace DataEditorX.Core.Info
{
    /// <summary>
    /// Card types
    /// </summary>
    public enum CardType : long
    {
        ///<summary>Monster Card</summary>
        TYPE_MONSTER = 0x1,
        ///<summary>Spell Card</summary>
        TYPE_SPELL = 0x2,
        ///<summary>Trap Card</summary>
        TYPE_TRAP = 0x4,

        ///<summary>Normal</summary>
        TYPE_NORMAL = 0x10,
        ///<summary>Effect</summary>
        TYPE_EFFECT = 0x20,
        ///<summary>Fusion</summary>
        TYPE_FUSION = 0x40,
        ///<summary>Ritual</summary>
        TYPE_RITUAL = 0x80,
        ///<summary>Trap Monster</summary>
        TYPE_TRAPMONSTER = 0x100,
        ///<summary>Spirit</summary>
        TYPE_SPIRIT = 0x200,
        ///<summary>Union</summary>
        TYPE_UNION = 0x400,
        ///<summary>Gemini</summary>
        TYPE_DUAL = 0x800,
        ///<summary>Tuner</summary>
        TYPE_TUNER = 0x1000,
        ///<summary>Synchro</summary>
        TYPE_SYNCHRO = 0x2000,
        ///<summary>Token</summary>
        TYPE_TOKEN = 0x4000,

        ///<summary>Quick-Play</summary>
        TYPE_QUICKPLAY = 0x10000,
        ///<summary>Continuous</summary>
        TYPE_CONTINUOUS = 0x20000,
        ///<summary>Equip</summary>
        TYPE_EQUIP = 0x40000,
        ///<summary>Field</summary>
        TYPE_FIELD = 0x80000,
        ///<summary>Counter</summary>
        TYPE_COUNTER = 0x100000,

        ///<summary>Flip</summary>
        TYPE_FLIP = 0x200000,
        ///<summary>Toon</summary>
        TYPE_TOON = 0x400000,
        ///<summary>Xyz</summary>
        TYPE_XYZ = 0x800000,
        ///<summary>Pendulum</summary>
        TYPE_PENDULUM = 0x1000000,
        ///<summary>Special Summon</summary>
        TYPE_SPSUMMON = 0x2000000,
        ///<summary>Link</summary>
        TYPE_LINK = 0x4000000,

    }
    public static class CardTypes
    {
        public static readonly CardType[] TYPE1 = [
            CardType.TYPE_TOKEN,
            CardType.TYPE_LINK,
            CardType.TYPE_RITUAL,
            CardType.TYPE_FUSION,
            CardType.TYPE_XYZ,
            CardType.TYPE_SYNCHRO,
            CardType.TYPE_PENDULUM,

            CardType.TYPE_SPIRIT,
            CardType.TYPE_UNION,
            CardType.TYPE_DUAL,
            CardType.TYPE_FLIP,
            CardType.TYPE_TOON,
        ];
        public static readonly CardType[] TYPE1_10 = [
            CardType.TYPE_TOKEN,
            CardType.TYPE_LINK,
            CardType.TYPE_RITUAL,
            CardType.TYPE_FUSION,
            CardType.TYPE_XYZ,
            CardType.TYPE_SYNCHRO,
            CardType.TYPE_PENDULUM,

            CardType.TYPE_SPIRIT,
            CardType.TYPE_UNION,
            CardType.TYPE_DUAL,
            CardType.TYPE_FLIP,
            CardType.TYPE_TOON,
            CardType.TYPE_SPSUMMON,
        ];
        public static readonly CardType[] TYPE2 = [
            CardType.TYPE_XYZ,
            CardType.TYPE_SYNCHRO,
            CardType.TYPE_PENDULUM,

            CardType.TYPE_SPIRIT,
            CardType.TYPE_UNION,
            CardType.TYPE_DUAL,
            CardType.TYPE_FLIP,
            CardType.TYPE_TOON,
        ];
        public static readonly CardType[] TYPE2_10 = [
            CardType.TYPE_XYZ,
            CardType.TYPE_SYNCHRO,
            CardType.TYPE_PENDULUM,

            CardType.TYPE_SPIRIT,
            CardType.TYPE_UNION,
            CardType.TYPE_DUAL,
            CardType.TYPE_FLIP,
            CardType.TYPE_TOON,
            CardType.TYPE_SPSUMMON,
        ];
        public static readonly CardType[] TYPE3 =[
            CardType.TYPE_SYNCHRO,
            CardType.TYPE_PENDULUM,

            CardType.TYPE_SPIRIT,
            CardType.TYPE_UNION,
            CardType.TYPE_DUAL,
            CardType.TYPE_FLIP,
            CardType.TYPE_TOON,
        ];
        public static readonly CardType[] TYPE3_10 =[
            CardType.TYPE_SYNCHRO,
            CardType.TYPE_PENDULUM,

            CardType.TYPE_SPIRIT,
            CardType.TYPE_UNION,
            CardType.TYPE_DUAL,
            CardType.TYPE_FLIP,
            CardType.TYPE_TOON,
            CardType.TYPE_SPSUMMON,
        ];
        public static readonly CardType[] TYPE4 =[
            CardType.TYPE_TUNER,
            CardType.TYPE_EFFECT,
//			CardType.TYPE_NORMAL,
		];

        public static readonly CardType[] TYPE4_10 =[
            CardType.TYPE_TUNER,
            CardType.TYPE_EFFECT,
            CardType.TYPE_NORMAL,
        ];
        public static CardType[] GetMonsterTypes(long type, bool no10 = false)
        {
            var list = new List<CardType>(5);
            var typeList = new List<CardType[]>(5);
            if (no10)
            {
                typeList.Add(TYPE1_10);
                typeList.Add(TYPE2_10);
                typeList.Add(TYPE3_10);
                typeList.Add(TYPE4_10);
                typeList.Add(TYPE4_10);
            }
            else
            {
                typeList.Add(TYPE1);
                typeList.Add(TYPE2);
                typeList.Add(TYPE3);
                typeList.Add(TYPE4);
                typeList.Add(TYPE4);
            }

            int count = typeList.Count;
            for (int i = 0; i < count; i++)
            {
                CardType[] types = typeList[i];
                foreach (var t in types)
                {
                    if ((type & (long)t) == (long)t)
                    {
                        if (!list.Contains(t))
                        {
                            list.Add(t);
                            break;
                        }
                    }
                }
            }
            return list.ToArray();
        }
    }
}
