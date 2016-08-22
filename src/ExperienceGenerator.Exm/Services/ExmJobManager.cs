using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
      foreach (var keyValuePair in jobDefinition)
      {
        var exmDataPreparationService = new ExmDataPreparationService(jobDefinition, keyValuePair.Value, keyValuePair.Key);
        exmDataPreparationService.CreateData();
      }

      jobDefinition.Job.JobStatus = JobStatus.Complete;
    }

    public ExmJob Poll(Guid id)
    {
      var spec = _jobDefinitions.FirstOrDefault(x => x.Job.Id == id);
      return spec?.Job;
    }
  }
}