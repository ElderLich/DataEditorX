/*
 * date :2014-02-07
 * desc: image processing, crop, resize, and save
 */
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace DataEditorX.Common
{
    /// <summary>
    /// Image crop, resize, and high-quality JPEG save helpers
    /// </summary>
    public static class MyBitmap
    {
        public static Bitmap ReadImage(string file)
        {
            if (!File.Exists(file))
            {
                return null;
            }

            MemoryStream ms = new(File.ReadAllBytes(file));
            return (Bitmap)Image.FromStream(ms);
        }

        #region Resize
        /// <summary>
        /// Resize image
        /// </summary>
        /// <param name="img">Source image</param>
        /// <param name="newW">New width</param>
        /// <param name="newH">New height</param>
        /// <returns>Processed image</returns>
        public static Bitmap Zoom(Bitmap sourceBitmap, int newWidth, int newHeight)
        {
            if (sourceBitmap != null)
            {
                Bitmap b = new(newWidth, newHeight);
                Graphics graphics = Graphics.FromImage(b);
                //Compositing: high quality, lower speed
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                //Antialiasing
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                //Pixel offset: high quality, lower speed
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                //Interpolation mode
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                Rectangle newRect = new(0, 0, newWidth, newHeight);
                Rectangle srcRect = new(0, 0, sourceBitmap.Width, sourceBitmap.Height);
                graphics.DrawImage(sourceBitmap, newRect, srcRect, GraphicsUnit.Pixel);
                graphics.Dispose();
                return b;
            }
            return sourceBitmap;
        }
        #endregion

        #region Crop
        /// <summary>
        /// Crop images
        /// </summary>
        /// <param name="sourceBitmap">Source image</param>
        /// <param name="area">Rectangle area</param>
        /// <returns></returns>
        public static Bitmap Cut(Bitmap sourceBitmap, Area area)
        {
            return Cut(sourceBitmap, area.left, area.top, area.width, area.height);
        }
        /// <summary>
        /// Crop image
        /// </summary>
        /// <param name="img">Source image</param>
        /// <param name="StartX">Start X</param>
        /// <param name="StartY">Start Y</param>
        /// <param name="iWidth">Crop width</param>
        /// <param name="iHeight">Crop height</param>
        /// <returns>Processed image</returns>
        public static Bitmap Cut(Bitmap sourceBitmap, int StartX, int StartY, int cutWidth, int cutHeight)
        {
            if (sourceBitmap != null)
            {
                int w = sourceBitmap.Width;
                int h = sourceBitmap.Height;
                //Adjust crop rectangle width
                if ((StartX + cutWidth) > w)
                {
                    cutWidth = w - StartX;
                }
                //Adjust crop rectangle height
                if ((StartY + cutHeight) > h)
                {
                    cutHeight = h - StartY;
                }
                Bitmap bitmap = new(cutWidth, cutHeight);
                Graphics graphics = Graphics.FromImage(bitmap);
                //Compositing: high quality, lower speed
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                //Antialiasing
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                //Pixel offset: high quality, lower speed
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                //Interpolation mode
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                Rectangle cutRect = new(0, 0, cutWidth, cutHeight);
                Rectangle srcRect = new(StartX, StartY, cutWidth, cutHeight);
                graphics.DrawImage(sourceBitmap, cutRect, srcRect, GraphicsUnit.Pixel);
                graphics.Dispose();
                return bitmap;
            }
            return sourceBitmap;
        }
        #endregion

        #region Save
        /// <summary>
        /// Save JPEG image
        /// </summary>
        /// <param name="bmp">Source image</param>
        /// <param name="filename">Destination path</param>
        /// <param name="quality">Quality</param>
        /// <returns>Check whether save succeeded</returns>
        public static bool SaveAsJPEG(Bitmap bitmap, string filename, int quality = 90)
        {
            if (bitmap != null)
            {
                string path = Path.GetDirectoryName(filename);
                if (!Directory.Exists(path))//Create directory
                {
                    _ = Directory.CreateDirectory(path);
                }

                if (File.Exists(filename))//Delete old file
                {
                    File.Delete(filename);
                }

                ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
                ImageCodecInfo ici = null;
                foreach (ImageCodecInfo codec in codecs)
                {
                    if (codec.MimeType.IndexOf("jpeg") > -1)
                    {
                        ici = codec;
                        break;
                    }
                }
                if (quality < 0 || quality > 100)
                {
                    quality = 60;
                }

                EncoderParameters encoderParams = new();
                encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);
                if (ici != null)
                {
                    bitmap.Save(filename, ici, encoderParams);
                    bitmap.Dispose();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }
        #endregion      
    }

}

