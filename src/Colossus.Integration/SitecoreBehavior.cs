using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Colossus.Integration
{
    public abstract class SitecoreBehavior : IVisitorBehavior
    {
        public string SitecoreUrl { get; set; }

        protected SitecoreBehavior(string sitecoreUrl)
        {
            SitecoreUrl = sitecoreUrl;
        }


        public IEnumerable<Visit> Commit(Visitor visitor)
        {
            return Commit(new SitecoreRequestContext(SitecoreUrl, visitor));
        }

        protected abstract IEnumerable<Visit> Commit(SitecoreRequestContext ctx);
    }
}
