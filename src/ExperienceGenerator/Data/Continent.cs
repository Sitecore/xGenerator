using System;
using System.Collections.Generic;
using System.Linq;
using ExperienceGenerator.Repositories;
using Sitecore.Analytics.Model;

namespace ExperienceGenerator.Data
{
    public class Continent
    {
        public string Code { get; set; }
        public string Name { get; set; }

        public IEnumerable<Continent> SubContinents { get; set; }
    }
}
