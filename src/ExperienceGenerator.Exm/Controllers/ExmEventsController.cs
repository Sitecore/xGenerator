using System;
using System.Web.Http;
using Sitecore.Common;
using Sitecore.EmailCampaign.Cm.Pipelines.HandleSentMessage;
using Sitecore.EmailCampaign.Cm.Pipelines.HandleUndeliveredMessage;
using Sitecore.ExM.Framework.Data;
using Sitecore.Modules.EmailCampaign.Core;

namespace ExperienceGenerator.Exm.Controllers
{
    public class ExmEventsController : ApiController
    {
        [HttpPost]
        public IHttpActionResult GenerateSent(Guid contactId, Guid messageId, string date)
        {
            var dateTime = TryParseDate(date);

            var eventData = new SerializationCollection();
            eventData.Set("MessageId", messageId);
            eventData.Set("FakeDateTime", dateTime.ToString("u"));

            var pipelineArgs = new HandleSentMessagePipelineArgs(contactId.ToID(), messageId.ToID(), messageId.ToID(), eventData);
            new PipelineHelper().RunPipeline(Sitecore.EmailCampaign.Cm.Constants.HandleSentMessagePipeline, pipelineArgs);

            return Ok();
        }

        [HttpPost]
        public IHttpActionResult GenerateBounce(Guid contactId, Guid messageId, string date)
        {
            var dateTime = TryParseDate(date);

            var eventData = new SerializationCollection();
            eventData.Set("MessageId", messageId);
            eventData.Set("FakeDateTime", dateTime.ToString("u"));

            var pipelineArgs = new HandleUndeliveredMessagePipelineArgs(contactId.ToID(), messageId.ToID(), messageId.ToID(), eventData, true);
            new PipelineHelper().RunPipeline(Sitecore.EmailCampaign.Cm.Constants.HandleUndeliveredMessagePipeline, pipelineArgs);

            return Ok();
        }

        [HttpPost]
        public IHttpActionResult GenerateSpam(Guid contactId, Guid messageId, string email, string date)
        {
            var dateTime = TryParseDate(date);

            var eventData = new SerializationCollection();
            eventData.Set("MessageId", messageId);
            eventData.Set("FakeDateTime", dateTime.ToString("u"));

            var pipelineArgs = new HandleSpamComplaintPipelineArgs(contactId.ToID(), messageId.ToID(), messageId.ToID(), email, eventData);
            new PipelineHelper().RunPipeline(Sitecore.EmailCampaign.Cm.Constants.HandleSpamComplaintPipeline, pipelineArgs);

            return Ok();
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
    }
}