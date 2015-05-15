using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExperienceGenerator.Data
{
    public class GeoArea
    {
        public string Id { get; set; }
        public string Label { get; set; }


        public Func<GeoData, Func<City>> Selector { get; set; }


        public static IEnumerable<GeoArea> Areas
        {
            get
            {

                var regions = FileHelpers.ReadLinesFromResource<GeoArea>("ExperienceGenerator.Data.Regions.txt");                

                yield return new GeoArea
                {
                    Id = "emea",
                    Label = "Europe, Middle East, Africa",
                    Selector = data =>
                    {
                        var builder = new GeoBuilder(data).BaseCountryWeight(0)
                            .BoostContinent("AF", 0.05)
                            .BoostContinent("EU", 4)
                            .BoostContinent("AS", 0.4);
                                           foreach (var c in data.Countries.Values.Where(c => c.Region == "EMEA"))
                                           {
                                               builder.BoostCountry(c.Iso, 1);
                                           }
                        return builder.Build();
                    }
                };

                yield return new GeoArea
                {
                    Id = "apac",
                    Label = "Asia Pacific",
                    Selector = data =>
                    {
                        var builder = new GeoBuilder(data).BaseCountryWeight(0)
                            .BoostContinent("OC", 2);
                        foreach (var c in data.Countries.Values.Where(c => c.Region == "APAC"))
                        {
                            builder.BoostCountry(c.Iso, 1);
                        }

                        builder.BoostCountry("AU", 2).BoostCountry("NZ", 2);

                        return builder.Build();
                    }
                };
                

                yield return new GeoArea
                {
                    Id = "amer",
                    Label = "Americas",
                    Selector = data =>
                    {
                        var builder = new GeoBuilder(data).BaseCountryWeight(0)
                            .BoostContinent("NA", 4);                            
                        foreach (var c in data.Countries.Values.Where(c => c.Region == "AMER"))
                        {
                            builder.BoostCountry(c.Iso, 1);
                        }
                        builder.BoostCountry("CA", 2.5);
                        return builder.Build();
                    }
                };
            }
        }
    }
}
