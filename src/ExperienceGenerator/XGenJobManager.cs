using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Colossus;
using Newtonsoft.Json;

namespace ExperienceGenerator
{
    public abstract class XGenJobManager
    {
        public static XGenJobManager Instance { get; set; }

        private ParallelOptions ParallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Convert.ToInt32(Math.Ceiling((Environment.ProcessorCount * 0.75) * 2.0)) };

        [JsonConstructor]
        protected XGenJobManager()
        {
        }

        // maxProcessingUsagePercentage is between 0 and 1 (ex: 0.75)
        [JsonConstructor]
        protected XGenJobManager(double maxProcessingUsagePercentage)
        {
            ParallelOptions.MaxDegreeOfParallelism = Convert.ToInt32(Math.Ceiling((Environment.ProcessorCount * maxProcessingUsagePercentage) * 2.0));
        }

        private readonly ConcurrentDictionary<Guid, JobInfo> _jobs = new ConcurrentDictionary<Guid, JobInfo>();

        //public int Threads { get; set; }
        public virtual TimeSpan WarmUpInterval { get; set; } = TimeSpan.FromSeconds(10);

        public IEnumerable<JobInfo> Jobs => _jobs.Values.ToArray();

        public JobInfo StartNew(JobSpecification spec)
        {
            var info = new JobInfo(spec);

            info = _jobs.GetOrAdd(info.Id, id => info);

            //Try create a simulator to see if the spec contains any errors, to report them in the creating thread
            spec.CreateSimulator();

            var segment = new JobSegment(info)
            {
                TargetVisitors = info.Specification.VisitorCount
            };
            info.Segments.Add(segment);

            StartJob(info, segment);

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
                    var visitorCount = simulator.GetVisitors(job.TargetVisitors);
                    var partitioner = Partitioner.Create(visitorCount, EnumerablePartitionerOptions.NoBuffering);

                    Parallel.ForEach(partitioner, ParallelOptions, (visitor, loopState) =>
                    {
                        while (job.JobStatus == JobStatus.Paused)
                        {
                            Thread.Sleep(1000);
                        }

                        if (job.JobStatus > JobStatus.Paused)
                        {
                            loopState.Break();
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
                    });
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
