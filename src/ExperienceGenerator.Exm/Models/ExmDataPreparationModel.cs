using System;
using System.Collections.Generic;

namespace ExperienceGenerator.Exm.Models
{
  public class ExmJobDefinitionModel : Dictionary<Guid, CampaignModel>
  {
    public int ListUnlockedAttempts { get; set; }
    public int Threads { get; set; }
    public ExmJob Job { get; set; }
    public ExmJobDefinitionModel()
    {
      ListUnlockedAttempts = 300;
      Threads = 1;
    }
  }
}
