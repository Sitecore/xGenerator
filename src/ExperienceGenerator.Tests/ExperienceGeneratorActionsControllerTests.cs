using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExperienceGenerator.Tests
{
  using ExperienceGenerator.Client;
  using ExperienceGenerator.Client.Controllers;
  using FluentAssertions;
  using Ploeh.AutoFixture.Xunit2;
  using Sitecore.Analytics.Data.Items;
  using Sitecore.Data;
  using Sitecore.Data.Items;
  using Sitecore.FakeDb;
  using Xunit;

  public class ExperienceGeneratorActionsControllerTests
  {
    [Theory]
    [AutoDbData]
    public void Options_OutcomesWithGroup_ShouldReturnSingleOutcomeGroup(Db db, [NoAutoProperties]ExperienceGeneratorActionsController controller)
    {
      var outcomeGroup = db.GetItem(KnownItems.TaxonomyRoot).Add("OutcomeGroup", new TemplateID(OutcomegroupItem.TemplateID));

      var item = db.GetItem(KnownItems.OutcomesRoot);
      var outcome = item.Add("SampleOutcome", new TemplateID(OutcomeDefinitionItem.TemplateID));
      using (new EditContext(outcome))
      {
        outcome["Group"] = outcomeGroup.ID.ToString();
      }
      var options = controller.Options();
      options.OutcomeGroups.Count.Should().Be(1);
      options.OutcomeGroups.First().Label.Should().Be("OutcomeGroup");
      options.OutcomeGroups.First().Options.Count().Should().Be(1);
      options.OutcomeGroups.First().Options.First().Id.Should().Be(outcome.ID.ToString());

    }

    [Theory]
    [AutoDbData]
    public void Options_OutcomesWithoutGroup_ShouldReturnSingleOutcomeGroup(Db db, [NoAutoProperties]ExperienceGeneratorActionsController controller)
    {

      var item = db.GetItem(KnownItems.OutcomesRoot);
      var outcome = item.Add("SampleOutcome", new TemplateID(OutcomeDefinitionItem.TemplateID));

      var options = controller.Options();
      options.OutcomeGroups.Count.Should().Be(1);
      options.OutcomeGroups.First().Options.Count().Should().Be(1);
      options.OutcomeGroups.First().Label.Should().Be("None");


    }
    [Theory]
    [InlineAutoDbData("Asia", new[] { "Southern Asia", "Western Asia", "South-Eastern Asia" })]
    [InlineAutoDbData("Africa", new[] { "Eastern Africa", "Middle Africa", "Northern Africa", "Southern Africa", "Western Africa" })]
    [InlineAutoDbData("Europe", new[] { "Eastern Europe", "Western Europe", "Southern Europe", "Northern Europe" })]
    [InlineAutoDbData("Americas", new[] { "Caribbean", "Northern America", "Central America", "South America" })]
    [InlineAutoDbData("Oceania", new[] { "Australia and New Zealand", "Melanesia", "Micronesia", "Polynesia" })]
    public void Options_Georegions_ShouldContainAllSubRegionsForAsia(string region, IEnumerable<string> expectedSubRegions)
    {
      var options = new ExperienceGeneratorActionsController().Options();
      var acualSubRegions = options.LocationGroups.Single(x => x.Label == region).Options.Select(x => x.Label);
      foreach (var expectedSubRegion in expectedSubRegions)
      {
        acualSubRegions.Should().Contain(expectedSubRegion);
      }




    }

  

  }
}
