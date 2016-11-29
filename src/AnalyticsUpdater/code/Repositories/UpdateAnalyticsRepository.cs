using System;
using System.Configuration;
using System.Linq;
using System.Threading;
using MongoDB.Bson;
using MongoDB.Driver;
using Sitecore;
using Sitecore.Analytics;
using Sitecore.Analytics.Data.DataAccess.MongoDb;
using Sitecore.Analytics.Model;
using Sitecore.Analytics.Processing.ProcessingPool;
using Sitecore.Configuration;
using Sitecore.ContentSearch;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.SqlServer;
using Sitecore.Diagnostics;
using Sitecore.Jobs;
using Sitecore.SecurityModel;

namespace AnalyticsUpdater.Repositories
{
    public class UpdateAnalyticsVisitsRepository
    {
        private readonly Item _controlPannelSettings;
        private readonly Database _coreDb;
        private readonly Database _masterDb;

        public UpdateAnalyticsVisitsRepository()
        {
            _coreDb = Factory.GetDatabase("core");
            _masterDb = Factory.GetDatabase("master");
            _controlPannelSettings = _coreDb.GetItem(new ID("{CFAF8BF6-7AB2-4A3D-8A87-35F64D0D8FD8}"));
        }

        public void Run()
        {
            var status = Context.Job.Status;
            try
            {
                var lastUpdate = GetLastRefreshDate();

                RefreshSqlAnalytics(lastUpdate);

                var span = DateTime.Now - lastUpdate;
                UpdateTestStartDate(span.Days, "master");
                UpdateTestStartDate(span.Days, "web");

                MoveMongoDate(span.Days);

                UpdateCampaignItems(lastUpdate);

                UpdateLastRefreshDate();

                RebuildAnalyticsIndexService.RebuildAnalyticsIndex();
            }
            catch (Exception ex)
            {
                Log.Error("Update analytics visits on today", ex, this);

                status.Failed = true;
                status.Messages.Add(ex.Message);
                status.State = JobState.Finished;
            }

            status.Messages.Add("Analytics has been successfully refreshed.");
            status.State = JobState.Finished;
        }

        private static void RefreshSqlAnalytics(DateTime lastUpdate)
        {
            var connectionString = Settings.GetConnectionString("reporting");
            var dataApi = new SqlServerDataApi(connectionString);
            const string sql = "EXEC [dbo].[sp_sc_Refresh_Analytics] {2}lastUpdate{3};";

            dataApi.Execute(sql, "lastUpdate", lastUpdate);
        }

        private static void UpdateTestStartDate(int dayspan, string dbName)
        {
            var connectionString = Settings.GetConnectionString(dbName);
            var dataApi = new SqlServerDataApi(connectionString);
            const string sql = "EXEC [dbo].[sp_sc_Update_TestStartDate] @testItemID='{0}', @dayspan={1};";
            var db = Factory.GetDatabase(dbName);
            var testsRoot = db.GetItem(AnalyticsIds.TestLaboratory);
            var tests = testsRoot.Axes.GetDescendants().Where(x => x.TemplateID == new ID("{45FB02E9-70B3-4CFE-8050-06EAD4B5DB3E}")); // Tests
            foreach (var test in tests)
            {
                var command = string.Format(sql, test.ID.Guid, dayspan);
                dataApi.ExecuteNoResult(command);

                using (new EditContext(test, false, false))
                {
                    var dateStr = test.Fields["__Updated"].Value;
                    var date = DateUtil.ParseDateTime(dateStr, DateTime.MinValue);
                    date = date.AddDays(dayspan);
                    var updateddateStr = DateUtil.ToIsoDate(date);
                    test.Fields["__Updated"].Value = updateddateStr;
                }
            }
        }

        private void MoveMongoDate(int days)
        {
            var refreshAnalyticsQuery = _masterDb.GetItem(new ID("{AD488697-75A8-4245-B391-BD1809D5BF58}"));
            var scriptTxt = refreshAnalyticsQuery.Fields["Query"].Value;
            var connectionStringName = refreshAnalyticsQuery.Fields["Data Source"].Value;
            var item = ConfigurationManager.ConnectionStrings[connectionStringName];
            var driver = new UpdateAnalyticsVisitsRepository.JsMongoDbDriver(item.ConnectionString);
            driver.Eval(scriptTxt, days);
        }

        private void UpdateLastRefreshDate()
        {
            // update setting
            using (new EditContext(_controlPannelSettings))
            {
                _controlPannelSettings.Editing.BeginEdit();
                var lastUpdateField = new DateField(_controlPannelSettings.Fields["Last Refresh Date"])
                                      {
                                          Value = DateUtil.ToIsoDate(DateTime.UtcNow)
                                      };
                _controlPannelSettings.Editing.EndEdit();
            }
        }

        private void UpdateCampaignItems(DateTime lastUpdate)
        {
            // update campaign items
            var campaignsRoot = _masterDb.GetItem(AnalyticsIds.CampaignRoot);
            var campaigns = campaignsRoot.Axes.GetDescendants().Where(x => x.TemplateID == AnalyticsIds.Campaign); // Campaign

            var daySpan = DateTime.UtcNow - lastUpdate;
            foreach (var campaign in campaigns)
            {
                UpdateCampaign(campaign, daySpan);
            }
        }

        private static void UpdateCampaign(Item campaign, TimeSpan daySpan)
        {
            var startDateField = new DateField(campaign.Fields["StartDate"]);
            var endDateField = new DateField(campaign.Fields["EndDate"]);

            // update item field
            using (new EditContext(campaign))
            {
                campaign.Editing.BeginEdit();
                if (!string.IsNullOrEmpty(startDateField.Value))
                {
                    startDateField.Value = DateUtil.ToIsoDate(startDateField.DateTime.AddDays(daySpan.Days));
                }

                if (!string.IsNullOrEmpty(endDateField.Value))
                {
                    endDateField.Value = DateUtil.ToIsoDate(endDateField.DateTime.AddDays(daySpan.Days));
                }

                campaign.Editing.EndEdit();
            }
        }

        private DateTime GetLastRefreshDate()
        {
            var lastUpdateField = new DateField(_controlPannelSettings.Fields["Last Refresh Date"]);
            return lastUpdateField.DateTime;
        }

        private class JsMongoDbDriver : MongoDbDriver
        {
            public JsMongoDbDriver(string connectionString) : base(connectionString)
            {
            }

            public BsonValue Eval(BsonJavaScript code, params object[] args)
            {
                return Database.Eval(EvalFlags.None, code, args);
            }
        }
    }
}
