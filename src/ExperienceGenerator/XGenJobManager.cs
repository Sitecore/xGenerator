namespace ExperienceGenerator
{
  using System;
  using System.Collections.Concurrent;
  using System.Collections.Generic;
  using System.Linq;
  using System.Threading;
  using Colossus;

  public abstract class XGenJobManager
  {
    public static XGenJobManager Instance { get; set; }


    public XGenJobManager()
    {
      this.Threads = Environment.ProcessorCount * 2;
      this.WarmUpInterval = TimeSpan.FromSeconds(1);
    }

    private readonly ConcurrentDictionary<Guid, JobInfo> _jobs = new ConcurrentDictionary<Guid, JobInfo>();


    public int Threads { get; set; }
    public virtual TimeSpan WarmUpInterval { get; set; }

    public IEnumerable<JobInfo> Jobs
    {
      get
      {
        return this._jobs.Values.ToArray();
      }
    }

    public JobInfo StartNew(JobSpecification spec)
    {
      var info = new JobInfo(spec);

      info = this._jobs.GetOrAdd(info.Id, id => info);

      //Try create a simulator to see if the spec contains any errors, to report them in the creating thread
      spec.CreateSimulator();

      var batchSize = (int)Math.Floor(info.Specification.VisitorCount / (double)this.Threads);

      for (var i = 0; i < this.Threads; i++)
      {
        var count = i == this.Threads - 1 ? info.Specification.VisitorCount - i * batchSize : batchSize;

        if (count > 0)
        {
          var segment = new JobSegment(info)
          {
            TargetVisitors = count
          };
          info.Segments.Add(segment);

          this.StartJob(info, segment);

          if (i == 0)
          {
            // Fix for concurrent creating of Contacts.
            Thread.Sleep(this.WarmUpInterval);
          }
        }
      }

      return info;
    }

    public JobInfo Poll(Guid id)
    {
      JobInfo info;
      return this._jobs.TryGetValue(id, out info) ? info : null;
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
          var sim = job.Specification.CreateSimulator();

          foreach (var visitor in
              sim.NextVisitors(job.TargetVisitors, false))
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