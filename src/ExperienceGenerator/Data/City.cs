using System;
using System.Globalization;

namespace ExperienceGenerator.Data
{
  using Newtonsoft.Json;

  public class City
  {
    public int GeoNameId { get; set; }
    public string Name { get; set; }
    public string AsciiName { get; set; }
    [JsonIgnore]
    public string[] AlternateNames { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string FeatureClass { get; set; }
    public string FeatureCode { get; set; }
    public string CountryCode { get; set; }
    public string[] Cc2 { get; set; }
    public string Admin1 { get; set; }
    public string Admin2 { get; set; }
    public string Admin3 { get; set; }
    public string Admin4 { get; set; }
    public int? Population { get; set; }
    public double? Elevation { get; set; }
    public int? DigitalElevationModel { get; set; }
    public string TimeZone { get; set; }
    public DateTime ModificationDate { get; set; }

    public Country Country { get; set; }
    [JsonIgnore]
    public TimeZoneInfo TimeZoneInfo { get; set; }

    private static CultureInfo enUs = CultureInfo.GetCultureInfo("en-US");
    public static City FromCsv(string[] row)
    {
      var index = 0;
      return new City
      {
        GeoNameId = int.Parse(row[index++]),
        Name = row[index++],
        AsciiName = row[index++],
        AlternateNames = row[index++].Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries),
        Latitude = IfSpecified(row[index++], s => double.Parse(s, enUs)),
        Longitude = IfSpecified(row[index++], s => double.Parse(s, enUs)),
        FeatureClass = row[index++],
        FeatureCode = row[index++],
        CountryCode = row[index++],
        Cc2 = row[index++].Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries),
        Admin1 = row[index++],
        Admin2 = row[index++],
        Admin3 = row[index++],
        Admin4 = row[index++],
        Population = IfSpecified(row[index++], s => int.Parse(s, enUs)),
        Elevation = IfSpecified(row[index++], s => double.Parse(s, enUs)),
        DigitalElevationModel = IfSpecified(row[index++], s => int.Parse(s, enUs)),
        TimeZone = row[index++],
        ModificationDate = DateTime.ParseExact(row[index++], "yyyy-MM-dd", enUs)
      };
    }

    static TValue? IfSpecified<TValue>(string s, Func<string, TValue> getter)
        where TValue : struct
    {
      return string.IsNullOrEmpty(s) ? (TValue?)null : getter(s);
    }
  }
}
