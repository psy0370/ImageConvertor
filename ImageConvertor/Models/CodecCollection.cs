using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace ImageConvertor
{
    public class CodecCollection : List<BitmapEncoder>
    {
        public CodecCollection()
        {
            Add(new BmpBitmapEncoder());
            Add(new PngBitmapEncoder());
            Add(new GifBitmapEncoder());
            Add(new JpegBitmapEncoder());
        }
    }
}
