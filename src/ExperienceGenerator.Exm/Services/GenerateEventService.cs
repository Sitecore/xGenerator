using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ExperienceGenerator.Exm.Models;
using ExperienceGenerator.Exm.Repositories;
using Newtonsoft.Json;
using Sitecore.Analytics.Model;
using Sitecore.Common;
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
        private static readonly IStringCipher Cipher;

        static GenerateEventService()
        {
            Cipher = Factory.CreateObject("exmAuthenticatedCipher", true) as IStringCipher;
        }

        public static int Errors { get; set; }

        public static int Timeouts { get; set; }

        private static ResponseData RequestUrl(string url, RequestHeaderInfo requestHeaderInfo = null, CookieContainer cookieContainer = null)
        {
            var responseData = new ResponseData();
            try
            {
                // Don't use IDisposable HttpClient, seems to cause problems with threads
                if (cookieContainer == null)
                {
                    cookieContainer = new CookieContainer();
                }
                HttpMessageHandler handler = new HttpClientHandler()
                                             {
                                                 CookieContainer = cookieContainer
                                             };
                var client = new HttpClient(handler);
                responseData.Cookies = cookieContainer;

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
                    Sitecore.Diagnostics.Log.Warn($"Exm Generator request to \'{url}\' failed with '{res.StatusCode}'", res);
                    return responseData;
                }
            }
            catch (TaskCanceledException)
            {
                Timeouts++;
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("GenerateEventService error", ex);
                return responseData;
            }
            responseData.IsSuccessful = true;
            return responseData;
        }

        public static void GenerateSent(string hostName, Guid contactId, MessageItem message, DateTime dateTime)
        {
            var url = $"{hostName}/api/xgen/exmevents/GenerateSent?contactId={contactId}&messageId={message.MessageId}&date={dateTime.ToString("u")}";
            if (!RequestUrl(url).IsSuccessful)
                Errors++;
        }

        private static void GenerateBounce(string hostName, Guid contactId, MessageItem message, DateTime dateTime)
        {
            var url = $"{hostName}/api/xgen/exmevents/GenerateBounce?contactId={contactId}&messageId={message.MessageId}&date={dateTime.ToString("u")}";
            if (!RequestUrl(url).IsSuccessful)
                Errors++;
        }

        private static async void GenerateSpamComplaint(string hostName, Guid contactId, MessageItem message, DateTime dateTime)
        {
            var messageHandler = new SpamComplaintHandler(Factory.CreateObject("exm/spamComplaintsTaskPool", true) as ShortRunningTaskPool, Factory.CreateObject("exm/recipientListManagementTaskPool", true) as ShortRunningTaskPool);

            var spam = new Complaint
                       {
                           ContactId = Cipher.Encrypt(contactId.ToString()),
                           EmailAddress = message.To,
                           MessageId = Cipher.Encrypt(message.MessageId.ToString())
                       };

            await messageHandler.HandleReportedMessages(new[] {spam});
        }

        public static void GenerateHandlerEvent(string hostName, Guid contactId, MessageItem messageItem, EventType eventType, DateTime dateTime, string userAgent, WhoIsInformation geoData, string link)
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
                case EventType.Bounce:
                    GenerateBounce(hostName, contactId, messageItem, dateTime);
                    return;
                case EventType.SpamComplaint:
                    GenerateSpamComplaint(hostName, contactId, messageItem, dateTime);
                    return;
                default:
                    throw new InvalidEnumArgumentException("No such event in ExmEvents");
            }

            var queryStrings = GetQueryParameters(contactId, messageItem, link);
            var encryptedQueryString = QueryStringEncryption.GetDefaultInstance().Encrypt(queryStrings);

            var parameters = encryptedQueryString.ToQueryString(true);

            var url = $"{hostName}/sitecore/{eventHandler}{parameters}";
            var fakeData = new RequestHeaderInfo
                           {
                               UserAgent = userAgent,
                               RequestTime = dateTime,
                               GeoData = geoData
                           };
            var response = RequestUrl(url, fakeData);
            if (!response.IsSuccessful)
                Errors++;

            if (response.IsSuccessful && eventType == EventType.Click)
            {
                EndSession(hostName, response);
            }
        }

        private static void EndSession(string hostName, ResponseData response)
        {
            var url = $"{hostName}/sitecore/EndSession.aspx";
            RequestUrl(url, cookieContainer: response.Cookies);
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

        public static void GenerateContactMessageEvents(Job job, MessageContactEvents events)
        {
            var startStatus = job.Status;
            var messageItem = events.MessageItem;
            var hostName = messageItem.ManagerRoot.Settings.BaseURL;
            var whoIsInformation = events.GeoData;
            var userAgent = events.UserAgent;
            var landingPageUrl = events.LandingPageUrl;
            var contactId = events.ContactId;

            foreach (var messageEvent in events.Events)
            {
                job.Status = startStatus + $" - {messageEvent.EventType}";
                GenerateHandlerEvent(hostName, contactId, messageItem, messageEvent.EventType, messageEvent.EventTime, userAgent, whoIsInformation, landingPageUrl);
            }
        }
    }

    internal class ResponseData
    {
        public bool IsSuccessful { get; set; }
        public CookieContainer Cookies { get; set; }
    }
}
