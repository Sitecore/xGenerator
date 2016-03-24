using System.Collections.Generic;

namespace ExperienceGenerator.Client.Models
{
    public class ConfigurationOptions
    {
        public string Version { get; set; }

        public List<SelectionOption> Websites { get; set; }
        public List<SelectionOption> Location { get; set; }

        public List<SelectionOption> Campaigns { get; set; }

        public List<ChannelGroup> ChannelGroups { get; set; }

        public List<SelectionOption> OrganicSearch { get; set; }
        public List<SelectionOption> PpcSearch { get; set; }

        public List<OutcomeGroup> OutcomeGroups { get; set; }        
    }
}
