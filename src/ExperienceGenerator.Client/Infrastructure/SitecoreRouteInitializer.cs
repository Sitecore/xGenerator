using System.Web.Http;
using Sitecore.Pipelines;

namespace ExperienceGenerator.Client.Infrastructure
{
    public class SitecoreRouteInitializer
    {

        public void Process(PipelineArgs args)
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}