using System;
using System.Collections.Generic;
using Sitecore.CES.GeoIp.Core.Model;
using Sitecore.Modules.EmailCampaign.Messages;

namespace ExperienceGenerator.Exm.Models
{
    public class MessageContactEvents
    {
        public MessageItem MessageItem { get; set; }
        public Guid ContactId { get; set; }
        public string UserAgent { get; set; }
        public WhoIsInformation GeoData { get; set; }
        public string LandingPageUrl { get; set; }
        public IEnumerable<MessageContactEvent> Events { get; set; }
    }
}
