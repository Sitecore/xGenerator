using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Colossus.RecordsLog;
using Sitecore.Text;

namespace Colossus.Web
{
    /// <summary>
    /// Owns recorded request parameters
    /// </summary>
    public class RecordedContext
    {
        private readonly RecordsLogStorage recordsStorage;
        private readonly string hostName;

        public RecordedContext(RecordsLogStorage storage, string host)
        {
            recordsStorage = storage;
            hostName = host;
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
                var url = new Uri(requestLogRecord.Url);
                var requestUrl = requestLogRecord.Url.Replace(url.Host, hostName);
                wc.DownloadString(requestUrl);
            }
            
        }
    }
}
