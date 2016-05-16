namespace ExperienceGenerator.Data
{
  using System;
  using System.Collections.Generic;
  using Colossus;
  using Colossus.Statistics;

  public class GeoBuilder
  {
    private readonly GeoData _data;

    private readonly Dictionary<string, double> _regions = new Dictionary<string, double>();
    private readonly Dictionary<string, double> _countries = new Dictionary<string, double>();

    private double _baseCountryWeight = 1;
    private double _baseRegionWeight = 1;

    public GeoBuilder(GeoData data)
    {
      this._data = data;
    }

    public GeoBuilder BaseContinentWeight(double weight)
    {
      this._baseRegionWeight = weight;
      return this;
    }

    public GeoBuilder BaseCountryWeight(double weight)
    {
      this._baseCountryWeight = weight;
      return this;
    }


    public GeoBuilder BoostRegion(string regionCode, double weight)
    {
      this._regions[regionCode] = weight;
      return this;
    }

    public GeoBuilder BoostCountry(string code, double weight)
    {
      this._countries[code] = weight;
      return this;
    }

    public Func<City> Build()
    {
      return Sets.Weighted<City>(builder =>
      {
        foreach (var city in this._data.Cities)
        {
          if(city.Country.SubRegionId==null) throw new ApplicationException($"{city.AsciiName}");
          
          builder.Add(city,
            (city.Population ?? 0) * this._regions.GetOrDefault(city.Country.SubRegionId, this._baseRegionWeight) * this._countries.GetOrDefault(city.CountryCode, this._baseCountryWeight));
        }
      });
    }

    public IVisitorVariables AsVariable(bool adjustToTimeZone)
    {
      return new GeoVariables(this.Build(), adjustToTimeZone);
    }
  }
}