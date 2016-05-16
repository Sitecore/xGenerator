using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExperienceGenerator.Data
{
  public class GeoRegion
  {
    public string Id { get; set; }
    public string Label { get; set; }

    public IEnumerable<GeoRegion> SubRegions { get; set; }

    public Func<GeoData, Func<City>> Selector { get; set; }


    public static IEnumerable<GeoRegion> Regions
    {
      get
      {
        var allRegions = FileHelpers.ReadLinesFromResource<GeoRegion>("ExperienceGenerator.Data.Regions.txt").Where(x => x[0] != '#').Select(x => x.Split('\t'))
          .Select(x => new
          {
            Region = x[5],
            SubRegion = x[6],
            Id = x[7],
            SubregionId = x[8],
          }).Where(x=>!string.IsNullOrEmpty(x.Region) || !string.IsNullOrEmpty(x.SubRegion));

        return allRegions.GroupBy(x => x.Id).Select(g =>
            new GeoRegion()
            {
              Id = g.Key,
              Label = g.First().Region,
              SubRegions =
              //distinct trick
              g.GroupBy(subregion=>subregion.SubregionId).Select(gr=>gr.First())
              .Select(r =>
                new GeoRegion()
                {
                  Id = r.SubregionId,
                  Label = r.SubRegion,
                  Selector = data =>
                  {
                    var builder = new GeoBuilder(data)
                      //exclude all countries  from return results
                      .BaseCountryWeight(0)
                      //boost weight for region
                      .BoostRegion(r.SubregionId, 5);

                    foreach (var c in data.Countries.Values.Where(c => c.SubRegionId == r.SubregionId))
                    {
                      //add weights for countries from this region
                      builder.BoostCountry(c.Iso, 2.5);
                    }
                    return builder.Build();
                  }
                })
            });
      }
    }
  }
}
