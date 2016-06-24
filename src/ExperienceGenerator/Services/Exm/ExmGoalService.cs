using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Colossus.Statistics;
using ExperienceGenerator.Models.Exm;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Marketing.Definitions;
using Sitecore.Marketing.Definitions.Goals;
using Sitecore.Modules.EmailCampaign.Core;

namespace ExperienceGenerator.Services.Exm
{
    public class ExmGoalService
    {
        private readonly ExmDataPreparationModel _specification;
        private readonly List<ExmGoal> _goals = new List<ExmGoal>();
        private readonly Database _db = Factory.GetDatabase("master");
        private readonly ItemUtilExt _itemUtil = new ItemUtilExt();

        public Func<ExmGoal> GoalsSet { get; private set; }

        public ExmGoalService(ExmDataPreparationModel specification)
        {
            _specification = specification;
        }

        public void CreateGoals()
        {
            if (_specification.Goals == null || !_specification.Goals.Any())
            {
                return;
            }

            foreach (var goal in _specification.Goals)
            {
                CreateGoal(goal);
                _specification.Job.CompletedGoals++;
            }

            GoalsSet = _goals.ToArray().Uniform();
        }

        public void CreateGoal(ExmGoal goal)
        {
            _goals.Add(goal);

            var goalItemPath = "/sitecore/system/Marketing Control Panel/Goals/" + goal.Name;
            var goalItem = _db.GetItem(goalItemPath);
            if (goalItem == null)
            {
                var goalTemplate = _db.GetTemplate("{475E9026-333F-432D-A4DC-52E03B75CB6B}");

                goalItem = _db.CreateItemPath(goalItemPath, goalTemplate);
                using (new EditContext(goalItem))
                {
                    goalItem["Points"] = goal.Points.ToString();
                }

                _itemUtil.ExecuteWorkflowCommandForItem(goalItem, ItemIds.DeployAnalyticsCommand);
                var manager = DefinitionManagerFactory.Default.GetDefinitionManager<IGoalDefinition>("item");
                var definition = manager.Get(goalItem.ID, CultureInfo.GetCultureInfo("en"));
                manager.SaveAsync(definition, true);
            }

            var sampleTemplate = _db.GetTemplate("{76036F5E-CBCE-46D1-AF0A-4143F9B557AA}");

            var pageItemPath = Context.Site.StartPath + "/" + goal.Item;
            var pageItem = _db.GetItem(pageItemPath);
            if (pageItem == null)
            {
                pageItem = _db.CreateItemPath(pageItemPath, sampleTemplate);

                using (new EditContext(pageItem))
                {
                    pageItem["__Tracking"] = string.Format("<tracking><event id=\"{0}\" name=\"{1}\" /></tracking>", goalItem.ID, goalItem.DisplayName);
                }
            }
        }
    }
}