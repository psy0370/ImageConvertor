using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace ImageConvertor
{
    /// <summary>
    /// コーデック情報を表すクラス。
    /// </summary>
    public class CodecInfo
    {
        /// <summary>
        /// コーデック名
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// コーデックに関連する拡張子
        /// </summary>
        public string Extension { get; private set; }
        /// <summary>
        /// コーデックに関連するエンコーダーの型
        /// </summary>
        public Type EncoderType { get; private set; }

        public CodecInfo(Type type)
        {
            var encoder = (BitmapEncoder)Activator.CreateInstance(type);
            Name = encoder.CodecInfo.FriendlyName;
            Extension = encoder.CodecInfo.FileExtensions.Split(',')[0];
            EncoderType = type;
        }
    }

    public class CodecCollection : List<CodecInfo>
    {
        public CodecCollection()
        {
            Add(new CodecInfo(typeof(BmpBitmapEncoder)));
            Add(new CodecInfo(typeof(PngBitmapEncoder)));
            Add(new CodecInfo(typeof(GifBitmapEncoder)));
            Add(new CodecInfo(typeof(JpegBitmapEncoder)));
            Add(new CodecInfo(typeof(TiffBitmapEncoder)));
            Add(new CodecInfo(typeof(WmpBitmapEncoder)));
        }
    }
}
