using System;

namespace Colossus.Web
{
    public class VisitEventArgs : EventArgs
    {
        public Visit Visit { get; private set; }

        public VisitEventArgs(Visit visit)
        {
            Visit = visit;
        }
    }
}
