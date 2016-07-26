namespace ExperienceGenerator.Exm.Models
{
  using System;
  using System.Collections.Generic;

  public class ExmSpecificCampaign
    {
        public string Name { get; set; }

        public List<string> IncludeLists { get; set; }

        public DateTime? Date { get; set; }

        public int DaysAgo { get; set; }

        public DateTime DateEffective
        {
            get
            {
                return this.Date.HasValue
                    ? this.Date.Value.ToUniversalTime()
                    : DateTime.UtcNow.AddDays(-1*this.DaysAgo);
            }
        }

        public ExmEventPercentages Events { get; set; }
    }
}