namespace ExperienceGenerator.Exm.Controllers
{
  using System;
  using System.Web.Http;
  using ExperienceGenerator.Exm.Services;

  public class ExperienceGeneratorExmJobsController : ApiController
  {
    [HttpPost]
    public IHttpActionResult Post(ExmJobDefinition specifications)
    {
      var job = ExmJobManager.Instance.StartJob(specifications);
      job.StatusUrl = this.Url.Route("ExperienceGeneratorExmJobsApi", new
      {
        action = "Get",
        id = job.Id
      });
      return this.Ok(job);
    }


    public IHttpActionResult Get(Guid id)
    {
      var job = ExmJobManager.Instance.Poll(id);
      if (job != null)
      {
        return this.Ok(job);
      }

      return this.NotFound();
    }
  }
}