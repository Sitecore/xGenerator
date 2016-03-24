namespace Colossus.Integration.Models
{
  using System.Collections.Generic;

  public class PageDefinition
  {
    public PageDefinition()
    {
      this.RequestVariables = new Dictionary<string, object>();

    }
    public string Path { get; set; }
    public Dictionary<string, object> RequestVariables { get; set; }
  }
}