namespace ExperienceGenerator.Tests
{
  using System.Collections.Generic;
  using System.Linq;
  using ExperienceGenerator.Data;
  using FluentAssertions;
  using Newtonsoft.Json.Linq;
  using Xunit;

  public class GeoRegionTests
  {
    private static readonly JArray EmptySpecification = JArray.Parse("[{}]");

    [Fact]
    public void Regions_ShouldNotReturnEmptyTopRegion()
    {
      GeoRegion.Regions.Count(x => string.IsNullOrEmpty(x.Id)).Should().Be(0);
    }

    [Fact]
    public void Regions_TopRegions_ShouldExist()
    {
      var expectedRegions = new[]
      {
        "Asia", "Europe", "Africa", "Oceania", "Americas"
      };
      foreach (var expectedRegion in expectedRegions)
      {
        GeoRegion.Regions.Count(x => x.Label == expectedRegion).Should().Be(1);
      }
    }

    [TheoryAttribute]
    [InlineAutoDbData("Asia", new[] { "Southern Asia", "Western Asia", "South-Eastern Asia" })]
    [InlineAutoDbData("Africa", new[] { "Eastern Africa", "Middle Africa", "Northern Africa", "Southern Africa", "Western Africa" })]
    [InlineAutoDbData("Europe", new[] { "Eastern Europe", "Western Europe", "Southern Europe", "Northern Europe" })]
    [InlineAutoDbData("Americas", new[] { "Caribbean", "Northern America", "Central America", "South America" })]
    [InlineAutoDbData("Oceania", new[] { "Australia and New Zealand", "Melanesia", "Micronesia", "Polynesia" })]
    public void Regions_EachTopRegion_ShouldContainAllSubregions(string region, IEnumerable<string> expectedSubRegions)
    {

      var actualSubRegions = GeoRegion.Regions.First(x => x.Label == region).SubRegions.Select(x => x.Label);
      foreach (var expectedRegion in expectedSubRegions)
      {
        actualSubRegions.Should().Contain(expectedRegion);
      }
    }
  }
}