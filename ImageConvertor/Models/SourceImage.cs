using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
        /// 色深度を取得します。
        /// </summary>
        public int BitsPerPixel { get; private set; }
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
            BitsPerPixel = bitmap.Format.BitsPerPixel;
            HasPalette = !(bitmap.Palette is null);
        }

        /// <summary>
        /// 指定されたエンコーダーで画像を保存します。
        /// </summary>
        /// <param name="codec">エンコードするためのコーデック情報を設定します。</param>
        /// <param name="directory">保存するパスを設定します。</param>
        /// <param name="removeSource">元画像を削除するかどうかを設定します。</param>
        /// <param name="trimming">トリミングするかどうかを設定します。</param>
        /// <param name="trimmingType">トリミングの基準となる位置を設定します。</param>
        /// <param name="line200">200ライン画像とみなすかを設定します。</param>
        /// <param name="color8">8色画像とみなすかを設定します。</param>
        public SaveResult Save(CodecInfo codec, string directory, bool removeSource, bool trimming, TrimType trimmingType, bool line200, bool color8)
        {
            if (!IsProcessed)
            {
                var filename = $"{Path.GetFileNameWithoutExtension(Filename)}{codec.Extension}";
                var path = Path.Combine(directory ?? Directory, filename);

                var bitmap = LoadImage();
                var pBitmap = ProcessImage(bitmap, trimming, trimmingType, line200, color8);
                var encoder = (BitmapEncoder)Activator.CreateInstance(codec.EncoderType);
                encoder.Frames.Add(BitmapFrame.Create(pBitmap));

                using (var stream = File.OpenWrite(path))
                {
                    encoder.Save(stream);
                    stream.Close();
                }

                // 元画像削除オンで出力したファイル名が異なる場合は削除
                if (removeSource && Filename != filename)
                {
                    File.Delete(fullPath);
                }

                IsProcessed = true;
                return SaveResult.Processed;
            }

            return SaveResult.Skipped;
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
        /// <param name="trimming">トリミングするかどうかを設定します。</param>
        /// <param name="trimmingType">トリミングの基準となる位置を設定します。</param>
        /// <param name="line200">200ライン画像とみなすかを設定します。</param>
        /// <param name="color8">8色画像とみなすかを設定します。</param>
        /// <returns>加工した新しい画像を返します。</returns>
        private BitmapSource ProcessImage(BitmapImage bitmap, bool trimming, TrimType trimmingType, bool line200, bool color8)
        {
            if(HasPalette && BitsPerPixel == 8 && CountUsedColor(bitmap) <= 16)
            {
                var wBmp = ShrinkBitsPerPixel(bitmap);

                if (trimming)
                {
                    // トリミング
                    unsafe
                    {
                        wBmp.Lock();

                        int sx, sy, ex, ey, oColorIndex;
                        var width = bitmap.PixelWidth;
                        var height = bitmap.PixelHeight;
                        var ptr = (byte*)wBmp.BackBuffer;

                        // 左上 or 右下 のパレットを取得
                        byte* oPtr = ptr + (trimmingType == TrimType.LeftTop ? 0 : (bitmap.PixelHeight - 1) * wBmp.BackBufferStride + (width - 1) / 2);

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
                                byte* pixel = ptr + sy * wBmp.BackBufferStride + x / 2;
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
                                byte* pixel = ptr + ey * wBmp.BackBufferStride + x / 2;
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
                                byte* pixel = ptr + y * wBmp.BackBufferStride + sx / 2;
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
                                byte* pixel = ptr + y * wBmp.BackBufferStride + ex / 2;
                                int dColorIndex = (ex % 2 == 0) ? *pixel >> 4 : *pixel & 0x0f;
                                if (dColorIndex != oColorIndex)
                                {
                                    check = true;
                                    break;
                                }
                            }

                            if (check) break;
                        }

                        wBmp.Unlock();

                        if (sx <= ex || sy <= ey)
                        {
                            return new CroppedBitmap(wBmp, new Int32Rect(sx, sy, ex - sx + 1, ey - sy + 1)) as BitmapSource;
                        }
                    }
                }

                return wBmp;
            }

            return bitmap;
        }

        /// <summary>
        /// パレット保持画像の色数をカウントします。
        /// </summary>
        /// <param name="bitmap">色数をカウントする画像を設定します。</param>
        /// <returns>色数を返します。</returns>
        private int CountUsedColor(BitmapImage bitmap)
        {
            var palettes = new bool[256];
            for (var i = 0; i < 256; i++)
            {
                palettes[i] = false;
            }

            var wBmp = new WriteableBitmap(bitmap);
            wBmp.Lock();

            unsafe
            {
                var width = bitmap.PixelWidth;
                var height = bitmap.PixelHeight;
                var ptr = (byte*)wBmp.BackBuffer;

                if (BitsPerPixel == 4)
                {
                    for (var y = 0; y < height; y++)
                    {
                        for (var x = 0; x < width; x++)
                        {
                            byte* pixel = ptr + y * wBmp.BackBufferStride + x / 2;
                            if (x % 2 == 0)
                            {
                                palettes[(uint)*pixel >> 4] = true;
                            }
                            else
                            {
                                palettes[*pixel & 0x0f] = true;
                            }
                        }
                    }
                }
                else
                {
                    for (var y = 0; y < height; y++)
                    {
                        for (var x = 0; x < width; x++)
                        {
                            byte* pixel = ptr + y * wBmp.BackBufferStride + x;
                            palettes[*pixel] = true;
                        }
                    }
                }
            }

            wBmp.Unlock();

            var count = 0;
            foreach (var palette in palettes)
            {
                if (palette) count++;
            }

            return count;
        }

        /// <summary>
        /// パレット保持画像を8bppから4bppへ縮小します。
        /// </summary>
        /// <param name="sBmp">8bppの画像を設定します。</param>
        /// <returns>縮小した画像を返します。</returns>
        private WriteableBitmap ShrinkBitsPerPixel(BitmapSource bitmap)
        {
            // パレット取得
            var colors = new Color[16];
            for (var i = 0; i < 16; i++)
            {
                colors[i] = bitmap.Palette.Colors[i];
            }
            var palette = new BitmapPalette(colors);

            // 画像準備
            var sBmp = new WriteableBitmap(bitmap);
            var dBmp = new WriteableBitmap(bitmap.PixelWidth, bitmap.PixelHeight, 72, 72, PixelFormats.Indexed4, palette);
            sBmp.Lock();
            dBmp.Lock();

            unsafe
            {
                var width = bitmap.PixelWidth;
                var height = bitmap.PixelHeight;
                var sPtr = (byte*)sBmp.BackBuffer;
                var dPtr = (byte*)dBmp.BackBuffer;

                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        byte* sPixel = sPtr + y * sBmp.BackBufferStride + x;
                        byte* dPixel = dPtr + y * dBmp.BackBufferStride + x / 2;

                        if (x % 2 == 0)
                        {
                            *dPixel = (byte)((uint)*sPixel << 4);
                        }
                        else
                        {
                            *dPixel |= *sPixel;
                        }
                    }
                }
            }

            sBmp.Unlock();
            dBmp.Unlock();
            return dBmp;
        }
    }
}
