using System;
using System.Collections.Generic;
using Colossus;
using Colossus.Integration.Behaviors;

namespace ExperienceGenerator.Data
{
    public class GeoVariables : VisitorVariableBase
    {
        public bool AdjustToTimeZone { get; set; }
        private readonly Func<City> _getCity;

        public GeoVariables(Func<City> getCity, bool adjustToTimeZone = false)
        {
            AdjustToTimeZone = adjustToTimeZone;
            _getCity = getCity;
        }

        public override void SetValues(SimulationObject target)
        {
            var city = _getCity();
            if (city == null) return;
            target.Variables[VariableKey.Country] = city.CountryCode;
            target.Variables[VariableKey.Continent] = city.Country.Continent;
            target.Variables[VariableKey.Region] = city.RegionCode;
            target.Variables[VariableKey.City] = city.AsciiName;
            target.Variables[VariableKey.Tld] = city.Country.TopLevelDomain;
            target.Variables[VariableKey.DomainPostfix] = city.Country.DomainPostFix;
            target.Variables[VariableKey.Latitude] = city.Latitude;
            target.Variables[VariableKey.Longitude] = city.Longitude;

            target.Variables[VariableKey.Currency] = city.Country.CurrencyCode;
            target.Variables[VariableKey.TimeZone] = city.TimeZoneInfo.StandardName;

            if (!AdjustToTimeZone)
                return;
            target.Start = DateTime.SpecifyKind(target.Start, DateTimeKind.Utc);
            target.End = DateTime.SpecifyKind(target.End, DateTimeKind.Utc);

            target.Start = TimeZoneInfo.ConvertTimeFromUtc(target.Start, city.TimeZoneInfo);
            target.End = TimeZoneInfo.ConvertTimeFromUtc(target.End, city.TimeZoneInfo);
        }

        public override IEnumerable<VariableKey> ProvidedVariables => new[]
                                                                 {
                                                                     VariableKey.Country, VariableKey.Continent, VariableKey.Region, VariableKey.City, VariableKey.Tld, VariableKey.DomainPostfix, VariableKey.Latitude, VariableKey.Longitude,
                                                                     VariableKey.Currency, VariableKey.TimeZone
                                                                 };
    }
}
