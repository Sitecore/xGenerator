using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Sitecore.Common;
using Sitecore.EmailCampaign.Cm.Pipelines.HandleSentMessage;
using Sitecore.EmailCampaign.Cm.Pipelines.HandleBounce;
using Sitecore.EmailCampaign.Cm.Pipelines.HandleSpamComplaint;
using Sitecore.EmailCampaign.Model.Messaging;
using Sitecore.EmailCampaign.Model.XConnect.Events;
using Sitecore.Modules.EmailCampaign.Core;
using Sitecore.Modules.EmailCampaign.Core.Contacts;
using Sitecore.Modules.EmailCampaign.Messages;
using Sitecore.Modules.EmailCampaign.Services;
using Constants = Sitecore.EmailCampaign.Cm.Constants;
using Sitecore.DependencyInjection;

namespace ExperienceGenerator.Exm.Controllers
{
    public class ExmEventsController : ApiController
    {
        public IExmCampaignService _exmService;
        public IContactService _contactService;

        public ExmEventsController()
        {
            _exmService = (IExmCampaignService)ServiceLocator.ServiceProvider.GetService(typeof(IExmCampaignService));
            _contactService = (IContactService)ServiceLocator.ServiceProvider.GetService(typeof(IContactService));
        }

        [HttpPost]
        public IHttpActionResult GenerateSent(Guid contactId, Guid messageId, string date)
        {
            var dateTime = TryParseDate(date);
            var messageItem = GetMessageItem(messageId);

            var sentContactList = new List<SentContactEntry>();
            sentContactList.Add(GenerateSentContactEntry(messageItem, contactId));
            
            var pipelineArgs = new HandleSentMessagePipelineArgs()
            {
                MessageId = messageId,
                InstanceId = messageId,
                TimeStamp = dateTime,
                MessageItem = messageItem,
                Contacts = sentContactList
            };

            new PipelineHelper().RunPipeline(Constants.HandleSentMessagePipeline, pipelineArgs);

            return Ok();
        }

        public SentContactEntry GenerateSentContactEntry(MessageItem messageItem, Guid contactId)
        {
            var entry = new SentContactEntry();
            var contact = _contactService.GetContact(contactId.ToID());

            entry.ContactIdentifier = contact.Identifiers.FirstOrDefault(x => x.Source.Equals("ExperienceGenerator"));
            entry.EmailAddressHistoryEntryId = messageItem.EmailAddressHistoryEntryId;
            entry.MessageLanguage = messageItem.TargetLanguage.Name;
            entry.TestValueIndex = messageItem.TestValueIndex;

            return entry;
        }

        [HttpPost]
        public IHttpActionResult GenerateBounce(Guid contactId, Guid messageId, string date)
        {
            var dateTime = TryParseDate(date);
            var contact = _contactService.GetContact(contactId.ToID());
            var eventData = new EventData(contact.Identifiers.FirstOrDefault(x => x.Source.Equals("ExperienceGenerator")), new BounceEvent(dateTime)
            {
                BounceReason = "Generated Bounce from Experience Generator",
                BounceType = "HardBounce",
                MessageId = messageId,
                InstanceId = messageId
            });
           

            var pipelineArgs = new HandleBounceArgs(eventData, ((BounceEvent)eventData.EmailEvent).BounceType == "HardBounce");
            new PipelineHelper().RunPipeline(Constants.HandleUndeliveredMessagePipeline, pipelineArgs);

            return Ok();
        }

        [HttpPost]
        public IHttpActionResult GenerateSpam(Guid contactId, Guid messageId, string date)
        {
            var dateTime = TryParseDate(date);
            var contact = _contactService.GetContact(contactId.ToID());
            var eventData = new EventData(contact.Identifiers.FirstOrDefault(x => x.Source.Equals("ExperienceGenerator")), new SpamComplaintEvent(dateTime)
            {
                MessageId = messageId,
                InstanceId = messageId
            });

            var pipelineArgs = new HandleSpamComplaintPipelineArgs(eventData);
            new PipelineHelper().RunPipeline(Constants.HandleSpamComplaintPipeline, pipelineArgs);

            return Ok();
        }

        public DateTime TryParseDate(string date)
        {
            DateTime dateTime;
            if (string.IsNullOrEmpty(date) || !DateTime.TryParse(date, out dateTime))
            {
                return DateTime.UtcNow;
            }

            return dateTime.ToUniversalTime();
        }

        public MessageItem GetMessageItem(Guid itemId)
        {
            return _exmService.GetMessageItem(itemId);
        }
    }
}
