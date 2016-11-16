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

        private static IEnumerable<string> _allRegions;

        private static IList<City> _allCities;
        private static IList<Country> _allCountries;

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
                  }).Where(x => !string.IsNullOrEmpty(x.Region) || !string.IsNullOrEmpty(x.SubRegion));

                return allRegions.GroupBy(x => x.Id).Select(g =>
                    new GeoRegion()
                    {
                        Id = g.Key,
                        Label = g.First().Region,
                        SubRegions =
                      //distinct trick
                      g.GroupBy(subregion => subregion.SubregionId).Select(gr => gr.First())
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
            if (_allRegions == null)
            {
                _allRegions = FileHelpers.ReadLinesFromResource<GeoRegion>("ExperienceGenerator.Data.Regions.txt");
            }

            var macthingRegions = _allRegions
                .Where(x => x[0] != '#')
                .Select(x => x.Split('\t'))
                .Where(
                    x =>
                        (!string.IsNullOrEmpty(x[5]) || !string.IsNullOrEmpty(x[6])) &&
                        Convert.ToInt32(x[8]) == subRegionId);

            if (_allCountries == null)
            {
                _allCountries = FileHelpers.ReadLinesFromResource<GeoData>("ExperienceGenerator.Data.countryInfo.txt")
                .Skip(1)
                .Where(l => !l.StartsWith("#"))
                .Select(l => Country.FromCsv(l.Split('\t')))
                .OrderByDescending(c => c.Population)
                .ToList();
            }

            var matchingCountries = _allCountries.Where(x => macthingRegions.Any(y=>y[1] == x.Fips))
                .Take(10)
                .ToList();
           
            var randomCountry = matchingCountries.Any()
                ? matchingCountries[Random.Next(matchingCountries.Count - 1)]
                : null;

            return randomCountry != null
                ? new WhoIsInformation()
                {
                    AreaCode = subRegionId.ToString(),
                    Country = randomCountry.Fips,
                    Region = randomCountry.Fips,
                    City = RandomCityForCountry(randomCountry.Fips)
                }
                : null;
        }

        public static string RandomCityForCountry(string countryCode)
        {
            if (_allCities == null)
            {
                _allCities = FileHelpers.ReadLinesFromResource<GeoData>("ExperienceGenerator.Data.cities15000.txt")
                 .Skip(1)
                 .Where(l => !l.StartsWith("#"))
                 .Select(l => City.FromCsv(l.Split('\t')))
                 .OrderByDescending(c => c.Population)
                 .ToList();
            }

            var matchingCities = _allCities.Where(x => x.CountryCode == countryCode).Take(10).ToList();

            return matchingCities.Any() ? matchingCities[Random.Next(matchingCities.Count - 1)].Name : string.Empty;
        }
    }
}
