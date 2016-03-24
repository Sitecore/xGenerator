using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Colossus.Statistics;

namespace Colossus
{
  public class Visitor : SimulationObject
  {
    public VisitorSegment Segment { get; set; }

    public List<Visit> Visits { get; set; }

    public Visitor(VisitorSegment segment)
    {
      Segment = segment;
      Visits = new List<Visit>();
    }

    public Visit AddVisit(TimeSpan? pause = null)
    {
      var lastVisit = Visits.Count > 0 ? Visits[Visits.Count - 1].End : Start;
      lastVisit += pause ?? TimeSpan.Zero;
      var visit = new Visit { Visitor = this, Start = lastVisit, End = lastVisit };

      Visits.Add(visit);
      End = visit.End;

      if (Segment != null)
      {
        foreach (var v in Segment.VisitVariables)
        {
          v.SetValues(visit);
        }
        foreach (var v in Segment.VisitorVariables)
        {
          v.SetValues(visit.Visitor);
        }
      }

      return visit;
    }


    public IEnumerable<Visit> Commit()
    {
      var behavior = Segment.Behavior();
      return behavior == null ? Enumerable.Empty<Visit>() : behavior.Commit(this);
    }
  }
}
