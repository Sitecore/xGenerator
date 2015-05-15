using System.Web.Http;
using ExperienceGenerator.Client.Infrastructure;

namespace ExperienceGenerator.Client
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            //config.MapHttpAttributeRoutes();

            XGenJobManager.Instance = new XGenSitecoreJobManager();

            config.Routes.MapHttpRoute(
                name: "ExperienceGeneratorJobsApi",
                routeTemplate: "api/xgen/jobs/{id}",
                defaults: new { controller = "ExperienceGeneratorJobs", id = RouteParameter.Optional }
            );

            config.Routes.MapHttpRoute(
                name: "ExperienceGeneratorActionsApi",
                routeTemplate: "api/xgen/{action}",
                defaults: new { controller = "ExperienceGeneratorActions" }
            );
        }
    }
}
