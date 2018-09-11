using System.Collections.Generic;

namespace ExperienceGenerator.Data
{
    public class Continent
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public IEnumerable<Continent> SubContinents { get; set; }
    }
}
