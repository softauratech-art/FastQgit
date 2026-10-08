using System;
using System.Linq;
using System.Web.Mvc;
using FastQ.Data.Db;
using FastQ.Web.Services;
namespace FastQ.Web.Controllers
{
    [FastQ.Web.Attributes.FQAuthorizeUser(AllowRole = "Lobby,SuperAdmin")]
    public class LobbyController : Controller
    {
        private readonly AuthService auth = new AuthService();
        [HttpGet]
        public ActionResult Index()
        {
            Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            ViewBag.EntityName=auth.GetCurrentUser().BusinessEntities.First(e => e.EntityId == auth.GetSessionEntityId()).EntityName;
            return View();
        }
        [HttpGet]
        public ActionResult Data()
        {
            Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            try
            {
                var entries=new DbLobbyRepository().Today(new AuthService().GetLoggedInWindowsUser(),auth.GetSessionEntityId());
                return Json(new { queues=entries.GroupBy(x=>x.QueueId).Select(g=>new {
                    id=g.Key,name=g.First().QueueName,
                    serving=g.Where(x=>x.Status=="IN PROGRESS").Select(x=>new {name=x.Name,time=x.Time,kind=x.SourceType=="W"?"Walk-in":"Appointment"}),
                    waiting=g.Where(x=>x.Status=="ARRIVED").Select(x=>new {name=x.Name,time=x.Time,kind=x.SourceType=="W"?"Walk-in":"Appointment"})
                }) },JsonRequestBehavior.AllowGet);
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException ex) when (ex.Number == 20002)
            {
                return new HttpStatusCodeResult(403, "Lobby access required");
            }
            catch(Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Lobby data failed: {0}",ex);
                return new HttpStatusCodeResult(503,"Lobby data unavailable");
            }
        }
    }
}
