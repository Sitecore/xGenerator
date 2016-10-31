using System;
using System.Collections.Generic;
using System.Linq;
using ExperienceGenerator.Exm.Models;
using Sitecore.Jobs;

namespace ExperienceGenerator.Exm.Services
{
  public class ExmJobManager
  {
    public static ExmJobManager Instance;

    private readonly List<ExmJobDefinitionModel> _jobDefinitions = new List<ExmJobDefinitionModel>();

    public ExmJob StartJob(ExmJobDefinitionModel jobDefinition)
    {
      jobDefinition.Job = new ExmJob();
      _jobDefinitions.Add(jobDefinition);
      var options = new JobOptions("ExperienceGeneratorExm-" + jobDefinition.Job.Id, "ExperienceGenerator", Sitecore.Context.Site.Name, this, "Run", new object[] { jobDefinition });
      JobManager.Start(options);
      return jobDefinition.Job;
    }

    public void Run(ExmJobDefinitionModel jobDefinition)
    {
      ExmEventsGenerator.Errors = 0;
      jobDefinition.Job.JobStatus = JobStatus.Running;
      var jobCount = 1;
      foreach (var keyValuePair in jobDefinition)
      {
        ClearCounters(jobDefinition.Job);
        jobDefinition.Job.CampaignCountLabel = $"Generating Data for campaign {jobCount ++} of {jobDefinition.Count}...";
        var exmDataPreparationService = new ExmDataPreparationService(jobDefinition, keyValuePair.Value, keyValuePair.Key);
        exmDataPreparationService.CreateData();
      }

      jobDefinition.Job.JobStatus = JobStatus.Complete;
    }

    private static void ClearCounters(ExmJob job)
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

    public ExmJob Poll(Guid id)
    {
      var spec = _jobDefinitions.FirstOrDefault(x => x.Job.Id == id);
      return spec?.Job;
    }
  }
}