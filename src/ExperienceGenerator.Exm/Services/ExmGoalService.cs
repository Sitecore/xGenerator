namespace ExperienceGenerator.Exm.Services
{
  using System;
  using System.Collections.Generic;
  using System.Globalization;
  using System.Linq;
  using Colossus.Statistics;
  using ExperienceGenerator.Exm.Models;
  using Sitecore;
  using Sitecore.Configuration;
  using Sitecore.Data;
  using Sitecore.Data.Items;
  using Sitecore.Marketing.Definitions;
  using Sitecore.Marketing.Definitions.Goals;
  using Sitecore.Modules.EmailCampaign.Core;

  public class ExmGoalService
    {
        private readonly ExmDataPreparationModel _specification;
        private readonly List<ExmGoal> _goals = new List<ExmGoal>();
        private readonly Database _db = Factory.GetDatabase("master");
        private readonly ItemUtilExt _itemUtil = new ItemUtilExt();

        public Func<ExmGoal> GoalsSet { get; private set; }

        public ExmGoalService(ExmDataPreparationModel specification)
        {
            this._specification = specification;
        }

        public void CreateGoals()
        {
            if (this._specification.Goals == null || !this._specification.Goals.Any())
            {
                return;
            }

            foreach (var goal in this._specification.Goals)
            {
                this.CreateGoal(goal);
                this._specification.Job.CompletedGoals++;
            }

            this.GoalsSet = this._goals.ToArray().Uniform();
        }

        public void CreateGoal(ExmGoal goal)
        {
            this._goals.Add(goal);

            var goalItemPath = "/sitecore/system/Marketing Control Panel/Goals/" + goal.Name;
            var goalItem = this._db.GetItem(goalItemPath);
            if (goalItem == null)
            {
                var goalTemplate = this._db.GetTemplate("{475E9026-333F-432D-A4DC-52E03B75CB6B}");

                goalItem = this._db.CreateItemPath(goalItemPath, goalTemplate);
                using (new EditContext(goalItem))
                {
                    goalItem["Points"] = goal.Points.ToString();
                }

                this._itemUtil.ExecuteWorkflowCommandForItem(goalItem, ItemIds.DeployAnalyticsCommand);
                var manager = DefinitionManagerFactory.Default.GetDefinitionManager<IGoalDefinition>("item");
                var definition = manager.Get(goalItem.ID, CultureInfo.GetCultureInfo("en"));
                manager.SaveAsync(definition, true);
            }

            var sampleTemplate = this._db.GetTemplate("{76036F5E-CBCE-46D1-AF0A-4143F9B557AA}");

            var pageItemPath = Context.Site.StartPath + "/" + goal.Item;
            var pageItem = this._db.GetItem(pageItemPath);
            if (pageItem == null)
            {
                pageItem = this._db.CreateItemPath(pageItemPath, sampleTemplate);

                using (new EditContext(pageItem))
                {
                    pageItem["__Tracking"] = string.Format("<tracking><event id=\"{0}\" name=\"{1}\" /></tracking>", goalItem.ID, goalItem.DisplayName);
                }
            }
        }
    }
}