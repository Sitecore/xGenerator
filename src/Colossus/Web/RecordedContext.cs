using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Colossus.RecordsLog;

namespace Colossus.Web
{
    /// <summary>
    /// Owns recorded request parameters
    /// </summary>
    public class RecordedContext
    {
        private readonly RecordsLogStorage recordsStorage;

        public RecordedContext(RecordsLogStorage storage)
        {
            recordsStorage = storage;
        }
        
        public void Process()
        {
            RecordedWebClient wc = new RecordedWebClient();
            foreach (var requestLogRecord in recordsStorage.GetRecordedRecords())
            {
                if (requestLogRecord.Info.EndVisit)
                {
                    wc = new RecordedWebClient();
                }
                wc.Context = requestLogRecord;
                wc.DownloadString(requestLogRecord.Url);
            }
            
        }
    }
}
