namespace ExperienceGenerator.Tests
{
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Runtime.Remoting.Messaging;
  using Colossus;
  using Colossus.Integration;
  using Colossus.Integration.Behaviors;
  using ExperienceGenerator.Data;
  using ExperienceGenerator.Models;
  using ExperienceGenerator.Parsing;
  using ExperienceGenerator.Parsing.Factories;
  using FluentAssertions;
  using Newtonsoft.Json.Linq;
  using Ploeh.AutoFixture.Xunit2;
  using Xunit;

  public class XGenParserTests
  {


    [Theory]
    [AutoDbData]
    public void DevicesFactory_WithDeviceDistibution_ShouldSetRequiredUserAgents(XGenParser parser, DeviceRepository repo)
    {
      var factory = XGenParser.Factories["Devices"];
      var devices = repo.GetAll();
      List<string> userAgentList = new List<string>();

      var definition = JObject.Parse(@"{
        'sony xperia z3':0.1,
        'sony xperia z2':0.1,
        'nokia lumia 930':0.3,
        'macintosh pc':0.3,
        'microsoft xbox one':0.2}");


      for (int i = 0; i < 1000; i++)
      {
        var segment = new VisitorSegment("");
        factory.UpdateSegment(segment, definition, parser);
        var target = new Request();
        segment.VisitorVariables.First(x => x.ProvidedVariables.Contains("UserAgent")).SetValues(target);
        userAgentList.Add(target.Variables["UserAgent"].ToString());

      }

      userAgentList.Count.Should().Be(1000);

      foreach (var device in definition)
      {

        var dev = devices.First(x => x.Id == device.Key);
        var expected = device.Value.Value<double>();
        (userAgentList.Count(x => x == dev.UserAgent) / 1000d).Should().BeApproximately(expected, 0.04);

      }

    }

    [Fact]
    public void DevicesFactory_EmptyDefinition_ShouldSetRandomUserAgent()
    {
      var factory = XGenParser.Factories["Devices"];
      List<string> userAgentList = new List<string>();

      var definition = JObject.Parse(@"{}");


      for (int i = 0; i < 10; i++)
      {
        var segment = new VisitorSegment("");
        factory.UpdateSegment(segment, definition, null);
        var target = new Request();
        segment.VisitorVariables.First(x => x.ProvidedVariables.Contains("UserAgent")).SetValues(target);
        userAgentList.Add(target.Variables["UserAgent"].ToString());

      }

      //all useragent should be set and not be null
      userAgentList.Count.Should().Be(10);
      userAgentList.All(x=>!string.IsNullOrWhiteSpace(x)).Should().BeTrue();
    }

    private string TestJson = @"{ ""Type"":1,""VisitorCount"":1,""Specification"":{""Contacts"":[{""email"":""dmd@sc.net"",""firstName"":""Dmytro"",""lastName"":""Dashko"",""interactions"":[{""channelName"":""Organic non-branded search"",""recency"":""-12"",""pages"":[{""itemId"":""{8235F890-E581-4F79-8ACF-B04F90CCC0DD}"",""path"":""/sitecore/content/Legal/Home/News""},{""itemId"":""{6EB0F80B-A1D5-40E0-A472-9308B24228B5}"",""path"":""/sitecore/content/Legal/Home""},{""itemId"":""{53C59369-B0C7-40B2-B76F-DED99916D309}"",""path"":""/sitecore/content/Legal/Home/About Us""}],""itemId"":""95bdd524-a9ca-4f4e-a6f4-e68f1eff11e2"",""channelId"":""{B979A670-5AAF-4E63-94AC-C3C3E5BFBE84}"",""searchEngine"":""Bing"",""searchKeyword"":""tax"",""location"":""New York City US"",""geoData"":{""GeoNameId"":5128581,""Name"":""New York City"",""AsciiName"":""New York City"",""Latitude"":40.71427,""Longitude"":-74.00597,""FeatureClass"":""P"",""FeatureCode"":""PPL"",""CountryCode"":""US"",""Cc2"":[],""Admin1"":""NY"",""Admin2"":"""",""Admin3"":"""",""Admin4"":"""",""Population"":8175133,""Elevation"":10,""DigitalElevationModel"":57,""TimeZone"":""America/New_York"",""ModificationDate"":""2015-02-02T00:00:00"",""Country"":{""Iso"":""US"",""Iso3"":""USA"",""IsoNumeric"":840,""Fips"":""US"",""Name"":""United States"",""Capital"":""Washington"",""Area"":9629091,""Population"":310232863,""Continent"":""NA"",""TopLevelDomain"":"".us"",""DomainPostFix"":"".com"",""CurrencyCode"":""USD"",""CurrencyName"":""Dollar"",""Phone"":""1"",""PostalCodeFormat"":""#####-####"",""PostalCodeRegex"":""^\\d{5}(-\\d{4})?$"",""Languages"":[""en-US"",""es-US"",""haw"",""fr""],""GeoNameId"":6252001,""Neighbours"":[""CA"",""MX"",""CU""],""EquivalentFipsCode"":"""",""Region"":""AMER""}}},{""channelName"":""Organic non-branded search"",""recency"":""-9"",""pages"":[{""itemId"":""{8235F890-E581-4F79-8ACF-B04F90CCC0DD}"",""path"":""/sitecore/content/Legal/Home/News""},{""itemId"":""{6EB0F80B-A1D5-40E0-A472-9308B24228B5}"",""path"":""/sitecore/content/Legal/Home""}],""geoData"":{""GeoNameId"":5128581,""Name"":""New York City"",""AsciiName"":""New York City"",""Latitude"":40.71427,""Longitude"":-74.00597,""FeatureClass"":""P"",""FeatureCode"":""PPL"",""CountryCode"":""US"",""Cc2"":[],""Admin1"":""NY"",""Admin2"":"""",""Admin3"":"""",""Admin4"":"""",""Population"":8175133,""Elevation"":10,""DigitalElevationModel"":57,""TimeZone"":""America/New_York"",""ModificationDate"":""2015-02-02T00:00:00"",""Country"":{""Iso"":""US"",""Iso3"":""USA"",""IsoNumeric"":840,""Fips"":""US"",""Name"":""United States"",""Capital"":""Washington"",""Area"":9629091,""Population"":310232863,""Continent"":""NA"",""TopLevelDomain"":"".us"",""DomainPostFix"":"".com"",""CurrencyCode"":""USD"",""CurrencyName"":""Dollar"",""Phone"":""1"",""PostalCodeFormat"":""#####-####"",""PostalCodeRegex"":""^\\d{5}(-\\d{4})?$"",""Languages"":[""en-US"",""es-US"",""haw"",""fr""],""GeoNameId"":6252001,""Neighbours"":[""CA"",""MX"",""CU""],""EquivalentFipsCode"":"""",""Region"":""AMER""},""TimeZoneInfo"":{""Id"":""Eastern Standard Time"",""DisplayName"":""(UTC-05:00) Eastern Time (US & Canada)"",""StandardName"":""Eastern Standard Time"",""DaylightName"":""Eastern Daylight Time"",""BaseUtcOffset"":""-05:00:00"",""SupportsDaylightSavingTime"":true}},""itemId"":""8db1b281-9aaf-4e94-bd37-62e0a5c8c354"",""channelId"":""{B979A670-5AAF-4E63-94AC-C3C3E5BFBE84}"",""searchEngine"":""Google"",""location"":""New York City US"",""searchKeyword"":""corporate law""},{""channelName"":""Direct"",""recency"":""-7"",""pages"":[{""itemId"":""{6EB0F80B-A1D5-40E0-A472-9308B24228B5}"",""path"":""/sitecore/content/Legal/Home""},{""itemId"":""{53C59369-B0C7-40B2-B76F-DED99916D309}"",""path"":""/sitecore/content/Legal/Home/About Us""},{""itemId"":""{C9BFB6A6-5B20-42EF-82FD-E79CEC01CC80}"",""path"":""/sitecore/content/Legal/Home/Our Team/Eduardo Perez Jr""},{""itemId"":""{1BEE7175-8126-4F1F-B0D3-1C0C3879795D}"",""path"":""/sitecore/content/Legal/Home/Our Team/Jenna Ridley""}],""geoData"":{""GeoNameId"":5128581,""Name"":""New York City"",""AsciiName"":""New York City"",""Latitude"":40.71427,""Longitude"":-74.00597,""FeatureClass"":""P"",""FeatureCode"":""PPL"",""CountryCode"":""US"",""Cc2"":[],""Admin1"":""NY"",""Admin2"":"""",""Admin3"":"""",""Admin4"":"""",""Population"":8175133,""Elevation"":10,""DigitalElevationModel"":57,""TimeZone"":""America/New_York"",""ModificationDate"":""2015-02-02T00:00:00"",""Country"":{""Iso"":""US"",""Iso3"":""USA"",""IsoNumeric"":840,""Fips"":""US"",""Name"":""United States"",""Capital"":""Washington"",""Area"":9629091,""Population"":310232863,""Continent"":""NA"",""TopLevelDomain"":"".us"",""DomainPostFix"":"".com"",""CurrencyCode"":""USD"",""CurrencyName"":""Dollar"",""Phone"":""1"",""PostalCodeFormat"":""#####-####"",""PostalCodeRegex"":""^\\d{5}(-\\d{4})?$"",""Languages"":[""en-US"",""es-US"",""haw"",""fr""],""GeoNameId"":6252001,""Neighbours"":[""CA"",""MX"",""CU""],""EquivalentFipsCode"":"""",""Region"":""AMER""},""TimeZoneInfo"":{""Id"":""Eastern Standard Time"",""DisplayName"":""(UTC-05:00) Eastern Time (US & Canada)"",""StandardName"":""Eastern Standard Time"",""DaylightName"":""Eastern Daylight Time"",""BaseUtcOffset"":""-05:00:00"",""SupportsDaylightSavingTime"":true}},""itemId"":""61bf7e41-7324-4ee8-8587-fe27b742825e"",""channelId"":""{B418E4F2-1013-4B42-A053-B6D4DCA988BF}"",""searchEngine"":""Bing"",""location"":""New York City US""},{""channelName"":""Direct"",""recency"":""-6"",""pages"":[{""itemId"":""{6EB0F80B-A1D5-40E0-A472-9308B24228B5}"",""path"":""/sitecore/content/Legal/Home""},{""itemId"":""{BCF8524F-D93A-47FB-BD2F-D4D9B887A2A6}"",""path"":""/sitecore/content/Legal/Home/Landing Pages/Taxation Webinar/Taxation Webinar More Info""},{""itemId"":""{B71088CD-0CB3-4C66-A066-B819312CB7ED}"",""path"":""/sitecore/content/Legal/Home/Landing Pages/Taxation Webinar/Thank You""}],""geoData"":{""GeoNameId"":4930956,""Name"":""Boston"",""AsciiName"":""Boston"",""Latitude"":42.35843,""Longitude"":-71.05977,""FeatureClass"":""P"",""FeatureCode"":""PPLA"",""CountryCode"":""US"",""Cc2"":[],""Admin1"":""MA"",""Admin2"":""025"",""Admin3"":"""",""Admin4"":"""",""Population"":617594,""Elevation"":14,""DigitalElevationModel"":38,""TimeZone"":""America/New_York"",""ModificationDate"":""2014-10-27T00:00:00"",""Country"":{""Iso"":""US"",""Iso3"":""USA"",""IsoNumeric"":840,""Fips"":""US"",""Name"":""United States"",""Capital"":""Washington"",""Area"":9629091,""Population"":310232863,""Continent"":""NA"",""TopLevelDomain"":"".us"",""DomainPostFix"":"".com"",""CurrencyCode"":""USD"",""CurrencyName"":""Dollar"",""Phone"":""1"",""PostalCodeFormat"":""#####-####"",""PostalCodeRegex"":""^\\d{5}(-\\d{4})?$"",""Languages"":[""en-US"",""es-US"",""haw"",""fr""],""GeoNameId"":6252001,""Neighbours"":[""CA"",""MX"",""CU""],""EquivalentFipsCode"":"""",""Region"":""AMER""}},""itemId"":""f9cf88ea-6217-40f5-9f5a-7d102c0ecca1"",""channelId"":""{B418E4F2-1013-4B42-A053-B6D4DCA988BF}"",""searchEngine"":""Bing"",""location"":""Boston US""},{""channelName"":""Email newsletter"",""recency"":""-5"",""pages"":[{""itemId"":""{62B8DE00-380C-4287-9541-CEE68E1DA9D7}"",""path"":""/sitecore/content/Legal/Home/Landing Pages/Taxation Webinar/Taxation Webinar Register"",""goals"":[{""itemId"":""{95F2499A-DC5E-4CE1-B45C-0CA42BF239FB}"",""$displayName"":""Registered Legal Webinar""}]},{""itemId"":""{B71088CD-0CB3-4C66-A066-B819312CB7ED}"",""path"":""/sitecore/content/Legal/Home/Landing Pages/Taxation Webinar/Thank You""},{""itemId"":""{6EB0F80B-A1D5-40E0-A472-9308B24228B5}"",""path"":""/sitecore/content/Legal/Home""},{""itemId"":""{53FDA0DA-101E-412C-AA38-04F5C9117DF7}"",""path"":""/sitecore/media library/Legal/Documents/WHITEPAPER A Legal Perspective on Recent Tax Developments""}],""geoData"":{""GeoNameId"":4930956,""Name"":""Boston"",""AsciiName"":""Boston"",""Latitude"":42.35843,""Longitude"":-71.05977,""FeatureClass"":""P"",""FeatureCode"":""PPLA"",""CountryCode"":""US"",""Cc2"":[],""Admin1"":""MA"",""Admin2"":""025"",""Admin3"":"""",""Admin4"":"""",""Population"":617594,""Elevation"":14,""DigitalElevationModel"":38,""TimeZone"":""America/New_York"",""ModificationDate"":""2014-10-27T00:00:00"",""Country"":{""Iso"":""US"",""Iso3"":""USA"",""IsoNumeric"":840,""Fips"":""US"",""Name"":""United States"",""Capital"":""Washington"",""Area"":9629091,""Population"":310232863,""Continent"":""NA"",""TopLevelDomain"":"".us"",""DomainPostFix"":"".com"",""CurrencyCode"":""USD"",""CurrencyName"":""Dollar"",""Phone"":""1"",""PostalCodeFormat"":""#####-####"",""PostalCodeRegex"":""^\\d{5}(-\\d{4})?$"",""Languages"":[""en-US"",""es-US"",""haw"",""fr""],""GeoNameId"":6252001,""Neighbours"":[""CA"",""MX"",""CU""],""EquivalentFipsCode"":"""",""Region"":""AMER""}},""itemId"":""43ea93b4-7373-4eb4-aa51-10f63c39a66c"",""channelId"":""{52B75873-4CE0-4E98-B63A-B535739E6180}"",""searchEngine"":""Bing"",""location"":""Boston US"",""campaignName"":""Email Legal Webinar"",""campaignId"":""{356A7E54-45E1-4860-AFBB-F922E8A3018F}"",""outcomes"":[{""itemId"":""{C2D9DFBC-E465-45FD-BA21-0A06EBE942D6}"",""$displayName"":""Sales Lead""}]},{""channelName"":""Direct"",""recency"":""-4"",""pages"":[{""itemId"":""{6EB0F80B-A1D5-40E0-A472-9308B24228B5}"",""path"":""/sitecore/content/Legal/Home""},{""itemId"":""{A48C26AA-CF72-4136-B111-5FD5CAD9AB43}"",""path"":""/sitecore/content/Legal/Home/News/2016/01/07/14/58/Corporate Legal Get Ready for Three Changes in 2016""},{""itemId"":""{44C47F39-9897-4C24-998A-2DB858F09401}"",""path"":""/sitecore/content/Legal/Home/News/2016/01/07/14/54/Legal Issues With Corporate Bring Your Own Device""},{""itemId"":""{53C59369-B0C7-40B2-B76F-DED99916D309}"",""path"":""/sitecore/content/Legal/Home/About Us""},{""itemId"":""{B7264CE2-345E-4760-A3A8-619435E65A89}"",""path"":""/sitecore/content/Legal/Home/Our Team/Brenda Barkley""},{""itemId"":""{F84917ED-9ACA-4A6F-B65F-00E6B209D371}"",""path"":""/sitecore/content/Legal/Home/Our Team/Shane Nevins""}],""geoData"":{""GeoNameId"":5128581,""Name"":""New York City"",""AsciiName"":""New York City"",""Latitude"":40.71427,""Longitude"":-74.00597,""FeatureClass"":""P"",""FeatureCode"":""PPL"",""CountryCode"":""US"",""Cc2"":[],""Admin1"":""NY"",""Admin2"":"""",""Admin3"":"""",""Admin4"":"""",""Population"":8175133,""Elevation"":10,""DigitalElevationModel"":57,""TimeZone"":""America/New_York"",""ModificationDate"":""2015-02-02T00:00:00"",""Country"":{""Iso"":""US"",""Iso3"":""USA"",""IsoNumeric"":840,""Fips"":""US"",""Name"":""United States"",""Capital"":""Washington"",""Area"":9629091,""Population"":310232863,""Continent"":""NA"",""TopLevelDomain"":"".us"",""DomainPostFix"":"".com"",""CurrencyCode"":""USD"",""CurrencyName"":""Dollar"",""Phone"":""1"",""PostalCodeFormat"":""#####-####"",""PostalCodeRegex"":""^\\d{5}(-\\d{4})?$"",""Languages"":[""en-US"",""es-US"",""haw"",""fr""],""GeoNameId"":6252001,""Neighbours"":[""CA"",""MX"",""CU""],""EquivalentFipsCode"":"""",""Region"":""AMER""}},""itemId"":""2f04937f-c2ac-488b-a41e-ec8f2d8ed4c3"",""channelId"":""{B418E4F2-1013-4B42-A053-B6D4DCA988BF}"",""searchEngine"":""Bing"",""location"":""New York City US""},{""channelName"":""Organic non-branded search"",""recency"":""-3"",""pages"":[{""itemId"":""{D302EFA4-1332-4D8C-9F5D-F5D99AC5884A}"",""path"":""/sitecore/content/Legal/Home/Practise Areas/Trusts Estates and Tax Law""},{""itemId"":""{65B4B9D9-14D9-4338-A495-7D217F41FEC6}"",""path"":""/sitecore/media library/Legal/Documents/WHITEPAPER Corporate Law""}],""geoData"":{""GeoNameId"":5128581,""Name"":""New York City"",""AsciiName"":""New York City"",""Latitude"":40.71427,""Longitude"":-74.00597,""FeatureClass"":""P"",""FeatureCode"":""PPL"",""CountryCode"":""US"",""Cc2"":[],""Admin1"":""NY"",""Admin2"":"""",""Admin3"":"""",""Admin4"":"""",""Population"":8175133,""Elevation"":10,""DigitalElevationModel"":57,""TimeZone"":""America/New_York"",""ModificationDate"":""2015-02-02T00:00:00"",""Country"":{""Iso"":""US"",""Iso3"":""USA"",""IsoNumeric"":840,""Fips"":""US"",""Name"":""United States"",""Capital"":""Washington"",""Area"":9629091,""Population"":310232863,""Continent"":""NA"",""TopLevelDomain"":"".us"",""DomainPostFix"":"".com"",""CurrencyCode"":""USD"",""CurrencyName"":""Dollar"",""Phone"":""1"",""PostalCodeFormat"":""#####-####"",""PostalCodeRegex"":""^\\d{5}(-\\d{4})?$"",""Languages"":[""en-US"",""es-US"",""haw"",""fr""],""GeoNameId"":6252001,""Neighbours"":[""CA"",""MX"",""CU""],""EquivalentFipsCode"":"""",""Region"":""AMER""}},""itemId"":""7838430b-17fd-4510-8fad-c434272ac178"",""channelId"":""{B979A670-5AAF-4E63-94AC-C3C3E5BFBE84}"",""searchEngine"":""Bing"",""location"":""New York City US"",""searchKeyword"":""takeover""}],""itemId"":""125a3339-6d48-4c66-b334-0f94778116cf"",""jobTitle"":""Dev"",""gender"":""Male"",""phone"":""066"",""address"":""Dnipro"",""middleName"":""Oleksandrovych"",""birthday"":""19920525T000000""}],""Segments"":{}}}";

    [Fact]
    public void ParseContacts_TestJson_ShouldNotFail()
    {
     
      var parser = new XGenParser("http://google.com");
      var segments = parser.ParseContacts(JToken.Parse(TestJson)["Specification"]["Contacts"],JobType.Contacts);
      segments.Count().Should().Be(7);
      segments.All(x => x.Behavior().GetType() == typeof(StrictWalk)).Should().BeTrue();
      segments.All(x => x.VisitVariables.OfType<IdentifiedContactDataVariable>().Single().Email =="dmd@sc.net").Should().BeTrue(); 

      DateTime.Now.Subtract(segments.First().DateGenerator.Start.Value).TotalDays.Should().BeGreaterOrEqualTo(0.91);
    }


    [Fact]
    public void ParseContacts_ShouldParseEmail()
    {

      
      var parser = new XGenParser("http://google.com");
      var segment = parser.ParseContacts(this.GetContactsDefinition(), JobType.Contacts).Single();
      segment.Behavior().Should().BeOfType<StrictWalk>();
      segment.VisitVariables.OfType<IdentifiedContactDataVariable>().Single().Email.Should().Be("dmd@sc.net");
    }

    private JToken GetContactsDefinition()
    {
      return JToken.FromObject(new[]
      {
        new
        {
          email = "dmd@sc.net",
          interactions = new[]
          {
            new Interaction()
            {
              Recency = "-1",
            }
          }
        }
      });
    }

    [Fact]
    public void ParseContacts_ContactBehaviorShouldBeStrictWalk()
    {
     

      var parser = new XGenParser("http://google.com");
      var segment = parser.ParseContacts(this.GetContactsDefinition(), JobType.Contacts).Single();
      segment.Behavior().Should().BeOfType<StrictWalk>();
    }

    [Fact]
    public void ParseContacts_EmptyInteraction_ShouldNotFail()
    {
      var json = new[]
      {
        new
        {
          interactions = new object[]
          {
            new
            {
              recency="-1",
            }
          }
        }
      };

      
      Action segments = ()=> new XGenParser("http://google.com").ParseContacts(JToken.FromObject(json), JobType.Contacts);
      segments.ShouldNotThrow();
    }

   
  }
}