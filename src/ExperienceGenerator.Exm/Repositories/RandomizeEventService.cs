using System;
using System.Collections.Generic;
using System.Linq;
using Colossus.Statistics;
using ExperienceGenerator.Data;
using ExperienceGenerator.Exm.Models;
using ExperienceGenerator.Exm.Services;
using Sitecore;
using Sitecore.Analytics.Model;
using Sitecore.Data;
using Sitecore.Links;
using Sitecore.ListManagement.ContentSearch.Model;
using Sitecore.Modules.EmailCampaign.Messages;
using GeoData = ExperienceGenerator.Exm.Models.GeoData;

namespace ExperienceGenerator.Exm.Repositories
{
    public class RandomContactMessageEventsFactory
    {
        private readonly ContactRepository _contactRepository;
        private readonly CampaignModel _campaign;
        private readonly Random _random = new Random();
        private Dictionary<string, int> _userAgents;
        private Func<WhoIsInformation> _geoData;
        private Func<int> _eventDay;
        private Func<string> _landingPages;

        public RandomContactMessageEventsFactory(ContactRepository contactRepository, CampaignModel campaign)
        {
            _contactRepository = contactRepository;
            _campaign = campaign;
        }

        private Dictionary<string, int> GetAllUserAgents()
        {
            var devices = new DeviceRepository().GetAll().ToDictionary(ga => ga.Id, ga => ga.UserAgent);
            return _campaign.Devices.ToDictionary(kvp => devices[kvp.Key], kvp => kvp.Value);
        }

        private string GetRandomUserAgent()
        {
            if (_userAgents == null)
                _userAgents = GetAllUserAgents();
            return _userAgents.Weighted().Invoke();
        }

        private List<GeoData> GetAllGeoData(Dictionary<int, int> weightedIDs)
        {
            return weightedIDs.Select(weightedId => new GeoData
                                                    {
                                                        Weight = weightedId.Value/100.0,
                                                        WhoIs = GeoRegion.RandomCountryForSubRegion(weightedId.Key)
                                                    }).ToList();
        }

        private WhoIsInformation GetRandomGeoData()
        {
            if (_geoData == null)
            {
                var geoData = GetAllGeoData(_campaign.Locations);
                _geoData = geoData.Select(x => x.WhoIs).Weighted(geoData.Select(x => x.Weight).ToArray());
            }
            return _geoData();
        }

        [NotNull]
        public MessageContactEvents CreateRandomContactMessageEvents(ContactData contactData, Funnel funnel, MessageItem messageItem)
        {
            var messageContactEvents = new MessageContactEvents();
            var events = new List<MessageContactEvent>();
            messageContactEvents.Events = events;
            messageContactEvents.MessageItem = messageItem;

            var contact = _contactRepository.GetContact(contactData.ContactId);
            if (contact == null)
            {
                return messageContactEvents;
            }
            messageContactEvents.ContactId = contact.ContactId;

            if (_random.NextDouble() < funnel.Bounced/100d)
            {
                events.Add(new MessageContactEvent
                           {
                               EventTime = messageItem.StartTime,
                               EventType = EventType.Bounce
                           });
            }
            else
            {
                messageContactEvents.UserAgent = GetRandomUserAgent();
                messageContactEvents.GeoData = GetRandomGeoData();
                var eventTime = GetRandomEventTime(messageItem);

                var spamPercentage = funnel.SpamComplaints/100d;
                if (_random.NextDouble() < funnel.OpenRate/100d)
                {
                    if (_random.NextDouble() < funnel.ClickRate/100d)
                    {
                        spamPercentage = Math.Min(spamPercentage, 0.01);
                        eventTime = eventTime.AddSeconds(_random.Next(10, 300));

                        messageContactEvents.LandingPageUrl = GetRandomLandingPageUrl(messageItem);

                        events.Add(new MessageContactEvent
                                   {
                            EventType = EventType.Click,
                            EventTime = eventTime
                                   });
                    }
                    else
                    {
                        eventTime = eventTime.AddSeconds(_random.Next(10, 300));
                        events.Add(new MessageContactEvent
                        {
                            EventType = EventType.Open,
                            EventTime = eventTime
                        });
                    }
                }

                if (_random.NextDouble() < spamPercentage)
                {
                    eventTime = eventTime.AddSeconds(_random.Next(10, 300));
                    events.Add(new MessageContactEvent
                    {
                        EventType = EventType.SpamComplaint,
                        EventTime = eventTime
                    });
                }

                if (_random.NextDouble() < funnel.Unsubscribed / 100d)
                {
                    eventTime = eventTime.AddSeconds(_random.Next(10, 300));

                    if (_random.NextDouble() < funnel.UnsubscribedFromAll / 100d)
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

        private DateTime GetRandomEventTime(MessageItem messageItem)
        {
            if (_eventDay == null)
                _eventDay = _campaign.DayDistribution.Select(x => x/100d).WeightedInts();
            var days = _eventDay.Invoke();
            var seconds = _random.Next(60, 86400);
            return messageItem.StartTime.AddDays(days).AddSeconds(seconds);
        }

        private string GetRandomLandingPageUrl(MessageItem message)
        {
            if (_campaign.LandingPages.Count == 0)
                return "/";
            if (_landingPages == null)
                _landingPages = _campaign.LandingPages.Keys.Weighted(_campaign.LandingPages.Values.Select(x => x / 100).ToArray());

            var stringID = _landingPages();
            ID landingPageID;
            if (!ID.TryParse(stringID, out landingPageID) || ID.IsNullOrEmpty(landingPageID))
                return null;

            var landingPageItem = message.InnerItem.Database.GetItem(landingPageID);

            return landingPageItem == null ? "/" : LinkManager.GetItemUrl(landingPageItem);
        }
    }

    public class MessageContactEvents
    {
        public MessageItem MessageItem { get; set; }
        public Guid ContactId { get; set; }
        public string UserAgent { get; set; }
        public WhoIsInformation GeoData { get; set; }
        public string LandingPageUrl { get; set; }
        public IEnumerable<MessageContactEvent> Events { get; set; }
    }

    public class MessageContactEvent
    {
        public DateTime EventTime { get; set; }
        public EventType EventType { get; set; }
    }
}