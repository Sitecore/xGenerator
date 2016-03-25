namespace ExperienceGenerator.Models
{
  using System.Collections.Generic;
  using Newtonsoft.Json;

  public class PageItemInfo : JsonItemInfo
  {
    [JsonProperty("goals", Required = Required.Default)]
    public IEnumerable<JsonItemInfo> Goals { get; set; }
  }
}