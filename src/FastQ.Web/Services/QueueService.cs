using FastQ.Data.Common;
using FastQ.Data.Db;
using FastQ.Data.Entities;
using FastQ.Data.Repositories;
using FastQ.Web.Models.Admin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services.Description;
namespace FastQ.Web.Services
{
    public class QueueService
    {
        private readonly IQueueRepository _queues;
        private readonly string _stampuser = new AuthService().GetLoggedInWindowsUser();
        private Int64 _stampuserentity;
        public QueueService()
           : this(
               DbRepositoryFactory.CreateQueueRepository())
        {
        }

        public QueueService(IQueueRepository Queues)
        {
            _queues = Queues;
        }
        public IList<QueueVM> ListQueues()
        {
            //if (HttpContext.Current.Session["fq_this_entity"]  == null  ||
            //        !Int32.TryParse(HttpContext.Current.Session["fq_this_entity"].ToString(), out _stampuserentity))
            //    throw new Exception("Entity is missing for this session");
            //_stampuserentity = 1;

            _stampuserentity = new AuthService().GetSessionEntityId();
            var rows = _queues.ListByEntity(_stampuserentity, new AuthService().GetLoggedInWindowsUser());
            return BuildQueueRows(rows);
        }


        public QueueVM GetQueue(long queueid)
        {
            var item = _queues.GetQueueDetails(queueid);
            var model = new QueueVM
            {
                Name = item.Name,
                NameCP = item.NameCp,
                NameES = item.NameEs,
                Id = item.Id,
                LocationId = item.LocationId,
                ActiveFlag = item.ActiveFlag,
                LeadTimeMin = item.LeadTimeMin,
                LeadTimeMax = item.LeadTimeMax,
                EmpOnly = item.EmpOnly,
                HideInKiosk = item.HideInKiosk,
                HideInMonitor = item.HideInMonitor,
                Schedules = BuildSchedules(item.Schedules),
                Services = BuildServices(item.Services),
                SelectedRefCriterias = item.RefCriterias,
                SelectedContactMethods = item.ContactMethods,
                HasUploads = item.HasUploads,
                HasGuidelines = item.HasGuidelines
            };

            return (model);
        }

        public long AddOrUpdateQueue(QueueVM qvm)
        {
            Int64 newid = _queues.AddOrUpdateQueue(new Queue
            {
                Id = qvm.Id,
                LocationId = qvm.LocationId,
                Name = qvm.Name,
                NameCp = qvm.NameCP,
                NameEs = qvm.NameES,
                ActiveFlag = qvm.ActiveFlag,
                LeadTimeMin = qvm.LeadTimeMin,
                LeadTimeMax = qvm.LeadTimeMax,
                EmpOnly = qvm.EmpOnly,
                HideInKiosk = qvm.HideInKiosk,
                HideInMonitor = qvm.HideInMonitor,
                RefCriterias = qvm.SelectedRefCriterias,
                ContactMethods = qvm.SelectedContactMethods,
                HasGuidelines = qvm.HasGuidelines,
                HasUploads = qvm.HasUploads
            }, _stampuser);

            if (qvm.Id > 0) newid = qvm.Id;
            return newid;
        }

        public QueueServiceVM GetQueueService(long serviceid)
        {
            var item = _queues.GetQService(serviceid, _stampuser);
            var model = new QueueServiceVM
            {
                Name = item.Name,
                NameCP = item.NameCp,
                NameES = item.NameEs,
                Id = item.Id,
                ActiveFlag = item.ActiveFlag,
                QueueId = item.QueueId,
            };
            return (model);
        }

        public QueueScheduleVM GetQueueSchedule(long scheduleid)
        {
            var item = _queues.GetQSchedule(scheduleid, _stampuser);
            var model = new QueueScheduleVM
            {
                Id = item.Id,
                QueueId = item.QueueId,
                BeginDate = item.BeginDate,
                EndDate = item.EndDate,
                OpenTime = item.OpenTime,
                CloseTime = item.CloseTime,
                Duration = item.Duration,
                WeeklySchedule = item.WeeklySchedule,
                ResourcesAvailable = item.ResourcesAvailable
            };
            return (model);
        }

        private static IList<QueueVM> BuildQueueRows(IList<Queue> rows)
        {
            return rows.Select(r =>
            {
                return new QueueVM
                {
                    Name = r.Name,
                    NameCP = r.NameCp,
                    NameES = r.NameEs,
                    Id = r.Id,
                    LocationId = r.LocationId,
                    ActiveFlag = r.ActiveFlag,
                    LeadTimeMin = r.LeadTimeMin,
                    LeadTimeMax = r.LeadTimeMax,
                    EmpOnly = r.EmpOnly,
                    HideInKiosk = r.HideInKiosk,
                    HideInMonitor = r.HideInMonitor,
                    SelectedContactMethods = r.ContactMethods,
                    SelectedRefCriterias = r.RefCriterias,
                    HasGuidelines = r.HasGuidelines,
                    HasUploads = r.HasUploads
                };
            }).OrderBy(r => r.Id).ToList();
        }
        private static IList<QueueScheduleVM> BuildSchedules(IList<QSchedule> rows)
        {
            if (rows == null || rows.Count == 0) return null;
            return rows.Select(r =>
            {
                return new QueueScheduleVM
                {
                    Id = r.Id,
                    QueueId = r.QueueId,
                    BeginDate = r.BeginDate.Date,
                    EndDate = r.EndDate.Date,
                    OpenTime = r.OpenTime,
                    CloseTime = r.CloseTime,
                    Duration = r.Duration,
                    WeeklySchedule = r.WeeklySchedule,
                    ResourcesAvailable = r.ResourcesAvailable
                };
            }).OrderBy(r => r.Id).ToList();
        }
        private static IList<QueueServiceVM> BuildServices(IList<FastQ.Data.Entities.QService> rows)
        {
            if (rows == null || rows.Count == 0) return null;
            return rows.Select(r =>
            {
                return new QueueServiceVM
                {
                    Id = r.Id,
                    QueueId = r.QueueId,
                    Name = r.Name,
                    NameCP = r.NameCp,
                    NameES = r.NameEs,
                    ActiveFlag = r.ActiveFlag
                };
            }).OrderBy(r => r.Id).ToList();
        }

        public Result HandleQueueAction(string action, string json)
        {
            //action = (action ?? string.Empty).Trim().ToLowerInvariant();
            //return action switch
            //{
            //    "update" => UpdateQueue( json),
            //    "delete" => DeleteQueue( json),
            //    "create" => CreateQueue( json),
            //    _ => Result.Fail("Unknown action")
            //};

            return Result.Fail("Not Implemented");
        }

        public void AddOrUpdateQService(QueueServiceVM qsvm)
        {
            _queues.AddOrUpdateQService(new QService
            {
                Id = qsvm.Id,
                QueueId = qsvm.QueueId,
                Name = qsvm.Name,
                NameEs = qsvm.NameES,
                NameCp = qsvm.NameCP,
                ActiveFlag = qsvm.ActiveFlag
            }, _stampuser);
        }

        public void AddOrUpdateQSchedule(QueueScheduleVM qsvm)
        {
            _queues.AddOrUpdateQSchedule(new QSchedule
            {
                Id = qsvm.Id,
                QueueId = qsvm.QueueId,
                BeginDate = qsvm.BeginDate,
                EndDate = qsvm.EndDate,
                OpenTime = Helpers.Utilities.ParseTimestampForDB(qsvm.OpenTime),
                CloseTime = Helpers.Utilities.ParseTimestampForDB(qsvm.CloseTime),
                Duration = Helpers.Utilities.ParseTimestampForDB(qsvm.Duration),
                WeeklySchedule = qsvm.WeeklySchedule,
                ResourcesAvailable = qsvm.ResourcesAvailable
            }, _stampuser);
        }

        public void Delete(long id)
        {
            _queues.Delete(id, _stampuser);
        }

        public void DeleteQService(long serviceid)
        {
            _queues.DeleteQService(serviceid, _stampuser);
        }
        public void DeleteQSchedule(long scheduleid)
        {
            _queues.DeleteQSchedule(scheduleid, _stampuser);
        }



        #region obsolete
        //public QueueVM MapEntityToModel(Data.Entities.Queue er)
        //{
        //    return new QueueVM
        //    {
        //        Name = er.Name,
        //        NameCP = er.NameCP,
        //        NameES = er.NameES,
        //        Id = er.Id,
        //        ActiveFlag = er.ActiveFlag,
        //        LeadTimeMin = er.LeadTimeMin,
        //        LeadTimeMax = er.LeadTimeMin,
        //        EmpOnly = er.EmpOnly,
        //        HideInKiosk = er.HideInKiosk,
        //        HideInMonitor = er.HideInMonitor
        //    };
        //}
        //public IList<QueueVM> TransformToModelList()
        //{
        //    if (string.IsNullOrWhiteSpace(_stampuser))
        //    {
        //        return new List<QueueVM>();
        //    }

        //    //var rows = _queues.ListAll(_stampuserentity, _stampuser);
        //    var rows = new List<QueueVM>();
        //    return rows.Select(r =>
        //    {               
        //        return new QueueVM
        //        {
        //            Name = er.Name,
        //            NameCP = er.NameCP,
        //            NameES = er.NameES,
        //            Id = er.Id,
        //            ActiveFlag = er.ActiveFlag,
        //            LeadTimeMin = er.LeadTimeMin,
        //            LeadTimeMax = er.LeadTimeMin,
        //            EmpOnly = er.EmpOnly,
        //            HideInKiosk = er.HideInKiosk,
        //            HideInMonitor = er.HideInMonitor
        //        };
        //    }).OrderBy(r => er.Name).ToList();
        //}
        //private IList<QueueScheduleVM> GetSchedules(string json)
        //{
        //    JArray jsonArray = JArray.Parse(json);
        //    List<QueueScheduleVM> items = new List<QueueScheduleVM>();
        //    //""services"":[  {""service_id"":10004,""service_name"":""Pick "",""service_name_es"":""Recogida"",""service_name_cp"":""Ranmase/depoze""},
        //    //                {""service_id"":10005,""service_name"":""Questions: general (Residential)"",""service_name_es"":""Preguntas)"",""service_name_cp"":""Kesyon: sy�l)""}]

        //    //schedules"":[{""schedule_id"":2,""date_begin"":""2026-01-01T00:00:00"",""date_end"":""2026-12-31T00:00:00"",""open_time"":""PT11H"",""close_time"":""PT14H"",""interval_time"":""PT1H"",""weekly_sch"":""24"",""available_resources"":2},
        //    //             {""schedule_id"":6,""date_begin"":""2026-01-01T00:00:00"",""date_end"":""2026-12-31T00:00:00"",""open_time"":""PT13H"",""close_time"":""PT15H30M"",""interval_time"":""PT1H"",""weekly_sch"":""3"",""available_resources"":1}]

        //    foreach (JObject item in jsonArray)
        //    {
        //        // Access values using keys
        //        var ovm = new QueueScheduleVM
        //        {
        //            //QueueId  = item.GetValue("lead_time_min").ToString(),
        //            Id = Convert.ToInt64(item.GetValue("schedule_id").ToString()),
        //            BeginDate = Convert.ToDateTime(item.GetValue("date_begin")),
        //            EndDate = Convert.ToDateTime(item.GetValue("date_end").ToString()),
        //            CloseTime = item.GetValue("close_time").ToString(),
        //            OpenTime = item.GetValue("open_time").ToString(),
        //            Duration = item.GetValue("interval_time").ToString(),
        //            ResourcesAvailable = Convert.ToInt16(item.GetValue("available_resources").ToString()),
        //            WeeklySchedule = item.GetValue("weekly_sch").ToString()
        //        };

        //        items.Add(ovm);
        //    }
        //    return items;
        //    //return new List<QueueScheduleVM>();        

        //}
        #endregion
    }
}
