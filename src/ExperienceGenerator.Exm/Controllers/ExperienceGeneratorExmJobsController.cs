namespace ExperienceGenerator.Exm.Controllers
{
  using System;
  using System.Collections.Generic;
  using System.Web.Http;
  using ExperienceGenerator.Exm.Models;
  using ExperienceGenerator.Exm.Services;
  using Sitecore.Common;
  using Sitecore.EmailCampaign.Cm.Pipelines.HandleSentMessage;
  using Sitecore.EmailCampaign.Cm.Pipelines.HandleUndeliveredMessage;
  using Sitecore.ExM.Framework.Data;
  using Sitecore.Modules.EmailCampaign.Core;

  public class ExperienceGeneratorExmJobsController : ApiController
  {

    [HttpPost]
    public IHttpActionResult Post(Dictionary<Guid,CampaignModel> specification)
    {
      var job = ExmJobManager.Instance.StartJob(new InitialExmDataPreparationModel());
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

    private DateTime TryParseDate(string date)
    {
      DateTime dateTime;
      if (string.IsNullOrEmpty(date) || !DateTime.TryParse(date, out dateTime))
      {
        return DateTime.UtcNow;
      }

      return dateTime.ToUniversalTime();
    }

    [HttpPost]
    public IHttpActionResult GenerateSent(Guid contactId, Guid messageId, string date)
    {
      var dateTime = this.TryParseDate(date);

      var eventData = new SerializationCollection();
      eventData.Set("MessageId", messageId);
      eventData.Set("FakeDateTime", dateTime.ToString("u"));

      var pipelineArgs = new HandleSentMessagePipelineArgs(contactId.ToID(), messageId.ToID(), messageId.ToID(), eventData);
      new PipelineHelper().RunPipeline(Sitecore.EmailCampaign.Cm.Constants.HandleSentMessagePipeline, pipelineArgs);

      return this.Ok();
    }

    [HttpPost]
    public IHttpActionResult GenerateBounce(Guid contactId, Guid messageId, string date)
    {
      var dateTime = this.TryParseDate(date);

      var eventData = new SerializationCollection();
      eventData.Set("MessageId", messageId);
      eventData.Set("FakeDateTime", dateTime.ToString("u"));

      var pipelineArgs = new HandleUndeliveredMessagePipelineArgs(contactId.ToID(), messageId.ToID(), messageId.ToID(), eventData, true);
      new PipelineHelper().RunPipeline(Sitecore.EmailCampaign.Cm.Constants.HandleUndeliveredMessagePipeline, pipelineArgs);

      return this.Ok();
    }

    [HttpPost]
    public IHttpActionResult GenerateSpam(Guid contactId, Guid messageId, string email, string date)
    {
      var dateTime = this.TryParseDate(date);

      var eventData = new SerializationCollection();
      eventData.Set("MessageId", messageId);
      eventData.Set("FakeDateTime", dateTime.ToString("u"));

      var pipelineArgs = new HandleSpamComplaintPipelineArgs(contactId.ToID(), messageId.ToID(), messageId.ToID(), email, eventData);
      new PipelineHelper().RunPipeline(Sitecore.EmailCampaign.Cm.Constants.HandleSpamComplaintPipeline, pipelineArgs);

      return this.Ok();
    }
  }
}