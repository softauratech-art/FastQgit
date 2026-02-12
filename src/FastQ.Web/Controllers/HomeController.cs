using System.Web.Mvc;

namespace FastQ.Web.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            HttpContext.Session["fq_this_entity"] = 1;
            return View();
        }
    }
}
