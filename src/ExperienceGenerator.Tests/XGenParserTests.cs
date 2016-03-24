namespace ExperienceGenerator.Tests
{
  using System;
  using System.Linq;
  using Colossus.Integration;
  using Colossus.Integration.Behaviors;
  using ExperienceGenerator.Parsing;
  using ExperienceGenerator.Parsing.Factories;
  using FluentAssertions;
  using Newtonsoft.Json.Linq;
  using Xunit;

  public class XGenParserTests
  {
    private class GeoDataTmpClass
    {
      public object TimeZoneInfo { get; set; }
    }

    private string jso2n = @"{""VisitorCount"":1,""Specification"":{""Contacts"":[{""email"":""dmd@sc.net"",""firstName"":""Dmytro"",""lastName"":""Dashko"",""interactions"":[{""channelName"":""Organic non-branded search"",""recency"":""-2 days"",""pages"":[{""itemId"":""{ 8235F890 - E581 - 4F79 - 8ACF - B04F90CCC0DD}"",""path"":""/sitecore/content/Legal/Home/News""},{""itemId"":""{6EB0F80B-A1D5-40E0-A472-9308B24228B5
  }"",""path"":""/sitecore/content/Legal/Home""},{""itemId"":""{53C59369-B0C7-40B2-B76F-DED99916D309
}"",""path"":""/sitecore/content/Legal/Home/About Us""}],""itemId"":""95bdd524-a9ca-4f4e-a6f4-e68f1eff11e2"",""channelId"":""{B979A670-5AAF-4E63-94AC-C3C3E5BFBE84}"",""searchEngine"":""Bing"",""searchKeyword"":""tax"",""location"":""New York City US"",""geoData"":{""GeoNameId"":5128581,""Name"":""New York City"",""AsciiName"":""New York City"",""Latitude"":40.71427,""Longitude"":-74.00597,""FeatureClass"":""P"",""FeatureCode"":""PPL"",""CountryCode"":""US"",""Cc2"":[],""Admin1"":""NY"",""Admin2"":"""",""Admin3"":"""",""Admin4"":"""",""Population"":8175133,""Elevation"":10,""DigitalElevationModel"":57,""TimeZone"":""America/New_York"",""ModificationDate"":""2015-02-02T00:00:00"",""Country"":{""Iso"":""US"",""Iso3"":""USA"",""IsoNumeric"":840,""Fips"":""US"",""Name"":""United States"",""Capital"":""Washington"",""Area"":9629091,""Population"":310232863,""Continent"":""NA"",""TopLevelDomain"":"".us"",""DomainPostFix"":"".com"",""CurrencyCode"":""USD"",""CurrencyName"":""Dollar"",""Phone"":""1"",""PostalCodeFormat"":""#####-####"",""PostalCodeRegex"":""^\\d{5}(-\\d{4})?$"",""Languages"":[""en-US"",""es-US"",""haw"",""fr""],""GeoNameId"":6252001,""Neighbours"":[""CA"",""MX"",""CU""],""EquivalentFipsCode"":"""",""Region"":""AMER""},""TimeZoneInfo"":{""Id"":""Eastern Standard Time"",""DisplayName"":""(UTC-05:00) Eastern Time (US & Canada)"",""StandardName"":""Eastern Standard Time"",""DaylightName"":""Eastern Daylight Time"",""BaseUtcOffset"":""-05:00:00"",""SupportsDaylightSavingTime"":true}}},{""channelName"":""Organic non-branded search"",""recency"":""-2 days"",""pages"":[{""itemId"":""{8235F890-E581-4F79-8ACF-B04F90CCC0DD}"",""path"":""/sitecore/content/Legal/Home/News""},{""itemId"":""{6EB0F80B-A1D5-40E0-A472-9308B24228B5}"",""path"":""/sitecore/content/Legal/Home""}],""geoData"":{""GeoNameId"":5128581,""Name"":""New York City"",""AsciiName"":""New York City"",""Latitude"":40.71427,""Longitude"":-74.00597,""FeatureClass"":""P"",""FeatureCode"":""PPL"",""CountryCode"":""US"",""Cc2"":[],""Admin1"":""NY"",""Admin2"":"""",""Admin3"":"""",""Admin4"":"""",""Population"":8175133,""Elevation"":10,""DigitalElevationModel"":57,""TimeZone"":""America/New_York"",""ModificationDate"":""2015-02-02T00:00:00"",""Country"":{""Iso"":""US"",""Iso3"":""USA"",""IsoNumeric"":840,""Fips"":""US"",""Name"":""United States"",""Capital"":""Washington"",""Area"":9629091,""Population"":310232863,""Continent"":""NA"",""TopLevelDomain"":"".us"",""DomainPostFix"":"".com"",""CurrencyCode"":""USD"",""CurrencyName"":""Dollar"",""Phone"":""1"",""PostalCodeFormat"":""#####-####"",""PostalCodeRegex"":""^\\d{5}(-\\d{4})?$"",""Languages"":[""en-US"",""es-US"",""haw"",""fr""],""GeoNameId"":6252001,""Neighbours"":[""CA"",""MX"",""CU""],""EquivalentFipsCode"":"""",""Region"":""AMER""},""TimeZoneInfo"":{""Id"":""Eastern Standard Time"",""DisplayName"":""(UTC-05:00) Eastern Time (US & Canada)"",""StandardName"":""Eastern Standard Time"",""DaylightName"":""Eastern Daylight Time"",""BaseUtcOffset"":""-05:00:00"",""SupportsDaylightSavingTime"":true}},""itemId"":""8db1b281-9aaf-4e94-bd37-62e0a5c8c354"",""channelId"":""{B979A670-5AAF-4E63-94AC-C3C3E5BFBE84}"",""searchEngine"":""Google"",""location"":""New York City US"",""searchKeyword"":""corporate law""},{""channelName"":""Direct"",""recency"":""-2 days"",""pages"":[{""itemId"":""{6EB0F80B-A1D5-40E0-A472-9308B24228B5}"",""path"":""/sitecore/content/Legal/Home""},{""itemId"":""{53C59369-B0C7-40B2-B76F-DED99916D309}"",""path"":""/sitecore/content/Legal/Home/About Us""},{""itemId"":""{3D1C0597-EB53-4216-BA7C-215624C3A641}"",""path"":""/sitecore/content/Legal/Home/Our Team/Dale Kent""},{""itemId"":""{1BEE7175-8126-4F1F-B0D3-1C0C3879795D}"",""path"":""/sitecore/content/Legal/Home/Our Team/Jenna Ridley""}],""geoData"":{""GeoNameId"":5128581,""Name"":""New York City"",""AsciiName"":""New York City"",""Latitude"":40.71427,""Longitude"":-74.00597,""FeatureClass"":""P"",""FeatureCode"":""PPL"",""CountryCode"":""US"",""Cc2"":[],""Admin1"":""NY"",""Admin2"":"""",""Admin3"":"""",""Admin4"":"""",""Population"":8175133,""Elevation"":10,""DigitalElevationModel"":57,""TimeZone"":""America/New_York"",""ModificationDate"":""2015-02-02T00:00:00"",""Country"":{""Iso"":""US"",""Iso3"":""USA"",""IsoNumeric"":840,""Fips"":""US"",""Name"":""United States"",""Capital"":""Washington"",""Area"":9629091,""Population"":310232863,""Continent"":""NA"",""TopLevelDomain"":"".us"",""DomainPostFix"":"".com"",""CurrencyCode"":""USD"",""CurrencyName"":""Dollar"",""Phone"":""1"",""PostalCodeFormat"":""#####-####"",""PostalCodeRegex"":""^\\d{5}(-\\d{4})?$"",""Languages"":[""en-US"",""es-US"",""haw"",""fr""],""GeoNameId"":6252001,""Neighbours"":[""CA"",""MX"",""CU""],""EquivalentFipsCode"":"""",""Region"":""AMER""},""TimeZoneInfo"":{""Id"":""Eastern Standard Time"",""DisplayName"":""(UTC-05:00) Eastern Time (US & Canada)"",""StandardName"":""Eastern Standard Time"",""DaylightName"":""Eastern Daylight Time"",""BaseUtcOffset"":""-05:00:00"",""SupportsDaylightSavingTime"":true}},""itemId"":""61bf7e41-7324-4ee8-8587-fe27b742825e"",""channelId"":""{B418E4F2-1013-4B42-A053-B6D4DCA988BF}"",""searchEngine"":""Bing"",""location"":""New York City US""}],""itemId"":""c0bfc2f2-0744-45a8-963d-463f14269a5e""}],""Segments"":{}}}";

    [Fact]
    public void ParseContacts_()
    {
      var json = new[]
      {
        new
        {
          email = "dmd@sc.net",
          interactions = new object[]
          {
            new
            {
              recency="-1",
              pages = new object[0],
              outcomes = new object[]
              {
                new
                {
                  itemId = Guid.NewGuid().ToString()
                }
              },
              geoData = new GeoDataTmpClass
              {
                TimeZoneInfo = new
                {
                  Id = "Eastern Standard Time"
                }
              }
            }
          }
        }
      };

      var parser = new XGenParser("http://google.com");
      var segments = parser.ParseContacts(JToken.FromObject(json), JobType.Contacts);
      segments.Count().Should().Be(1);
      segments.All(x => x.Behavior().GetType() == typeof(StrictWalk)).Should().BeTrue();
      segments.First().VisitVariables.OfType<IdentifiedContactDataVariable>().Single().Email.Should().Be("dmd@sc.net");
      DateTime.Now.Subtract(segments.First().DateGenerator.Start.Value).TotalDays.Should().BeGreaterOrEqualTo(0.91);
    }

    [Fact]
    public void ParseContacts_NoOutcomes()
    {
      var json = new[]
      {
        new
        {
          email = "dmd@sc.net",
          interactions = new object[]
          {
            new
            {
              recency="-1",
              pages = new object[0],
              geoData = new GeoDataTmpClass
              {
                TimeZoneInfo = new
                {
                  Id = "Eastern Standard Time"
                }
              }
            }
          }
        }
      };

      var parser = new XGenParser("http://google.com");
      var segments = parser.ParseContacts(JToken.FromObject(json), JobType.Contacts);
      segments.Count().Should().Be(1);
      segments.All(x => x.Behavior().GetType() == typeof(StrictWalk)).Should().BeTrue();
      segments.First().VisitVariables.OfType<IdentifiedContactDataVariable>().Single().Email.Should().Be("dmd@sc.net");
      DateTime.Now.Subtract(segments.First().DateGenerator.Start.Value).TotalDays.Should().BeGreaterOrEqualTo(0.94);
    }
  }
}