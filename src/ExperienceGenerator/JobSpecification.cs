using System;
using Colossus;
using Colossus.Integration;
using Newtonsoft.Json.Linq;
using ExperienceGenerator.Parsing;

namespace ExperienceGenerator
{
    public class JobSpecification
    {
        public string RootUrl { get; set; }

        public int VisitorCount { get; set; }

        public JObject Specification { get; set; }
                        

        public VisitSimulator CreateSimulator()
        {
            var parser = new XGenParser(RootUrl);
            var segments = parser.ParseSegments(Specification["Segments"]);

            return new VisitSimulator(segments);
        }        
    }
}