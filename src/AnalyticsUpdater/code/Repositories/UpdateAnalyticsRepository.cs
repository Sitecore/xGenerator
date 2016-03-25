namespace AnalyticsUpdater.Repositories
{
  using System;
  using System.Configuration;
  using System.Linq;
  using System.Threading;
  using Sitecore;
  using Sitecore.Analytics.Data.DataAccess.MongoDb;
  using Sitecore.Analytics.Model;
  using Sitecore.Analytics.Processing.ProcessingPool;
  using Sitecore.Configuration;
  using Sitecore.ContentSearch;
  using Sitecore.Data;
  using Sitecore.Data.Fields;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;
  using Sitecore.Jobs;

  public class UpdateAnalyticsVisitsRepository
  {
    private readonly Item _controlPannelSettings;
    private readonly Database _coreDb;
    private readonly Database _masterDb;

    public UpdateAnalyticsVisitsRepository()
    {
      _coreDb = Sitecore.Configuration.Factory.GetDatabase("core");
      _masterDb = Sitecore.Configuration.Factory.GetDatabase("master");
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
        
        RebuildAnalyticsIndex();
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
        var dataApi = new Sitecore.Data.SqlServer.SqlServerDataApi(connectionString);
        const string sql = "EXEC [dbo].[sp_sc_Refresh_Analytics] {2}lastUpdate{3};";

        dataApi.Execute(sql, new object[] { "lastUpdate", lastUpdate });
    }

    private static void UpdateTestStartDate(int dayspan, string dbName)
    {
        var connectionString = Settings.GetConnectionString(dbName);
        var dataApi = new Sitecore.Data.SqlServer.SqlServerDataApi(connectionString);
        const string sql = "EXEC [dbo].[sp_sc_Update_TestStartDate] @testItemID='{0}', @dayspan={1};";
        var db = Sitecore.Configuration.Factory.GetDatabase(dbName);
        var testsRoot = db.GetItem(ItemIDs.Analytics.MarketingCenter.TestLaboratory);
        var tests =
        testsRoot.Axes.GetDescendants()
          .Where(x => x.TemplateID == new ID("{45FB02E9-70B3-4CFE-8050-06EAD4B5DB3E}")); // Tests
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

    private static void RebuildAnalyticsIndex()
    {
        using (new Sitecore.SecurityModel.SecurityDisabler())
        {
          ContentSearchManager.GetIndex("sitecore_analytics_index").Reset();
          var poolPath = "aggregationProcessing/processingPools/live";
          var pool = Factory.CreateObject(poolPath, true) as ProcessingPool;
          var beforeRebuild = pool.GetCurrentStatus().ItemsPending;
          var driver = MongoDbDriver.FromConnectionString("analytics");
          var visitorData = driver.Interactions.FindAllAs<VisitData>();
          var keys = visitorData.Select(data => new InteractionKey(data.ContactId, data.InteractionId));
          foreach (var key in keys)
          {
            var poolItem = new ProcessingPoolItem(key.ToByteArray());
            pool.Add(poolItem);
          }

          while (pool.GetCurrentStatus().ItemsPending > beforeRebuild)
          {
            Thread.Sleep(1000);
          }
        }
    }

    private void MoveMongoDate(int days)
    {
      var refreshAnalyticsQuery = _masterDb.GetItem(new ID("{AD488697-75A8-4245-B391-BD1809D5BF58}"));
      var scriptTxt = refreshAnalyticsQuery.Fields["Query"].Value;
      var connectionStringName = refreshAnalyticsQuery.Fields["Data Source"].Value;
      var item = ConfigurationManager.ConnectionStrings[connectionStringName];
      //var driver = new JSMongoDbDriver(item.ConnectionString);
      //driver.Eval(scriptTxt, new object[] { days });
    }

    private void UpdateLastRefreshDate()
    {
      // update setting
      using (new EditContext(_controlPannelSettings))
      {
        _controlPannelSettings.Editing.BeginEdit();
        var lastUpdateField = new DateField(_controlPannelSettings.Fields["Last Refresh Date"]);
        lastUpdateField.Value = DateUtil.ToIsoDate(DateTime.UtcNow);
        _controlPannelSettings.Editing.EndEdit();
      }
    }

    private void UpdateCampaignItems(DateTime lastUpdate)
    {
      // update campaign items
      var campaignsRoot = _masterDb.GetItem(ItemIDs.Analytics.MarketingCenter.Campaigns);
      var campaigns =
        campaignsRoot.Axes.GetDescendants()
          .Where(x => x.TemplateID == new ID("{94FD1606-139E-46EE-86FF-BC5BF3C79804}")); // Campaign

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
  }
}
