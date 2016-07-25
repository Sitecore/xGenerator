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
                routeTemplate: "api/xgen/exmjobs/{action}/{id}",
                defaults: new {controller = "ExperienceGeneratorExmJobs", action = "Index", id = RouteParameter.Optional}
                );
        }
    }
}
