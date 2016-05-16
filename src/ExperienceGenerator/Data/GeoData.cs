using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Colossus;

namespace ExperienceGenerator.Data
{
  public class GeoData
  {

    public Dictionary<string, Country> Countries { get; set; }
    public List<City> Cities { get; set; }


    public Dictionary<string, string> TimeZoneNames { get; set; }

    public Dictionary<int, City[]> CitiesByCountry { get; set; }


    public TimeZoneInfo GetTimeZone(string tzid)
    {
      var windowsName = TimeZoneNames.GetOrDefault(tzid);
      TimeZoneInfo tz;
      if (windowsName == null)
      {
        tz = TimeZoneInfo.Local;
      }
      else
      {
        tz = TimeZoneInfo.FindSystemTimeZoneById(windowsName);
      }

      return tz;
    }


    public static GeoData FromResource()
    {
      var data = new GeoData();

      data.Countries = FileHelpers.ReadLinesFromResource<GeoData>("ExperienceGenerator.Data.countryInfo.txt")
          .Where(l => !l.StartsWith("#"))
          .Select(l => Country.FromCsv(l.Split('\t')))
          .ToDictionary(c => c.Iso);

      data.Cities = FileHelpers.ReadLinesFromResource<GeoData>("ExperienceGenerator.Data.cities15000.txt")
          .Skip(1)
          .Where(l => !l.StartsWith("#"))
          .Select(l => City.FromCsv(l.Split('\t')))
          .OrderBy(c => c.Population)
          .ToList();

      data.TimeZoneNames = FileHelpers.ReadLinesFromResource<GeoData>("ExperienceGenerator.Data.timezones.txt")
          .Skip(1)
          .Select(l => l.Split('\t'))
          .Select(row => new { TzId = row[2], Windows = row[0] })
          .GroupBy(tz => tz.TzId)
          .ToDictionary(tz => tz.Key, tz => tz.First().Windows);


      foreach (var kv in data.TimeZoneNames.ToArray())
      {
        var parts = kv.Key.Split(' ');
        if (parts.Length > 1)
        {
          foreach (var p in parts.Where(p => !data.TimeZoneNames.ContainsKey(p)))
          {
            data.TimeZoneNames.Add(p, kv.Value);
          }
        }
      }


      foreach (var city in data.Cities)
      {
        city.Country = data.Countries[city.CountryCode];
        city.TimeZoneInfo = data.GetTimeZone(city.TimeZone);
      }

      foreach (var country in data.Countries.Values)
      {
        if (country.TopLevelDomain == ".uk")
        {
          country.DomainPostFix = ".co.uk";
        }
        else if (country.TopLevelDomain == ".us")
        {
          country.DomainPostFix = ".com";
        }
        else
        {
          country.DomainPostFix = country.TopLevelDomain;
        }
      }

      var countriesByName = data.Countries.Values.ToDictionary(c => c.IsoNumeric);

      foreach (
          var row in
              FileHelpers.ReadLinesFromResource<GeoRegion>("ExperienceGenerator.Data.Regions.txt").Where(l=>l[0]!='#').Select(l => l.Split('\t')))
      {
        if (string.IsNullOrEmpty(row[3]) || string.IsNullOrEmpty(row[7]) || string.IsNullOrEmpty(row[8])) continue;
        countriesByName[int.Parse(row[3])].RegionId = row[7];
        countriesByName[int.Parse(row[3])].SubRegionId = row[8];
      }

      data.CitiesByCountry = data.Cities.GroupBy(c => c.Country.IsoNumeric)
               .ToDictionary(c => c.Key, c => c.ToArray());

      data.Countries = data.Countries.Values.Where(x => data.CitiesByCountry.ContainsKey(x.IsoNumeric)).ToDictionary(x => x.Iso);

      return data;
    }

  }
}
