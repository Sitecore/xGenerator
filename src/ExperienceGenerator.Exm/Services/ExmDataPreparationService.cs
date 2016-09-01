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
using Sitecore.EmailCampaign.Server.Contexts;
using Sitecore.EmailCampaign.Server.Controllers.Dispatch;
using Sitecore.EmailCampaign.Server.Controllers.Message;
using Sitecore.EmailCampaign.Server.Responses;
using Sitecore.Modules.EmailCampaign;
using Sitecore.Modules.EmailCampaign.Core;
using Sitecore.Modules.EmailCampaign.Core.Gateways;
using Sitecore.Modules.EmailCampaign.Factories;
using Sitecore.Modules.EmailCampaign.Messages;
using Sitecore.Publishing;
using Sitecore.SecurityModel;
using Sitecore.Modules.EmailCampaign.Core.Dispatch;
using Sitecore.Modules.EmailCampaign.Core.Pipelines.DispatchNewsletter;
using ContactData = Sitecore.ListManagement.ContentSearch.Model.ContactData;
using Context = Sitecore.Context;
using Factory = Sitecore.Configuration.Factory;

namespace ExperienceGenerator.Exm.Services
{
  public class ExmDataPreparationService
  {
    private const string ListManagerOwner = "xGenerator";

    private readonly CampaignModel _campaignDefinition;
    private readonly Guid _exmCampaignId;
    private readonly Random _random = new Random();
    private readonly Database _db = Factory.GetDatabase("master");
    private readonly List<Guid> _unsubscribeFromAllContacts = new List<Guid>();
    private readonly Func<string> _userAgent;
    private readonly ExmContactService _contactService;
    private readonly ExmLandingPageService _landingPageService;
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
    private readonly Func<int> _eventDay;
    private readonly ExmJobDefinitionModel _specification;

    public ExmDataPreparationService(ExmJobDefinitionModel specification, CampaignModel campaignDefinition,
      Guid exmCampaignId)
    {
      _specification = specification;
      _campaignDefinition = campaignDefinition;
      _exmCampaignId = exmCampaignId;
      _contactService = new ExmContactService(specification);
      _landingPageService = new ExmLandingPageService(campaignDefinition.LandingPages);
      _listService = new ExmListService(specification);

      var devices = new DeviceRepository().GetAll().ToDictionary(ga => ga.Id, ga => ga.UserAgent);
      _userAgent = campaignDefinition.Devices.ToDictionary(kvp => devices[kvp.Key], kvp => kvp.Value).Weighted();

      _eventDay = campaignDefinition.DayDistribution.Select(x => x / 100d).WeightedInts();
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
          ExmEventsGenerator.Pool = new SemaphoreSlim(_specification.Threads);

          _specification.Job.Status = "Cleanup...";
          Cleanup();

          _specification.Job.Status = "Creating goals...";
          //TODO-To Implement
          //this._goalService.CreateGoals();

          _specification.Job.Status = "Smart Publish...";
          PublishSmart();

          _specification.Job.Status = "Get Email Message...";
          var messageItem = GetEmailMessage(_exmCampaignId);

          _specification.Job.Status = "Get Contacts For Email...";
          var contactsForThisEmail = GetContactsForEmail(messageItem.RecipientManager.IncludedRecipientListIds);
          _contactService.AddContacts(contactsForThisEmail);

          CreateContactsAndList(messageItem, contactsForThisEmail);

          _specification.Job.Status = "Identify manager root...";
          GetManagerRoot();

          _specification.Job.Status = "Back-dating segments...";
          BackDateSegments();

          _specification.Job.Status = "Creating emails...";
          SendCampaign(messageItem, contactsForThisEmail);
        }

        _specification.Job.Status = "DONE!";
        _specification.Job.CampaignCountLabel = "";
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

    private void CreateContactsAndList(MessageItem messageItem, List<ContactData> contactsForThisEmail)
    {
      var contactsRequired = _campaignDefinition.Events.TotalSent - contactsForThisEmail.Count;
      _specification.Job.TargetContacts = Math.Max(contactsRequired, 0);
      _specification.Job.TargetEvents = Math.Max(_campaignDefinition.Events.TotalSent, contactsForThisEmail.Count);
      _specification.Job.TargetEmails = _specification.Job.TargetEvents;
      if (contactsRequired > 0)
      {
        _specification.Job.Status = "Creating contacts";
        var addedContacts = _contactService.CreateContacts(contactsRequired);
        _specification.Job.Status = "Creating lists...";
        var addedList = _listService.CreateList("Auto List " + DateTime.Now.Ticks, addedContacts);
        _listService.WaitUntilListsUnlocked();
        messageItem.RecipientManager.AddIncludedRecipientListId(ID.Parse(addedList.Id));
        contactsForThisEmail.AddRange(_listService.GetContacts(addedList));
      }
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
      var targetDatabases = new[] { Factory.GetDatabase("web") };
      var languages = _db.Languages;
      PublishManager.PublishSmart(_db, targetDatabases, languages);
    }

    private void Cleanup()
    {
      var emailMessage = _db.SelectItems("/sitecore/content/Email Campaign/Messages/" + DateTime.Now.Year + "//*")?.FirstOrDefault(x => x.ID.Guid == _exmCampaignId);
      if (emailMessage == null) return;

      var excludedRecipientsListIDs = emailMessage.Fields["Excluded Recipient Lists"].Value.Split('|').ToList();

      //var engagementPlan = _db.SelectItems("/sitecore/system/Marketing Control Panel/Engagement Plans/Email Campaign/Emails//*").FirstOrDefault(x => x.ID.ToString() == emailMessage.Fields["Engagement Plan"].Value);
      //engagementPlan?.Delete();

      //var campaign = _db.SelectItems("/sitecore/system/Marketing Control Panel/Campaigns/Emails//*").FirstOrDefault(x => x.ID.ToString() == emailMessage.Fields["Campaign"].Value);
      //campaign?.Delete();

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

      var lists = _db.SelectItems("/sitecore/system/List Manager/All Lists//*[@@templatename='Contact List']").Where(x => x.Fields["Owner"].Value == ListManagerOwner);
      foreach (var list in lists)
      {
        list.Delete();
      }

      var excludeLists = _db.SelectItems("/sitecore/system/List Manager/All Lists//*[@@templatename='Contact List']").Where(x => excludedRecipientsListIDs.Contains(x.ID.ToString())).ToList();
      foreach (var excludeList in excludeLists)
      {
        using (new EditContext(excludeList))
        {
          excludeList.Fields["IncludedSources"].Value = string.Empty;
          excludeList.Fields["ExcludedSources"].Value = string.Empty;
        }
      }

      var globalLists = _db.SelectItems("/sitecore/system/List Manager/All Lists/#E-mail Campaign Manager#/System//*[@@templatename='Contact List']");
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

      var contactIndex = 1;
      foreach (var contactId in contactsForThisEmail)
      {
        _specification.Job.Status = $"Generating events for contact {contactIndex++} of {contactsForThisEmail.Count}";

        var contact = _contactService.GetContact(contactId.ContactId);
        if (contact == null)
        {
          continue;
        }

        if (_random.NextDouble() < funnelDefinition.Bounced / 100d)
        {
          ExmEventsGenerator.GenerateBounce(_managerRoot.Settings.BaseURL, contact.ContactId.ToID(),
            email.MessageId.ToID(), email.StartTime.AddMinutes(1));
        }
        else
        {
          var userAgent = _userAgent();
          var ip = _ip();
          var eventDay = _eventDay();
          var seconds = _random.Next(60, 86400);
          var eventDate = email.StartTime.AddDays(eventDay).AddSeconds(seconds);


          var spamPercentage = funnelDefinition.SpamComplaints / 100d;

          if (_random.NextDouble() < funnelDefinition.OpenRate / 100d)
          {
            ExmEventsGenerator.GenerateHandlerEvent(_managerRoot.Settings.BaseURL, contact.ContactId, email,
              ExmEvents.Open, eventDate, userAgent, ip);

            eventDate = eventDate.AddSeconds(_random.Next(10, 300));

            if (_random.NextDouble() < funnelDefinition.ClickRate / 100d)
            {
              // Much less likely to complain if they were interested enough to click the link.
              spamPercentage = 0.01;

              var link = _landingPageService.GetLandingPage();

              ExmEventsGenerator.GenerateHandlerEvent(_managerRoot.Settings.BaseURL, contact.ContactId, email,
                ExmEvents.Click, eventDate, userAgent, ip, link);
              eventDate = eventDate.AddSeconds(_random.Next(10, 300));
            }
          }

          if (_random.NextDouble() < spamPercentage)
          {
            await ExmEventsGenerator.GenerateSpamComplaint(_managerRoot.Settings.BaseURL, contact.ContactId.ToID(), email.MessageId.ToID(), "email", eventDate);
            eventDate = eventDate.AddSeconds(_random.Next(10, 300));
          }

          var unsubscribePercentage = funnelDefinition.Unsubscribed / 100d;
          if (_random.NextDouble() < unsubscribePercentage)
          {
            //TODO - Warning: UnsubscribeFromAll not supported
            var unsubscribeFromAllPercentage = 0.5;
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
        _specification.Job.CompletedEvents++;
      }
    }

    private void GetManagerRoot()
    {
      var query = $"fast:/sitecore/content//*[@@templateid='{TemplateIds.ManagerRoot}']";
      var rootItem = _db.SelectSingleItem(query);

      if (rootItem == null)
      {
        throw new Exception("ManagerRoot not found");
      }

      _managerRoot = ManagerRoot.FromItem(rootItem);
    }

    private void SendCampaign(MessageItem messageItem, List<ContactData> contactsForThisEmail)
    {
      var sendingProcessData = new SendingProcessData(new ID(messageItem.MessageId));

      var dateMessageSent = _campaignDefinition.StartDate;
      var dateMessageFinished = _campaignDefinition.EndDate;

      //_specification.Job.Status = "Dispatching email...";
      //var dispatchController = new DispatchController();
      //dispatchController.Dispatch(new DispatchRequestContext
      //{
      //  AbTest = new ABTestContext(),
      //  Schedule = new ScheduleContext(),
      //  RecurringSchedule = new RecurringScheduleContext(),
      //  EmulationMode = false,
      //  Language = "en",
      //  MessageId = messageItem.ID,
      //  MultiLanguageEnabled = false,
      //  NotificationEmail = null,
      //  UseNotificationEmail = false,
      //  UsePreferredLanguage = false
      //});

      //WaitUntilSent(messageItem.ID);

      _specification.Job.Status = "Adjusting email stats...";

      //AdjustEmailStats(messageItem, dateMessageSent, dateMessageFinished, contactsForThisEmail.Count);
      AdjustEmailStatsWithRetry(messageItem, sendingProcessData, dateMessageSent, dateMessageFinished, 30);

      PublishEmail(messageItem, sendingProcessData);

      var contactIndex = 1;
      foreach (var contact in contactsForThisEmail)
      {
        _specification.Job.Status = $"Sending email to contact {contactIndex++} of {contactsForThisEmail.Count}";
        try
        {
          SendEmailToContact(contact, messageItem);
        }
        catch (Exception ex)
        {
          _specification.Job.Status = ex.ToString();
          Log.Error("Failed", ex, this);
        }
      }

      //messageItem.Source.State = MessageState.Sent;
      GenerateEvents(messageItem, _campaignDefinition.Events, contactsForThisEmail);
    }



    private void AdjustEmailStatsWithRetry(MessageItem messageItem, SendingProcessData sendingProcessData,
      DateTime dateMessageSent, DateTime dateMessageFinished, int retryCount)
    {
      int sleepTime = 1000;

      for (var i = 0; i < retryCount; i++)
      {
        try
        {
          AdjustEmailStats(messageItem, sendingProcessData, dateMessageSent, dateMessageFinished);
          return;
        }
        catch (Exception)
        {
          Thread.Sleep(sleepTime);
          sleepTime += 1000;
        }
      }
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

      ExmEventsGenerator.GenerateSent(_managerRoot.Settings.BaseURL, new ID(contact.ContactId), messageItem.InnerItem.ID,
        messageItem.StartTime);
      _specification.Job.CompletedEmails++;
    }

    private MessageItem GetEmailMessage(Guid itemId)
    {
      return Sitecore.Modules.EmailCampaign.Factory.Instance.GetMessageItem(itemId.ToID());
    }

    private List<ContactData> GetContactsForEmail(IEnumerable<ID> lists)
    {
      var contactsForThisEmail = new List<ContactData>();

      foreach (var listId in lists)
      {
        var list = _listService.GetList(listId);
        if (list != null)
        {
          var contacts = _listService.ListManager.GetContacts(list);
          contactsForThisEmail.AddRange(contacts);
        }
      }

      contactsForThisEmail = contactsForThisEmail
        .DistinctBy(x => x.ContactId)
        .Where(x => !_unsubscribeFromAllContacts.Contains(x.ContactId))
        .ToList();

      return contactsForThisEmail;
    }

    private void WaitUntilSent(string messageId)
    {
      _specification.Job.Status = "Waiting until email sent...";

      while (true)
      {
        var messageController = new MessageController();
        var messageResponse = (MessageResponse)messageController.Message(new MessageContext
        {
          Language = "en",
          MessageId = messageId
        });

        var messageState = (MessageState)messageResponse.Message.MessageState;
        if (messageState == MessageState.Sent)
        {
          return;
        }

        Thread.Sleep(1000);
      }
    }

    //private void AdjustEmailStats(MessageItem messageItem, DateTime dateMessageSent, DateTime dateMessageFinished, int numContactsForThisEmail)
    //{
    //  messageItem.Source.StartTime = dateMessageSent;
    //  messageItem.Source.EndTime = dateMessageFinished;

    //  var innerItem = messageItem.InnerItem;
    //  using (new EditContext(innerItem))
    //  {
    //    innerItem.RuntimeSettings.ReadOnlyStatistics = true;
    //    innerItem[FieldIDs.Updated] = DateUtil.ToIsoDate(dateMessageSent);
    //  }

    //  // Updates the totalRecipients and endTime in the EmailCampaign collection.
    //  EcmFactory.GetDefaultFactory()
    //      .Gateways.EcmDataGateway.SetMessageStatisticData(messageItem.CampaignId.ToGuid(), dateMessageSent,
    //          dateMessageFinished, FieldUpdate.Set(numContactsForThisEmail));
    //}

    private void AdjustEmailStats(MessageItem messageItem, SendingProcessData sendingProcessData,
      DateTime dateMessageSent, DateTime dateMessageFinished)
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
  }
}