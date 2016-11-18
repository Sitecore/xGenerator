using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ExperienceGenerator.Exm.Models;
using Newtonsoft.Json;
using Sitecore.Analytics.Model;
using Sitecore.Data;
using Sitecore.EDS.Core.Reporting;
using Sitecore.EmailCampaign.Cm.Handlers;
using Sitecore.ExM.Framework.Diagnostics;
using Sitecore.ExM.Framework.Distributed.Tasks.TaskPools.ShortRunning;
using Sitecore.Modules.EmailCampaign;
using Sitecore.Modules.EmailCampaign.Core.Crypto;
using Sitecore.Modules.EmailCampaign.Core.Extensions;
using Sitecore.Modules.EmailCampaign.Core.Links;
using Sitecore.Modules.EmailCampaign.Messages;
using Factory = Sitecore.Configuration.Factory;

namespace ExperienceGenerator.Exm.Services
{
    public static class GenerateEventService
    {
        public static SemaphoreSlim Pool;
        private static readonly IStringCipher Cipher;

        static GenerateEventService()
        {
            Cipher = Factory.CreateObject("exmAuthenticatedCipher", true) as IStringCipher;
        }

        public static int Errors { get; set; }

        public static int Timeouts { get; set; }

        private static void RequestUrl(string url, RequestHeaderInfo requestHeaderInfo = null)
        {
            Pool.Wait();

            try
            {
                // Don't use IDisposable HttpClient, seems to cause problems with threads
                var client = new HttpClient();

                if (requestHeaderInfo != null)
                {
                    if (requestHeaderInfo.UserAgent != null)
                    {
                        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", requestHeaderInfo.UserAgent);
                    }

                    var json = JsonConvert.SerializeObject(requestHeaderInfo);
                    client.DefaultRequestHeaders.TryAddWithoutValidation("X-Exm-FakeData", json);
                }

                client.Timeout = TimeSpan.FromSeconds(120);

                var res = client.PostAsync(url, new StringContent(string.Empty)).Result;
                if (!res.IsSuccessStatusCode)
                {
                    Errors++;
                }
            }
            catch (TaskCanceledException)
            {
                Timeouts++;
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("GenerateEventService error", ex);
                Errors++;
            }
            finally
            {
                Pool.Release();
            }
        }

        public static void GenerateSent(string hostName, ID contactId, ID messageId, DateTime dateTime)
        {
            var url = $"{hostName}/api/xgen/exmevents/GenerateSent?contactId={contactId}&messageId={messageId}&date={dateTime.ToString("u")}";
            RequestUrl(url);
        }

        public static void GenerateBounce(string hostName, ID contactId, ID messageId, DateTime dateTime)
        {
            var url = $"{hostName}/api/xgen/exmevents/GenerateBounce?contactId={contactId}&messageId={messageId}&date={dateTime.ToString("u")}";
            RequestUrl(url);
        }

        public static async Task GenerateSpamComplaint(string hostName, ID contactId, ID messageId, string email, DateTime dateTime)
        {
            var messageHandler = new SpamComplaintHandler(Factory.CreateObject("exm/spamComplaintsTaskPool", true) as ShortRunningTaskPool, Factory.CreateObject("exm/recipientListManagementTaskPool", true) as ShortRunningTaskPool);

            var spam = new Complaint
                       {
                           ContactId = Cipher.Encrypt(contactId.ToString()),
                           EmailAddress = email,
                           MessageId = Cipher.Encrypt(messageId.ToString())
                       };

            await messageHandler.HandleReportedMessages(new[] {spam});
        }

        public static void GenerateHandlerEvent(string hostName, Guid userId, MessageItem messageItem, EventType eventType, DateTime dateTime, string userAgent = null, WhoIsInformation geoData = null, string link = null)
        {
            string eventHandler;
            switch (eventType)
            {
                case EventType.Open:
                    eventHandler = "RegisterEmailOpened.ashx";
                    break;
                case EventType.Unsubscribe:
                    eventHandler = "RedirectUrlPage.aspx";
                    link = "/sitecore/Unsubscribe.aspx";
                    break;
                case EventType.UnsubscribeFromAll:
                    eventHandler = "RedirectUrlPage.aspx";
                    link = "/sitecore/UnsubscribeFromAll.aspx";
                    break;
                case EventType.Click:
                    eventHandler = "RedirectUrlPage.aspx";
                    break;
                default:
                    throw new InvalidEnumArgumentException("No such event in ExmEvents");
            }

            var queryStrings = GetQueryParameters(userId, messageItem, link);
            var encryptedQueryString = QueryStringEncryption.GetDefaultInstance().Encrypt(queryStrings);

            var parameters = encryptedQueryString.ToQueryString(true);

            var url = $"{hostName}/sitecore/{eventHandler}{parameters}";
            var fakeData = new RequestHeaderInfo
                           {
                               UserAgent = userAgent,
                               RequestTime = dateTime,
                               GeoData = geoData
                           };
            RequestUrl(url, fakeData);
        }


        private static NameValueCollection GetQueryParameters(Guid userId, MessageItem messageItem, string link = null)
        {
            var queryStrings = new NameValueCollection
                               {
                                   [GlobalSettings.AnalyticsContactIdQueryKey] = new ShortID(userId).ToString(),
                                   [GlobalSettings.MessageIdQueryKey] = messageItem.InnerItem.ID.ToShortID().ToString()
                               };

            if (link != null)
            {
                queryStrings[LinksManager.UrlQueryKey] = link;
            }
            return queryStrings;
        }
    }
}