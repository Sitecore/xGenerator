using System;
using System.Collections.Generic;

namespace ExperienceGenerator.Exm.Models
{
    public class ExmGeneratorSettings : Dictionary<Guid, CampaignSettings>
    {
        public Job Job { get; set; }
    }
}
