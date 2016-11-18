using System;
using System.Collections.Generic;

namespace ExperienceGenerator.Exm.Models
{
    public class JobDefinition : Dictionary<Guid, CampaignModel>
    {
        public int Threads { get; set; }
        public Job Job { get; set; }

        public JobDefinition()
        {
            Threads = 1;
        }
    }
}