using System;
using System.Web.Http;
using ExperienceGenerator.Exm.Models;
using ExperienceGenerator.Exm.Services;

namespace ExperienceGenerator.Exm.Controllers
{
    public class ExperienceGeneratorExmJobsController : ApiController
    {
        [HttpPost]
        public IHttpActionResult Post(JobDefinition specifications)
        {
            var job = JobManager.Instance.StartJob(specifications);
            job.StatusUrl = Url.Route("ExperienceGeneratorExmJobsApi", new
                                                                       {
                                                                           action = "Get",
                                                                           id = job.Id
                                                                       });
            return Ok(job);
        }


        public IHttpActionResult Get(Guid id)
        {
            var job = JobManager.Instance.Poll(id);
            if (job != null)
            {
                return Ok(job);
            }

            return NotFound();
        }
    }
}