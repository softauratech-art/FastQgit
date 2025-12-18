using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(FastQ.Web.Startup))]

namespace FastQ.Web
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
        }
    }
}
