namespace ExperienceGenerator.Tests
{
  using System;
  using System.Linq;
  using ExperienceGenerator.Client;
  using ExperienceGenerator.Client.Controllers;
  using ExperienceGenerator.Client.Repositories;
  using FluentAssertions;
  using Newtonsoft.Json.Linq;
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
    public void Save_OnePresent_ShouldReturnSingleSetting(Db db)
    {
      CreateItem(db, "/sitecore/client/Applications/ExperienceGenerator/Common/Contacts");

      var repo = new ContactSettingsRepository(db.Database);

      repo.Save("name", EmptySpecification);
      repo.GetPresets().Count.Should().Be(1);
    }



    [Theory]
    [AutoDbData]
    public void Save_SpecPassed_ShouldBeSavedCorrectly(Db db)
    {
      CreateItem(db, "/sitecore/client/Applications/ExperienceGenerator/Common/Contacts");

      var repo = new ContactSettingsRepository(db.Database);

      repo.Save("name", EmptySpecification);
      var savedSpec = db.GetItem("/sitecore/client/Applications/ExperienceGenerator/Common/Contacts/name")[Templates.Preset.Fields.Specification];
      JArray.Parse(savedSpec).ToString().Should().Be(EmptySpecification.ToString());
    }


    [Theory]
    [AutoDbData]
    public void ContactSettingsRepository_NoPresets_ShouldReturnEmptyCollection(Db db)
    {
      CreateItem(db, "/sitecore/client/Applications/ExperienceGenerator/Common/Contacts");

      var repo = new ContactSettingsRepository(db.Database);
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