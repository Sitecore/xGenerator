namespace ExperienceGenerator.Exm.Models
{
  using System;
  using System.Collections.Generic;

  public class CampaignModel
  {
    public IEnumerable<int> DayDistribution { get; set; }
    public Dictionary<string, int> Devices { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Funnel Events { get; set; }
    public Dictionary<Guid, int> LandingPages { get; set; }
    public Dictionary<int, int> Locations { get; set; }
  }
}