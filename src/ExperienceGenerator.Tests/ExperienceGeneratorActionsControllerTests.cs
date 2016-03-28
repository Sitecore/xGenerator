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
  using Sitecore.Analytics.Data.Items;
  using Sitecore.Data;
  using Sitecore.Data.Items;
  using Sitecore.FakeDb;
  using Xunit;

  public class ExperienceGeneratorActionsControllerTests
  {
    [Theory]
    [AutoDbData]
    public void Options_OutcomesWithGroup_ShouldReturnSingleOutcomeGroup(Db db)
    {
      var controller = new ExperienceGeneratorActionsController();
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
      options.OutcomeGroups.First().Channels.Count.Should().Be(1);
      options.OutcomeGroups.First().Channels.First().Id.Should().Be(outcome.ID.ToString());

    }

    [Theory]
    [AutoDbData]
    public void Options_OutcomesWithoutGroup_ShouldReturnSingleOutcomeGroup(Db db)
    {
      var controller = new ExperienceGeneratorActionsController();

      var item = db.GetItem(KnownItems.OutcomesRoot);
      var outcome = item.Add("SampleOutcome", new TemplateID(OutcomeDefinitionItem.TemplateID));
     
      var options = controller.Options();
      options.OutcomeGroups.Count.Should().Be(1);
      options.OutcomeGroups.First().Channels.Count.Should().Be(1);
      options.OutcomeGroups.First().Label.Should().Be("None");


    }


  }
}
