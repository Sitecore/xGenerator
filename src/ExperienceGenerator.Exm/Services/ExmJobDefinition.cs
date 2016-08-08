namespace ExperienceGenerator.Exm.Services
{
  using System;
  using System.Collections.Generic;
  using ExperienceGenerator.Exm.Controllers;
  using ExperienceGenerator.Exm.Models;

  public class ExmJobDefinition : Dictionary<Guid, CampaignModel>
  {
    public ExmJob Job { get; set; }
  }
}