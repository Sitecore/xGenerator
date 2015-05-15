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

        public Dictionary<string, City[]> CitiesByContinent { get; set; }

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

            data.Cities = FileHelpers.ReadLinesFromResource<GeoData>("ExperienceGenerator.Data.cities15000.txt.gz", true)
                .Skip(1)
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

            var countriesByName = data.Countries.Values.ToDictionary(c => c.Name);
            foreach (
                var row in
                    FileHelpers.ReadLinesFromResource<GeoArea>("ExperienceGenerator.Data.Regions.txt").Select(l => l.Split('\t')))
            {
                countriesByName[row[0]].Region = row[1];
            }

            data.CitiesByContinent = data.Cities.GroupBy(c => c.Country.Continent)
                .ToDictionary(c => c.Key, c => c.ToArray());

            return data;
        }
    }
}
