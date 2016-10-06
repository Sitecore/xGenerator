using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Analytics.Model;
using Sitecore.Shell.Framework.Commands.Preferences;

namespace ExperienceGenerator.Data
{
  public class GeoRegion
  {
    public string Id { get; set; }
    public string Label { get; set; }

    public IEnumerable<GeoRegion> SubRegions { get; set; }

    public Func<GeoData, Func<City>> Selector { get; set; }

    private static readonly Random Random = new Random();

    public static IEnumerable<GeoRegion> Regions
    {
      get
      {
        var allRegions = FileHelpers.ReadLinesFromResource<GeoRegion>("ExperienceGenerator.Data.Regions.txt").Where(x => x[0] != '#').Select(x => x.Split('\t'))
          .Select(x => new
          {
            Region = x[5],
            SubRegion = x[6],
            Id = x[7],
            SubregionId = x[8],
          }).Where(x=>!string.IsNullOrEmpty(x.Region) || !string.IsNullOrEmpty(x.SubRegion));

        return allRegions.GroupBy(x => x.Id).Select(g =>
            new GeoRegion()
            {
              Id = g.Key,
              Label = g.First().Region,
              SubRegions =
              //distinct trick
              g.GroupBy(subregion=>subregion.SubregionId).Select(gr=>gr.First())
              .Select(r =>
                new GeoRegion()
                {
                  Id = r.SubregionId,
                  Label = r.SubRegion,
                  Selector = data =>
                  {
                    var builder = new GeoBuilder(data)
                      //exclude all countries  from return results
                      .BaseCountryWeight(0)
                      //boost weight for region
                      .BoostRegion(r.SubregionId, 5);

                    foreach (var c in data.Countries.Values.Where(c => c.SubRegionId == r.SubregionId))
                    {
                      //add weights for countries from this region
                      builder.BoostCountry(c.Iso, 2.5);
                    }
                    return builder.Build();
                  }
                })
            });
      }
    }

    public static WhoIsInformation RandomCountryForSubRegion(int subRegionId)
    {
      var regions = FileHelpers.ReadLinesFromResource<GeoRegion>("ExperienceGenerator.Data.Regions.txt")
        .Where(x => x[0] != '#')
        .Select(x => x.Split('\t')).Where(x => !string.IsNullOrEmpty(x[5]) || !string.IsNullOrEmpty(x[6]))
        .Select(x => new WhoIsInformation()
        {
          AreaCode = x[8],
          Country = x[1],
          Region = x[1],
          City= RandomCityForCountry(x[1])
        }).ToList();
      var macthingRegions = regions.Where(x => Convert.ToInt32(x.AreaCode) == subRegionId).ToList();
      return macthingRegions[Random.Next(macthingRegions.Count - 1)];
    }

    public static string RandomCityForCountry(string countryCode)
    {
     var cities = FileHelpers.ReadLinesFromResource<GeoData>("ExperienceGenerator.Data.cities15000.txt")
          .Skip(1)
          .Where(l => !l.StartsWith("#"))
          .Select(l => City.FromCsv(l.Split('\t')))
          .OrderBy(c => c.Population)
          .ToList();
      var matchingCities = cities.Where(x=> x.CountryCode == countryCode).ToList();
      return matchingCities.Any()? matchingCities[Random.Next(matchingCities.Count - 1)].Name:string.Empty;
    }
  }
}
