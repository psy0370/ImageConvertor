using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace ImageFormatChanger
{
    /// <summary>
    /// 変換元画像の情報を表すクラス
    /// </summary>
    public class SourceImage
    {
        /// <summary>
        /// ファイルネームを取得します。
        /// </summary>
        public string Filename
        {
            get
            {
                return Path.GetFileName(fullPath);
            }
        }
        /// <summary>
        /// ディレクトリを取得します。
        /// </summary>
        public string Directory
        {
            get
            {
                return Path.GetDirectoryName(fullPath);
            }
        }
        /// <summary>
        /// コーデックを取得します。
        /// </summary>
        public string BitsPerPixel
        {
            get
            {
                return bitmap is null ? null : $"{bitmap.Format.BitsPerPixel}bpp";
            }
        }
        /// <summary>
        /// 画像の幅を取得します。
        /// </summary>
        public int? Width
        {
            get
            {
                return bitmap?.PixelWidth;
            }
        }
        /// <summary>
        /// 画像の高さを取得します。
        /// </summary>
        public int? Height
        {
            get
            {
                return bitmap?.PixelHeight;
            }
        }

        private string fullPath;
        private BitmapImage bitmap;

        public SourceImage(string path)
        {            
            fullPath = path;
            bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.None;
            bitmap.UriSource = new Uri(path);
            bitmap.EndInit();
            bitmap.Freeze();
        }
    }
}
