using System;
using System.Collections.Generic;

namespace Colossus.Integration.Processing
{
    public class TriggerOutcomeData
    {
        public Guid DefinitionId { get; set; }

        public decimal MonetaryValue { get; set; }

        public DateTime? DateTime { get; set; }

        public Dictionary<string, string> CustomValues { get; set; }
    }
}
