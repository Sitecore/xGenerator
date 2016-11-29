using System;
using System.Collections.Generic;
using System.Linq;
using Colossus.Statistics;
using ExperienceGenerator.Data;
using ExperienceGenerator.Exm.Models;
using ExperienceGenerator.Exm.Services;
using ExperienceGenerator.Repositories;
using ExperienceGenerator.Services;
using Sitecore;
using Sitecore.Analytics.Model;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Links;
using Sitecore.ListManagement.ContentSearch.Model;
using Sitecore.Modules.EmailCampaign.Messages;

namespace ExperienceGenerator.Exm.Repositories
{
    public class RandomContactMessageEventsFactory
    {
        private readonly ContactRepository _contactRepository;
        private readonly CampaignSettings _campaign;
        private readonly Random _random = new Random();
        private Dictionary<string, int> _userAgents;
        private Func<string> _getRandomRegion;
        private Func<int> _getRandomEventDay;
        private Func<string> _getRandomLandingPage;
        private readonly GeoDataRepository _geoDataRepository;
        private readonly GetRandomCityService _getRandomCityService;

        public RandomContactMessageEventsFactory(CampaignSettings campaign)
        {
            _contactRepository = new ContactRepository();
            _campaign = campaign;
            _getRandomCityService = new GetRandomCityService();
            _geoDataRepository = new GeoDataRepository();
        }

        private Dictionary<string, int> GetAllUserAgents()
        {
            var devices = new DeviceRepository().GetAll().ToDictionary(ga => ga.Id, ga => ga.UserAgent);
            return _campaign.Devices.ToDictionary(kvp => devices[kvp.Key], kvp => kvp.Value);
        }

        private string GetRandomUserAgent(City addCityKey)
        {
            if (_userAgents == null)
                _userAgents = GetAllUserAgents();
            var userAgent = _userAgents.Weighted().Invoke();

            var cityUserAgent = $"city:{addCityKey.GeoNameId}";
            var lastComment = userAgent.LastIndexOf(")", StringComparison.Ordinal);
            if (lastComment == -1)
            {
                userAgent += $"({cityUserAgent})";
            }
            else
            {
                userAgent = $"{userAgent.Substring(0, lastComment)}; {cityUserAgent}{userAgent.Substring(lastComment)}";
            }
            return userAgent;
        }

        private City GetRandomCity()
        {
            if (_getRandomRegion == null)
            {
                _getRandomRegion = _campaign.Locations.Select(x => x.Key).Weighted(_campaign.Locations.Values.Select(i => (double) i).ToArray());
            }
            var randomCity = _getRandomCityService.GetRandomCity(_getRandomRegion());
            return randomCity;
        }

        [NotNull]
        public MessageContactEvents CreateRandomContactMessageEvents(ContactData contactData, Funnel funnel, MessageItem messageItem)
        {
            var messageContactEvents = new MessageContactEvents();
            var events = new List<MessageContactEvent>();
            messageContactEvents.Events = events;
            messageContactEvents.MessageItem = messageItem;
            var randomCity = GetRandomCity();
            messageContactEvents.GeoData = randomCity.ToWhoIsInformation();
            messageContactEvents.UserAgent = GetRandomUserAgent(randomCity);

            var contact = _contactRepository.GetContact(contactData.ContactId);
            if (contact == null)
            {
                return messageContactEvents;
            }
            messageContactEvents.ContactId = contact.ContactId;

            if (RandomizeBounceEvent(funnel))
            {
                events.Add(new MessageContactEvent
                           {
                               EventTime = messageItem.StartTime,
                               EventType = EventType.Bounce
                           });
            }
            else
            {
                var eventTime = GetRandomEventTime(messageItem);

                if (RandomizeOpenEvent(funnel))
                {
                    AddEventDelay(ref eventTime);
                    events.Add(new MessageContactEvent {
                        EventType = EventType.Open,
                        EventTime = eventTime
                    });

                    if (RandomizeClickEvent(funnel))
                    {
                        AddEventDelay(ref eventTime);

                        messageContactEvents.LandingPageUrl = GetRandomLandingPageUrl(messageItem);

                        events.Add(new MessageContactEvent
                                   {
                                       EventType = EventType.Click,
                                       EventTime = eventTime
                                   });
                    }
                }

                if (RandomizeSpamComplaintEvent(funnel, events))
                {
                    AddEventDelay(ref eventTime);
                    events.Add(new MessageContactEvent
                               {
                                   EventType = EventType.SpamComplaint,
                                   EventTime = eventTime
                               });
                }

                if (RandomizeUnsubscribeEvent(funnel))
                {
                    AddEventDelay(ref eventTime);

                    if (RandomizeUnsubscribeAllEvent(funnel))
                    {
                        events.Add(new MessageContactEvent
                                   {
                                       EventType = EventType.UnsubscribeFromAll,
                                       EventTime = eventTime
                                   });
                    }
                    else
                    {
                        events.Add(new MessageContactEvent
                                   {
                                       EventType = EventType.Unsubscribe,
                                       EventTime = eventTime
                                   });
                    }
                }
            }
            return messageContactEvents;
        }

        private void AddEventDelay(ref DateTime eventTime, int seconds = 300)
        {
            eventTime = eventTime.AddSeconds(_random.Next(10, seconds));
        }

        private bool RandomizeUnsubscribeAllEvent(Funnel funnel)
        {
            return _random.NextDouble() < funnel.UnsubscribedFromAll / 100d;
        }

        private bool RandomizeUnsubscribeEvent(Funnel funnel)
        {
            return _random.NextDouble() < funnel.Unsubscribed / 100d;
        }

        private bool RandomizeSpamComplaintEvent(Funnel funnel, List<MessageContactEvent> events)
        {
            return _random.NextDouble() < funnel.SpamComplaints/100d && events.All(e => e.EventType != EventType.Click);
        }

        private bool RandomizeClickEvent(Funnel funnel)
        {
            return _random.NextDouble() < funnel.ClickRate/100d;
        }

        private bool RandomizeOpenEvent(Funnel funnel)
        {
            return _random.NextDouble() < funnel.OpenRate/100d;
        }

        private bool RandomizeBounceEvent(Funnel funnel)
        {
            return _random.NextDouble() < funnel.Bounced/100d;
        }

        private DateTime GetRandomEventTime(MessageItem messageItem)
        {
            if (_getRandomEventDay == null)
                _getRandomEventDay = _campaign.DayDistribution.Select(x => x/100d).WeightedInts();
            var days = _getRandomEventDay.Invoke();
            var seconds = _random.Next(60, 86400);
            return messageItem.StartTime.AddDays(days).AddSeconds(seconds);
        }

        private string GetRandomLandingPageUrl(MessageItem message)
        {
            if (_campaign.LandingPages.Count == 0)
                return "/";
            if (_getRandomLandingPage == null)
                _getRandomLandingPage = _campaign.LandingPages.Keys.Weighted(_campaign.LandingPages.Values.Select(x => x / 100).ToArray());

            var stringID = _getRandomLandingPage();
            ID landingPageID;
            if (!ID.TryParse(stringID, out landingPageID) || ID.IsNullOrEmpty(landingPageID))
                return null;

            var landingPageItem = message.InnerItem.Database.GetItem(landingPageID);

            return landingPageItem == null ? "/" : GetItemUrl(landingPageItem);
        }

        private static string GetItemUrl(Item landingPageItem)
        {
            var uri = new Uri(LinkManager.GetItemUrl(landingPageItem));
            return uri.PathAndQuery;
        }
    }
}
