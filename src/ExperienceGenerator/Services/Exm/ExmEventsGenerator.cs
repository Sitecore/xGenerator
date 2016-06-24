// © 2016 Sitecore Corporation A/S. All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Net.Http;
using System.Threading;
using Sitecore.Data;
using Sitecore.Modules.EmailCampaign;
using Sitecore.Modules.EmailCampaign.Core.Crypto;
using Sitecore.Modules.EmailCampaign.Core.Extensions;
using Sitecore.Modules.EmailCampaign.Core.Links;
using Sitecore.Modules.EmailCampaign.Messages;

namespace ExperienceGenerator.Services.Exm
{
    public static class ExmEventsGenerator
    {
        public static int Threads { get; set; }

        public static int Errors { get; set; }

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

        public static async void RequestUrl(string url, string userAgent = null, string ip = null, string dateTime = null)
        {
            while (Threads > 50)
            {
                Thread.Sleep(100);
            }

            Threads++;

            using (var client = new HttpClient())
            {
                if (userAgent != null)
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
                }

                if (ip != null)
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation("X-Forwarded-For", ip);
                }

                if (dateTime != null)
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation("X-Exm-RequestTime", dateTime);
                }

                var res = await client.PostAsync(url, new StringContent(string.Empty));
                if (!res.IsSuccessStatusCode)
                {
                    Errors++;
                }
            }

            Threads--;
        }

        public static void GenerateSent(string hostName, ID contactId, ID messageId, DateTime dateTime)
        {
            var url = string.Format("{0}/api/xgen/exmjobs/GenerateSent?contactId={1}&messageId={2}&date={3}", hostName, contactId, messageId, dateTime.ToString("u"));
            RequestUrl(url);
        }

        public static void GenerateBounce(string hostName, ID contactId, ID messageId, DateTime dateTime)
        {
            var url = string.Format("{0}/api/xgen/exmjobs/GenerateBounce?contactId={1}&messageId={2}&date={3}", hostName, contactId, messageId, dateTime.ToString("u"));
            RequestUrl(url);
        }

        public static void GenerateSpamComplaint(string hostName, ID contactId, ID messageId, string email, DateTime dateTime)
        {
            var url = string.Format("{0}/api/xgen/exmjobs/GenerateSpam?contactId={1}&messageId={2}&email={3}&&date={4}", hostName, contactId, messageId, email, dateTime.ToString("u"));
            RequestUrl(url);
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

            var url = string.Format("{0}/sitecore/{1}{2}", hostName, eventHandler, parameters);
            RequestUrl(url, userAgent, ip, dateTime.ToString("u"));
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