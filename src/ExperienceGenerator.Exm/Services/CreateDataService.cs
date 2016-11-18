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
        private const string ListManagerOwner = "xGenerator";

        private readonly RandomContactMessageEventsFactory _randomContactMessageEventsFactory;

        private readonly CampaignModel _campaign;
        private readonly Guid _exmCampaignId;
        private readonly List<Guid> _unsubscribeFromAllContacts = new List<Guid>();
        private readonly ContactRepository _contactRepository;
        private readonly ContactListRepository _contactListRepository;
        private readonly AdjustEmailStatisticsService _adjustEmailStatisticsService;
        private readonly Job _job;

        public CreateDataService(Job job, Guid exmCampaignId, CampaignModel campaign)
        {
            _job = job;
            _campaign = campaign;
            _exmCampaignId = exmCampaignId;
            _contactRepository = new ContactRepository(job);
            _contactListRepository = new ContactListRepository(job);
            _adjustEmailStatisticsService = new AdjustEmailStatisticsService();
            _randomContactMessageEventsFactory = new RandomContactMessageEventsFactory(_contactRepository, _campaign);
        }

        public void CreateData()
        {
            using (new SiteContextSwitcher(SiteContext.GetSite("website")))
            {
                using (new SecurityDisabler())
                {
                    SetJobStatus("Get Message Item...");
                    var messageItem = GetMessageItem(_exmCampaignId);

                    SetJobStatus("Cleanup...");
                    Cleanup(messageItem);

                    SetJobStatus("Smart Publish...");
                    PublishSmart();

                    SetJobStatus("Get Contacts For Message...");
                    var contactsForMessage = GetMessageContacts(messageItem);

                    SetJobStatus("Back-dating segments...");
                    BackDateSegments();

                    SetJobStatus("Creating emails...");
                    SendCampaign(messageItem, contactsForMessage);
                }
            }
        }

        private List<ContactData> GetMessageContacts(MessageItem messageItem)
        {
            AssociateListToMessage(messageItem);
            var contactsForMessage = GetExistingContactsForMessage(messageItem);

            CreateContactsAndList(messageItem, contactsForMessage);
            return contactsForMessage;
        }

        private void AssociateListToMessage(MessageItem messageItem)
        {
            if (string.IsNullOrEmpty(_campaign.ContactList))
                return;
            ID contactListID;
            if (!ID.TryParse(_campaign.ContactList, out contactListID) || ID.IsNullOrEmpty(contactListID))
                return;
            if (!_contactListRepository.Exists(contactListID))
                return;
            if (messageItem.RecipientManager.IncludedRecipientListIds.Contains(contactListID))
                return;

            messageItem.RecipientManager.AddIncludedRecipientListId(contactListID);
        }

        private List<ContactData> GetExistingContactsForMessage(MessageItem messageItem)
        {
            var contactsForThisEmail = _contactListRepository.GetContactsForEmail(messageItem.RecipientManager.IncludedRecipientListIds, _unsubscribeFromAllContacts);
            _contactRepository.AddContacts(contactsForThisEmail);
            return contactsForThisEmail;
        }

        private void SetJobStatus(string statusText)
        {
            _job.Status = statusText;
        }

        private void CreateContactsAndList(MessageItem messageItem, List<ContactData> contactsForMessage)
        {
            var contactsRequired = _campaign.Events.TotalSent - contactsForMessage.Count;
            _job.TargetContacts = Math.Max(contactsRequired, 0);
            _job.TargetEvents = Math.Max(_campaign.Events.TotalSent, contactsForMessage.Count);
            _job.TargetEmails = _job.TargetEvents;
            if (contactsRequired <= 0)
                return;

            SetJobStatus("Creating contacts");
            var addedContacts = _contactRepository.CreateContacts(contactsRequired);

            SetJobStatus("Creating lists...");
            var addedList = _contactListRepository.CreateList("Auto List " + DateTime.Now.Ticks, addedContacts);

            messageItem.RecipientManager.AddIncludedRecipientListId(ID.Parse(addedList.Id));
            contactsForMessage.AddRange(_contactListRepository.GetContacts(addedList, contactsRequired));
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

        private void PublishSmart()
        {
            var targetDatabases = new[] {Factory.GetDatabase("web")};
            Database masterDatabase = Factory.GetDatabase("master");
            var languages = masterDatabase.Languages;
            var handle = PublishManager.PublishSmart(masterDatabase, targetDatabases, languages);
            var start = DateTime.Now;
            while (!PublishManager.GetStatus(handle).IsDone)
            {
                if (DateTime.Now - start > TimeSpan.FromMinutes(1))
                    break;
            }
        }

        private void Cleanup(MessageItem messageItem)
        {
            var excludedRecipientsListIDs = messageItem.InnerItem.Fields["Excluded Recipient Lists"].Value.Split('|').ToList();
            var managerRootPath = messageItem.ManagerRoot.InnerItem.Paths.FullPath;
            var serviceMessages = messageItem.InnerItem.Database.SelectItems($"{managerRootPath}/Messages/Service Messages//*[@@templatename='HTML Message']");
            foreach (var serviceMessage in serviceMessages)
            {
                if (serviceMessage.Fields["Campaign"].Value != string.Empty)
                {
                    using (new EditContext(serviceMessage))
                    {
                        serviceMessage.Fields["Campaign"].Value = string.Empty;
                        serviceMessage.Fields["Engagement Plan"].Value = string.Empty;
                    }
                }
            }

            var lists = messageItem.InnerItem.Database.SelectItems("/sitecore/system/List Manager/All Lists//*[@@templatename='Contact List']").Where(x => x.Fields["Owner"].Value == ListManagerOwner);
            foreach (var list in lists)
            {
                list.Delete();
            }

            var excludeLists = messageItem.InnerItem.Database.SelectItems("/sitecore/system/List Manager/All Lists//*[@@templatename='Contact List']").Where(x => excludedRecipientsListIDs.Contains(x.ID.ToString())).ToList();
            foreach (var excludeList in excludeLists)
            {
                using (new EditContext(excludeList))
                {
                    excludeList.Fields["IncludedSources"].Value = string.Empty;
                    excludeList.Fields["ExcludedSources"].Value = string.Empty;
                }
            }

            var globalLists = messageItem.InnerItem.Database.SelectItems("/sitecore/system/List Manager/All Lists/#E-mail Campaign Manager#/System//*[@@templatename='Contact List']");
            foreach (var globalList in globalLists)
            {
                globalList.Delete();
            }
        }

        private async void GenerateEvents(MessageItem email, Funnel funnelDefinition, List<ContactData> contactsForThisEmail)
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
                    SetJobStatus($"Generating events for contact {contactIndex++} of {contactsForThisEmail.Count}");

                    var events = _randomContactMessageEventsFactory.CreateRandomContactMessageEvents(contactData, funnelDefinition, email);
                    foreach (var messageEvent in events.Events)
                    {
                        switch (messageEvent.EventType)
                        {
                            case EventType.Open:
                                GenerateEventService.GenerateHandlerEvent(events.MessageItem.ManagerRoot.Settings.BaseURL, events.ContactId, email, EventType.Open, messageEvent.EventTime, events.UserAgent, events.GeoData);
                                break;
                            case EventType.Unsubscribe:
                                GenerateEventService.GenerateHandlerEvent(events.MessageItem.ManagerRoot.Settings.BaseURL, events.ContactId, email, EventType.Unsubscribe, messageEvent.EventTime, events.UserAgent, events.GeoData);
                                break;
                            case EventType.UnsubscribeFromAll:
                                GenerateEventService.GenerateHandlerEvent(events.MessageItem.ManagerRoot.Settings.BaseURL, events.ContactId, email, EventType.UnsubscribeFromAll, messageEvent.EventTime, events.UserAgent, events.GeoData);
                                _unsubscribeFromAllContacts.Add(events.ContactId);
                                break;
                            case EventType.Click:
                                GenerateEventService.GenerateHandlerEvent(events.MessageItem.ManagerRoot.Settings.BaseURL, events.ContactId, email, EventType.Click, messageEvent.EventTime, events.UserAgent, events.GeoData, events.LandingPageUrl);
                                break;
                            case EventType.Bounce:
                                GenerateEventService.GenerateBounce(events.MessageItem.ManagerRoot.Settings.BaseURL, events.ContactId.ToID(), email.MessageId.ToID(), email.StartTime.AddMinutes(1));
                                break;
                            case EventType.SpamComplaint:
                                await GenerateEventService.GenerateSpamComplaint(events.MessageItem.ManagerRoot.Settings.BaseURL, events.ContactId.ToID(), email.MessageId.ToID(), "email", messageEvent.EventTime);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    _job.CompletedEvents++;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Failed to generate events", ex);
                throw;
            }
        }

        private void SendCampaign(MessageItem messageItem, List<ContactData> contactsForThisEmail)
        {
            SetJobStatus("Adjusting email stats...");

            _adjustEmailStatisticsService.AdjustEmailStatistics(_job, messageItem, _campaign);

            PublishEmail(messageItem);

            var contactIndex = 1;
            foreach (var contact in contactsForThisEmail)
            {
                SetJobStatus($"Sending email to contact {contactIndex++} of {contactsForThisEmail.Count}");
                try
                {
                    SendEmailToContact(contact, messageItem);
                }
                catch (Exception ex)
                {
                    SetJobStatus(ex.ToString());
                    Log.Error("Failed", ex, this);
                }
            }

            GenerateEvents(messageItem, _campaign.Events, contactsForThisEmail);
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

        private void SendEmailToContact(ContactData contact, MessageItem messageItem)
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

            GenerateEventService.GenerateSent(messageItem.ManagerRoot.Settings.BaseURL, new ID(contact.ContactId), messageItem.InnerItem.ID, messageItem.StartTime);
            _job.CompletedEmails++;
        }

        private MessageItem GetMessageItem(Guid itemId)
        {
            return Sitecore.Modules.EmailCampaign.Factory.Instance.GetMessageItem(itemId.ToID());
        }
    }
}