using System;
using ExperienceGenerator.Exm.Services;

namespace ExperienceGenerator.Exm.Models
{
    public class MessageContactEvent
    {
        public DateTime EventTime { get; set; }
        public EventType EventType { get; set; }
    }
}
