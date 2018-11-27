using System;
using System.Collections.Generic;
using System.Linq;
using Colossus;
using Colossus.Statistics;
using ExperienceGenerator.Data;
using ExperienceGenerator.Repositories;
using Sitecore;

namespace ExperienceGenerator.Services
{
    public class GetRandomCityService
    {
        private readonly GeoDataRepository _geoDataRepository;

        public GetRandomCityService()
        {
            _geoDataRepository = new GeoDataRepository();
        }

        public City GetRandomCity([NotNull] string subcontinentCode)
        {
            if (subcontinentCode == null)
                throw new ArgumentNullException(nameof(subcontinentCode));

            var cities = _geoDataRepository.Cities.Where(c => c.Country.SubcontinentCode == subcontinentCode);
            return Sets.Weighted<City>(builder =>
               {
                   foreach (var city in cities)
                   {
                       builder.Add(city, city.Population ?? 0);
                   }
               })();
        }
    }
}
