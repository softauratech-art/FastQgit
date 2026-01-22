using System;
using System.Web.Mvc;
using NLog;

namespace FT.FastQ.Controllers
{
    public class DashboardController : Controller
    {
        private static Logger _logger = LogManager.GetLogger("DashboardController");
        
        // GET: Dashboard
        public ActionResult Index(string email)
        {
            try
            {
                _logger.Info($"Dashboard Index - Email: {email}");
                ViewBag.Email = email;
                return View();
            }
            catch (Exception ex)
            {
                _logger.Error($"Dashboard Index - Exception: {ex.Message} - {ex.StackTrace}");
                throw;
            }
        }
    }
}


