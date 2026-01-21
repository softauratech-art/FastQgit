using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace FastQ.Web
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            App_Start.CompositionRoot.Initialize();
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }
    }
}

