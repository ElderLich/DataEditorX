
namespace DataEditorX.Core.Info
{
    public enum CardRule : int
    {
        /// <summary>无</summary>
        NONE = 0,
        /// <summary>OCG</summary>
        OCG = 1,
        /// <summary>TCG</summary>
        TCG = 2,
        /// <summary>OT</summary>
        OCGTCG = 3,
        /// <summary>DIY,原创卡</summary>
        DIY = 4,
        /// <summary>简体中文</summary>
        CCG = 9,
        /// <summary>简体中文/TCG</summary>
        CCGTCG = 0xb,
    }
}
