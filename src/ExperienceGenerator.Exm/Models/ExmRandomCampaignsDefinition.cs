namespace ExperienceGenerator.Exm.Models
{
  using System;

  public class ExmRandomCampaignsDefinition
    {
        public int NumCampaigns { get; set; }

        public int ListsPerCampaignMin { get; set; }

        public int ListsPerCampaignMax { get; set; }

        public DateTime DateRangeStart { get; set; }

        public ExmEventPercentages Events { get; set; }
    }
}