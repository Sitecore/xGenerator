using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Configuration;
using Sitecore.Publishing;

namespace ExperienceGenerator.Exm.Services
{
    public static class PublishService
    {
        public static void PublishSmart()
        {
            var targetDatabases = new[] { Factory.GetDatabase("web") };
            var masterDatabase = Factory.GetDatabase("master");
            var languages = masterDatabase.Languages;
            var handle = PublishManager.PublishSmart(masterDatabase, targetDatabases, languages);
            var start = DateTime.Now;
            while (!PublishManager.GetStatus(handle).IsDone)
            {
                if (DateTime.Now - start > TimeSpan.FromMinutes(1))
                    break;
            }
        }
    }
}
