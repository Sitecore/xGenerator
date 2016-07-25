namespace ExperienceGenerator.Exm.Models
{
  using System;
  using ExperienceGenerator.Exm.Services;

  public class ExmJob
    {
        public Guid Id { get; set; }

        public ExmJob()
        {
            this.Id = Guid.NewGuid();
        }

        public DateTime? Started { get; set; }

        public DateTime? Ended { get; set; }

        public JobStatus JobStatus { get; set; }

        public int CompletedContacts { get; set; }

        public int CompletedGoals { get; set; }

        public int CompletedEmails { get; set; }

        public int CompletedLists { get; set; }

        public string Status { get; set; }

        public string LastException { get; set; }

        public string StatusUrl { get; set; }

        public int Threads
        {
            get { return ExmEventsGenerator.Threads; }
        }

        public int Errors
        {
            get { return ExmEventsGenerator.Errors; }
        }
    }
}