using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using ExperienceGenerator.Exm.Models;
using ExperienceGenerator.Exm.Repositories;
using Sitecore.Caching;
using Sitecore.Common;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.DependencyInjection;
using Sitecore.Diagnostics;
using Sitecore.EmailCampaign.Cm.Dispatch;
using Sitecore.EmailCampaign.Cm.Pipelines.DispatchNewsletter;
using Sitecore.ExM.Framework.Diagnostics;
using Sitecore.Modules.EmailCampaign.Core;
using Sitecore.Modules.EmailCampaign.Core.Data;
using Sitecore.Modules.EmailCampaign.Factories;
using Sitecore.Modules.EmailCampaign.Messages;
using Sitecore.Modules.EmailCampaign.Services;
using Sitecore.SecurityModel;
using Sitecore.Sites;
using Sitecore.XConnect;


namespace ExperienceGenerator.Exm.Services
{
    public class GenerateCampaignDataService
    {
        private readonly RandomContactMessageEventsFactory _randomContactMessageEventsFactory;

        private readonly CampaignSettings _campaign;
        private readonly Guid _exmCampaignId;
        private readonly List<Guid> _unsubscribeFromAllContacts = new List<Guid>();
        private readonly ContactListRepository _contactListRepository;
        private readonly IExmCampaignService _exmCampaignService;
        private readonly ItemUtilExt _itemUtilExt;
        private readonly ILogger _logger;
        private readonly AdjustEmailStatisticsService _adjustEmailStatisticsService;
        private readonly IDispatchManager _dispatchManager;
        private readonly IRecipientManagerFactory _recipientManagerFactory;
        private readonly EcmDataProvider _ecmDataProvider;

        public GenerateCampaignDataService(Guid exmCampaignId, CampaignSettings campaign)
        {
            _campaign = campaign;
            _exmCampaignId = exmCampaignId;
            _contactListRepository = new ContactListRepository();
            _exmCampaignService = (IExmCampaignService)ServiceLocator.ServiceProvider.GetService(typeof(IExmCampaignService));
            _dispatchManager = (IDispatchManager)ServiceLocator.ServiceProvider.GetService(typeof(IDispatchManager));
            _ecmDataProvider = (EcmDataProvider)ServiceLocator.ServiceProvider.GetService(typeof(EcmDataProvider));
            _recipientManagerFactory = (IRecipientManagerFactory)ServiceLocator.ServiceProvider.GetService(typeof(IRecipientManagerFactory));
            _logger = (ILogger)ServiceLocator.ServiceProvider.GetService(typeof(ILogger));
            _itemUtilExt = (ItemUtilExt)ServiceLocator.ServiceProvider.GetService(typeof(ItemUtilExt));
            _adjustEmailStatisticsService = new AdjustEmailStatisticsService();
            _randomContactMessageEventsFactory = new RandomContactMessageEventsFactory(_campaign);
            
        }

        public void CreateData(Job job)
        {
            using (new SecurityDisabler())
            {
                job.Status = "Get Message Item...";
                var messageItem = GetMessageItem(_exmCampaignId);

                var siteContext = GetExmSiteContext();
                using (new SiteContextSwitcher(siteContext))
                {
                    job.Status = "Cleanup...";
                    Cleanup(messageItem);

                    job.Status = "Get Contacts For Message...";
                    var contactsForMessage = GetMessageContacts(messageItem);

                    job.Status = "Back-dating segments...";
                    BackDateSegments();

                    job.Status = "Creating emails...";
                    SendCampaign(job, messageItem, contactsForMessage);
                }
            }
        }

        private static SiteContext GetExmSiteContext()
        {
            return SiteContext.GetSite(Sitecore.EmailCampaign.Model.Constants.ExmSiteName);
        }

        private IEnumerable<Contact> GetMessageContacts(MessageItem messageItem)
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

        private void GenerateEvents(Job job, MessageItem email, Funnel funnelDefinition, IReadOnlyCollection<Contact> contactsForThisEmail)
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
                _logger.LogError("Failed to generate events", ex);
                throw;
            }
        }

        private void SendCampaign(Job job, MessageItem messageItem, IEnumerable<Contact> contactsForThisEmail)
        {
            job.Status = "Adjusting email stats...";

            _adjustEmailStatisticsService.AdjustEmailStatistics(_ecmDataProvider,_recipientManagerFactory.GetRecipientManager(messageItem), job, messageItem, _campaign);

            PublishEmail(messageItem);
            var listOfContacts = contactsForThisEmail.ToList();
            var contactIndex = 1;
            foreach (var contact in listOfContacts)
            {
                if (job.JobStatus == JobStatus.Cancelling)
                    return;

                job.Status = $"Sending email to contact {contactIndex++} of {listOfContacts.Count}";
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

            GenerateEvents(job, messageItem, _campaign.Events, listOfContacts);
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

            new PublishDispatchItems(_itemUtilExt, _logger, _exmCampaignService).Process(dispatchArgs);
        }

        private void SendEmailToContact(Job job, Contact contact, MessageItem messageItem)
        {
            if (contact == null || !contact.Id.HasValue)
                return;

            var dispatchArgs = new DispatchNewsletterArgs(messageItem,new SendingProcessData(messageItem.MessageId.ToID()));
            
            _dispatchManager.AddRecipientToDispatchQueue(dispatchArgs,contact.Identifiers.FirstOrDefault(x => x.Source.Equals("ExperienceGenerator")));

            GenerateEventService.GenerateSent(messageItem.ManagerRoot.Settings.BaseURL, contact.Id.Value, messageItem, messageItem.StartTime);
            job.CompletedEmails++;
        }

        private MessageItem GetMessageItem(Guid itemId)
        {
            return _exmCampaignService.GetMessageItem(itemId);
        }
    }
}
