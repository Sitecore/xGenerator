using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colossus
{
    public abstract class SimulationObject
    {
        public Dictionary<string, object> Variables { get; private set; }

        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        protected SimulationObject()
        {
            Start = DateTime.Now;
            End = DateTime.Now;

            Variables = new Dictionary<string, object>();
        }
    }
}
