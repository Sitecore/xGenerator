namespace ExperienceGenerator.Tests
{
  using ExperienceGenerator.Client;
  using Ploeh.AutoFixture;
  using Ploeh.AutoFixture.Xunit2;
  using Sitecore.Analytics.Data.Items;
  using Sitecore.FakeDb;
  using Sitecore.FakeDb.AutoFixture;

  internal class AutoDbDataAttribute : AutoDataAttribute
  {
    public AutoDbDataAttribute()
      : base(new Fixture().Customize(new AutoDbCustomization()))
    {
      var db = Fixture.Create<Db>();
      db.Add(new DbItem("Outcome", KnownItems.OutcomesRoot));
      db.Add(new DbTemplate("OutcomeDef", OutcomeDefinitionItem.TemplateID)
      {
        new DbField("Group")
      });

      db.Add(new DbTemplate("Spec", Templates.Preset.ID)
      {
        new DbField("Spec",Templates.Preset.Fields.Specification)
      });
      db.Add(new DbItem("Online", KnownItems.OnlineChannelRoot));
      db.Add(new DbItem("TR", KnownItems.TaxonomyRoot));
      db.Add(new DbItem("CR", KnownItems.CampaignsRoot));

    }
  }
}