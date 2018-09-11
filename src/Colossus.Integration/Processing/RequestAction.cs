using Colossus.Web;
using Sitecore.Analytics;

namespace Colossus.Integration.Processing
{
    /// <summary>
    /// Classes implementing this interface are executed just before the page is rendered
    /// This is the right place to trigger events, outcomes etc.
    /// </summary>
    public interface IRequestAction
    {
        void Execute(ITracker tracker, RequestInfo requestInfo);
    }
}
