namespace ExperienceGenerator.Exm.Models
{
    public class SelectionOption
    {
        public string Id { get; set; }
        public string Label { get; set; }
        public double DefaultWeight { get; set; }

        public SelectionOption()
        {
            DefaultWeight = 50;
        }
    }
}