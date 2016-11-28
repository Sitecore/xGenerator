using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Colossus;
using Newtonsoft.Json;
using Sitecore.Analytics.Model;
using Sitecore.Diagnostics;

namespace ExperienceGenerator.Data
{
    public class City
    {
        public int GeoNameId { get; set; }
        public string Name { get; set; }
        public string AsciiName { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string CountryCode { get; set; }
        public string RegionCode { get; set; }
        public int? Population { get; set; }
        public string TimeZone { get; set; }

        public Country Country { get; set; }

        [JsonIgnore]
        public TimeZoneInfo TimeZoneInfo { get; set; }

        private static readonly CultureInfo _defaultCultureInfo = CultureInfo.GetCultureInfo("en-US");

        public static City FromCsv(string[] row, Dictionary<string, Country> countries, List<Region> regions, Dictionary<string, string> timeZones)
        {
            var city = new City
                   {
                       GeoNameId = int.Parse(row[0]),
                       CountryCode = row[1].ToUpperInvariant(),
                       AsciiName = row[2],
                       Name = row[3],
                       RegionCode = row[4],
                       Population = IfSpecified(row[5], s => int.Parse(s, _defaultCultureInfo)),
                       Latitude = IfSpecified(row[6], s => double.Parse(s, _defaultCultureInfo)),
                       Longitude = IfSpecified(row[7], s => double.Parse(s, _defaultCultureInfo)),
                       TimeZone = row[8]
                   };

            if (!countries.ContainsKey(city.CountryCode))
                return null;

            var region = regions.FirstOrDefault(r => r.CountryCode == city.CountryCode && r.RegionCode == city.RegionCode);
            if (region == null)
                return null;

            var timeZoneInfo = GetTimeZone(timeZones, city.TimeZone);
            if (timeZoneInfo == null)
                return null;

            city.Country = countries[city.CountryCode];
            city.TimeZoneInfo = timeZoneInfo;
            return city;
        }

        private static TimeZoneInfo GetTimeZone(Dictionary<string, string> timeZonesNames, string id)
        {
            var windowsName = timeZonesNames.GetOrDefault(id);
            return windowsName == null ? TimeZoneInfo.Local : TimeZoneInfo.FindSystemTimeZoneById(windowsName);
        }


        private static TValue? IfSpecified<TValue>(string s, Func<string, TValue> getter) where TValue : struct
        {
            return string.IsNullOrEmpty(s) ? (TValue?) null : getter(s);
        }

        public WhoIsInformation ToWhoIsInformation()
        {
            return new WhoIsInformation
                   {
                       AreaCode = Country.SubcontinentCode,
                       Country = CountryCode,
                       Region = RegionCode,
                       City = Name,
                       Latitude = Latitude,
                       Longitude = Longitude
                   };
        }
    }
}
