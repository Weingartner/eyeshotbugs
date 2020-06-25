using System.IO;
using System.IO.Compression;
using System.Text;

namespace Weingartner.EyeShot.Assembly3D
{
    /// <summary>
    /// Base class for tests based on IGES dumps
    /// </summary>
    public class GeomTest
    {

        public static byte[] Zip(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            return Zip(bytes);
        }

        public static byte[] Zip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            {
                var mso = new MemoryStream();
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    msi.CopyTo(gs);
                }

                return mso.ToArray();
            }
        }

        public static string Unzip(byte[] bytes)
        {
            using (var mso = new MemoryStream())
            {
                var msi = new MemoryStream(bytes);
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    gs.CopyTo(mso);
                }

                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }
    }
}