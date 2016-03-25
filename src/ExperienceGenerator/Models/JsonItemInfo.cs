namespace ExperienceGenerator.Models
{
  using System;
  using Newtonsoft.Json;

  public class JsonItemInfo
  {

    [JsonProperty("itemId")]
    public Guid Id { get; set; }

    [JsonProperty("$displayName", Required = Required.Default)]
    public string DisplayName { get; set; }
    [JsonProperty("path", Required = Required.Default)]

    public string Path { get; set; }

  }
}