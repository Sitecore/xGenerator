using System;
using System.Globalization;

namespace ExperienceGenerator.Data
{
    public class Country
    {
        public string Iso { get; set; }
        public string Iso3 { get; set; }
        public int IsoNumeric { get; set; }
        public string Fips { get; set; }
        public string Name { get; set; }
        public string Capital { get; set; }
        public double? Area { get; set; }
        public int? Population { get; set; }
        public string Continent { get; set; }
        public string TopLevelDomain { get; set; }
        public string DomainPostFix { get; set; }
        public string CurrencyCode { get; set; }
        public string CurrencyName { get; set; }
        public string Phone { get; set; }
        public string PostalCodeFormat { get; set; }
        public string PostalCodeRegex { get; set; }
        public string[] Languages { get; set; }
        public int? GeoNameId { get; set; }
        public string[] Neighbours { get; set; }
        public string EquivalentFipsCode { get; set; }

        public string Region { get; set; }

        private static CultureInfo enUs = CultureInfo.GetCultureInfo("en-US");
        public static Country FromCsv(string[] row)
        {
            var index = 0;
            return new Country
            {
                Iso = row[index++],
                Iso3 = row[index++],
                IsoNumeric = int.Parse(row[index++]),
                Fips = row[index++],
                Name = row[index++],
                Capital = row[index++],
                Area = IfSpecified(row[index++], s=>double.Parse(s, enUs)),
                Population = IfSpecified(row[index++], int.Parse),
                Continent = row[index++],
                TopLevelDomain = row[index++],
                CurrencyCode = row[index++],
                CurrencyName = row[index++],
                Phone = row[index++],
                PostalCodeFormat = row[index++],
                PostalCodeRegex = row[index++],
                Languages = row[index++].Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries),
                GeoNameId = IfSpecified(row[index++], int.Parse),
                Neighbours = row[index++].Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries),
                EquivalentFipsCode = row[index++]
            };
        }

        static TValue? IfSpecified<TValue>(string s, Func<string, TValue> getter)
            where TValue : struct
        {
            return string.IsNullOrEmpty(s) ? (TValue?)null : getter(s);
        }
    }
}
