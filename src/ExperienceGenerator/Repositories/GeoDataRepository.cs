using System;
using System.Collections.Generic;
using System.Linq;
using ExperienceGenerator.Data;
using Sitecore.Analytics.Model;

namespace ExperienceGenerator.Repositories
{
    public class GeoDataRepository
    {
        private static GeoData _cache;
        private static readonly object _lock = new object();

        private static GeoData Cache
        {
            get
            {
                lock (_lock)
                {
                    return _cache ?? (_cache = GeoData.FromResource());
                }
            }
        }


        public List<City> Cities => Cache.Cities;
        public List<Continent> Continents => Cache.Continents;
        public List<Country> Countries => Cache.Countries.Values.ToList();

        public List<City> CitiesByCountry(int isoNumeric)
        {
            return Cache.CitiesByCountry[isoNumeric].ToList();
        }
    }
}
