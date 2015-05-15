using System;
using System.Collections.Generic;
using System.Linq;

namespace Colossus.Web
{
    public abstract class SimulationObjectInfo
    {
        public IDictionary<string, object> Variables { get; set; }

        public DateTime Start { get; set; }
        public DateTime End { get; set; }




        public void SetValuesFromObject(SimulationObject simObject)
        {            
            Start = simObject.Start;
            End = simObject.End;
            Variables = simObject.Variables.GetSerializableVariables();
        }

    }
}
