using System.Web;
using System.Web.Mvc;

namespace FT.FastQ.Controllers
{
    public class LanguageController : Controller
    {
        public ActionResult ChangeLanguage(string lang, string returnUrl)
        {
            if (!string.IsNullOrEmpty(lang))
            {
                HttpCookie cookie = new HttpCookie("Language");
                cookie.Value = lang;
                cookie.Expires = System.DateTime.Now.AddYears(1);
                Response.Cookies.Add(cookie);
            }

            if (string.IsNullOrEmpty(returnUrl))
            {
                returnUrl = Request.UrlReferrer?.ToString() ?? Url.Action("Index", "Home");
            }

            return Redirect(returnUrl);
        }
    }
}


