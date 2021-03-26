using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace ImageConvertor
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
        /// 画像の高さを取得します。
        /// </summary>
        public string Information { get; private set; }
        /// <summary>
        /// パレットの有無を取得します。
        /// </summary>
        public bool HasPalette { get; private set; }

        private readonly string fullPath;

        public SourceImage(string path)
        {
            fullPath = path;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.None;
            bitmap.UriSource = new Uri(fullPath);
            bitmap.EndInit();
            bitmap.Freeze();

            Information = $"{bitmap.PixelWidth}x{bitmap.PixelHeight} / {bitmap.Format.BitsPerPixel}bpp";
            HasPalette = !(bitmap.Palette is null);
        }
    }
}
