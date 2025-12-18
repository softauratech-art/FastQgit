using System;
using System.Web;

namespace FastQ.Web
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            App_Start.CompositionRoot.Initialize();
        }
    }
}
