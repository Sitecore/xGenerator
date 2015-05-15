using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
