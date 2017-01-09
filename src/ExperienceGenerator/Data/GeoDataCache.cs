using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using Colossus;
using Sitecore.Diagnostics;
using Sitecore.ExperienceAnalytics.Api;

namespace ExperienceGenerator.Data
{
    internal class GeoDataCache
    {
        public Dictionary<string, Country> Countries { get; set; }
        public List<City> Cities { get; set; }
        public Dictionary<string, string> TimeZoneNames { get; set; }
        public Dictionary<int, City[]> CitiesByCountry { get; set; }
        public List<Continent> Continents { get; set; }

        public static GeoDataCache FromResource()
        {
            var regions = LoadRegionsFromSitecore();
            var continents = LoadContinentsFromResource();
            var countries = LoadCountriesFromResource(regions);
            var timeZones = LoadTimeZonesFromResource();
            var cities = LoadCitiesFromResource(countries, regions, timeZones);

            var citiesByCountry = GetCitiesByCountry(cities);

            countries = ReduceToCountriesWithCities(countries, citiesByCountry);

            return new GeoDataCache
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

        private static Dictionary<int, City[]> GetCitiesByCountry(IEnumerable<City> cities)
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
            var timeZoneNames = FileHelpers.ReadLinesFromResource<GeoDataCache>("ExperienceGenerator.Data.timezones.txt").Skip(1).Select(l => l.Split('\t')).Select(row => new
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
            var linesFromResource = FileHelpers.ReadLinesFromResource<GeoDataCache>("ExperienceGenerator.Data.cities.txt");
            var cityObjects = linesFromResource.Select(l => City.FromCsv(l.Split('\t'), countries, regions, timeZones)).Where(c => c != null);
            return cityObjects.OrderByDescending(c => c.Population).ToList();
        }

        private static Dictionary<string, Country> LoadCountriesFromResource(List<Region> regions)
        {
            var linesFromResource = FileHelpers.ReadLinesFromResource<GeoDataCache>("ExperienceGenerator.Data.countryInfo.txt");
            var excludingComments = linesFromResource.Where(l => !l.StartsWith("#"));
            var countryObjects = excludingComments.Select(l => Country.FromCsv(l.Split('\t')));
            var excludingWithoutRegions = countryObjects.Where(c => regions.Any(r => r.CountryCode == c.Iso));
            var countries = excludingWithoutRegions.ToDictionary(c => c.Iso);

            FixCountryDomains(countries);

            SetContinentOnContries(countries);

            return countries;
        }

        private static List<Region> LoadRegionsFromSitecore()
        {
            var regions = Type.GetType("Sitecore.ExperienceAnalytics.Api.GeoLocationTranslations.Regions, Sitecore.ExperienceAnalytics.Api");
            if (regions == null)
                return new List<Region>();
            var members = regions.GetFields();
            return members.Where(memberInfo => memberInfo.IsLiteral).Select(CreateRegionFromConst).ToList();
        }

        private static Region CreateRegionFromConst(FieldInfo member)
        {
            var keys = ((string)member.Name).Split('_');
            var regionName = (string)member.GetRawConstantValue();
            return new Region
                   {
                CountryCode = keys[0],
                RegionCode = keys[1],
                Name = regionName
            };
        }

        private static void SetContinentOnContries(IReadOnlyDictionary<string, Country> countries)
        {
            var linesFromResource = FileHelpers.ReadLinesFromResource<GeoDataCache>("ExperienceGenerator.Data.CountriesByContinent.txt");
            var excludingComments = linesFromResource.Where(l => l[0] != '#');
            var toColumns = excludingComments.Select(l => l.Split('\t'));
            foreach (var row in toColumns)
            {
                var countryCode = row[1];
                var continentCode = row[7];
                var subcontinentCode = row[8];

                if (string.IsNullOrEmpty(countryCode) || string.IsNullOrEmpty(continentCode) || string.IsNullOrEmpty(subcontinentCode))
                    continue;
                if (!countries.ContainsKey(countryCode ))
                    continue;
                var country = countries[countryCode];
                country.ContinentCode = continentCode;
                country.SubcontinentCode = subcontinentCode;
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
