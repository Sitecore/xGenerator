namespace ExperienceGenerator.Exm.Models
{
    public class Funnel
    {
        public int OpenRate { get; set; }
        public int ClickRate { get; set; }
        public int Bounced { get; set; }
        public int Unsubscribed { get; set; }
        public int Delivered { get; set; }
        public int SpamComplaints { get; set; }
        //TODO - Warning: UnsubscribeFromAll not supported in UI
        public int UnsubscribedFromAll => 50;
    }
}
