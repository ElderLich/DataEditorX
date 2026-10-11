
namespace DataEditorX.Core.Info
{
    public enum CardRule : int
    {
        /// <summary>None</summary>
        NONE = 0,
        /// <summary>OCG</summary>
        OCG = 1,
        /// <summary>TCG</summary>
        TCG = 2,
        /// <summary>OT</summary>
        OCGTCG = 3,
        /// <summary>DIY/original cards</summary>
        DIY = 4,
        /// <summary>Editor Rush filter; MDPro3 identifies Rush by the card ID range.</summary>
        RUSH = 0x100,
        /// <summary>Simplified Chinese</summary>
        CCG = 9,
        /// <summary>Simplified Chinese/TCG</summary>
        CCGTCG = 0xb,
    }
}
