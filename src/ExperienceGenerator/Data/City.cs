using System;
using System.Globalization;
using Newtonsoft.Json;
using Sitecore.Analytics.Model;

namespace ExperienceGenerator.Data
{
    public class City
    {
        public int GeoNameId { get; set; }
        public string Name { get; set; }
        public string AsciiName { get; set; }

        [JsonIgnore]
        public string[] AlternateNames { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string CountryCode { get; set; }
        public string RegionCode { get; set; }
        public int? Population { get; set; }
        public string TimeZone { get; set; }
        public DateTime ModificationDate { get; set; }

        public Country Country { get; set; }

        [JsonIgnore]
        public TimeZoneInfo TimeZoneInfo { get; set; }

        private static readonly CultureInfo enUs = CultureInfo.GetCultureInfo("en-US");

        public static City FromCsv(string[] row)
        {
            return new City
                   {
                       GeoNameId = int.Parse(row[0]),
                       Name = row[1],
                       AsciiName = row[2],
                       AlternateNames = row[3].Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries),
                       Latitude = IfSpecified(row[4], s => double.Parse(s, enUs)),
                       Longitude = IfSpecified(row[5], s => double.Parse(s, enUs)),
                       CountryCode = row[8],
                       RegionCode = row[10],
                       Population = IfSpecified(row[14], s => int.Parse(s, enUs)),
                       TimeZone = row[17],
                       ModificationDate = DateTime.ParseExact(row[18], "yyyy-MM-dd", enUs)
                   };
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
