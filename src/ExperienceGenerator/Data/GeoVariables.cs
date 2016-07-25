using System;
using System.Collections.Generic;
using Colossus;
using Colossus.Integration.Behaviors;

namespace ExperienceGenerator.Data
{
    public class GeoVariables : VisitorVariablesBase
    {
        public bool AdjustToTimeZone { get; set; }
        private readonly Func<City> _city;

        public GeoVariables(Func<City> city, bool adjustToTimeZone = false)
        {
            AdjustToTimeZone = adjustToTimeZone;
            _city = city;
        }

        public override void SetValues(SimulationObject target)
        {
            var city = _city();
            if (city == null) return;
            target.Variables["Country"] = city.CountryCode;
            target.Variables["Continent"] = city.Country.Continent;
            target.Variables["Region"] = city.Admin1;
            target.Variables["City"] = city.AsciiName;
            target.Variables["Tld"] = city.Country.TopLevelDomain;
            target.Variables["DomainPostfix"] = city.Country.DomainPostFix;
            target.Variables["Latitude"] = city.Latitude;
            target.Variables["Longitude"] = city.Longitude;

            target.Variables["Currency"] = city.Country.CurrencyCode;
            target.Variables["TimeZone"] = city.TimeZoneInfo.StandardName;

            if (AdjustToTimeZone)
            {
                target.Start = DateTime.SpecifyKind(target.Start, DateTimeKind.Utc);
                target.End = DateTime.SpecifyKind(target.End, DateTimeKind.Utc);

                target.Start = TimeZoneInfo.ConvertTimeFromUtc(target.Start, city.TimeZoneInfo);
                target.End = TimeZoneInfo.ConvertTimeFromUtc(target.End, city.TimeZoneInfo);
              }
            }
        }

        public override IEnumerable<string> ProvidedVariables
        {
            get
            {
                return new[]
                {
                    "Country", "Continent", "Region", "City", "Tld", "DomainPostfix", "Latitude", "Longitude",
                    "Currency", "TimeZone"
                };
            }
        }
    }
}
