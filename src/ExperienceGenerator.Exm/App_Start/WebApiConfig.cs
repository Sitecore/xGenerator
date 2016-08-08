namespace ExperienceGenerator.Exm
{
  using System.Web.Http;
  using ExperienceGenerator.Exm.Services;

  public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {

            ExmJobManager.Instance = new ExmJobManager();

            config.Routes.MapHttpRoute(
                name: "ExperienceGeneratorExmJobsApi",
                routeTemplate: "api/xgen/exmjobs/{id}",
                defaults: new {controller = "ExperienceGeneratorExmJobs",  id = RouteParameter.Optional}
                );
            config.Routes.MapHttpRoute(
                name: "ExperienceGeneratorExmEventsApi",
                routeTemplate: "api/xgen/exmevents/{action}",
                defaults: new {controller = "ExmEvents"}
                );
        }
    }
}
