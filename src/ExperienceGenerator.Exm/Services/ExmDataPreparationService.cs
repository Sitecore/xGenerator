using ContactData = Sitecore.ListManagement.ContentSearch.Model.ContactData;

namespace ExperienceGenerator.Exm.Services
{
  using System;
  using System.Collections.Generic;
  using System.Configuration;
  using System.Data.SqlClient;
  using System.Linq;
  using System.Threading;
  using Colossus.Statistics;
  using ExperienceGenerator.Data;
  using ExperienceGenerator.Exm.Models;
  using Sitecore;
  using Sitecore.Analytics.Data.Items;
  using Sitecore.Caching;
  using Sitecore.Common;
  using Sitecore.Data;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;
  using Sitecore.EmailCampaign.Analytics.Model;
  using Sitecore.Modules.EmailCampaign;
  using Sitecore.Modules.EmailCampaign.Core;
  using Sitecore.Modules.EmailCampaign.Core.Dispatch;
  using Sitecore.Modules.EmailCampaign.Core.Gateways;
  using Sitecore.Modules.EmailCampaign.Core.Pipelines.DispatchNewsletter;
  using Sitecore.Modules.EmailCampaign.Factories;
  using Sitecore.Modules.EmailCampaign.Messages;
  using Sitecore.Publishing;
  using Sitecore.SecurityModel;

  public class ExmDataPreparationService
    {
        private readonly InitialExmDataPreparationModel _specification;
        private readonly Random _random = new Random();
        private readonly Database _db = Sitecore.Configuration.Factory.GetDatabase("master");
        private readonly Dictionary<string, List<Guid>> _contactsPerEmail = new Dictionary<string, List<Guid>>();
        private readonly List<Guid> _unsubscribeFromAllContacts = new List<Guid>();
        private readonly Func<string> _userAgent = FileHelpers.ReadLinesFromResource<GeoData>("ExperienceGenerator.Data.useragents.txt").ToArray().Uniform();
        private readonly Func<int> _eventDay;
        private readonly ExmContactService _contactService;
        private readonly ExmGoalService _goalService;
        private readonly ExmListService _listService;
        private readonly Func<string> _ip = new[]
        {
            //Denmark
            "194.255.38.57",
            //USA
            "204.15.21.186",
            //Netherlands
            "94.169.168.18",
            //Japan
            "202.246.252.97",
            //Canada
            "192.206.151.131"
        }.Uniform();

        private ManagerRoot _managerRoot;

        public ExmDataPreparationService(InitialExmDataPreparationModel specification)
        {
            this._specification = specification;
            this._contactService = new ExmContactService(specification);
            this._goalService = new ExmGoalService(specification);
            this._listService = new ExmListService(specification, this._contactService);

            this._eventDay = specification.EventDayDistribution.Keys.Weighted(specification.EventDayDistribution.Values.ToArray());
        }

        public void CreateData()
        {
            this._specification.Job.JobStatus = JobStatus.Running;
            this._specification.Job.Started = DateTime.Now;

            Context.SetActiveSite("website");

            try
            {
                using (new SecurityDisabler())
                {
                    this._specification.Job.Status = "Cleanup...";
                    this.Cleanup();

                    if (this._specification.RebuildMasterIndex)
                    {
                        this._specification.Job.Status = "Rebuilding master index...";
                        Sitecore.ContentSearch.ContentSearchManager.GetIndex("sitecore_master_index").Rebuild();
                    }

                    this._specification.Job.Status = "Creating goals...";
                    this._goalService.CreateGoals();

                    this._specification.Job.Status = "Smart Publish...";
                    this.PublishSmart();

                    this._specification.Job.Status = "Creating contacts";
                    this._contactService.CreateContacts(this._specification.NumContacts);

                    this._specification.Job.Status = "Creating lists...";
                    this._listService.CreateLists();

                    this._specification.Job.Status = "Identify manager root...";
                    this.GetManagerRoot();

                    this._specification.Job.Status = "Back-dating segments...";
                    this.BackDateSegments();

                    this._specification.Job.Status = "Creating emails...";
                    this.CreateAndSendCampaigns();
                }

                this._specification.Job.Status = "DONE!";
                this._specification.Job.JobStatus = JobStatus.Complete;
            }
            catch (Exception ex)
            {
                this._specification.Job.JobStatus = JobStatus.Failed;
                this._specification.Job.LastException = ex.ToString();
                Log.Error("Failed", ex, this);
            }

            this._specification.Job.Ended = DateTime.Now;
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
            var targetDatabases = new[] { Sitecore.Configuration.Factory.GetDatabase("web") };
            var languages = this._db.Languages;
            PublishManager.PublishSmart(this._db, targetDatabases, languages);
        }

        private void Cleanup()
        {
            var emailsPath = "/sitecore/content/Email Campaign/Messages/" + DateTime.Now.Year;
            this._db.GetItem(emailsPath)?.Delete();

            var serviceMessages = this._db.SelectItems("/sitecore/content/Email Campaign/Messages/Service Messages//*[@@templatename='HTML Message']");
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

            var engagementPlansPath = "/sitecore/system/Marketing Control Panel/Engagement Plans/Email Campaign/Emails/" + DateTime.Now.Year;
            this._db.GetItem(engagementPlansPath)?.Delete();

            var campaignsPath = "/sitecore/system/Marketing Control Panel/Campaigns/Emails/" + DateTime.Now.Year;
            this._db.GetItem(campaignsPath)?.Delete();

            var lists = this._db.SelectItems("/sitecore/system/List Manager/All Lists/*[@@templatename='Contact List']");
            foreach (var list in lists)
            {
                list.Delete();
            }
        }

        private void GenerateEvents(MessageItem email, ExmEventPercentages percentages)
        {
            if (percentages == null)
            {
                return;
            }

            var contactIndex = 1;
            var contactsThisEmail = this._contactsPerEmail[email.ID];
            var numContactsForThisEmail = contactsThisEmail.Count;
            foreach (var contactId in contactsThisEmail)
            {
                this._specification.Job.Status = string.Format(
                    "Generating events for contact {0} of {1}",
                    contactIndex++,
                    numContactsForThisEmail);

                var contact = this._contactService.GetContact(contactId);
                if (contact == null)
                {
                    continue;
                }

                var bouncePercentage = this.GetRandomPercentage(percentages.BounceMin, percentages.BounceMax);
                if (this._random.NextDouble() < bouncePercentage)
                {
                    ExmEventsGenerator.GenerateBounce(this._managerRoot.Settings.BaseURL, contact.ContactId.ToID(), email.MessageId.ToID(), email.StartTime.AddMinutes(1));
                }
                else
                {
                    var userAgent = this._userAgent();
                    var ip = this._ip();
                    var eventDay = this._eventDay();
                    var seconds = this._random.Next(60, 86400);
                    var eventDate = email.StartTime.AddDays(eventDay).AddSeconds(seconds);

                    var openPercentage = this.GetRandomPercentage(percentages.OpenMin, percentages.OpenMax);
                    var spamPercentage = this.GetRandomPercentage(percentages.SpamMin, percentages.SpamMax);

                    if (this._random.NextDouble() < openPercentage)
                    {
                        ExmEventsGenerator.GenerateHandlerEvent(this._managerRoot.Settings.BaseURL, contact.ContactId, email, ExmEvents.Open, eventDate, userAgent, ip);

                        eventDate = eventDate.AddSeconds(this._random.Next(10, 300));

                        var clickPercentage = this.GetRandomPercentage(percentages.ClickMin, percentages.ClickMax);
                        if (this._random.NextDouble() < clickPercentage)
                        {
                            // Much less likely to complain if they were interested enough to click the link.
                            spamPercentage = 0.01;

                            var link = "/";
                            if (this._goalService.GoalsSet != null)
                            {
                                link += this._goalService.GoalsSet().Item;
                            }

                            ExmEventsGenerator.GenerateHandlerEvent(this._managerRoot.Settings.BaseURL, contact.ContactId, email, ExmEvents.Click, eventDate, userAgent, ip, link);
                            eventDate = eventDate.AddSeconds(this._random.Next(10, 300));
                        }
                    }

                    if (this._random.NextDouble() < spamPercentage)
                    {
                        ExmEventsGenerator.GenerateSpamComplaint(this._managerRoot.Settings.BaseURL, contact.ContactId.ToID(), email.MessageId.ToID(), "email", eventDate);
                        eventDate = eventDate.AddSeconds(this._random.Next(10, 300));
                    }

                    var unsubscribePercentage = this.GetRandomPercentage(percentages.UnsubscribeMin, percentages.UnsubscribeMax);
                    if (this._random.NextDouble() < unsubscribePercentage)
                    {
                        var unsubscribeFromAllPercentage = this.GetRandomPercentage(percentages.UnsubscribeFromAllMin, percentages.UnsubscribeFromAllMax);
                        ExmEvents unsubscribeEvent;

                        if (this._random.NextDouble() < unsubscribeFromAllPercentage)
                        {
                            unsubscribeEvent = ExmEvents.UnsubscribeFromAll;
                            this._unsubscribeFromAllContacts.Add(contact.ContactId);
                        }
                        else
                        {
                            unsubscribeEvent = ExmEvents.Unsubscribe;
                        }

                        ExmEventsGenerator.GenerateHandlerEvent(this._managerRoot.Settings.BaseURL, contact.ContactId, email,
                            unsubscribeEvent, eventDate, userAgent, ip);
                    }
                }
            }
        }

        public double GetRandomPercentage(double minimum, double maximum)
        {
            if (Math.Abs(minimum - maximum) < 0.01)
            {
                return minimum / 100;
            }

            return (this._random.NextDouble() * (maximum - minimum) + minimum) / 100;
        }

        private void GetManagerRoot()
        {
            var query = string.Format("fast:/sitecore/content//*[@@templateid='{0}']", TemplateIds.ManagerRoot);
            var rootItem = Context.Database.SelectSingleItem(query);

            if (rootItem == null)
            {
                throw new Exception("ManagerRoot not found");
            }

            this._managerRoot = ManagerRoot.FromItem(rootItem);
        }

        private void CreateAndSendCampaigns()
        {
            if (this._specification.SpecificCampaigns != null && this._specification.SpecificCampaigns.Any())
            {
                this.CreateAndSendSpecificCampaigns();
            }
            else if (this._specification.RandomCampaigns != null)
            {
                this.CreateAndSendRandomCampaigns();
            }
        }

        private void CreateAndSendRandomCampaigns()
        {
            var totalDays = (int)(DateTime.UtcNow - this._specification.RandomCampaigns.DateRangeStart).TotalDays;

            var dates = new List<DateTime>();
            for (var i = 0; i < this._specification.RandomCampaigns.NumCampaigns; i++)
            {
                var daysAgo = this._random.Next(0, totalDays);
                var dateMessageSent = DateTime.UtcNow.Date.AddDays(-1 * daysAgo);
                var secondsAgo = this._random.Next(0, 86400);
                dateMessageSent = dateMessageSent.AddSeconds(-1 * secondsAgo);
                dates.Add(dateMessageSent);
            }

            // Sort dates so that growth over time makes sense
            dates.Sort();

            var randomListNames = new List<string>();
            for (var i = 0; i < this._specification.RandomLists.NumLists; i++)
            {
                randomListNames.Add("Auto List " + i);
            }

            for (var i = 0; i < this._specification.RandomCampaigns.NumCampaigns; i++)
            {
                var emailName = "Auto campaign " + i;
                var dateMessageSent = dates[i];

                var numListsToTake = this._random.Next(this._specification.RandomCampaigns.ListsPerCampaignMin, this._specification.RandomCampaigns.ListsPerCampaignMax);
                var includeLists = randomListNames.OrderBy(x => Guid.NewGuid()).Take(numListsToTake).ToList();

                this.CreateAndSendEmail(emailName, includeLists, dateMessageSent, this._specification.RandomCampaigns.Events);
            }
        }

        private void CreateAndSendSpecificCampaigns()
        {
            foreach (var emailSpecification in this._specification.SpecificCampaigns.OrderBy(x => x.DateEffective))
            {
                this.CreateAndSendEmail(emailSpecification.Name, emailSpecification.IncludeLists, emailSpecification.DateEffective, emailSpecification.Events);
            }
        }

        private void CreateAndSendEmail(string name, List<string> lists, DateTime dateMessageSent, ExmEventPercentages percentages)
        {
            var messageItem = this.CreateEmailMessage(name, name);

            var contactsForThisEmail = this.GetContactsForEmail(lists, messageItem);
            var sendingProcessData = new SendingProcessData(new ID(messageItem.MessageId));

            var dateMessageFinished = dateMessageSent.AddMinutes(5);

            this.AdjustEmailStatsWithRetry(messageItem, sendingProcessData, dateMessageSent, dateMessageFinished, 30);
            
            this.PublishEmail(messageItem, sendingProcessData);

            this._contactsPerEmail[messageItem.ID] = new List<Guid>();

            var numContactsForThisEmail = contactsForThisEmail.Count;

            var contactIndex = 1;
            foreach (var contact in contactsForThisEmail)
            {
                this._specification.Job.Status = string.Format(
                    "Sending email to contact {0} of {1}",
                    contactIndex++,
                    numContactsForThisEmail);

                try
                {
                    this.SendEmailToContact(contact, messageItem);
                    this._contactsPerEmail[messageItem.ID].Add(contact.ContactId);
                }
                catch (Exception ex)
                {
                    this._specification.Job.LastException = ex.ToString();
                }
            }

            messageItem.Source.State = MessageState.Sent;

            this.GenerateEvents(messageItem, percentages);
            this._listService.GrowLists();
            this._specification.Job.CompletedEmails++;
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

            EcmFactory.GetDefaultFactory()
                .Bl.DispatchManager.EnrollOrUpdateContact(contact.ContactId, new DispatchQueueItem(),
                    messageItem.PlanId.ToGuid(), Sitecore.Modules.EmailCampaign.Core.Constants.SendCompletedStateName,
                    customValues);

            ExmEventsGenerator.GenerateSent(this._managerRoot.Settings.BaseURL, new ID(contact.ContactId), messageItem.InnerItem.ID, messageItem.StartTime);
        }

        private void PublishEmail(MessageItem messageItem, SendingProcessData sendingProcessData)
        {
            var dispatchArgs = new DispatchNewsletterArgs(messageItem, sendingProcessData)
            {
                IsTestSend = false,
                SendingAborted = false,
                DedicatedInstance = false,
                Queued = false
            };

            new PublishDispatchItems().Process(dispatchArgs);
        }

        private void AdjustEmailStatsWithRetry(MessageItem messageItem, SendingProcessData sendingProcessData,
            DateTime dateMessageSent, DateTime dateMessageFinished, int retryCount)
        {
            int sleepTime = 1000;

            for (var i = 0; i < retryCount; i++)
            {
                try
                {
                    this.AdjustEmailStats(messageItem, sendingProcessData, dateMessageSent, dateMessageFinished);
                    return;
                }
                catch (Exception)
                {
                    Thread.Sleep(sleepTime);
                    sleepTime += 1000;
                }
            }
        }

        private void AdjustEmailStats(MessageItem messageItem, SendingProcessData sendingProcessData, DateTime dateMessageSent, DateTime dateMessageFinished)
        {
            var deployAnalytics = new DeployAnalytics();
            deployAnalytics.Process(new DispatchNewsletterArgs(messageItem, sendingProcessData));

            messageItem.Source.StartTime = dateMessageSent;
            messageItem.Source.EndTime = dateMessageFinished;

            var innerItem = messageItem.InnerItem;
            using (new EditContext(innerItem))
            {
                innerItem.RuntimeSettings.ReadOnlyStatistics = true;
                innerItem[FieldIDs.Updated] = DateUtil.ToIsoDate(dateMessageSent);
            }

            var itemUtil = new ItemUtilExt();
            var campaignItem = itemUtil.GetItem(messageItem.CampaignId);
            using (new EditContext(campaignItem))
            {
                campaignItem["StartDate"] = DateUtil.ToIsoDate(dateMessageSent);
                campaignItem[CampaignclassificationItem.FieldIDs.Channel] =
                    EcmFactory.GetDefaultFactory().Io.EcmSettings.CampaignClassificationChannel;
                campaignItem["EndDate"] = DateUtil.ToIsoDate(dateMessageFinished);
            }

            // Updates the totalRecipients and endTime in the EmailCampaign collection.
            EcmFactory.GetDefaultFactory()
                .Gateways.EcmDataGateway.SetMessageStatisticData(messageItem.CampaignId.ToGuid(), dateMessageSent,
                    dateMessageFinished, FieldUpdate.Set(messageItem.SubscribersIds.Value.Count));
        }

        private List<ContactData> GetContactsForEmail(IEnumerable<string> lists, MessageItem messageItem)
        {
            var contactsForThisEmail = new List<ContactData>();

            foreach (var listName in lists)
            {
                var list = this._listService.GetList(listName);
                if (list != null)
                {
                    messageItem.RecipientManager.AddIncludedRecipientListId(ID.Parse(list.Id));

                    var contacts = this._listService. ListManager.GetContacts(list);
                    contactsForThisEmail.AddRange(contacts);
                }
            }

            contactsForThisEmail = contactsForThisEmail
                .DistinctBy(x => x.ContactId)
                .Where(x => !this._unsubscribeFromAllContacts.Contains(x.ContactId))
                .ToList();

            return contactsForThisEmail;
        }

        private MessageItem CreateEmailMessage(string messageName, string messageSubject)
        {
            var oneColumnMessageBranchId = "{6FE51EB4-1D30-4E6B-8BA0-0EBB1405D283}";
            var query = string.Format("./descendant::*[@@tid='{0}']", TemplateIds.OneTimeMessageType);
            var typeId = this._managerRoot.InnerItem.Axes.SelectSingleItem(query).ID.ToString();

            var messageItem = MessageItemSource.Create(messageName, oneColumnMessageBranchId, typeId);
            messageItem.Source.DisplayName = messageName;
            ((MailMessageItemSource)messageItem.Source).Subject = messageSubject;
            messageItem.Source.UsePreferredLanguage = false;

            return messageItem;
        }
    }
}