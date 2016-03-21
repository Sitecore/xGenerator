namespace Colossus
{
  using System;
  using System.Collections.Generic;
  using System.Linq;

  public class ContactBasedSimulator : IVisitSimulator
  {
    private IEnumerable<VisitorSegment> VisitorSegment;

    public ContactBasedSimulator(IEnumerable<VisitorSegment> segments)
    {
      this.VisitorSegment = segments;
    }

    public Visitor NextVisitor()
    {
      return null;
    }

    public IEnumerable<Visitor> NextVisitors(int count, bool sort = true)
    {

      return this.VisitorSegment.Select(x => new Visitor(x) { Start = x.DateGenerator?.Start ?? DateTime.Now });
    }

  }
}