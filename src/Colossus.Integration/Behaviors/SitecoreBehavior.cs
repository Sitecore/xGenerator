namespace Colossus.Integration.Behaviors
{
  using System.Collections.Generic;
  using Colossus.Integration.Models;

  public abstract class SitecoreBehavior : IVisitorBehavior
  {
    public string SitecoreUrl { get; set; }

    protected SitecoreBehavior(string sitecoreUrl)
    {
      this.SitecoreUrl = sitecoreUrl;
    }


    public IEnumerable<Visit> Commit(Visitor visitor)
    {
      return this.Commit(new SitecoreRequestContext(this.SitecoreUrl, visitor));
    }

    protected abstract IEnumerable<Visit> Commit(SitecoreRequestContext ctx);
  }
}