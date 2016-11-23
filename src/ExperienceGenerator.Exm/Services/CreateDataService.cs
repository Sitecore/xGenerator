using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using ExperienceGenerator.Exm.Models;
using ExperienceGenerator.Exm.Repositories;
using Sitecore.Caching;
using Sitecore.Common;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.EmailCampaign.Analytics.Model;
using Sitecore.ListManagement;
using Sitecore.ListManagement.ContentSearch.Model;
using Sitecore.Modules.EmailCampaign.Core.Dispatch;
using Sitecore.Modules.EmailCampaign.Core.Pipelines.DispatchNewsletter;
using Sitecore.Modules.EmailCampaign.Factories;
using Sitecore.Modules.EmailCampaign.Messages;
using Sitecore.Publishing;
using Sitecore.SecurityModel;
using Sitecore.Sites;
using Constants = Sitecore.Modules.EmailCampaign.Core.Constants;

namespace ExperienceGenerator.Exm.Services
{
    public class CreateDataService
    {
        private readonly RandomContactMessageEventsFactory _randomContactMessageEventsFactory;

        private readonly CampaignSettings _campaign;
        private readonly Guid _exmCampaignId;
        private readonly List<Guid> _unsubscribeFromAllContacts = new List<Guid>();
        private readonly ContactListRepository _contactListRepository;
        private readonly AdjustEmailStatisticsService _adjustEmailStatisticsService;

        public CreateDataService(Guid exmCampaignId, CampaignSettings campaign)
        {
            _campaign = campaign;
            _exmCampaignId = exmCampaignId;
            _contactListRepository = new ContactListRepository();
            _adjustEmailStatisticsService = new AdjustEmailStatisticsService();
            _randomContactMessageEventsFactory = new RandomContactMessageEventsFactory(_campaign);
        }

        public void CreateData(Job job)
        {
            using (new SecurityDisabler())
            {
                job.Status = "Get Message Item...";
                var messageItem = GetMessageItem(_exmCampaignId);

                var siteContext = GetSiteContextForMessage(messageItem);
                using (new SiteContextSwitcher(siteContext))
                {
                    job.Status = "Cleanup...";
                    Cleanup(messageItem);

                    job.Status = "Get Contacts For Message...";
                    var contactsForMessage = GetMessageContacts(job, messageItem);

                    job.Status = "Back-dating segments...";
                    BackDateSegments();

                    job.Status = "Creating emails...";
                    SendCampaign(job, messageItem, contactsForMessage);
                }
            }
        }

        private static SiteContext GetSiteContextForMessage(MessageItem messageItem)
        {
            return SiteContext.GetSite(messageItem.ManagerRoot.Settings.WebsiteSiteConfigurationName);
        }

        private List<ContactData> GetMessageContacts(Job job, MessageItem messageItem)
        {
            return _contactListRepository.GetContacts(messageItem, _unsubscribeFromAllContacts);
        }

        private void BackDateSegments()
        {
            var reportingConnectionString = ConfigurationManager.ConnectionStrings["Reporting"].ConnectionString;
            using (var conn = new SqlConnection(reportingConnectionString))
            {
                conn.Open();
                using (var cmd = new SqlCommand("UPDATE Segments SET DeployDate='2015-01-01'", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            //TODO: Just clear ExperienceAnalytics.Segments
            CacheManager.ClearAllCaches();
        }

        private void Cleanup(MessageItem messageItem)
        {
            RemoveTrackingOnServiceMails(messageItem);
        }

        private static void RemoveTrackingOnServiceMails(MessageItem messageItem)
        {
            var managerRootPath = messageItem.ManagerRoot.InnerItem.Paths.FullPath;
            var serviceMessages = messageItem.InnerItem.Database.SelectItems($"{managerRootPath}/Messages/Service Messages//*[@@templatename='HTML Message']");
            foreach (var serviceMessage in serviceMessages)
            {
                if (serviceMessage.Fields["Campaign"].Value == string.Empty)
                    continue;
                using (new EditContext(serviceMessage))
                {
                    serviceMessage.Fields["Campaign"].Value = string.Empty;
                    serviceMessage.Fields["Engagement Plan"].Value = string.Empty;
                }
            }
        }

        private void GenerateEvents(Job job, MessageItem email, Funnel funnelDefinition, List<ContactData> contactsForThisEmail)
        {
            if (funnelDefinition == null)
            {
                return;
            }

            try
            {
                var contactIndex = 1;
                foreach (var contactData in contactsForThisEmail)
                {
                    if (job.JobStatus == JobStatus.Cancelling)
                        return;

                    job.Status = $"Generating events for contact {contactIndex++} of {contactsForThisEmail.Count}";

                    var events = _randomContactMessageEventsFactory.CreateRandomContactMessageEvents(contactData, funnelDefinition, email);

                    GenerateEventService.GenerateContactMessageEvents(job, events);

                    if (events.Events.Any(e => e.EventType == EventType.UnsubscribeFromAll))
                    {
                        _unsubscribeFromAllContacts.Add(events.ContactId);
                    }

                    job.CompletedEvents++;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Failed to generate events", ex);
                throw;
            }
        }

        private void SendCampaign(Job job, MessageItem messageItem, List<ContactData> contactsForThisEmail)
        {
            job.Status = "Adjusting email stats...";

            _adjustEmailStatisticsService.AdjustEmailStatistics(job, messageItem, _campaign);

            PublishEmail(messageItem);

            var contactIndex = 1;
            foreach (var contact in contactsForThisEmail)
            {
                if (job.JobStatus == JobStatus.Cancelling)
                    return;

                job.Status = $"Sending email to contact {contactIndex++} of {contactsForThisEmail.Count}";
                try
                {
                    SendEmailToContact(job, contact, messageItem);
                }
                catch (Exception ex)
                {
                    job.Status = ex.ToString();
                    Log.Error("Failed", ex, this);
                }
            }

            GenerateEvents(job, messageItem, _campaign.Events, contactsForThisEmail);
        }


        private void PublishEmail(MessageItem messageItem)
        {
            var sendingProcessData = new SendingProcessData(new ID(messageItem.MessageId));
            var dispatchArgs = new DispatchNewsletterArgs(messageItem, sendingProcessData)
                               {
                                   IsTestSend = false,
                                   SendingAborted = false,
                                   DedicatedInstance = false,
                                   Queued = false
                               };

            new PublishDispatchItems().Process(dispatchArgs);
        }

        private void SendEmailToContact(Job job, ContactData contact, MessageItem messageItem)
        {
            var customValues = new ExmCustomValues
                               {
                                   DispatchType = DispatchType.Normal,
                                   Email = contact.PreferredEmail,
                                   MessageLanguage = messageItem.TargetLanguage.ToString(),
                                   ManagerRootId = messageItem.ManagerRoot.InnerItem.ID.ToGuid(),
                                   MessageId = messageItem.InnerItem.ID.ToGuid()
                               };

            EcmFactory.GetDefaultFactory().Bl.DispatchManager.EnrollOrUpdateContact(contact.ContactId, new DispatchQueueItem(), messageItem.PlanId.ToGuid(), Constants.SendCompletedStateName, customValues);

            GenerateEventService.GenerateSent(messageItem.ManagerRoot.Settings.BaseURL, contact.ContactId, messageItem, messageItem.StartTime);
            job.CompletedEmails++;
        }

        private MessageItem GetMessageItem(Guid itemId)
        {
            return Sitecore.Modules.EmailCampaign.Factory.Instance.GetMessageItem(itemId.ToID());
        }
    }
}
