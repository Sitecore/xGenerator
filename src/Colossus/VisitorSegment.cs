using System;
using System.Collections.Generic;
using System.Linq;
using Colossus.Statistics;

namespace Colossus
{
    public class VisitorSegment
    {
        public string Name { get; set; }

        public List<IVisitorVariable> VisitorVariables { get; set; }

        public List<IVisitorVariable> VisitVariables { get; set; }

        public List<IVisitorVariable> RequestVariables { get; set; }

        public DateGenerator DateGenerator { get; set; }

        public Func<IVisitorBehavior> Behavior { get; set; }

        public VisitorSegment(string name)
        {
            Name = name;
            VisitorVariables = new List<IVisitorVariable>();
            VisitVariables = new List<IVisitorVariable>();
            RequestVariables = new List<IVisitorVariable>();
            DateGenerator = new DateGenerator();
        }


        public void SortVariables()
        {
            VisitorVariables = VisitorVariables.TopologicalSort((x, other) => other.Any(x.DependsOn));
            VisitVariables = VisitVariables.TopologicalSort((x, other) => other.Any(x.DependsOn));
            RequestVariables = RequestVariables.TopologicalSort((x, other) => other.Any(x.DependsOn));
        }
    }
}
