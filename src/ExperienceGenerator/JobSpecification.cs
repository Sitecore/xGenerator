using System.Linq;
using Colossus;
using ExperienceGenerator.Parsing;
using Newtonsoft.Json.Linq;

namespace ExperienceGenerator
{
    public class JobSpecification
    {
        public JobType Type { get; set; }
        public string RootUrl { get; set; }

        public int VisitorCount { get; set; }

        public JObject Specification { get; set; }

        public IVisitSimulator CreateSimulator()
        {
            var parser = new XGenParser(RootUrl);
            if (!Specification["Segments"].Any())
                return new ContactBasedSimulator(parser.ParseContacts(Specification["Contacts"], Type));

            var segments = parser.ParseSegments(Specification["Segments"], Type);
            return new SegmentBasedSimulator(segments);
        }
    }
}
