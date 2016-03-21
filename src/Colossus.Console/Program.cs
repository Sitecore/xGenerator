using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Colossus.Integration;
using Colossus.Integration.Processing;
using Colossus.Statistics;
using Colossus.Web;
using ExperienceGenerator;
using ExperienceGenerator.Data;
using ExperienceGenerator.Parsing;
using MathNet.Numerics.Distributions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sitecore.Analytics.Model;
using Sitecore.Common;
using Sitecore.Jobs;
using Sitecore.Pipelines.LoadHtml;
using Sitecore.Shell.Framework.Commands.Masters;
using Sitecore.Text;
using Sitecore.Web.UI;
using Sitecore.Workflows.Simple;

namespace Colossus.Console
{
    internal class Program
    {

        public static void Outcomes()
        {

                        

            var segments = new JObject();            
            var seg = segments["Default"] = new JObject();
            var outcomes = seg["Outcomes"] = new JObject();


            seg["StartDate"] = "2010-01-01";
            seg["EndDate"] = "2010-01-02";
            outcomes["{75D53206-47B3-4391-BD48-75C42E5FC2CE}"] = .5;
            outcomes["{F4830B80-1BB1-4746-89C7-96EFE40DA572}"] = .5;


            var parser = new XGenParser("http://sc80rev150427");


            var sim = new SegmentBasedSimulator(parser.ParseSegments(segments, JobType.Visits));


            var counts = new Dictionary<Guid, int>();

            var visits = 0;
            foreach (var visitor in sim.NextVisitors(1))
            {
                foreach (var visit in visitor.Commit())
                {
                    ++visits;
                    foreach (var req in visit.Requests)
                    {
                        var oc = req.GetVariable<IEnumerable<TriggerOutcomeData>>("TriggerOutcomes");
                        if (oc != null)
                        {
                            foreach (var o in oc)
                            {
                                //System.Console.Out.WriteLine(o.DefinitionId);
                                counts[o.DefinitionId] = counts.GetOrDefault(o.DefinitionId) + 1;
                            }
                        }
                    }
                }
            }

            System.Console.Out.WriteLine("{0} visits", visits);
            foreach (var c in counts)
            {
                System.Console.Out.WriteLine("{0}: {1}", c.Key, c.Value);
            }


        }


        public static void Main(string[] args)
        {


            Outcomes();
            return;



            //Parser2();
            //return;

            //XeroxParser.Configure();

            var json = File.ReadAllText(@"C:\Temp\Xerox.js");            
            var root = JsonConvert.DeserializeObject<JObject>(json);


            //var p = new XeroxParser();

            //p.ParseSegments(root["Segments"]);

            //return;
            //var gen = p.ParseDateGenerator(root["DateTest"]);

            var spec = new JobSpecification {VisitorCount = 100000, Specification = root};
            
            using (var tmp = File.CreateText(@"C:\Temp\XeroxDates.txt"))
            {
                tmp.WriteLine("Date\tCount\tPct");
                                
                var visits = spec.CreateSimulator().NextVisitors(spec.VisitorCount).ToArray();


                var abs = visits.GroupBy(v => v.GetVariable<string>("Test"));
                foreach (var g in abs.OrderBy(g => g.Key))
                {
                    System.Console.Out.WriteLine("{0}: {1:P2}", g.Key, g.Count()/(double)spec.VisitorCount);
                }

                var visitCounts = visits.GroupBy(v => v.Start.Date)
                    .ToDictionary(g => g.Key, g => g.Count());

                

                var start = visitCounts.Keys.Min();
                var end = visitCounts.Keys.Max();
                var current = start;
                while (current <= end)
                {
                    int c;
                    c = visitCounts.TryGetValue(current, out c) ? c : 0;
                    tmp.WriteLine("{0}\t{1}\t{2:P2}", current, c, c/(double) spec.VisitorCount);
                    current = current.AddDays(1);
                }
            }
        }


        
        
        private static void Main2(string[] args)
        {
            Randomness.Seed(1337);
            

            var segment = new VisitorSegment("Test");

            segment.VisitorVariables.Add(Variables.Fixed("Country", "DK"));

            //Simulate visitors from Jan 1 2012 until now
            segment.StartDateTime(new DateTime(2012, 1, 1), DateTime.Now,
                //Create a linear trend in year
                d => d.Year(trend => trend.SetLevel(0).LineRelativePercentage(1, 1))
                    //Add a peak in the summer
                    .PartOfYear(trend => trend.AddPeak(0.5, 0.1, pct: true)));


            var simulator = new SegmentBasedSimulator(segment);

            //Create a 1000 visitors. These are ordered by start date
            foreach (var visitor in simulator.NextVisitors(1000))
            {
                System.Console.Out.WriteLine(".");
                var ctx = new SitecoreRequestContext("http://sc80rev150209/", visitor);

                using (var visit = ctx.NewVisit())
                {
                    //Request home page as if coming from Google
                    visit.Request("/", TimeSpan.FromSeconds(2), new { Referrer = "http://www.google.com" });


                    //Request home page again
                    var info = visit.Request("/", TimeSpan.FromSeconds(2));

                    //"info" contains information from Sitecore, including the item displayed, it's fields, the visit's current VisitData from the tracker etc.
                    //This can be used to change the behavior of the visit
                    if (info.VisitData.ContactVisitIndex == 2)
                    {

                    }
                }

                //Wait 14 days before making the next visit
                ctx.Pause(TimeSpan.FromDays(14));


                using (var visit = ctx.NewVisit())
                {
                    visit.Request("/", TimeSpan.FromSeconds(2));

                    System.Console.Out.WriteLine(visit.VisitData.ContactVisitIndex);
                }
            }

        }

        static void Skynet()
        {
            var serverUrl = "http://xdbrpc.local/";

            var testUrl = "/testPage";

            var goalPage1 = "/trigger-goal1";
            //var goalPage2 = "/trigger-goal2";


            Randomness.Seed(1337);

            var random = Randomness.Random;

            var segment = new VisitorSegment("Test visitors");

            var sim = new SegmentBasedSimulator(segment);
            foreach (var visitorContext in sim.NextVisitors(1000)
                .Select(v => new SitecoreRequestContext(serverUrl, v)))
            {
                using (var visitContext = visitorContext.NewVisit())
                {
                    visitContext.Request(goalPage1);

                    var response = visitContext.Request(testUrl);
                    if (response.Test == null)
                    {
                        throw new Exception("A test was expected");
                    }

                    var pageVersionIndex = response.Test.Variables.FindIndex(v => v.Label == "Page version");
                    if (pageVersionIndex == -1)
                    {
                        throw new Exception("Component not found");
                    }

                    var conversionRate = response.Test.Combination[pageVersionIndex] == 0 ? 0.5 : 0.1;
                    if (random.NextDouble() < conversionRate)
                    {
                        visitContext.Request(goalPage1);
                    }
                }
            }
        }


        static void Parser2()
        {


            var emea = GeoArea.Areas.First(area => area.Id == "amer");

            var geodata = GeoData.FromResource();

            var country = emea.Selector(geodata);


            var freqs = Enumerable.Range(0, 10000).Select(i => country()).GroupBy(c => c.Country.Name)
                .ToDictionary(g => g.Key, g => g.Count());

            File.WriteAllLines(@"C:\Temp\Countries.txt", new[] { "Country\tCount" }.Concat(freqs.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + "\t" + kv.Value)));

            return;



            Randomness.Seed(1337);


            var json = File.ReadAllText(@"C:\Temp\Xerox2.js");

            var def = JObject.Parse(json);
            var parser = new XGenParser("http://sc80rev150427/api/xgen/");



            //var vars = new[]{""}


            var sw = Stopwatch.StartNew();

            var visits = 0;
            var id = 0;
            var contactId = 0;
            using (var f = File.CreateText(@"C:\Temp\XeroxOut2.txt"))
            {
                f.Write("Contact\tId\tVisitIndex\tStart\tEnd\tHour\tDuration\tPageViews\tCampaign\tCountry\tTimeZone\tContinent\tReferrer\tLandingPage\tSite");
                f.WriteLine();

                var threads = Enumerable.Range(0, 25).Select(i =>
                {
                    var t = new Thread(() =>
                    {
                        Randomness.Seed(1337 + i);
                        var segments = parser.ParseSegments(def["Segments"], JobType.Visits);
                        var sim = new SegmentBasedSimulator(segments);

                        foreach (var v in sim.NextVisitors(80))
                        {
                            var myId = Interlocked.Increment(ref contactId);
                            System.Console.Out.WriteLine("Visitor at {0}", v.Start);
                            try
                            {
                                var visitIndex = 0;
                                foreach (var visit in v.Segment.Behavior().Commit(v))
                                {
                                    Interlocked.Increment(ref visits);
                                    System.Console.Out.WriteLine(" - Visit at {0}", visit.Start);
                                    //foreach (var req in visit.Requests)
                                    //{
                                    //    System.Console.Out.WriteLine("   " + req.Url);
                                    //}

                                    //var visit = v.AddVisit();
                                    //visit.AddRequest("");
                                    lock (f)
                                    {
                                        f.Write("Contact" + myId);
                                        f.Write("\t");
                                        f.Write("Visit" + Interlocked.Increment(ref id));
                                        f.Write("\t");
                                        f.Write(++visitIndex);
                                        f.Write("\t");
                                        f.Write(v.Start.Date.ToString("yyyy-MM-dd"));
                                        f.Write("\t");
                                        f.Write(v.End.Date.ToString("yyyy-MM-dd"));
                                        f.Write("\t");
                                        f.Write(v.Start.Hour);
                                        f.Write("\t");
                                        f.Write((v.End - v.Start).TotalSeconds);
                                        f.Write("\t");
                                        f.Write(visit.GetVariable<double>("PageViews"));
                                        f.Write("\t");
                                        f.Write(visit.GetVariable("Campaign", ""));
                                        f.Write("\t");
                                        f.Write(visit.GetVariable("Country", ""));
                                        f.Write("\t");
                                        f.Write(visit.GetVariable("TimeZone", ""));
                                        f.Write("\t");
                                        f.Write(visit.GetVariable("Continent", ""));
                                        f.Write("\t");
                                        f.Write(visit.GetVariable("Referrer", ""));
                                        f.Write("\t");
                                        f.Write(visit.GetVariable("LandingPage", ""));
                                        f.Write("\t");
                                        f.Write(visit.GetVariable("Site", ""));

                                        f.WriteLine();
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Console.Out.WriteLine("Error: " + ex);
                            }
                            System.Console.Out.WriteLine("{0:N0} visits ({1:N2} visits per second)", visits, visits / sw.Elapsed.TotalSeconds);
                        }
                    });
                    t.Start();
                    return t;
                }).ToArray();

                foreach (var t in threads)
                {
                    t.Join();
                }

                System.Console.Out.WriteLine("{0:N0} visits in {1:N2} seconds", visits, sw.Elapsed.TotalSeconds);
            }
        }
    }
}
