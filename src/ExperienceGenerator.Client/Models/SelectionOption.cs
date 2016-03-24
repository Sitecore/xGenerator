namespace ExperienceGenerator.Client.Models
{
  public class SelectionOption
  {
    public string Id { get; set; }
    public string Label { get; set; }
    public double DefaultWeight { get; set; }

    public SelectionOption()
    {
      this.DefaultWeight = 50;
    }
  }
}