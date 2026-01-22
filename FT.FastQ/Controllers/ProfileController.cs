using System.Web.Mvc;

namespace FT.FastQ.Controllers
{
    public class ProfileController : Controller
    {
        // GET: Profile/ManageProfile
        public ActionResult ManageProfile()
        {
            ViewBag.Email = Session["UserEmail"]?.ToString();
            return View();
        }
    }
}


