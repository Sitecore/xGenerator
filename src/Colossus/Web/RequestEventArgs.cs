using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colossus.Web
{
    public class RequestEventArgs : EventArgs
    {
        public Request Request { get; private set; }

        public RequestEventArgs(Request request)
        {
            Request = request;
        }
    }
}
