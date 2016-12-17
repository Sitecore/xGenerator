using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Colossus;
using Sitecore;
using Sitecore.Jobs;

namespace ExperienceGenerator
{
    public class XGenJobManager
    {
        public static XGenJobManager Instance { get; set; }


        public XGenJobManager()
        {
            Threads = 1; //Environment.ProcessorCount*2;
        }

        private readonly ConcurrentDictionary<Guid, JobInfo> _jobs = new ConcurrentDictionary<Guid, JobInfo>();

        public int Threads { get; set; }

        public virtual TimeSpan WarmUpInterval { get; set; } = TimeSpan.FromSeconds(1);

        public IEnumerable<JobInfo> Jobs => _jobs.Values.ToArray();

        public JobInfo StartNew(JobSpecification spec)
        {
            //Try create a simulator to see if the spec contains any errors, to report them in the creating thread
            spec.CreateSimulator();

            var info = new JobInfo(spec);
            info = _jobs.GetOrAdd(info.Id, id => info);

            StartJobSegments(info);

            return info;
        }

        private void StartJobSegments(JobInfo info)
        {
            var batchSize = (int) Math.Floor(info.Specification.VisitorCount/(double) Threads);
            for (var i = 0; i < Threads; i++)
            {
                var visitorsToCreate = i == Threads - 1 ? info.Specification.VisitorCount - i*batchSize : batchSize;

                if (visitorsToCreate <= 0)
                    continue;

                StartJobSegment(info, visitorsToCreate);

                if (i == 0)
                {
                    // Fix for concurrent creating of Contacts.
                    Thread.Sleep(WarmUpInterval);
                }
            }
        }

        private void StartJobSegment(JobInfo info, int visitorsToCreate)
        {
            var segment = new JobSegment(info)
                          {
                              TargetVisitors = visitorsToCreate
                          };
            info.Segments.Add(segment);

            StartJob(info, segment);
        }

        public JobInfo Poll(Guid id)
        {
            JobInfo info;
            return _jobs.TryGetValue(id, out info) ? info : null;
        }

        protected void Process(JobSegment job)
        {
            try
            {
                if (IsJobStopped(job, false))
                {
                    job.Started = DateTime.Now;

                    if (job.JobStatus == JobStatus.Pending)
                    {
                        job.JobStatus = JobStatus.Running;
                    }
                    Randomness.Seed((job.Id.GetHashCode() + DateTime.Now.Ticks).GetHashCode());

                    SimulateVisitors(job);
                }

                job.Ended = DateTime.Now;
                job.JobStatus = job.CompletedVisitors < job.TargetVisitors ? JobStatus.Cancelled : JobStatus.Complete;
            }
            catch (Exception ex)
            {
                job.JobStatus = JobStatus.Failed;
                job.Ended = DateTime.Now;
                job.LastException = ex.ToString();
            }
        }

        private static void SimulateVisitors(JobSegment job)
        {
            var simulator = job.Specification.CreateSimulator();
            foreach (var visitor in simulator.GetVisitors(job.TargetVisitors))
            {
                if (IsJobStopped(job))
                {
                    break;
                }

                try
                {
                    foreach (var visit in visitor.Commit())
                    {
                        ++job.CompletedVisits;
                    }
                }
                catch (Exception ex)
                {
                    ++job.Exceptions;
                    job.LastException = ex.ToString();
                }

                ++job.CompletedVisitors;
            }
        }

        private static bool IsJobStopped(IJobInfo job, bool pauseExecution = true)
        {
            while (job.JobStatus == JobStatus.Paused && pauseExecution)
            {
                Thread.Sleep(100);
            }

            return job.JobStatus > JobStatus.Paused;
        }

        protected virtual void StartJob(JobInfo info, JobSegment segment)
        {            
            var options = new JobOptions("ExperienceGenerator-" + info.Id + "/" + segment.Id, "ExperienceGenerator", Context.Site.Name, this, "Run", new object[] { segment });

            JobManager.Start(options);
        }

        public void Run(JobSegment job)
        {
            Process(job);
        }
    }
}
