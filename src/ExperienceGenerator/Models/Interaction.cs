using System.Collections.Generic;
using ExperienceGenerator.Data;
using Newtonsoft.Json;

namespace ExperienceGenerator.Models
{

  public class Interaction

  {
    [JsonProperty("geoData")]
    public City GeoData { get; set; }

    [JsonProperty("channelId")]
    public string ChannelId { get; set; }
    [JsonProperty("pages")]

    public IEnumerable<PageItemInfo> Pages { get; set; }

    [JsonProperty("campaignId")]
    public string CampaignId { get; set; }

    [JsonProperty("outcomes")]
    public IEnumerable<JsonItemInfo> Outcomes { get; set; }

    [JsonProperty("recency")]
    public string Recency { get; set; }

    [JsonProperty("searchEngine")]
    public string SearchEngine { get; set; }
    [JsonProperty("searchKeyword")]

    public string SearchKeyword { get; set; }
  }
}
