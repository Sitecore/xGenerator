using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ExperienceGenerator;

namespace Colossus.Console
{
    class XGenConsoleJobManager : XGenJobManager
    {
        private List<Task> _tasks = new List<Task>();

        protected override void StartJob(JobInfo info, JobSegment segment)
        {
            var task = Task.Factory.StartNew(() =>
            {
                this.Run(segment);
            });
            _tasks.Add(task);

            //var options = new JobOptions("ExperienceGenerator-" + info.Id + "/" + segment.Id, "ExperienceGenerator", Context.Site.Name, this, "Run",
            //            new object[] { segment });

            //JobManager.Start(options);
        }

        public void Run(JobSegment job)
        {
            Process(job);
        }

        public void AddMonitoringTask()
        {
            var task = Task.Factory.StartNew(() =>
            {
                
            });
            _tasks.Add(task);
        }

        public void WaitAll()
        {
            var isRunning = false;
            do
            {
                Thread.Sleep(5000);
                var completedVisitors = Jobs.Select(job => job.CompletedVisitors).Aggregate((a, b) => a + b);
                var completedVisits = Jobs.Select(job => job.CompletedVisits).Aggregate((a, b) => a + b);
                isRunning = Jobs.Any(job => job.JobStatus == JobStatus.Running);
                System.Console.WriteLine("CompletedVisitors:{0} CompletedVisits:{1}", completedVisitors, completedVisits);
            } while (isRunning);

            Task.WaitAll(_tasks.ToArray());
        }
    }
}
