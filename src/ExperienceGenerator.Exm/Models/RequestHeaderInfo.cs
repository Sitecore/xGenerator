using System;
using Sitecore.Analytics.Model;

namespace ExperienceGenerator.Exm.Models
{
    public class RequestHeaderInfo
    {
        public string UserAgent { get; set; }
        public DateTime? RequestTime { get; set; }
        public WhoIsInformation GeoData { get; set; }
    }
}
