using System.Web.Http;
using ExperienceGenerator.Client.Infrastructure;
using Sitecore.Analytics.Aggregation.Data.Model;

namespace ExperienceGenerator.Client
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            //config.MapHttpAttributeRoutes();

            //Disable dimension cache to enable flush of reporting database
            Dimension.EnableCaching = false;

            XGenJobManager.Instance = new XGenSitecoreJobManager();

            config.Routes.MapHttpRoute(
                name: "ExperienceGeneratorJobsApi",
                routeTemplate: "clientapi/xgen/jobs/{id}",
                defaults: new {controller = "ExperienceGeneratorJobs", id = RouteParameter.Optional}
                );

            
            config.Routes.MapHttpRoute(
                name: "ExperienceGeneratorActionsApi",
                routeTemplate: "clientapi/xgen/{action}/{id}",
                defaults: new {controller = "ExperienceGeneratorActions", id = RouteParameter.Optional}
                );
        }
    }
}
