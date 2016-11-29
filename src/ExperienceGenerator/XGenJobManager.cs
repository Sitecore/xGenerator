using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Colossus;

namespace ExperienceGenerator
{
    public abstract class XGenJobManager
    {
        public static XGenJobManager Instance { get; set; }


        protected XGenJobManager()
        {
            Threads = Environment.ProcessorCount*2;
        }

        private readonly ConcurrentDictionary<Guid, JobInfo> _jobs = new ConcurrentDictionary<Guid, JobInfo>();


        public int Threads { get; set; }
        public virtual TimeSpan WarmUpInterval { get; set; } = TimeSpan.FromSeconds(1);

        public IEnumerable<JobInfo> Jobs => _jobs.Values.ToArray();

        public JobInfo StartNew(JobSpecification spec)
        {
            var info = new JobInfo(spec);

            info = _jobs.GetOrAdd(info.Id, id => info);

            //Try create a simulator to see if the spec contains any errors, to report them in the creating thread
            spec.CreateSimulator();

            var batchSize = (int) Math.Floor(info.Specification.VisitorCount/(double) Threads);

            for (var i = 0; i < Threads; i++)
            {
                var count = i == Threads - 1 ? info.Specification.VisitorCount - i*batchSize : batchSize;

                if (count <= 0)
                    continue;
                var segment = new JobSegment(info)
                              {
                                  TargetVisitors = count
                              };
                info.Segments.Add(segment);

                StartJob(info, segment);

                if (i == 0)
                {
                    // Fix for concurrent creating of Contacts.
                    Thread.Sleep(WarmUpInterval);
                }
            }

            return info;
        }

        public JobInfo Poll(Guid id)
        {
            JobInfo info;
            return _jobs.TryGetValue(id, out info) ? info : null;
        }


        protected abstract void StartJob(JobInfo info, JobSegment job);


        protected void Process(JobSegment job)
        {
            try
            {
                if (job.JobStatus <= JobStatus.Paused)
                {
                    job.Started = DateTime.Now;

                    if (job.JobStatus == JobStatus.Pending)
                    {
                        job.JobStatus = JobStatus.Running;
                    }
                    Randomness.Seed((job.Id.GetHashCode() + DateTime.Now.Ticks).GetHashCode());
                    var simulator = job.Specification.CreateSimulator();

                    foreach (var visitor in simulator.GetVisitors(job.TargetVisitors))
                    {
                        while (job.JobStatus == JobStatus.Paused)
                        {
                            Thread.Sleep(100);
                        }

                        if (job.JobStatus > JobStatus.Paused)
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
    }
}
