using System;
using System.Threading;
using ExperienceGenerator.Exm.Models;
using Sitecore;
using Sitecore.Analytics.Data.Items;
using Sitecore.Data.Items;
using Sitecore.EmailCampaign.Model.Message;
using Sitecore.EmailCampaign.Model.Web.Settings;
using Sitecore.Modules.EmailCampaign;
using Sitecore.Modules.EmailCampaign.Core;
using Sitecore.Modules.EmailCampaign.Core.Data;
using Sitecore.Modules.EmailCampaign.Core.Gateways;
using Sitecore.Modules.EmailCampaign.Messages;

namespace ExperienceGenerator.Exm.Services
{
    public class AdjustEmailStatisticsService
    {
        const int RetryCount = 30;
        private IRecipientManager _recipientManager;
        private EcmDataProvider _ecmDataProvider;

        public void AdjustEmailStatistics(EcmDataProvider ecmDataProvider, IRecipientManager recipientManager, Job job, MessageItem messageItem,
            CampaignSettings campaign)
        {
            _recipientManager = recipientManager;
            _ecmDataProvider = ecmDataProvider;
            SetStatisticsOnMessageItem(messageItem, campaign.StartDate, campaign.EndDate);

            SetStatisticsOnCampaignItem(messageItem, campaign.StartDate, campaign.EndDate);

            SetStatisticsInExmDatabase(job, messageItem, campaign.StartDate, campaign.EndDate);
        }

        private void SetStatisticsInExmDatabase(Job job, MessageItem messageItem, DateTime dateMessageSent, DateTime dateMessageFinished)
        {
            var sleepTime = 1000;
            for (var i = 0; i < RetryCount; i++)
            {
                try
                {
                    var messageRecipients = _recipientManager.GetMessageRecipients(200);
                    var totalCount = checked((int)messageRecipients.TotalCount);
                    var num = messageItem.MessageType == MessageType.Regular ? 1 : 0;
                    var recipientsCount = num != 0 ? FieldUpdate.Set(totalCount) : null;
                    var includedRecipients = num != 0 ? FieldUpdate.Set(_recipientManager.GetTargetRecipientCountFromIncludeLists()) : null;
                    var excludedRecipients = num != 0 ? FieldUpdate.Set(_recipientManager.GetTargetRecipientCountFromExcludeLists()) : null;
                    var globallyExcluded = num != 0 ? FieldUpdate.Set(_recipientManager.GetTargetRecipientCountFromGlobalOptOutList()) : null;
                    
                    _ecmDataProvider.SetMessageStatisticData(
                        messageItem.CampaignId.ToGuid(), 
                        dateMessageSent, 
                        dateMessageFinished,
                        recipientsCount, 
                        includedRecipients, 
                        excludedRecipients, 
                        globallyExcluded,null,null
                        );
                    return;
                }
                catch (Exception)
                {
                    job.Status = $"Setting Statistics in the EXM database (retry {i} of {RetryCount})";
                    Thread.Sleep(sleepTime);
                    sleepTime += 1000;
                }
            }
        }

        private void SetStatisticsOnCampaignItem(MessageItem messageItem, DateTime dateMessageSent, DateTime dateMessageFinished)
        {
            var itemUtil = new ItemUtilExt();
            var campaignItem = itemUtil.GetItem(messageItem.CampaignId);
            using (new EditContext(campaignItem))
            {
                campaignItem["StartDate"] = DateUtil.ToIsoDate(dateMessageSent);
                campaignItem[CampaignclassificationItem.FieldIDs.Channel] = GlobalSettings.Instance.CampaignClassificationChannel;
                campaignItem["EndDate"] = DateUtil.ToIsoDate(dateMessageFinished);
            }
        }

        private void SetStatisticsOnMessageItem(MessageItem messageItem, DateTime dateMessageSent, DateTime dateMessageFinished)
        {
            messageItem.Source.StartTime = dateMessageSent;
            messageItem.Source.EndTime = dateMessageFinished;

            var innerItem = messageItem.InnerItem;
            using (new EditContext(innerItem))
            {
                innerItem.RuntimeSettings.ReadOnlyStatistics = true;
                innerItem[FieldIDs.Updated] = DateUtil.ToIsoDate(dateMessageSent);
            }
        }
    }
}
