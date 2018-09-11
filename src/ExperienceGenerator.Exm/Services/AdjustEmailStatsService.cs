using System;
using System.Threading;
using ExperienceGenerator.Exm.Models;
using Sitecore;
using Sitecore.Analytics.Data.Items;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Modules.EmailCampaign.Core;
using Sitecore.Modules.EmailCampaign.Core.Dispatch;
using Sitecore.Modules.EmailCampaign.Core.Gateways;
using Sitecore.Modules.EmailCampaign.Core.Pipelines.DispatchNewsletter;
using Sitecore.Modules.EmailCampaign.Factories;
using Sitecore.Modules.EmailCampaign.Messages;

namespace ExperienceGenerator.Exm.Services
{
    public class AdjustEmailStatisticsService
    {
        const int RETRY_COUNT = 30;

        public void AdjustEmailStatistics(Job job, MessageItem messageItem, CampaignSettings campaign)
        {
            SetStatisticsOnCampaignAndEngagementPlan(messageItem);

            SetStatisticsOnMessageItem(messageItem, campaign.StartDate, campaign.EndDate);

            SetStatisticsOnCampaignItem(messageItem, campaign.StartDate, campaign.EndDate);

            SetStatisticsInEXMDatabase(job, messageItem, campaign.StartDate, campaign.EndDate);
        }

        private static void SetStatisticsInEXMDatabase(Job job, MessageItem messageItem, DateTime dateMessageSent, DateTime dateMessageFinished)
        {
            var sleepTime = 1000;
            for (var i = 0; i < RETRY_COUNT; i++)
            {
                try
                {
                    EcmFactory.GetDefaultFactory().Gateways.EcmDataGateway.SetMessageStatisticData(messageItem.CampaignId.ToGuid(), dateMessageSent, dateMessageFinished, FieldUpdate.Set(messageItem.SubscribersIds.Value.Count), FieldUpdate.Set(messageItem.SubscribersIncludeCount.Value), FieldUpdate.Set(messageItem.SubscribersExcludeCount.Value), FieldUpdate.Set(messageItem.SubscribersGlobalOptOutCount.Value));
                    return;
                }
                catch (Exception)
                {
                    job.Status = $"Setting Statistics in the EXM database (retry {i} of {RETRY_COUNT})";
                    Thread.Sleep(sleepTime);
                    sleepTime += 1000;
                }
            }
        }

        private static void SetStatisticsOnCampaignItem(MessageItem messageItem, DateTime dateMessageSent, DateTime dateMessageFinished)
        {
            var itemUtil = new ItemUtilExt();
            var campaignItem = itemUtil.GetItem(messageItem.CampaignId);
            using (new EditContext(campaignItem))
            {
                campaignItem["StartDate"] = DateUtil.ToIsoDate(dateMessageSent);
                campaignItem[CampaignclassificationItem.FieldIDs.Channel] = EcmFactory.GetDefaultFactory().Io.EcmSettings.CampaignClassificationChannel;
                campaignItem["EndDate"] = DateUtil.ToIsoDate(dateMessageFinished);
            }
        }

        private static void SetStatisticsOnMessageItem(MessageItem messageItem, DateTime dateMessageSent, DateTime dateMessageFinished)
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

        private static void SetStatisticsOnCampaignAndEngagementPlan(MessageItem messageItem)
        {
            new DeployAnalytics().Process(new DispatchNewsletterArgs(messageItem, new SendingProcessData(new ID(messageItem.MessageId))));
        }
    }
}
