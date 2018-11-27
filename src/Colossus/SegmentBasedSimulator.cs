using System;
using System.Collections.Generic;
using Colossus.Statistics;

namespace Colossus
{
    public class SegmentBasedSimulator : IVisitSimulator
    {
        private readonly Func<VisitorSegment> _segments;

        public SegmentBasedSimulator(VisitorSegment segment)
        {
            _segments = () => segment;
        }

        public SegmentBasedSimulator(Action<WeightedSetBuilder<VisitorSegment>> segments) : this(Sets.Weighted(segments))
        {
        }

        public SegmentBasedSimulator(Func<VisitorSegment> segments)
        {
            _segments = segments;
        }

        public Visitor GetNextVisitor()
        {
            var segment = _segments();

            var visitor = new Visitor(segment)
                          {
                              Start = segment.DateGenerator.NextDate()
                          };
            visitor.End = visitor.Start;

            foreach (var var in segment.VisitorVariables)
            {
                var.SetValues(visitor);
            }

            return visitor;
        }

        public IEnumerable<Visitor> GetVisitors(int count)
        {
            for (var i = 0; i < count; i++)
            {
                yield return GetNextVisitor();
            }
        }

        public static SegmentBasedSimulator RoundRobin(params VisitorSegment[] segments)
        {
            var index = 0;
            return new SegmentBasedSimulator(() => segments[index++%segments.Length]);
        }
    }
}
