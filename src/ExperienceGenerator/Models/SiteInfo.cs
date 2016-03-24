using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExperienceGenerator.Models
{
  using Newtonsoft.Json;

  public class SiteInfo
    {
        public string Id { get; set; }

        public string Host { get; set; }        

        public string StartPath { get; set; }

        public string Label { get; set; }

        public string Database { get; set; }
    }

  public class JsonItemInfo
  {

    [JsonProperty("itemId")]
    public Guid Id { get; set; }

    [JsonProperty("$displayName")]
    public string DisplayName { get; set; }
    [JsonProperty("path")]

    public string Path { get; set; }

  }

  public class PageItemInfo : JsonItemInfo
  {
    [JsonProperty("goals")]
    public IEnumerable<JsonItemInfo> Goals { get; set; }
  }
}
