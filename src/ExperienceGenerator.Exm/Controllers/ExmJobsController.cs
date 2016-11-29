using System;
using System.Web.Http;
using ExperienceGenerator.Exm.Models;
using ExperienceGenerator.Exm.Services;

namespace ExperienceGenerator.Exm.Controllers
{
    public class ExmJobsController : ApiController
    {
        [HttpPost]
        public IHttpActionResult CreateCampaignData(ExmGeneratorSettings settings)
        {
            var job = JobManager.Instance.StartJob(settings);
            job.StatusUrl = Url.Route("ExperienceGeneratorExmJobsApi", new
                                                                       {
                                                                           action = "Status",
                                                                           id = job.Id
                                                                       });
            return Ok(job);
        }

        [HttpPost]
        public IHttpActionResult CreateList(ListSettings settings)
        {
            var job = JobManager.Instance.StartCreateListJob(settings);
            job.StatusUrl = Url.Route("ExperienceGeneratorExmJobsApi", new
            {
                action = "Status",
                id = job.Id
            });
            return Ok(job);
        }

        [HttpGet]
        public IHttpActionResult Status(Guid id)
        {
            var job = JobManager.Instance.Poll(id);
            if (job != null)
            {
                return Ok(job);
            }

            return NotFound();
        }

        [HttpGet]
        public IHttpActionResult Stop(Guid id)
        {
            var job = JobManager.Instance.Stop(id);
            if (job == null)
            {
                return Ok(job);
            }

            return NotFound();
        }
    }
}
