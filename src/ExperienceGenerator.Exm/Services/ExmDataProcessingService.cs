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
  using ExperienceGenerator.Exm.Controllers;
  using ExperienceGenerator.Exm.Models;
  using Sitecore;
  using Sitecore.Analytics.Data.Items;
  using Sitecore.Caching;
  using Sitecore.Common;
  using Sitecore.Data;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;
  using Sitecore.EmailCampaign.Analytics.Model;
  using Sitecore.EmailCampaign.ExperienceAnalytics;
  using Sitecore.Modules.EmailCampaign;
  using Sitecore.Modules.EmailCampaign.Core;
  using Sitecore.Modules.EmailCampaign.Core.Dispatch;
  using Sitecore.Modules.EmailCampaign.Core.Gateways;
  using Sitecore.Modules.EmailCampaign.Core.Pipelines.DispatchNewsletter;
  using Sitecore.Modules.EmailCampaign.Factories;
  using Sitecore.Modules.EmailCampaign.Messages;
  using Sitecore.Publishing;
  using Sitecore.SecurityModel;

  public class ExmDataProcessingService
  {

    public event Action<Guid, JobStatus, string> StatusChanged;
    private readonly CampaignModel campaignDefinition;
    private readonly Guid exmCampaignId;
    private readonly Random _random = new Random();
    private readonly Database _db = Sitecore.Configuration.Factory.GetDatabase("master");
    private readonly Dictionary<string, List<Guid>> _contactsPerEmail = new Dictionary<string, List<Guid>>();
    private readonly List<Guid> _unsubscribeFromAllContacts = new List<Guid>();
    private readonly Func<string> _userAgent;
    private readonly ExmContactService _contactService;
    private readonly ExmLandingPageService landingPageService;
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
    private Func<int> _eventDay;

    public ExmDataProcessingService(CampaignModel campaignDefinition, Guid exmCampaignId)
    {
      this.campaignDefinition = campaignDefinition;
      this.exmCampaignId = exmCampaignId;
      this._contactService = new ExmContactService();
      this.landingPageService = new ExmLandingPageService(campaignDefinition.LandingPages);
      this._listService = new ExmListService();

      var devices = new DeviceRepository().GetAll().ToDictionary(ga => ga.Id, ga => ga.UserAgent);
      _userAgent = campaignDefinition.Devices.ToDictionary(kvp => devices[kvp.Key], kvp => kvp.Value).Weighted();
  
      this._eventDay = campaignDefinition.DayDistribution.Select(x => x / 100d).WeightedInts();
    }

    public void CreateData()
    {
      NotifyJobStatus(JobStatus.Running);

      //Context.SetActiveSite("website");

      try
      {
        using (new SecurityDisabler())
        {

          NotifyJobStatus(JobStatus.Running, "Identify manager root...");
          this.GetManagerRoot();

          NotifyJobStatus(JobStatus.Running, "Cleanup...");
          //this.Cleanup();


          //NotifyJobStatus(JobStatus.Running, "Creating goals...");
          //this._goalService.CreateGoals();

          //NotifyJobStatus(JobStatus.Running, "Smart Publish...");
          //this.PublishSmart();

          //NotifyJobStatus(JobStatus.Running, "Creating contacts");
          //this._contactService.CreateContacts(this.campaignDefinition.NumContacts);

          //NotifyJobStatus(JobStatus.Running, "Creating lists...");
          //this._listService.CreateLists();

        

          NotifyJobStatus(JobStatus.Running, "Back-dating segments...");
          this.BackDateSegments();

          NotifyJobStatus(JobStatus.Running, "Creating emails...");
          this.SendCampaign();
        }

        NotifyJobStatus(JobStatus.Complete, "DONE!");
      }
      catch (Exception ex)
      {

        NotifyJobStatus(JobStatus.Failed, ex.ToString());
        Log.Error("Failed", ex, this);
      }

    }


    private void NotifyJobStatus(JobStatus status, string statusMessage = "")
    {
      this.StatusChanged?.Invoke(this.exmCampaignId, status, statusMessage);
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
      var targetDatabases = new[]
      {
        Sitecore.Configuration.Factory.GetDatabase("web")
      };
      var languages = this._db.Languages;
      PublishManager.PublishSmart(this._db, targetDatabases, languages);
    }

    private void Cleanup()
    {

      var key = new KeyBuilder().Add(this._managerRoot.InnerItem.ID.ToGuid()).Add(this.exmCampaignId).ToString();
      var removeCampaingRecords = $@"
SELECT [DimensionKeyId] INTO #Dimensions FROM [dbo].[DimensionKeys] WHERE [DimensionKey] LIKE '{key}%';
Select SegmentRecordId INTO #SegmentRecords from [dbo].[SegmentRecords]
  where DimensionKeyId in (select * from #Dimensions);
Select SegmentRecordId INTO #SegmentRecordsReduced from [dbo].[SegmentRecordsReduced]
  where DimensionKeyId in (select * from #Dimensions);

DELETE FROM [dbo].[Fact_SegmentMetrics] WHERE SegmentRecordId in (select * from #SegmentRecords)
DELETE FROM [dbo].[Fact_SegmentMetricsReduced] WHERE SegmentRecordId in (select * from #SegmentRecordsReduced)
DELETE FROM [dbo].[SegmentRecords] WHERE [DimensionKeyId] IN (SELECT * FROM #Dimensions);
DELETE FROM [dbo].[SegmentRecordsReduced] WHERE [DimensionKeyId] IN (SELECT * FROM #Dimensions);

Drop table #Dimensions
Drop table #SegmentRecords
Drop table #SegmentRecordsReduced
";

      var connectionstring = ConfigurationManager.ConnectionStrings["reporting"];
      var connection = new SqlConnection(connectionstring.ConnectionString);

      try
      {
        connection.Open();

        var clearCommand = new SqlCommand(removeCampaingRecords, connection);
        clearCommand.ExecuteNonQuery();
      }
      finally
      {
        connection.Close();
      }


    }

    private void GenerateEvents(MessageItem email, Funnel funnelDefinition)
    {
      if (funnelDefinition == null)
      {
        return;
      }

      var contactIndex = 1;
      var contactsThisEmail = this._contactsPerEmail[email.ID];
      var numContactsForThisEmail = contactsThisEmail.Count;
      foreach (var contactId in contactsThisEmail)
      {
        NotifyJobStatus(JobStatus.Running, string.Format(
          "Generating events for contact {0} of {1}",
          contactIndex++,
          numContactsForThisEmail));


        var contact = this._contactService.GetContact(contactId);
        if (contact == null)
        {
          continue;
        }

        if (this._random.NextDouble() < funnelDefinition.Bounced / 100d)
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


          double spamPercentage = funnelDefinition.SpamComplaints / 100d;

          if (this._random.NextDouble() < funnelDefinition.OpenRate / 100d)
          {
            ExmEventsGenerator.GenerateHandlerEvent(this._managerRoot.Settings.BaseURL, contact.ContactId, email, ExmEvents.Open, eventDate, userAgent, ip);

            eventDate = eventDate.AddSeconds(this._random.Next(10, 300));

            if (this._random.NextDouble() < funnelDefinition.ClickRate / 100d)
            {
              // Much less likely to complain if they were interested enough to click the link.
              spamPercentage = 0.01;

              var link = this.landingPageService.GetLandingPage();

              ExmEventsGenerator.GenerateHandlerEvent(this._managerRoot.Settings.BaseURL, contact.ContactId, email, ExmEvents.Click, eventDate, userAgent, ip, link);
              eventDate = eventDate.AddSeconds(this._random.Next(10, 300));
            }
          }

          if (this._random.NextDouble() < spamPercentage)
          {
            ExmEventsGenerator.GenerateSpamComplaint(this._managerRoot.Settings.BaseURL, contact.ContactId.ToID(), email.MessageId.ToID(), "email", eventDate);
            eventDate = eventDate.AddSeconds(this._random.Next(10, 300));
          }

          var unsubscribePercentage = funnelDefinition.Unsubscribed / 100d;
          if (this._random.NextDouble() < unsubscribePercentage)
          {
#warning: UnsubscribeFromAll not supported
            var unsubscribeFromAllPercentage = 0.5;
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


    private void SendCampaign()
    {
      var messageItem = this.GetEmailMessage(this.exmCampaignId);

      var contactsForThisEmail = this.GetContactsForEmail(messageItem.RecipientManager.IncludedRecipientListIds, messageItem);
      var sendingProcessData = new SendingProcessData(new ID(messageItem.MessageId));

      DateTime dateMessageSent = this.campaignDefinition.StartDate;
      var dateMessageFinished = dateMessageSent.AddMinutes(5);

      this.AdjustEmailStatsWithRetry(messageItem, sendingProcessData, dateMessageSent, dateMessageFinished, 30);

      this.PublishEmail(messageItem, sendingProcessData);

      this._contactsPerEmail[messageItem.ID] = new List<Guid>();

      var numContactsForThisEmail = contactsForThisEmail.Count;
      var contactsRequired = this.campaignDefinition.Events.TotalSent - numContactsForThisEmail;
      if (contactsRequired>0)
      {
        var addlContacts = this._contactService.CreateContacts(contactsRequired);
        var xaList = this._listService.CreateList("Auto List " + DateTime.Now.Ticks, addlContacts );
        messageItem.RecipientManager.AddIncludedRecipientListId(ID.Parse(xaList.Id));

        contactsForThisEmail.AddRange(this._listService.GetContacts(xaList));
      }

      var contactIndex = 1;
      foreach (var contact in contactsForThisEmail)
      {
        NotifyJobStatus(JobStatus.Running, string.Format("Sending email to contact {0} of {1}", contactIndex++, numContactsForThisEmail));

        try
        {
          this.SendEmailToContact(contact, messageItem);
          this._contactsPerEmail[messageItem.ID].Add(contact.ContactId);
        }
        catch (Exception ex)
        {
          NotifyJobStatus(JobStatus.Failed, ex.ToString());
          Log.Error("Failed", ex, this);
        }
      }

      messageItem.Source.State = MessageState.Sent;
      this.GenerateEvents(messageItem, this.campaignDefinition.Events);
      NotifyJobStatus(JobStatus.Complete);
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

    private List<ContactData> GetContactsForEmail(IEnumerable<ID> lists, MessageItem messageItem)
    {
      var contactsForThisEmail = new List<ContactData>();

      foreach (var listId in lists)
      {
        var list = this._listService.GetList(listId);
        if (list != null)
        {
          //messageItem.RecipientManager.AddIncludedRecipientListId(ID.Parse(list.Id));

          var contacts = this._listService.ListManager.GetContacts(list);
          contactsForThisEmail.AddRange(contacts);
        }
      }

      contactsForThisEmail = contactsForThisEmail
        .DistinctBy(x => x.ContactId)
        .Where(x => !this._unsubscribeFromAllContacts.Contains(x.ContactId))
        .ToList();

      return contactsForThisEmail;
    }

    private MessageItem GetEmailMessage(Guid itemId)
    {
      return Factory.Instance.GetMessageItem(itemId.ToID());
    }
  }
}