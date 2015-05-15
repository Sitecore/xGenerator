using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Colossus.Statistics;

namespace Colossus
{
    public class VisitSimulator
    {
        private readonly Func<VisitorSegment> _segments;

        public VisitSimulator(VisitorSegment segment)
        {
            _segments = () => segment;
        }

        public VisitSimulator(Action<WeightedSetBuilder<VisitorSegment>> segments)
            : this(Sets.Weighted(segments))
        {            
        }


        public VisitSimulator(Func<VisitorSegment> segments)
        {
            _segments = segments;
        }

        public Visitor NextVisitor()
        {
            var segment = _segments();

            var visitor = new Visitor(segment) {Start = segment.DateGenerator.NextDate()};
            visitor.End = visitor.Start;

            foreach (var var in segment.VisitorVariables)
            {
                var.SetValues(visitor);
            }            

            return visitor;
        }

        public IEnumerable<Visitor> NextVisitors(int count, bool sort = true)
        {
            var visitors = Enumerable.Range(0, count).Select(i => NextVisitor());
            if (sort)
            {
                visitors = visitors.OrderBy(v => v.Start);
            }
            return visitors;
        }
        


        public static VisitSimulator RoundRobin(params VisitorSegment[] segments)
        {
            var index = 0;
            return new VisitSimulator(()=>segments[index++ % segments.Length]);
        }
    }
}
