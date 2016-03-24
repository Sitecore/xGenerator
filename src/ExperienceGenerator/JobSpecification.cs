using System;
using Colossus;
using Colossus.Integration;
using Newtonsoft.Json.Linq;
using ExperienceGenerator.Parsing;

namespace ExperienceGenerator
{
  using System.Linq;

  public class JobSpecification
  {
    public JobType Type { get; set; }
    public string RootUrl { get; set; }

    public int VisitorCount { get; set; }

    public JObject Specification { get; set; }


    public IVisitSimulator CreateSimulator()
    {
      var parser = new XGenParser(RootUrl);
      if (Specification["Segments"].Any())
      {
        var segments = parser.ParseSegments(Specification["Segments"], Type);
        return new SegmentBasedSimulator(segments);
      }
     return new  ContactBasedSimulator(parser.ParseContacts(Specification["Contacts"],Type));

    }
  }
}