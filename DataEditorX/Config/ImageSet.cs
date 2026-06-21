/*
 * Created with SharpDevelop.
 * User: Acer
 * Date: 2014-10-13
 * Time: 9:02
 * 
 */
using DataEditorX.Common;

namespace DataEditorX.Config
{
    /// <summary>
    /// Image crop configuration
    /// </summary>
	public class ImageSet
    {
        public ImageSet()
        {
            Init();
        }
        // Initialize defaults.
        void Init()
        {
            int[] ints = DEXConfig.ReadIntegers(DEXConfig.TAG_IMAGE_SIZE, 4);

            w = ints[0];
            h = ints[1];
            W = ints[2];
            H = ints[3];

            quality = DEXConfig.ReadInteger(DEXConfig.TAG_IMAGE_QUALITY, 95);
        }
        /// <summary>
        /// jpegQuality
        /// </summary>
		public int quality;
        /// <summary>
        /// Thumbnail width
        /// </summary>
        public int w;
        /// <summary>
        /// Thumbnail height
        /// </summary>
        public int h;
        /// <summary>
        /// Full image width
        /// </summary>
        public int W;
        /// <summary>
        /// Full image height
        /// </summary>
        public int H;
    }
}
