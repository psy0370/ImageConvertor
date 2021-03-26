using System.Collections.Generic;
using System.IO;

namespace ImageConvertor
{
    public class FileManager
    {
        /// <summary>
        /// ファイルまたはディレクトリのフルパスの配列から元画像の列挙可能なコレクションを取得します。
        /// </summary>
        /// <param name="entries">ファイルまたはディレクトリのフルパスの配列を設定します。</param>
        /// <returns>取得した元画像のコレクションを返します。</returns>
        public static IEnumerable<SourceImage> GetImageFiles(string[] entries)
        {
            foreach (var entry in entries)
            {
                if (File.GetAttributes(entry) == FileAttributes.Directory)
                {
                    // ディレクトリの場合は再帰的にファイルを取得
                    var subEntries = Directory.GetFileSystemEntries(entry);
                    foreach (var source in GetImageFiles(subEntries))
                    {
                        yield return source;
                    }
                }
                else
                {
                    SourceImage source;

                    try
                    {
                        source = new SourceImage(entry);
                    }
                    catch
                    {
                        continue;
                    }

                    yield return source;
                }
            }
        }
    }
}
