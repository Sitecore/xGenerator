using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ExperienceGenerator.Exm.Models;
using Sitecore;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.Jobs;
using Job = ExperienceGenerator.Exm.Models.Job;

namespace ExperienceGenerator.Exm.Services
{
    public class JobManager
    {
        public static JobManager Instance;

        private readonly List<JobDefinition> _jobDefinitions = new List<JobDefinition>();

        public Job StartJob(JobDefinition jobDefinition)
        {
            jobDefinition.Job = new Job();
            _jobDefinitions.Add(jobDefinition);
            var options = new JobOptions("ExperienceGeneratorExm-" + jobDefinition.Job.Id, "ExperienceGenerator", Context.Site.Name, this, "Run", new object[] {jobDefinition});
            Sitecore.Jobs.JobManager.Start(options);
            return jobDefinition.Job;
        }

        public void Run(JobDefinition jobDefinition)
        {
            GenerateEventService.Errors = 0;
            GenerateEventService.Pool = new SemaphoreSlim(jobDefinition.Threads);

            jobDefinition.Job.JobStatus = JobStatus.Running;
            foreach (var keyValuePair in jobDefinition)
            {
                ClearCounters(jobDefinition.Job);
                CreateData(jobDefinition.Job, keyValuePair);
            }

            jobDefinition.Job.JobStatus = JobStatus.Complete;
        }

        private void CreateData(Job job, KeyValuePair<Guid, CampaignModel> keyValuePair)
        {
            MarkCampaignJobAsStarted(job, keyValuePair.Key);
            try
            {
                var createDataService = new CreateDataService(job, keyValuePair.Key, keyValuePair.Value);
                createDataService.CreateData();
                MarkCampaignJobAsComplete(job);
            }
            catch (Exception ex)
            {
                MarkCampaignJobAsFailed(job, ex);
            }
        }

        private void MarkCampaignJobAsFailed(Job job, Exception ex)
        {
            job.Status = "Failed!";
            job.Ended = DateTime.Now;
            job.JobStatus = JobStatus.Failed;
            job.LastException = ex.ToString();
            Log.Error("Failed", ex, this);
        }

        private void MarkCampaignJobAsComplete(Job job)
        {
            job.Status = "DONE!";
            job.Ended = DateTime.Now;
            job.JobStatus = JobStatus.Complete;
            job.CampaignCountLabel = "";
            Log.Info($"EXMGenerator completed: {job.CompletedContacts}", this); 
        }

        private void MarkCampaignJobAsStarted(Job job, Guid campaignId)
        {
            var campaignItem = Context.Database.GetItem(new ID(campaignId));
            job.CampaignCountLabel = $"Generating Data for campaign {campaignItem.Name}";
            job.JobStatus = JobStatus.Running;
            job.Started = DateTime.Now;
        }


        private static void ClearCounters(Job job)
        {
            job.CompletedContacts = 0;
            job.CompletedEmails = 0;
            job.CompletedEvents = 0;
            job.CompletedLists = 0;
            job.TargetContacts = 0;
            job.TargetEmails = 0;
            job.TargetEvents = 0;
            job.TargetLists = 0;
        }

        public Job Poll(Guid id)
        {
            var spec = _jobDefinitions.FirstOrDefault(x => x.Job.Id == id);
            return spec?.Job;
        }
    }
}