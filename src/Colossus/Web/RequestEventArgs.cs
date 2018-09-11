using System;

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
