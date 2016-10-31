namespace ExperienceGenerator.Exm.Infrastructure
{
  using System.Web.Http;
  using Sitecore.Pipelines;

  public class SitecoreRouteInitializer
    {

        public void Process(PipelineArgs args)
        {
            GlobalConfiguration.Configure(Exm.WebApiConfig.Register);
        }
    }
}