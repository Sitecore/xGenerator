namespace ExperienceGenerator.Tests
{
  using System;
  using System.IO;
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
  using Sitecore.FakeDb.Sites;
  using Sitecore.Sites;
  using Xunit;

  public class XgenSettingsRepositoryTests
  {
    private static readonly JObject EmptySpecification = JObject.Parse("{}");
    private static readonly string PresetsRoot = "/sitecore/client/Applications/ExperienceGenerator/Common/Presets/";
    [Theory]
    [AutoDbData]

    public void Save_OnePresent_ShouldReturnSingleSetting(string settingName, [Frozen(As = typeof(SiteContext))] FakeSiteContext sitecoreContext, FakeSiteContextSwitcher siteContextSwitcher, Db db, [Greedy] SettingsRepository repo)
    {
      CreateItem(db, PresetsRoot + sitecoreContext.Name);
      repo.Save(settingName, EmptySpecification);
      repo.GetPresets().Count.Should().Be(1);
    }



    [Theory]
    [AutoDbData]
    public void Save_SpecPassed_ShouldBeSavedCorrectly(string settingName, [Frozen(As = typeof(SiteContext))] FakeSiteContext sitecoreContext, FakeSiteContextSwitcher siteContextSwitcher, Db db, [Greedy]SettingsRepository repo)
    {
      CreateItem(db, PresetsRoot + sitecoreContext.Name);
      repo.Save(settingName, EmptySpecification);
      var itemPath = String.Concat(PresetsRoot, sitecoreContext.Name, "/" , settingName);
      var savedSpec = db.GetItem(itemPath)[Templates.Preset.Fields.Specification];
      JObject.Parse(savedSpec).ToString().Should().Be(EmptySpecification.ToString());
    }



    [Theory]
    [AutoDbData]
    public void Save_WithExistingItemName_ShouldOverrideItem(string settingName, [Frozen(As = typeof(SiteContext))] FakeSiteContext sitecoreContext, FakeSiteContextSwitcher siteContextSwitcher, Db db, [Greedy]SettingsRepository repo)
    {
      CreateItem(db, PresetsRoot + sitecoreContext.Name);
      repo.Save(settingName, EmptySpecification);
      var settingItemPath = String.Concat(PresetsRoot, sitecoreContext.Name, "/", settingName);
      var savedSpec = db.GetItem(settingItemPath)[Templates.Preset.Fields.Specification];
      JObject.Parse(savedSpec).ToString().Should().Be(EmptySpecification.ToString());
      var jToken = (JObject)EmptySpecification.DeepClone();
      jToken["someKey"] = "someVal";
      repo.Save(settingName, jToken);
      savedSpec = db.GetItem(settingItemPath)[Templates.Preset.Fields.Specification];
      JObject.Parse(savedSpec).ToString().Should().Be(jToken.ToString());

    }


    [Theory]
    [AutoDbData]
    public void ContactSettingsRepository_NoPresets_ShouldReturnEmptyCollection([Frozen(As = typeof(SiteContext))] FakeSiteContext sitecoreContext, FakeSiteContextSwitcher siteContextSwitcher, Db db, [Greedy]SettingsRepository repo)
    {
      CreateItem(db, PresetsRoot + sitecoreContext.Name);

      repo.GetPresets().Count.Should().Be(0);
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