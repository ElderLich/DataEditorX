/*
 * Created with SharpDevelop.
 * User: Administrator
 * Date: 2017/5/11
 * Time: 16:14
 * 
 * Legacy SharpDevelop template note.
 */

namespace DataEditorX.Core.Info
{
    public static class CardLink
    {
        /*
				0x1	↙
0x2	↓
0x4	↘
0x8	←
0x20	→
0x40	↖
0x80	↑
0x100	↗*/
        public const int DownLeft = 0x1;
        public const int Down = 0x2;
        public const int DownRight = 0x4;
        public const int Left = 0x8;
        public const int Right = 0x20;
        public const int UpLeft = 0x40;
        public const int Up = 0x80;
        public const int UpRight = 0x100;

        public static bool IsLink(int marks, int mark)
        {
            return (marks & mark) == mark;
        }
    }
}
