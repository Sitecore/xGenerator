using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Colossus.Statistics;

namespace Colossus
{
    public class VisitorSegment
    {
        public string Name { get; set; }

        public List<IVisitorVariables> VisitorVariables { get; set; }

        public List<IVisitorVariables> VisitVariables { get; set; }

        public List<IVisitorVariables> RequestVariables { get; set; }

        public DateGenerator DateGenerator { get; set; }

        public Func<IVisitorBehavior> Behavior { get; set; }
        

        public VisitorSegment(string name)
        {
            Name = name;
            VisitorVariables = new List<IVisitorVariables>();     
            VisitVariables = new List<IVisitorVariables>();
            RequestVariables = new List<IVisitorVariables>();
            DateGenerator = new DateGenerator();
        }
               

        public void SortVariables()
        {
            VisitorVariables = VisitorVariables.TopologicalSort(
                (x, other) => other.Any(x.DependsOn));
            VisitVariables = VisitVariables.TopologicalSort(
                (x, other) => other.Any(x.DependsOn));
            RequestVariables = RequestVariables.TopologicalSort(
                (x, other) => other.Any(x.DependsOn));
        }
    }
}
