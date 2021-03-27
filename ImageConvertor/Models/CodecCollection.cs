using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace ImageConvertor
{
    public class CodecCollection : Dictionary<string, Type>
    {
        public CodecCollection()
        {
            Add("BMP", typeof(BmpBitmapEncoder));
            Add("PNG", typeof(PngBitmapEncoder));
            Add("GIF", typeof(GifBitmapEncoder));
            Add("JPG", typeof(JpegBitmapEncoder));
        }
    }
}
