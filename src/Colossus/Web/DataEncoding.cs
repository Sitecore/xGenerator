using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Sitecore.Shell.Feeds.Sections;

namespace Colossus.Web
{
    public static class DataEncoding
    {
        public static string RequestDataKey = "X-Colossus-Request";

        public static string ResponseDataKey = "X-Colossus-Response";


        public static int ChunkSize = 8096;

        public static void AddChunked(this NameValueCollection headers, string key, string value)
        {
            var chunks = Enumerable.Range(0, (int) Math.Ceiling(value.Length/(double) ChunkSize))
                .Select(i => value.Substring(i*ChunkSize, Math.Min(value.Length - i*ChunkSize, ChunkSize))).ToArray();
            
            headers.Add(key, ""+chunks.Length);

            var ix = 0;
            foreach (var chunk in chunks)
            {
                headers.Add(key + "-" + ix++, chunk);
            }
        }

        public static string GetChunked(this NameValueCollection headers, string key)
        {
            var chunks = headers[key];
            if (!string.IsNullOrEmpty(chunks))
            {
                var sb = new StringBuilder();
                for (var i = 0; i < int.Parse(chunks); i++)
                {
                    sb.Append(headers[key + "-" + i]);
                }
                return sb.ToString();
            }

            return null;
        }


        private static byte[] Compress(byte[] bytes)
        {
            using (var s = new MemoryStream())
            {
                using (var zipper = new GZipStream(s, CompressionMode.Compress, true))
                {
                    zipper.Write(bytes, 0, bytes.Length);
                }
                return s.ToArray();
            }
        }

        private static byte[] Decompress(byte[] bytes)
        {
            using( var s = new MemoryStream() )
            using (var zipper = new GZipStream(new MemoryStream(bytes), CompressionMode.Decompress))
            {                
                zipper.CopyTo(s);                
                return s.ToArray();
            }
        }

        public static string EncodeHeaderValue<TValue>(TValue value)
        {
            var json = JsonConvert.SerializeObject(value, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });

            return Convert.ToBase64String(Compress(Encoding.UTF8.GetBytes(json)));
        }

        public static TValue DecodeHeaderValue<TValue>(string value)
        {
            var bytes = Convert.FromBase64String(value);            
            var json = Encoding.UTF8.GetString(Decompress(bytes));            
            try
            {
                return JsonConvert.DeserializeObject<TValue>(json,
                    new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    });
            }
            catch (JsonReaderException)
            {
                Console.Out.WriteLine("Invalid JSON: {0}", json);

                throw;
            }
        }
    }
}
