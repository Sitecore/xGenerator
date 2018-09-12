using System;
using System.Collections.Generic;
using System.Threading;
using ExperienceGenerator.Exm.Models;
using ExperienceGenerator.Exm.Repositories;
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
        private static string _repeatStatus;
        private static int _repeatStatusCount;
        private Job _activeJob;

        public Job StartJob(ExmGeneratorSettings settings)
        {
            _activeJob = new Job();
            settings.Job = _activeJob;           
            var options = new JobOptions("ExperienceGeneratorExm-" + _activeJob.Id, "ExperienceGenerator", Context.Site.Name, this, "GenerateCampaignData", new object[] { settings });
            Sitecore.Jobs.JobManager.Start(options);
            return _activeJob;
        }

        public Job StartCreateListJob(ListSettings settings)
        {
            _activeJob = new Job();
            settings.Job = _activeJob;
            var options = new JobOptions("ExperienceGeneratorExmLists-" + _activeJob.Id, "ExperienceGenerator", Context.Site.Name, this, "GenerateList", new object[] { settings });
            Sitecore.Jobs.JobManager.Start(options);
            return _activeJob;
        }

        public void GenerateList(ListSettings settings)
        {
            settings.Job.JobStatus = JobStatus.Running;
            settings.Job.Started = DateTime.Now;
            settings.Job.JobName = $"Generating new list '{settings.Name}' with '{settings.Recipients}' recipients";

            try
            {
                settings.Job.Status = $"Creating {settings.Recipients} Contacts";
                var contactRepository = new ContactRepository();
                var contacts = contactRepository.CreateContacts(settings.Job, settings.Recipients);

                var contactListRepository = new ContactListRepository();
                contactListRepository.CreateList(settings.Job, settings.Name, contacts);

                IndexService.RebuildListIndexes(settings.Job);

                settings.Job.JobStatus = JobStatus.Complete;
                settings.Job.Status = "DONE!";
                Log.Info($"EXMGenerator completed: {settings.Job.CompletedContacts}", this);
            }
            catch
            {
                settings.Job.Status = "Failed!";
                settings.Job.JobStatus = JobStatus.Failed;
            }
            finally
            {
                settings.Job.Ended = DateTime.Now;
            }
        }

        public void GenerateCampaignData(ExmGeneratorSettings settings)
        {
            GenerateEventService.Errors = 0;

            settings.Job.JobStatus = JobStatus.Running;
            settings.Job.Started = DateTime.Now;
            settings.Job.JobName = $"Generating campaign data for {settings.Count} campaign(s)";

            try
            {
                CreateDataForAllCampaigns(settings.Job, settings);

                settings.Job.JobStatus = JobStatus.Complete;
                settings.Job.Status = "DONE!";
                Log.Info($"EXMGenerator completed: {settings.Job.CompletedContacts}", this);
            }
            catch
            {
                settings.Job.Status = "Failed!";
                settings.Job.JobStatus = JobStatus.Failed;
            }
            finally
            {
                settings.Job.Ended = DateTime.Now;
            }
        }

        private void CreateDataForAllCampaigns(Job job, Dictionary<Guid, CampaignSettings> settings)
        {
            foreach (var keyValuePair in settings)
            {
                if (job.JobStatus == JobStatus.Cancelling)
                    return;

                ClearCounters(job);

                try
                {
                    CreateDataForCampaign(job, keyValuePair);
                }
                catch (Exception ex)
                {
                    Log.Error($"EXM Generator failed for campaign: {keyValuePair.Key}", ex, this);
                    job.LastException = ex.ToString();
                    throw;
                }
            }
        }

        private void CreateDataForCampaign(Job job, KeyValuePair<Guid, CampaignSettings> keyValuePair)
        {
            var campaignId = keyValuePair.Key;
            var campaign = keyValuePair.Value;

            var campaignItem = Context.Database.GetItem(new ID(campaignId));
            job.JobName = $"Generating data for campaign {campaignItem.Name}";
            try
            {
                var createDataService = new GenerateCampaignDataService(campaignId, campaign);
                createDataService.CreateData(job);
            }
            finally
            {
                job.JobName = "";
            }
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
            if (_activeJob == null || _activeJob.Id != id)
                return null;

            AddActivityIndicatorToStatus();

            return _activeJob;
        }

        public Job Stop(Guid id)
        {
            if (_activeJob == null || _activeJob.Id != id)
                return null;
            _activeJob.JobStatus = JobStatus.Cancelling;
            var secondsWaited = 0;
            while (_activeJob.JobStatus == JobStatus.Cancelling && secondsWaited < 60)
            {
                Thread.Sleep(1000);
                secondsWaited++;
            }
            return _activeJob;
        }

        private void AddActivityIndicatorToStatus()
        {
            if (_activeJob.Status == _repeatStatus)
            {
                _activeJob.Status += new string('.', _repeatStatusCount);
                _repeatStatusCount++;
            }
            else
            {
                _repeatStatus = _activeJob.Status;
                _repeatStatusCount = 0;
            }
        }
    }
}
