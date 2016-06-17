// © 2016 Sitecore Corporation A/S. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Net.Http;
using Sitecore.Data;
using Sitecore.Modules.EmailCampaign;
using Sitecore.Modules.EmailCampaign.Core.Crypto;
using Sitecore.Modules.EmailCampaign.Core.Extensions;
using Sitecore.Modules.EmailCampaign.Core.Links;
using Sitecore.Modules.EmailCampaign.Messages;

namespace ExperienceGenerator.Services
{
    public static class ExmEventsGenerator
    {
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>
            (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
            {
                HashSet<TKey> knownKeys = new HashSet<TKey>();
                foreach (TSource element in source)
                {
                    if (knownKeys.Add(keySelector(element)))
                    {
                        yield return element;
                    }
                }
            }

        public static void GenerateSent(string hostName, ID contactId, ID messageId, DateTime dateTime)
        {
            using (var client = new HttpClient())
            {
                var url = string.Format("{0}/api/xgen/exmjobs/GenerateSent?contactId={1}&messageId={2}&date={3}", hostName, contactId, messageId, dateTime.ToString("u"));
                var res = client.PostAsync(url, new StringContent(string.Empty)).Result;
            }
        }

        public static void GenerateBounce(string hostName, ID contactId, ID messageId, DateTime dateTime)
        {
            using (var client = new HttpClient())
            {
                var url = string.Format("{0}/api/xgen/exmjobs/GenerateBounce?contactId={1}&messageId={2}&date={3}", hostName, contactId, messageId, dateTime.ToString("u"));
                var res = client.PostAsync(url, new StringContent(string.Empty)).Result;
            }
        }

        public static void GenerateSpamComplaint(string hostName, ID contactId, ID messageId, string email, DateTime dateTime)
        {
            using (var client = new HttpClient())
            {
                var url = string.Format("{0}/api/xgen/exmjobs/GenerateSpam?contactId={1}&messageId={2}&email={3}&&date={4}", hostName, contactId, messageId, email, dateTime.ToString("u"));
                var res = client.PostAsync(url, new StringContent(string.Empty)).Result;
            }
        }

        public static void GenerateHandlerEvent(string hostName, Guid userId, MessageItem messageItem, ExmEvents exmEvent, DateTime dateTime, string userAgent = null, string ip = null, string link = null)
        {
            string eventHandler;
            switch (exmEvent)
            {
                case ExmEvents.Open:
                    eventHandler = "RegisterEmailOpened.ashx";
                    break;
                case ExmEvents.Unsubscribe:
                    eventHandler = "RedirectUrlPage.aspx";
                    link = "/sitecore/Unsubscribe.aspx";
                    break;
                case ExmEvents.UnsubscribeFromAll:
                    eventHandler = "RedirectUrlPage.aspx";
                    link = "/sitecore/UnsubscribeFromAll.aspx";
                    break;
                case ExmEvents.Click:
                    eventHandler = "RedirectUrlPage.aspx";
                    break;
                default:
                    throw new InvalidEnumArgumentException("No such event in ExmEvents");
            }

            var messageId = messageItem.InnerItem.ID;

            var queryStrings = GetQueryParameters(userId, messageId, link);
            var encryptedQueryString = QueryStringEncryption.GetDefaultInstance().Encrypt(queryStrings);

            var parameters = encryptedQueryString.ToQueryString(true);

            using (var client = new HttpClient())
            {
                var url = string.Format("{0}/sitecore/{1}{2}", hostName, eventHandler, parameters);

                //Add the user agent header. the device is resolved based on this user agent. use devices you want from http://www.useragentstring.com/. Also make sure Device Detection Service is subscribed to.
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);

                //set what ever ip you want to set, the location is calculated based on the ip address. Remember to setup the configuration according to Sitecore IP Geolocation Service
                client.DefaultRequestHeaders.TryAddWithoutValidation("X-Forwarded-For", ip);

                client.DefaultRequestHeaders.TryAddWithoutValidation("X-Exm-RequestTime", dateTime.ToString("u"));

                //Fire request to handler
                var res = client.GetAsync(url).Result;
            }
        }

        private static NameValueCollection GetQueryParameters(Guid userId, ID messageId, string link = null)
        {
            var queryStrings = new NameValueCollection
            {
                [GlobalSettings.AnalyticsContactIdQueryKey] = new ShortID(userId).ToString(),
                [GlobalSettings.MessageIdQueryKey] = messageId.ToShortID().ToString()
            };

            if (link != null)
            {
                queryStrings[MailLinks.UrlQueryKey] = link;
            }

            return queryStrings;
        }
    }

    public enum ExmEvents
    {
        Open,
        Unsubscribe,
        UnsubscribeFromAll,
        Click
    }
}