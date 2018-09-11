using System.Web.Http;
using ExperienceGenerator.Client;

namespace ExperienceGenerator
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
        }
    }
}
