using Sitecore;
using Sitecore.Jobs;

namespace ExperienceGenerator.Client.Infrastructure
{
    /// <summary>
    /// A job manager that wraps Sitecore's job manager
    /// </summary>
    public class XGenSitecoreJobManager : XGenJobManager
    {
        protected override void StartJob(JobInfo info, JobSegment segment)
        {            
            var options = new JobOptions("ExperienceGenerator-" + info.Id + "/" + segment.Id, "ExperienceGenerator", Context.Site.Name, this, "Run",
                        new object[] { segment });

            JobManager.Start(options);
        }

        public void Run(JobSegment job)
        {
            Process(job);
        }
    }
}