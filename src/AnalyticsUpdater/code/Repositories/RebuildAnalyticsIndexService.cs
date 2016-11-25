using System.Linq;
using System.Threading;
using Sitecore.Analytics.Data.DataAccess.MongoDb;
using Sitecore.Analytics.Model;
using Sitecore.Analytics.Processing.ProcessingPool;
using Sitecore.Configuration;
using Sitecore.ContentSearch;
using Sitecore.SecurityModel;

namespace AnalyticsUpdater.Repositories
{
    public class RebuildAnalyticsIndexService
    {
        public static void RebuildAnalyticsIndex()
        {
            using (new SecurityDisabler())
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
    }
}
