namespace ExperienceGenerator.Tests
{
  using System;
  using ExperienceGenerator.Client;
  using ExperienceGenerator.Parsing;
  using Ploeh.AutoFixture;
  using Ploeh.AutoFixture.AutoNSubstitute;
  using Ploeh.AutoFixture.Kernel;
  using Ploeh.AutoFixture.Xunit2;
  using Sitecore.Analytics.Data.Items;
  using Sitecore.FakeDb;
  using Sitecore.FakeDb.AutoFixture;
  using Xunit.Abstractions;

  internal class InlineAutoDbDataAttribute : InlineAutoDataAttribute
  {
    public InlineAutoDbDataAttribute(params object[] values)
      : base(new AutoDbDataAttribute(),values)
    {
      
    }
  }



  internal class AutoDbDataAttribute : AutoDataAttribute
  {
    public AutoDbDataAttribute()
      : base(new Fixture().Customize(new AutoDbCustomization()))
    {
      Fixture.Customize(new AutoConfiguredNSubstituteCustomization());
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

      Fixture.Customizations.Add(new XGenBuilder());

    }
  }
  public class XGenBuilder : ISpecimenBuilder
  {
    public object Create(object request, ISpecimenContext context)
    {
      var type = request as Type;
      if (type != null && type == typeof(XGenParser))
      {
        return new XGenParser("http://type.info");
      }

      return new NoSpecimen();
    }
  }
}
