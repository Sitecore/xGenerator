using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ExperienceGenerator
{

    public interface  IJobInfo
    {
        int TargetVisitors { get; }

        int CompletedVisitors { get; }

        int CompletedVisits { get; }

        int Exceptions { get; }

        string LastException { get; }

        DateTime? Started { get; }

        DateTime? Ended { get; }

        JobSpecification Specification { get; }

        [JsonConverter(typeof(StringEnumConverter))]
        JobStatus JobStatus { get; }        
    }

    public enum JobStatus
    {
        Pending,
        Running,  
        Paused,
        Cancelling,
        Cancelled,
        Failed,
        Complete
    }

    public class JobInfo : IJobInfo
    {
        public Guid Id { get; set; }

        public string StatusUrl { get; set; }

        public string LastException { get
        {
            return Segments.Select(s => s.LastException).FirstOrDefault(e => e != null);
        }}

        public DateTime? Started
        {
            get { return Segments.Where(s=>s.Started.HasValue).Min(s => s.Started); }
        }

        public DateTime? Ended
        {
            get
            {
                return Segments.All(s => s.Ended.HasValue) ? Segments.Max(s => s.Ended) : null;
            }
        }

        public JobStatus JobStatus
        {
            get
            {
                if (Segments.All(s => s.JobStatus == JobStatus.Complete)) return JobStatus.Complete;
                if (Segments.All(s => s.JobStatus == JobStatus.Pending)) return JobStatus.Pending;
                if (Segments.All(s => s.JobStatus == JobStatus.Paused)) return JobStatus.Paused;
                if (Segments.Any(s => s.JobStatus == JobStatus.Cancelling)) return JobStatus.Cancelling;
                if (Segments.Any(s => s.JobStatus == JobStatus.Running)) return JobStatus.Running;                

                return Segments.Min(s => s.JobStatus);
            }
        }

        public int TargetVisitors { get { return Segments.Sum(segment => segment.TargetVisitors); } }
        public int CompletedVisitors { get { return Segments.Sum(segment => segment.CompletedVisitors); }}

        public double Progress
        {
            get
            {
                if (TargetVisitors == 0) return 1d;
                return CompletedVisitors/(double) TargetVisitors;
            }
        }

        public int CompletedVisits { get { return Segments.Sum(segment => segment.CompletedVisits); } }        

        public int Exceptions { get { return Segments.Sum(segment => segment.Exceptions); } }
               
        public JobSpecification Specification { get; set; }

        public List<JobSegment> Segments { get; set; }

        public JobInfo(JobSpecification specification)
        {
            Id = Guid.NewGuid();
            Specification = specification;
            Segments = new List<JobSegment>();
        }

        public void Stop()
        {
            foreach (var s in Segments)
            {
                if (s.JobStatus <= JobStatus.Paused)
                {
                    s.JobStatus = JobStatus.Cancelling;
                }
            }
        }

        public void Pause()
        {
            foreach (var s in Segments)
            {
                if (s.JobStatus <= JobStatus.Running)
                {
                    s.JobStatus = JobStatus.Paused;
                }
            }
        }

        public void Resume()
        {
            foreach (var s in Segments)
            {
                if (s.JobStatus == JobStatus.Paused)
                {
                    s.JobStatus = JobStatus.Running;
                }
            }
        }
    }

    public class JobSegment : IJobInfo
    {
        public Guid Id { get; set; }

        public JobSegment(JobInfo owner)
        {
            Id = Guid.NewGuid();
            Specification = owner.Specification;
        }

        public int TargetVisitors { get; set; }

        public int CompletedVisitors { get; set; }
        public int CompletedVisits { get; set; }
        public int Exceptions { get; set; }
        public string LastException { get; set; }
        public DateTime? Started { get; set; }
        public DateTime? Ended { get; set; }
        public JobStatus JobStatus { get; set; }

        [JsonIgnore]
        public JobSpecification Specification { get; set; }        
    }
}