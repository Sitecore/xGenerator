using System.Collections.Generic;
using Colossus.Integration.Models;

namespace Colossus.Integration.Behaviors
{

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
