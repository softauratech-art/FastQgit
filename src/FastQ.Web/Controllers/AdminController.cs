using FastQ.Data.Entities;
using FastQ.Data.Repositories;
using FastQ.Web.Attributes;
using FastQ.Web.Helpers;
using FastQ.Web.Models;
using FastQ.Web.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace FastQ.Web.Controllers
{
    [FQAuthorizeUser(AllowRole = $"{nameof(Utilities.FQRole.QueueAdmin)},{nameof(Utilities.FQRole.SuperAdmin)}")]

   
    public class AdminController : Controller
    {
        private readonly AdminService _service;

        public AdminController()
        {
            _service = new AdminService();
        }

        [HttpGet]
        public ActionResult Dashboard()
        {
            //return View(BuildDashboardModel());
            ViewBag.Entity = _service.GetCurrentEntity();
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult EditEntity(FormCollection collection)
        { 
            long id = Convert.ToInt64(collection["EntityId"]);
            string name = collection["name"];
            string description = collection["description"];
            bool active = true;
            string address = collection["address"];
            string phone = collection["phone"];
            DateTime? opensat = collection["opensat"] == null || string.IsNullOrWhiteSpace(collection["opensat"]) ? null : Convert.ToDateTime(collection["opensat"]) ;
            DateTime? closessat = collection["closesat"] == null || string.IsNullOrWhiteSpace(collection["closesat"]) ? null : Convert.ToDateTime(collection["closesat"]);
            DateTime? outagebeginsat = collection["outagebegin"] == null || string.IsNullOrWhiteSpace(collection["outagebegin"]) ? null : Convert.ToDateTime(collection["outagebegin"]);
            DateTime? outageendsat = collection["outageend"] == null || string.IsNullOrWhiteSpace(collection["outageend"]) ? null : Convert.ToDateTime(collection["outageend"]);
            DateTime? outagenotifyat = collection["outagenotifybegin"] == null || string.IsNullOrWhiteSpace(collection["outagenotifybegin"]) ? null : Convert.ToDateTime(collection["outagenotifybegin"]);
            string outagemessge = collection["outagemessage"];

            Data.Entities.Entity oentity = new Data.Entities.Entity
            {
                Name = name,
                Description = description,
                ActiveFlag = active,
                Address = address,
                Phone = phone,
                OpensAt = opensat,
                ClosesAt = closessat,
                OutageBegin = outagebeginsat,
                OutageEnd = outageendsat,
                OutageNotifyBegin = outagenotifyat,
                OutageMessage = outagemessge,
                Id = id
            };

            try
            {
                _service.UpdateEntity(oentity);               
                return Json(new { response = "success", message = "Update successful." });
            }
            catch (Exception ex)
            {
                if (ex.Message.StartsWith("Note:"))
                    return Json(new { response = "success", message = $"Update successful. <div>{ex.Message}</div>"});
                else
                    return Json(new { response = "error", message = ex.Message });
            }
        }

        //PReddy: May be used later if necessary (for a quick-view dashboard section)
        //[HttpGet]
        //public JsonResult AdminSnapshot(string locationId)
        //{
        //    long locId;
        //    var hasLocation = long.TryParse(locationId, out locId);

        //    var queues = _service.ListQueues(1);

        //    var providers = _service.ListProviders(hasLocation ? (long?)locId : null);

        //    var entities = _service.ListEntities();

        //    var thisentity = _service.GetCurrentEntity();

        //    var queueRows = queues.Select(q => new
        //    {
        //        QueueId = q.Id,
        //        QueueName = q.Name
        //    }).ToList();

        //    var providerRows = providers.Select(p =>
        //    {
        //        var locationName = entities.FirstOrDefault(l => l.Id == p.EntityId)?.Name ?? "Unknown";
        //        return new
        //        {
        //            ProviderId = p.Id,
        //            ProviderName = p.Name,
        //            LocationName = locationName
        //        };
        //    }).ToList();

        //    return Json(new
        //    {
        //        ok = true,
        //        data = new
        //        {
        //            Queues = queueRows,
        //            Providers = providerRows
        //        }
        //    }, JsonRequestBehavior.AllowGet);
        //}
    }
}
