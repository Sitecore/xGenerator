using System;
using System.Collections.Generic;

namespace ExperienceGenerator.Exm.Models
{
    public class CampaignSettings
    {
        public IEnumerable<int> DayDistribution { get; set; }
        public Dictionary<string, int> Devices { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Funnel Events { get; set; }
        public Dictionary<string, double> LandingPages { get; set; }
        public Dictionary<int, int> Locations { get; set; }
    }
}
