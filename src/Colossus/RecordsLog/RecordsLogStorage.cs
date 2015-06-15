using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colossus.RecordsLog
{
    public abstract class RecordsLogStorage
    {
        public abstract IEnumerable<RequestLogRecord> GetRecordedRecords();
    }
}
