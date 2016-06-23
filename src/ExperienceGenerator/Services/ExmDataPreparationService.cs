using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Threading;
using Colossus.Statistics;
using ExperienceGenerator.Data;
using ExperienceGenerator.Models.Exm;
using Sitecore;
using Sitecore.Analytics.Data;
using Sitecore.Analytics.Data.Items;
using Sitecore.Analytics.DataAccess;
using Sitecore.Analytics.Model;
using Sitecore.Analytics.Model.Entities;
using Sitecore.Caching;
using Sitecore.Common;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.EmailCampaign.Analytics.Model;
using Sitecore.ListManagement;
using Sitecore.ListManagement.ContentSearch.Model;
using Sitecore.ListManagement.Services;
using Sitecore.Marketing.Definitions;
using Sitecore.Marketing.Definitions.Goals;
using Sitecore.Modules.EmailCampaign;
using Sitecore.Modules.EmailCampaign.Core;
using Sitecore.Modules.EmailCampaign.Core.Dispatch;
using Sitecore.Modules.EmailCampaign.Core.Gateways;
using Sitecore.Modules.EmailCampaign.Core.Pipelines.DispatchNewsletter;
using Sitecore.Modules.EmailCampaign.Factories;
using Sitecore.Modules.EmailCampaign.Messages;
using Sitecore.Publishing;
using Sitecore.SecurityModel;
using AuthenticationLevel = Sitecore.Analytics.Model.AuthenticationLevel;
using Contact = Sitecore.Analytics.Tracking.Contact;
using ContactData = Sitecore.ListManagement.ContentSearch.Model.ContactData;
using Factory = Sitecore.Configuration.Factory;

namespace ExperienceGenerator.Services
{
    public class ExmDataPreparationService
    {
        private readonly ExmDataPreparationModel _specification;
        private readonly string[] _languages = { "en", "uk" };
        private readonly ListManager<ContactList, ContactData> _listManager;
        private readonly List<Contact> _contacts = new List<Contact>();
        private readonly List<ContactList> _lists = new List<ContactList>();
        private readonly List<ExmGoal> _goals = new List<ExmGoal>();
        private Func<ExmGoal> _goalsSet;
        private readonly Random _random = new Random();
        private readonly Database _db = Factory.GetDatabase("master");
        private readonly Dictionary<string, List<Guid>> _contactsPerEmail = new Dictionary<string, List<Guid>>();
        private readonly List<Guid> _unsubscribeFromAllContacts = new List<Guid>();
        private readonly ItemUtilExt _itemUtil = new ItemUtilExt();
        private readonly Func<string> _userAgent = FileHelpers.ReadLinesFromResource<GeoData>("ExperienceGenerator.Data.useragents.txt").ToArray().Uniform();
        private readonly Func<int> _eventDay;
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

        public ExmDataPreparationService(ExmDataPreparationModel specification)
        {
            _specification = specification;
            _listManager = (ListManager<ContactList, ContactData>)Factory.CreateObject("contactListManager", false);
            _eventDay = specification.EventDayDistribution.Keys.Weighted(specification.EventDayDistribution.Values.ToArray());
        }

        public void CreateData()
        {
            _specification.Job.JobStatus = JobStatus.Running;
            _specification.Job.Started = DateTime.Now;

            Context.SetActiveSite("website");

            try
            {
                using (new SecurityDisabler())
                {
                    _specification.Job.Status = "Cleanup...";
                    Cleanup();

                    _specification.Job.Status = "Creating goals...";
                    CreateGoals();

                    _specification.Job.Status = "Smart Publish...";
                    PublishSmart();

                    _specification.Job.Status = "Creating contacts";
                    CreateContacts();

                    _specification.Job.Status = "Creating lists...";
                    CreateLists();

                    _specification.Job.Status = "Identify manager root...";
                    GetManagerRoot();

                    //_specification.Job.Status = "Back-dating segments...";
                    BackDateSegments();

                    _specification.Job.Status = "Creating emails...";
                    CreateAndSendCampaigns();
                }

                _specification.Job.Status = "DONE!";
                _specification.Job.JobStatus = JobStatus.Complete;
            }
            catch (Exception ex)
            {
                _specification.Job.JobStatus = JobStatus.Failed;
                _specification.Job.LastException = ex.ToString();
                Log.Error("Failed", ex, this);
            }

            _specification.Job.Ended = DateTime.Now;
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

            //ExperienceAnalytics.Segments
            CacheManager.ClearAllCaches();
        }

        private void PublishSmart()
        {
            var targetDatabases = new[] { Factory.GetDatabase("web") };
            var languages = _db.Languages;
            PublishManager.PublishSmart(_db, targetDatabases, languages);
        }

        private void CreateGoals()
        {
            if (_specification.Goals == null || !_specification.Goals.Any())
            {
                return;
            }

            foreach (var goal in _specification.Goals)
            {
                CreateGoal(goal);
                _specification.Job.CompletedGoals++;
            }

            _goalsSet = _goals.ToArray().Uniform();
        }

        private void CreateGoal(ExmGoal goal)
        {
            _goals.Add(goal);

            var goalItemPath = "/sitecore/system/Marketing Control Panel/Goals/" + goal.Name;
            var goalItem = _db.GetItem(goalItemPath);
            if (goalItem == null)
            {
                var goalTemplate = _db.GetTemplate("{475E9026-333F-432D-A4DC-52E03B75CB6B}");

                goalItem = _db.CreateItemPath(goalItemPath, goalTemplate);
                using (new EditContext(goalItem))
                {
                    goalItem["Points"] = goal.Points.ToString();
                }

                _itemUtil.ExecuteWorkflowCommandForItem(goalItem, ItemIds.DeployAnalyticsCommand);
                var manager = DefinitionManagerFactory.Default.GetDefinitionManager<IGoalDefinition>("item");
                var definition = manager.Get(goalItem.ID, CultureInfo.GetCultureInfo("en"));
                manager.SaveAsync(definition, true);
            }

            var sampleTemplate = _db.GetTemplate("{76036F5E-CBCE-46D1-AF0A-4143F9B557AA}");

            var pageItemPath = Context.Site.StartPath + "/" + goal.Item;
            var pageItem = _db.GetItem(pageItemPath);
            if (pageItem == null)
            {
                pageItem = _db.CreateItemPath(pageItemPath, sampleTemplate);

                using (new EditContext(pageItem))
                {
                    pageItem["__Tracking"] = string.Format("<tracking><event id=\"{0}\" name=\"{1}\" /></tracking>", goalItem.ID, goalItem.DisplayName);
                }
            }
        }

        private void Cleanup()
        {
            var emailsPath = "/sitecore/content/Email Campaign/Messages/" + DateTime.Now.Year;
            _db.GetItem(emailsPath)?.Delete();

            var serviceMessages = _db.SelectItems("/sitecore/content/Email Campaign/Messages/Service Messages//*[@@templatename='HTML Message']");
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
            _db.GetItem(engagementPlansPath)?.Delete();

            var campaignsPath = "/sitecore/system/Marketing Control Panel/Campaigns/Emails/" + DateTime.Now.Year;
            _db.GetItem(campaignsPath)?.Delete();

            var lists = _db.SelectItems("/sitecore/system/List Manager/All Lists/*[@@templatename='Contact List']");
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
            var contactsThisEmail = _contactsPerEmail[email.ID];
            var numContactsForThisEmail = contactsThisEmail.Count;
            foreach (var contactId in contactsThisEmail)
            {
                _specification.Job.Status = string.Format(
                    "Generating events for contact {0} of {1}",
                    contactIndex++,
                    numContactsForThisEmail);

                var contact = _contacts.FirstOrDefault(x => x.ContactId == contactId);
                if (contact == null)
                {
                    continue;
                }

                var bouncePercentage = GetRandomPercentage(percentages.BounceMin, percentages.BounceMax);
                if (_random.NextDouble() < bouncePercentage)
                {
                    ExmEventsGenerator.GenerateBounce(_managerRoot.Settings.BaseURL, contact.ContactId.ToID(), email.MessageId.ToID(), email.StartTime.AddMinutes(1));
                }
                else
                {
                    var userAgent = _userAgent();
                    var ip = _ip();
                    var eventDay = _eventDay();
                    var seconds = _random.Next(60, 86400);
                    var eventDate = email.StartTime.AddDays(eventDay).AddSeconds(seconds);

                    var openPercentage = GetRandomPercentage(percentages.OpenMin, percentages.OpenMax);
                    var spamPercentage = GetRandomPercentage(percentages.SpamMin, percentages.SpamMax);

                    if (_random.NextDouble() < openPercentage)
                    {
                        ExmEventsGenerator.GenerateHandlerEvent(_managerRoot.Settings.BaseURL, contact.ContactId, email, ExmEvents.Open, eventDate, userAgent, ip);

                        eventDate = eventDate.AddSeconds(_random.Next(10, 300));

                        var clickPercentage = GetRandomPercentage(percentages.ClickMin, percentages.ClickMax);
                        if (_random.NextDouble() < clickPercentage)
                        {
                            // Much less likely to complain if they were interested enough to click the link.
                            spamPercentage = 0.01;

                            var link = "/";
                            if (_goalsSet != null)
                            {
                                link += _goalsSet().Item;
                            }

                            ExmEventsGenerator.GenerateHandlerEvent(_managerRoot.Settings.BaseURL, contact.ContactId, email, ExmEvents.Click, eventDate, userAgent, ip, link);
                            eventDate = eventDate.AddSeconds(_random.Next(10, 300));
                        }
                    }

                    if (_random.NextDouble() < spamPercentage)
                    {
                        ExmEventsGenerator.GenerateSpamComplaint(_managerRoot.Settings.BaseURL, contact.ContactId.ToID(), email.MessageId.ToID(), "email", eventDate);
                        eventDate = eventDate.AddSeconds(_random.Next(10, 300));
                    }

                    var unsubscribePercentage = GetRandomPercentage(percentages.UnsubscribeMin, percentages.UnsubscribeMax);
                    if (_random.NextDouble() < unsubscribePercentage)
                    {
                        var unsubscribeFromAllPercentage = GetRandomPercentage(percentages.UnsubscribeFromAllMin, percentages.UnsubscribeFromAllMax);
                        ExmEvents unsubscribeEvent;

                        if (_random.NextDouble() < unsubscribeFromAllPercentage)
                        {
                            unsubscribeEvent = ExmEvents.UnsubscribeFromAll;
                            _unsubscribeFromAllContacts.Add(contact.ContactId);
                        }
                        else
                        {
                            unsubscribeEvent = ExmEvents.Unsubscribe;
                        }

                        ExmEventsGenerator.GenerateHandlerEvent(_managerRoot.Settings.BaseURL, contact.ContactId, email,
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

            return (_random.NextDouble() * (maximum - minimum) + minimum) / 100;
        }

        private void GetManagerRoot()
        {
            var query = string.Format("fast:/sitecore/content//*[@@templateid='{0}']", TemplateIds.ManagerRoot);
            var rootItem = Context.Database.SelectSingleItem(query);

            if (rootItem == null)
            {
                throw new Exception("ManagerRoot not found");
            }

            _managerRoot = ManagerRoot.FromItem(rootItem);
        }

        private void CreateAndSendCampaigns()
        {
            if (_specification.SpecificCampaigns != null && _specification.SpecificCampaigns.Any())
            {
                CreateAndSendSpecificCampaigns();
            }
            else if (_specification.RandomCampaigns != null)
            {
                CreateAndSendRandomCampaigns();
            }
        }

        private void CreateAndSendRandomCampaigns()
        {
            var totalDays = (int)(DateTime.UtcNow - _specification.RandomCampaigns.DateRangeStart).TotalDays;

            var randomListNames = new List<string>();
            for (var i = 0; i < _specification.RandomLists.NumLists; i++)
            {
                randomListNames.Add("Auto List " + i);
            }

            for (var i = 0; i < _specification.RandomCampaigns.NumCampaigns; i++)
            {
                var emailName = "Auto campaign " + i;

                var daysAgo = _random.Next(0, totalDays);
                var dateMessageSent = DateTime.UtcNow.AddDays(-1 * daysAgo);

                var numListsToTake = _random.Next(_specification.RandomCampaigns.ListsPerCampaignMin, _specification.RandomCampaigns.ListsPerCampaignMax);
                var includeLists = randomListNames.OrderBy(x => Guid.NewGuid()).Take(numListsToTake).ToList();

                CreateAndSendEmail(emailName, includeLists, dateMessageSent, _specification.RandomCampaigns.Events);
            }
        }

        private void CreateAndSendSpecificCampaigns()
        {
            foreach (var emailSpecification in _specification.SpecificCampaigns)
            {
                CreateAndSendSpecificCampaign(emailSpecification);
            }
        }

        private void CreateAndSendSpecificCampaign(ExmSpecificCampaign emailSpecification)
        {
            var dateMessageSent = emailSpecification.Date.HasValue
                ? emailSpecification.Date.Value.ToUniversalTime()
                : DateTime.UtcNow.AddDays(-1 * emailSpecification.DaysAgo);

            CreateAndSendEmail(emailSpecification.Name, emailSpecification.IncludeLists, dateMessageSent, emailSpecification.Events);
        }

        private void CreateAndSendEmail(string name, List<string> lists, DateTime dateMessageSent, ExmEventPercentages percentages)
        {
            var messageItem = CreateEmailMessage(name, name);

            var contactsForThisEmail = GetContactsForEmail(lists, messageItem);
            var sendingProcessData = new SendingProcessData(new ID(messageItem.MessageId));

            var dateMessageFinished = dateMessageSent.AddMinutes(5);

            AdjustEmailStats(messageItem, sendingProcessData, dateMessageSent, dateMessageFinished);

            PublishEmail(messageItem, sendingProcessData);

            _contactsPerEmail[messageItem.ID] = new List<Guid>();


            var numContactsForThisEmail = contactsForThisEmail.Count;

            var contactIndex = 1;
            foreach (var contact in contactsForThisEmail)
            {
                _specification.Job.Status = string.Format(
                    "Sending email to contact {0} of {1}",
                    contactIndex++,
                    numContactsForThisEmail);
                _contactsPerEmail[messageItem.ID].Add(contact.ContactId);
                SendEmailToContact(contact, messageItem);
            }

            messageItem.Source.State = MessageState.Sent;

            GenerateEvents(messageItem, percentages);
            _specification.Job.CompletedEmails++;
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

            ExmEventsGenerator.GenerateSent(_managerRoot.Settings.BaseURL, new ID(contact.ContactId), messageItem.InnerItem.ID, messageItem.StartTime);
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
                var list = _lists.FirstOrDefault(x => x.Name.Equals(listName, StringComparison.InvariantCultureIgnoreCase));
                if (list != null)
                {
                    messageItem.RecipientManager.AddIncludedRecipientListId(ID.Parse(list.Id));

                    var contacts = _listManager.GetContacts(list);
                    contactsForThisEmail.AddRange(contacts);
                }
            }

            contactsForThisEmail = contactsForThisEmail
                .DistinctBy(x => x.ContactId)
                .Where(x => !_unsubscribeFromAllContacts.Contains(x.ContactId))
                .ToList();

            return contactsForThisEmail;
        }

        private MessageItem CreateEmailMessage(string messageName, string messageSubject)
        {
            var oneColumnMessageBranchId = "{6FE51EB4-1D30-4E6B-8BA0-0EBB1405D283}";
            var query = string.Format("./descendant::*[@@tid='{0}']", TemplateIds.OneTimeMessageType);
            var typeId = _managerRoot.InnerItem.Axes.SelectSingleItem(query).ID.ToString();

            var messageItem = MessageItemSource.Create(messageName, oneColumnMessageBranchId, typeId);
            messageItem.Source.DisplayName = messageName;
            ((MailMessageItemSource)messageItem.Source).Subject = messageSubject;
            messageItem.Source.UsePreferredLanguage = false;

            return messageItem;
        }

        private void CreateLists()
        {
            if (_specification.SpecificLists != null && _specification.SpecificLists.Any())
            {
                CreateSpecificLists();
            }
            else if (_specification.RandomLists != null)
            {
                CreateRandomLists();
            }

            WaitUntilListsUnlocked();
        }

        private void CreateRandomLists()
        {
            for (var i = 0; i < _specification.RandomLists.NumLists; i++)
            {
                var name = "Auto List " + i;
                var xaList = CreateList(name);
                _listManager.AssociateContacts(xaList, SelectRandomContacts(_specification.RandomLists.ContactsMin, _specification.RandomLists.ContactsMax));
                _lists.Add(xaList);
            }
        }

        private void CreateSpecificLists()
        {
            foreach (var listSpecification in _specification.SpecificLists)
            {
                var xaList = CreateList(listSpecification.Name);
                _listManager.AssociateContacts(xaList, SelectRandomContacts(listSpecification.NumContacts));
                _lists.Add(xaList);
            }
        }

        private ContactList CreateList(string name)
        {
            var contactList = new ContactList
            {
                Name = name,
                Owner = "xGenerator",
                Type = ListRowType.ContactList
            };

            _listManager.Create(contactList);
            _specification.Job.CompletedLists++;
            return contactList;
        }

        private void WaitUntilListsUnlocked()
        {
            bool hasUnlocked;

            _specification.Job.Status = "Waiting for lists to unlock...";

            do
            {
                hasUnlocked = false;

                var lockedLists = new List<string>();
                foreach (var list in _lists)
                {
                    if (_listManager.IsLocked(list))
                    {
                        hasUnlocked = true;
                        lockedLists.Add(list.Name);
                    }
                }

                if (hasUnlocked)
                {
                    _specification.Job.Status = string.Format(
                        "Waiting for lists to unlock ({0})...", 
                        string.Join(", ", lockedLists));
                    Thread.Sleep(1000);
                }
            } while (hasUnlocked);
        }

        private IEnumerable<ContactData> SelectRandomContacts(int min, int max)
        {
            var numberToTake = _random.Next(min, max);
            return SelectRandomContacts(numberToTake);
        }

        private IEnumerable<ContactData> SelectRandomContacts(int numberToTake)
        {
            return _contacts.OrderBy(x => Guid.NewGuid()).Select(ContactToContactData).Take(numberToTake);
        }

        // TODO: Isn't this in Sitecore's API somewhere?
        private ContactData ContactToContactData(Contact contact)
        {
            var result = new ContactData
            {
                ContactId = contact.ContactId,
                Identifier = contact.Identifiers.Identifier
            };

            var contactPersonalInfo = contact.GetFacet<IContactPersonalInfo>("Personal");
            result.FirstName = contactPersonalInfo.FirstName;
            result.MiddleName = contactPersonalInfo.MiddleName;
            result.Surname = contactPersonalInfo.Surname;
            result.Nickname = contactPersonalInfo.Nickname;

            if (contactPersonalInfo.BirthDate != null)
            {
                result.BirthDate = contactPersonalInfo.BirthDate.Value;
            }

            result.Gender = contactPersonalInfo.Gender;
            result.JobTitle = contactPersonalInfo.JobTitle;
            result.Suffix = contactPersonalInfo.Suffix;
            result.Title = contactPersonalInfo.Title;

            var contactEmailAddresses = contact.GetFacet<IContactEmailAddresses>("Emails");
            result.PreferredEmail = contactEmailAddresses.Entries[contactEmailAddresses.Preferred].SmtpAddress;

            result.IdentificationLevel = contact.Identifiers.IdentificationLevel.ToString();
            result.Classification = contact.System.Classification;
            result.VisitCount = contact.System.VisitCount;
            result.Value = contact.System.Value;
            result.IntegrationLabel = contact.System.IntegrationLabel;

            return result;
        }

        private void CreateContacts()
        {
            for (var i = 0; i < _specification.NumContacts; i++)
            {
                var contact = CreateContact(i);
                _contacts.Add(contact);
                _specification.Job.CompletedContacts++;
            }
        }

        private Contact CreateContact(int index)
        {
            var identifier = "XGen" + index;

            var contactRepository = new ContactRepository();

            var contact = contactRepository.LoadContactReadOnly(identifier);
            if (contact != null)
            {
                return contact;
            }

            contact = contactRepository.CreateContact(ID.NewID);
            contact.Identifiers.AuthenticationLevel = AuthenticationLevel.None;
            contact.System.Classification = 0;
            contact.ContactSaveMode = ContactSaveMode.AlwaysSave;
            contact.Identifiers.Identifier = "XGen" + index;
            contact.System.OverrideClassification = 0;
            contact.System.Value = 0;
            contact.System.VisitCount = 0;

            var contactPreferences = contact.GetFacet<IContactPreferences>("Preferences");
            contactPreferences.Language = _languages[index % _languages.Length];

            var contactPersonalInfo = contact.GetFacet<IContactPersonalInfo>("Personal");
            contactPersonalInfo.FirstName = Faker.Name.First();
            contactPersonalInfo.Surname = Faker.Name.Last();

            var contactEmailAddresses = contact.GetFacet<IContactEmailAddresses>("Emails");
            contactEmailAddresses.Entries.Create("Work").SmtpAddress =
                Faker.Internet.Email(string.Format("{0} {1}", contactPersonalInfo.FirstName, contactPersonalInfo.Surname));
            contactEmailAddresses.Preferred = "Work";

            contactRepository.SaveContact(contact, new ContactSaveOptions(true, null));
            return contact;
        }
    }
}