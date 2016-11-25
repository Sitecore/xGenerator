using System;
using System.Collections.Generic;
using System.Linq;
using Colossus;
using Sitecore.Diagnostics;

namespace ExperienceGenerator.Data
{
    public class GeoData
    {
        public Dictionary<string, Country> Countries { get; set; }
        public List<City> Cities { get; set; }
        public Dictionary<string, string> TimeZoneNames { get; set; }
        public Dictionary<int, City[]> CitiesByCountry { get; set; }
        public List<Continent> Continents { get; set; }

        private static TimeZoneInfo GetTimeZone(Dictionary<string, string> timeZonesNames, string id)
        {
            var windowsName = timeZonesNames.GetOrDefault(id);
            return windowsName == null ? TimeZoneInfo.Local : TimeZoneInfo.FindSystemTimeZoneById(windowsName);
        }

        public static GeoData FromResource()
        {
            var continents = LoadContinentsFromResource();
            var countries = LoadCountriesFromResource();
            var timeZones = LoadTimeZonesFromResource();
            var regions = LoadRegionsFromResource();
            var cities = LoadCitiesFromResource(countries, regions, timeZones);

            var citiesByCountry = GetCitiesByCountry(cities);

            countries = ReduceToCountriesWithCities(countries, citiesByCountry);

            return new GeoData
                   {
                       Countries = countries,
                       Cities = cities,
                       TimeZoneNames = timeZones,
                       CitiesByCountry = citiesByCountry,
                       Continents = continents,
                       Regions = regions
                   };
        }

        public List<Region> Regions { get; set; }

        private static Dictionary<string, Country> ReduceToCountriesWithCities(Dictionary<string, Country> countries, Dictionary<int, City[]> citiesByCountry)
        {
            return countries.Values.Where(x => citiesByCountry.ContainsKey(x.IsoNumeric)).ToDictionary(x => x.Iso);
        }

        private static Dictionary<int, City[]> GetCitiesByCountry(List<City> cities)
        {
            return cities.GroupBy(c => c.Country.IsoNumeric).ToDictionary(c => c.Key, c => c.ToArray());
        }

        private static List<Continent> LoadContinentsFromResource()
        {
            var allContinents = FileHelpers.ReadLinesFromResource<Continent>("ExperienceGenerator.Data.CountriesByContinent.txt").Where(x => x[0] != '#').Select(x => x.Split('\t')).Select(x => new
                                                                                                                                                                                                 {
                                                                                                                                                                                                     CountryCode = x[3],
                                                                                                                                                                                                     ContinentName = x[5],
                                                                                                                                                                                                     SubcontinentName = x[6],
                                                                                                                                                                                                     ContinentCode = x[7],
                                                                                                                                                                                                     SubcontinentCode = x[8]
                                                                                                                                                                                                 }).Where(x => !string.IsNullOrEmpty(x.ContinentName) || !string.IsNullOrEmpty(x.SubcontinentName));

            return allContinents.GroupBy(x => x.ContinentCode).Select(g => new Continent
                                                                           {
                                                                               Code = g.Key,
                                                                               Name = g.First().ContinentName,
                                                                               SubContinents =
                                                                                   //distinct trick
                                                                                   g.GroupBy(SetCountryContinent => SetCountryContinent.SubcontinentCode).Select(gr => gr.First()).Select(r => new Continent
                                                                                                                                                                                               {
                                                                                                                                                                                                   Code = r.SubcontinentCode,
                                                                                                                                                                                                   Name = r.SubcontinentName
                                                                                                                                                                                               })
                                                                           }).ToList();
        }

        private static Dictionary<string, string> LoadTimeZonesFromResource()
        {
            var timeZoneNames = FileHelpers.ReadLinesFromResource<GeoData>("ExperienceGenerator.Data.timezones.txt").Skip(1).Select(l => l.Split('\t')).Select(row => new
                                                                                                                                                                      {
                                                                                                                                                                          TzId = row[2],
                                                                                                                                                                          Windows = row[0]
                                                                                                                                                                      }).GroupBy(tz => tz.TzId).ToDictionary(tz => tz.Key, tz => tz.First().Windows);

            foreach (var kv in timeZoneNames.ToArray())
            {
                var parts = kv.Key.Split(' ');
                if (parts.Length <= 1)
                    continue;
                foreach (var p in parts.Where(p => !timeZoneNames.ContainsKey(p)))
                {
                    timeZoneNames.Add(p, kv.Value);
                }
            }
            return timeZoneNames;
        }

        private static List<City> LoadCitiesFromResource(Dictionary<string, Country> countries, List<Region> regions, Dictionary<string, string> timeZones)
        {
            var cities = FileHelpers.ReadLinesFromResource<GeoData>("ExperienceGenerator.Data.cities15000.txt").Skip(1).Where(l => !l.StartsWith("#")).Select(l => City.FromCsv(l.Split('\t'))).OrderBy(c => c.Population).ToList();

            foreach (var city in cities)
            {
                city.Country = countries[city.CountryCode];
                city.TimeZoneInfo = GetTimeZone(timeZones, city.TimeZone);
                var region = regions.FirstOrDefault(r => r.CountryCode == city.CountryCode && r.RegionCode == city.RegionCode);
                if (region == null)
                {
                    Log.Warn($"Region '{city.RegionCode}' for country '{city.CountryCode} {city.Country}' does not exist in Sitecore", city);
                    city.RegionCode = "";
                }
            }

            return cities;
        }

        private static Dictionary<string, Country> LoadCountriesFromResource()
        {
            var countries = FileHelpers.ReadLinesFromResource<GeoData>("ExperienceGenerator.Data.countryInfo.txt").Where(l => !l.StartsWith("#")).Select(l => Country.FromCsv(l.Split('\t'))).ToDictionary(c => c.Iso);

            FixCountryDomains(countries);

            SetContinentOnContries(countries);

            return countries;
        }

        private static List<Region> LoadRegionsFromResource()
        {
            var regions = FileHelpers.ReadLinesFromResource<GeoData>("ExperienceGenerator.Data.SitecoreRegions.txt").Select(l => Region.FromCsv(l.Split('\t'))).ToList();

            return regions;
        }

        private static void SetContinentOnContries(Dictionary<string, Country> countries)
        {
            foreach (var row in FileHelpers.ReadLinesFromResource<GeoData>("ExperienceGenerator.Data.CountriesByContinent.txt").Where(l => l[0] != '#').Select(l => l.Split('\t')))
            {
                if (string.IsNullOrEmpty(row[3]) || string.IsNullOrEmpty(row[7]) || string.IsNullOrEmpty(row[8]))
                    continue;
                var country = countries.Values.FirstOrDefault(c => c.IsoNumeric == int.Parse(row[3]));
                if (country == null)
                    continue;
                country.ContinentCode = row[7];
                country.SubcontinentCode = row[8];
            }
        }

        private static void FixCountryDomains(Dictionary<string, Country> countries)
        {
            foreach (var country in countries.Values)
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
        }
    }
}
