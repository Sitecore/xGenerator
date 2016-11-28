using System;
using System.Collections.Generic;
using System.Linq;
using ExperienceGenerator.Data;
using Sitecore.Analytics.Model;

namespace ExperienceGenerator.Repositories
{
    public class GeoDataRepository
    {
        private static GeoDataCache _cache;
        private static readonly object _lock = new object();
        private List<City> _cities;

        private static GeoDataCache Cache
        {
            get
            {
                lock (_lock)
                {
                    return _cache ?? (_cache = GeoDataCache.FromResource());
                }
            }
        }


        public List<City> Cities => _cities ?? (_cities = Cache.Cities.ToList());
        public List<Continent> Continents => Cache.Continents;
        public List<Country> Countries => Cache.Countries.Values.ToList();

        public List<City> CitiesByCountry(int isoNumeric)
        {
            return Cache.CitiesByCountry[isoNumeric].ToList();
        }
        public City CityByID(int id)
        {
            return Cache.Cities.FirstOrDefault(c => c.GeoNameId == id);
        }
    }
}
