using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Colossus.RecordsLog
{
    public class RecordsLogFileStorage : RecordsLogStorage
    {
        private readonly string storagefileName;

        public RecordsLogFileStorage(string fname)
        {
            storagefileName = fname;
        }

        public override IEnumerable<RequestLogRecord> GetRecordedRecords()
        {
            using (StreamReader sr = new StreamReader(File.OpenRead(storagefileName)))
            {
                while (sr.Peek() >= 0)
                {
                    var logRecord = sr.ReadLine();
                    var json = JsonConvert.DeserializeObject(logRecord, new JsonSerializerSettings
                    {
                        TypeNameHandling = TypeNameHandling.All,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    });
                    yield return (RequestLogRecord) json;
                }
            }
        }
    }
}
