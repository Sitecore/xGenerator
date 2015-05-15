using System;
using System.Collections.Generic;
using Colossus;
using Colossus.Statistics;

namespace ExperienceGenerator.Data
{
    public class GeoBuilder
    {
        private readonly GeoData _data;

        private Dictionary<string, double> _continents = new Dictionary<string, double>();
        private Dictionary<string, double> _countries = new Dictionary<string, double>();

        private double _baseCountryWeight = 1;
        private double _baseContinentWeight = 1;

        public  GeoBuilder(GeoData data)
        {
            _data = data;
        }

        public GeoBuilder BaseContinentWeight(double weight)
        {
            _baseContinentWeight = weight;
            return this;
        }

        public GeoBuilder BaseCountryWeight(double weight)
        {
            _baseCountryWeight = weight;
            return this;
        }

        public GeoBuilder BoostContinent(string code, double weight)
        {
            _continents[code] = weight;
            return this;
        }

        public GeoBuilder BoostCountry(string code, double weight)
        {
            _countries[code] = weight;
            return this;
        }

        public Func<City> Build()
        {
            return Sets.Weighted<City>(builder =>
            {
                foreach (var city in _data.Cities)
                {
                    builder.Add(city,
                        (city.Population ?? 0) * 
                        _continents.GetOrDefault(city.Country.Continent, _baseContinentWeight)*
                        _countries.GetOrDefault(city.CountryCode, _baseCountryWeight));
                }
            });
        }

        public IVisitorVariables AsVariable(bool adjustToTimeZone)
        {
            return new GeoVariables(Build(), adjustToTimeZone);
        }
    }
}
