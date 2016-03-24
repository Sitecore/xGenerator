namespace ExperienceGenerator.Client.Models
{
  using System.Collections.Generic;

  public class ChannelGroup
  {
    public string Label { get; set; }

    public List<SelectionOption> Channels { get; set; }
  }
}