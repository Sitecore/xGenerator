using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExperienceGenerator.Parsing;

namespace ExperienceGenerator.Data
{
    public static class FileHelpers
    {

        public static IEnumerable<string> ReadLinesFromResource<TAssembly>(string path, bool zipped = false)
        {
            var stream = typeof(TAssembly).Assembly.GetManifestResourceStream(path);
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            if (zipped)
            {
                stream = new GZipStream(stream, CompressionMode.Decompress);
            }

            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    yield return reader.ReadLine();
                }
            }
        }
    }
}
