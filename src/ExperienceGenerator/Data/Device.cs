namespace ExperienceGenerator.Data
{
  public class Device
  {
    public string Type { get; set; }
    public string UserAgent { get; set; }
    public string Name { get; set; }
    public string Id { get; set; }

    public static Device FromCsv(string csv)
    {
      var parts = csv.Split('\t');
      if (parts.Length < 3)
        return null;

      return new Device()
      {
        Type = parts[0],
        Name = parts[1],
        UserAgent = parts[2],
        Id = parts[1].ToLowerInvariant().Replace("(","").Replace(")","").Replace(",","")
      };
    }
  }
}
