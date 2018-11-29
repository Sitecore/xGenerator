using System;
using System.Collections.Generic;
using System.Linq;

namespace Colossus
{
    public class ContactBasedSimulator : IVisitSimulator
    {
        private readonly IEnumerable<VisitorSegment> _visitorSegment;

        public ContactBasedSimulator(IEnumerable<VisitorSegment> segments)
        {
            _visitorSegment = segments;
        }

        public IEnumerable<Visitor> GetVisitors(int count)
        {
            return _visitorSegment.Select(visitorSegment => new Visitor(visitorSegment)
              {
                  Start = visitorSegment.DateGenerator?.Start ?? DateTime.Now
              });
        }
    }
}
