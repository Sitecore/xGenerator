using System.Collections.Generic;
using Newtonsoft.Json;

namespace ExperienceGenerator.Models
{
  public class PageItemInfo : JsonItemInfo
  {
    [JsonProperty("goals", Required = Required.Default)]
    public IEnumerable<JsonItemInfo> Goals { get; set; }
  }
}
