using System;
using System.Collections.Generic;
using System.Linq;
using Colossus;
using Colossus.Integration.Processing;

namespace ExperienceGenerator.Parsing.Factories
{
    public class OutcomeVariable : VisitorVariableBase
    {
        public Func<ISet<string>> Outcomes { get; set; }
        public Func<double> ValueDistribution { get; set; }

        public OutcomeVariable(Func<ISet<string>> outcomes, Func<double> valueDistribution)
        {
            Outcomes = outcomes;
            ValueDistribution = valueDistribution;
        }

        public override void SetValues(SimulationObject target)
        {
            var outcomes = Outcomes();

            if (outcomes.Any())
            {
                target.Variables[VariableKey.TriggerOutcomes] = outcomes.Select(oc => new TriggerOutcomeData
                                                                                      {
                                                                                          DefinitionId = Guid.Parse(oc),
                                                                                          MonetaryValue = (decimal) ValueDistribution()
                                                                                      }).ToList();
            }
        }

        public override IEnumerable<VariableKey> ProvidedVariables => new[] { VariableKey.TriggerOutcomes };
    }
}
