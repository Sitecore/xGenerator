using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ExperienceGenerator.Exm.Models;
using Newtonsoft.Json;
using Sitecore.Analytics.Model;
using Sitecore.Data;
using Sitecore.ExM.Framework.Diagnostics;
using Sitecore.EmailCampaign.Model.Web.Settings;
using Sitecore.Modules.EmailCampaign.Core.Crypto;
using Sitecore.Modules.EmailCampaign.Core.Extensions;
using Sitecore.Modules.EmailCampaign.Core.Links;
using Sitecore.Modules.EmailCampaign.Messages;

namespace ExperienceGenerator.Exm.Services
{
    public static class GenerateEventService
    {
        //private static readonly IStringCipher Cipher;

        //static GenerateEventService()
        //{
            //Cipher = Factory.CreateObject("exmAuthenticatedCipher", true) as IStringCipher;
        //}

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
                    client.DefaultRequestHeaders.TryAddWithoutValidation("X-DisableDemo", "true");
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
            var url = $"{hostName}/clientapi/xgen/exmevents/GenerateSent?contactId={contactId}&messageId={message.MessageId}&date={dateTime.ToString("u")}";
            if (!RequestUrl(url).IsSuccessful)
                Errors++;
        }

        private static void GenerateBounce(string hostName, Guid contactId, MessageItem message, DateTime dateTime)
        {
            var eventHandlePath = GetEventHandlePath(EventType.Bounce);
            var url = $"{hostName}{eventHandlePath}?contactId={contactId}&messageId={message.MessageId}&date={dateTime.ToString("u")}";
            if (!RequestUrl(url).IsSuccessful)
                Errors++;
        }

        private static void GenerateSpam(string hostName, Guid contactId, MessageItem message, DateTime dateTime)
        {
            var eventHandlePath = GetEventHandlePath(EventType.SpamComplaint);
            var url = $"{hostName}{eventHandlePath}?contactId={contactId}&messageId={message.MessageId}&date={dateTime.ToString("u")}";
            if (!RequestUrl(url).IsSuccessful)
                Errors++;
        }


        public static string GetEventHandlePath(EventType eventType)
        {
            switch (eventType)
            {
                case EventType.Open:
                    return "/sitecore modules/Web/EXM/RegisterEmailOpened.ashx";
                case EventType.Unsubscribe:
                case EventType.UnsubscribeFromAll:
                case EventType.Click:
                    return "/sitecore modules/Web/EXM/RedirectUrlPage.aspx";
                case EventType.Bounce:
                    return "/clientapi/xgen/exmevents/GenerateBounce";
                case EventType.SpamComplaint:
                    return "/clientapi/xgen/exmevents/GenerateSpam";
                default:
                    throw new ArgumentOutOfRangeException(nameof(eventType), eventType, null);
            }
        }

        public static void GenerateHandlerEvent(string hostName, Guid contactId, MessageItem messageItem, EventType eventType, DateTime dateTime, string userAgent, Sitecore.CES.GeoIp.Core.Model.WhoIsInformation geoData, string link)
        {
            string eventHandlerPath;
            switch (eventType)
            {
                case EventType.Open:
                    eventHandlerPath = GetEventHandlePath(EventType.Open);
                    break;
                case EventType.Unsubscribe:
                    eventHandlerPath = GetEventHandlePath(EventType.Unsubscribe);
                    link = "/sitecore modules/Web/EXM/Unsubscribe.aspx";
                    break;
                case EventType.UnsubscribeFromAll:
                    eventHandlerPath = GetEventHandlePath(EventType.UnsubscribeFromAll);
                    link = "/sitecore modules/Web/EXM/UnsubscribeFromAll.aspx";
                    break;
                case EventType.Click:
                    eventHandlerPath = GetEventHandlePath(EventType.Click);
                    break;
                case EventType.Bounce:
                    GenerateBounce(hostName, contactId, messageItem, dateTime);
                    return;
                case EventType.SpamComplaint:
                    GenerateSpam(hostName, contactId, messageItem, dateTime);
                    return;
                default:
                    throw new InvalidEnumArgumentException("No such event in ExmEvents");
            }

            var queryStrings = GetQueryParameters(contactId, messageItem, link);
            var encryptedQueryString = QueryStringEncryption.GetDefaultInstance().Encrypt(queryStrings);

            var parameters = encryptedQueryString.ToQueryString(true);

            var url = $"{hostName}{eventHandlerPath}{parameters}";
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
