using Sitecore.FakeDb.Sites;
using Sitecore.Sites;

namespace ExperienceGenerator.Tests
{
  using System;
  using System.Linq;
  using ExperienceGenerator.Client;
  using ExperienceGenerator.Client.Controllers;
  using ExperienceGenerator.Client.Repositories;
  using FluentAssertions;
  using Newtonsoft.Json.Linq;
  using Ploeh.AutoFixture.Xunit2;
  using Sitecore.Analytics.Data.Items;
  using Sitecore.Data;
  using Sitecore.Data.Items;
  using Sitecore.FakeDb;
  using Xunit;

  public class ContactSettingsRepositoryTests
  {
    private static readonly JArray EmptySpecification = JArray.Parse("[{}]");

    [Theory]
    [AutoDbData]
    public void Save_OnePresent_ShouldReturnSingleSetting(Db db, [Greedy]ContactSettingsRepository repo)
    {
      var siteContext = new FakeSiteContext("siteName");
      using (new FakeSiteContextSwitcher(siteContext))
      {
        CreateItem(db, $"/sitecore/client/Applications/ExperienceGenerator/Common/Contacts/{siteContext.Name}");
        repo.Save("name", EmptySpecification);
        repo.GetPresets().Count.Should().Be(1);
      }
    }



    [Theory]
    [AutoDbData]
    public void Save_SpecPassed_ShouldBeSavedCorrectly(Db db, [Greedy]ContactSettingsRepository repo)
    {
      var siteContext = new FakeSiteContext("siteName");
      using (new FakeSiteContextSwitcher(siteContext))
      {
        CreateItem(db, $"/sitecore/client/Applications/ExperienceGenerator/Common/Contacts/{siteContext.Name}");
        repo.Save("name", EmptySpecification);
        var savedSpec =
          db.GetItem($"/sitecore/client/Applications/ExperienceGenerator/Common/Contacts/{siteContext.Name}/name")[
            Templates.Preset.Fields.Specification];
        JArray.Parse(savedSpec).ToString().Should().Be(EmptySpecification.ToString());
      }
    }



    [Theory]
    [AutoDbData]
    public void Save_WithExistingItemName_ShouldOverrideItem(Db db,[Greedy]ContactSettingsRepository repo)
    {
      var siteContext = new FakeSiteContext("siteName");
      using (new FakeSiteContextSwitcher(siteContext))
      {
        CreateItem(db, $"/sitecore/client/Applications/ExperienceGenerator/Common/Contacts/{siteContext.Name}");
        repo.Save("name", EmptySpecification);
        var savedSpec =
          db.GetItem($"/sitecore/client/Applications/ExperienceGenerator/Common/Contacts/{siteContext.Name}/name")[
            Templates.Preset.Fields.Specification];
        JArray.Parse(savedSpec).ToString().Should().Be(EmptySpecification.ToString());
        var jToken = (JArray) EmptySpecification.DeepClone();
        jToken[0]["someKey"] = "someVal";
        repo.Save("name", jToken);
        savedSpec =
          db.GetItem($"/sitecore/client/Applications/ExperienceGenerator/Common/Contacts/{siteContext.Name}/name")[
            Templates.Preset.Fields.Specification];
        JArray.Parse(savedSpec).ToString().Should().Be(jToken.ToString());
      }
    }


    [Theory]
    [AutoDbData]
    public void ContactSettingsRepository_NoPresets_ShouldReturnEmptyCollection(Db db, [Greedy]ContactSettingsRepository repo)
    {
      var siteContext = new FakeSiteContext("siteName");
      using (new FakeSiteContextSwitcher(siteContext))
      {
        CreateItem(db, $"/sitecore/client/Applications/ExperienceGenerator/Common/Contacts/{siteContext.Name}");
        repo.GetPresets().Count.Should().Be(0);
      }
    }

    private void CreateItem(Db db, string fullItemPath)
    {
      var paths = fullItemPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

      var currentPath = "/" + paths.First();
      foreach (var path in paths.Skip(1))
      {
        var item = db.GetItem(currentPath);
        if (item != null)
        {
          item.Add(path, new TemplateID(ID.NewID));
          currentPath += "/" + path;
        }
      }
    }
  }
}