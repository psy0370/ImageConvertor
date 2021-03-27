using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.ComponentModel;

namespace ImageConvertor
{
    /// <summary>
    /// 変換元画像の情報を表すクラス
    /// </summary>
    public class SourceImage : INotifyPropertyChanged
    {
        /// <summary>
        /// プロパティ変更通知のイベントハンドラ
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

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
        /// <summary>
        /// 処理済みかどうかを取得します。
        /// </summary>
        public bool IsProcessed
        {
            get
            {
                return isProcessed;
            }
            private set
            {
                if (value != isProcessed)
                {
                    isProcessed = value;
                    OnPropertyChanged("IsProcessed");
                }
            }
        }

        private readonly string fullPath;
        private bool isProcessed;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public SourceImage(string path)
        {
            fullPath = path;
            var bitmap = LoadImage();
            Information = $"{bitmap.PixelWidth}x{bitmap.PixelHeight} / {bitmap.Format.BitsPerPixel}bpp";
            HasPalette = !(bitmap.Palette is null);
        }

        /// <summary>
        /// 指定されたエンコーダーで画像を保存します。
        /// </summary>
        /// <param name="encoder">エンコーダーを設定します。</param>
        /// <param name="path">保存するパスを設定します。</param>
        public void Save(BitmapEncoder encoder, string directory, bool removeSource, bool trim, TrimType type, bool line200, bool color8)
        {
            if (!IsProcessed)
            {
                var filename = $"{Path.GetFileNameWithoutExtension(Filename)}{encoder.CodecInfo.FileExtensions.Split(',')[0]}";
                var path = Path.Combine(directory ?? Directory, filename);

                var bitmap = LoadImage();
                BitmapSource pBitmap;

                if (trim == true || line200 == true || color8 == true)
                {
                    pBitmap = ProcessImage(bitmap, trim == true, type, line200 == true, color8 == true);
                }
                else
                {
                    pBitmap = bitmap;
                }

                var tempEncoder = (BitmapEncoder)Activator.CreateInstance(encoder.GetType());
                tempEncoder.Frames.Add(BitmapFrame.Create(pBitmap));

                using (var stream = File.OpenWrite(path))
                {
                    tempEncoder.Save(stream);
                    stream.Close();
                }

                // 元画像削除オンで出力したファイル名が異なる場合は削除
                if (removeSource && Filename != filename)
                {
                    File.Delete(fullPath);
                }

                IsProcessed = true;
            }
        }

        /// <summary>
        /// ファイルから画像を読み取ります。
        /// </summary>
        /// <returns>読み取った画像を返します。</returns>
        private BitmapImage LoadImage()
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.None;
            bitmap.UriSource = new Uri(fullPath);
            bitmap.EndInit();
            bitmap.Freeze();

            return bitmap;
        }

        /// <summary>
        /// 元画像を指定の方法で加工した新しい画像を取得します。
        /// </summary>
        /// <param name="bitmap">元画像を設定します。</param>
        /// <param name="trim">トリミングするかどうかを設定します。</param>
        /// <param name="type">トリミングの基準となる位置を設定します。</param>
        /// <param name="line200">200ライン画像とみなすかを設定します。</param>
        /// <param name="color8">8色画像とみなすかを設定します。</param>
        /// <returns>加工した新しい画像を返します。</returns>
        private BitmapSource ProcessImage(BitmapImage bitmap, bool trim, TrimType type, bool line200, bool color8)
        {
            var wBmp = new WriteableBitmap(bitmap);
            wBmp.Lock();

            if (trim && bitmap.Format.BitsPerPixel == 4 && HasPalette)
            {
                // 4bitインデックスカラーかつトリミングがオンの場合に処理
                unsafe
                {
                    int sx, sy, ex, ey, oColorIndex;
                    var ptr = (byte*)wBmp.BackBuffer;
                    var width = bitmap.PixelWidth;
                    var height = bitmap.PixelHeight;
                    var stride = (bitmap.PixelWidth * bitmap.Format.BitsPerPixel + 7) / 8;

                    // 左上 or 右下 のパレットを取得
                    byte* oPtr = ptr + (type == TrimType.LeftTop ? 0 : (bitmap.PixelHeight - 1) * stride + (width - 1) / 2);

                    // トリミングするカラーインデックスを取得
                    if ((width - 1) % 2 == 0)
                    {
                        oColorIndex = *oPtr >> 4;
                    }
                    else
                    {
                        oColorIndex = *oPtr & 0x0f;
                    }

                    // 上端取得
                    for (sy = 0; sy < height; sy++)
                    {
                        var check = false;
                        for (var x = 0; x < width; x++)
                        {
                            byte* pixel = ptr + sy * stride + x / 2;
                            var dColorIndex = (x % 2 == 0) ? *pixel >> 4 : *pixel & 0x0f;
                            if (dColorIndex != oColorIndex)
                            {
                                check = true;
                                break;
                            }
                        }

                        if (check) break;
                    }

                    // 下端取得
                    for (ey = height - 1; ey >= 0; ey--)
                    {
                        var check = false;
                        for (var x = 0; x < width; x++)
                        {
                            byte* pixel = ptr + ey * stride + x / 2;
                            var dColorIndex = (x % 2 == 0) ? *pixel >> 4 : *pixel & 0x0f;
                            if (dColorIndex != oColorIndex)
                            {
                                check = true;
                                break;
                            }
                        }

                        if (check) break;
                    }

                    // 左端取得
                    for (sx = 0; sx < width; sx++)
                    {
                        var check = false;
                        for (var y = 0; y < height; y++)
                        {
                            byte* pixel = ptr + y * stride + sx / 2;
                            int dColorIndex = (sx % 2 == 0) ? *pixel >> 4 : *pixel & 0x0f;
                            if (dColorIndex != oColorIndex)
                            {
                                check = true;
                                break;
                            }
                        }

                        if (check) break;
                    }

                    // 右端取得
                    for (ex = width - 1; ex >= 0; ex--)
                    {
                        var check = false;
                        for (var y = 0; y < height; y++)
                        {
                            byte* pixel = ptr + y * stride + ex / 2;
                            int dColorIndex = (ex % 2 == 0) ? *pixel >> 4 : *pixel & 0x0f;
                            if (dColorIndex != oColorIndex)
                            {
                                check = true;
                                break;
                            }
                        }

                        if (check) break;
                    }

                    if (sx <= ex || sy <= ey)
                    {
                        return new CroppedBitmap(bitmap, new Int32Rect(sx, sy, ex - sx + 1, ey - sy + 1)) as BitmapSource;
                    }
                }
            }

            return bitmap;
        }
    }
}
