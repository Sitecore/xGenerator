using System;
using Sitecore.Analytics.Model;

namespace ExperienceGenerator.Exm.Models
{
    public class ExmFakeData
    {
        public string UserAgent { get; set; }

        public DateTime? RequestTime { get; set; }

        public WhoIsInformation GeoData { get; set; }
    }
}