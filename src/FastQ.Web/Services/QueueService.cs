using FastQ.Data.Common;
using FastQ.Data.Db;
using FastQ.Data.Entities;
using FastQ.Data.Repositories;
using FastQ.Web.Models.Admin;
using Newtonsoft.Json.Linq;
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
            _stampuserentity = new AuthService().GetSessionEntityId();
            var rows = _queues.ListByEntity(_stampuserentity, new AuthService().GetLoggedInWindowsUser());
            return BuildQueueRows(rows);
        }


        public QueueVM GetQueue(long queueid)
        {
            var item = _queues.Get(queueid);
            var model = new QueueVM
            {
                Name = item.Name,
                NameCP = item.NameCp,
                NameES = item.NameEs,
                Address = item.Address,
                Phone = item.Phone,
                Id = item.Id,
                EntityId = item.EntityId,
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
                HasGuidelines = item.HasGuidelines,
                UserAccessList = item.UserAccess
            };

            return (model);
        }

        public long AddOrUpdateQueue(QueueVM qvm)
        {
            Int64 newid = _queues.AddOrUpdateQueue(new Queue
            {
                Id = qvm.Id,
                EntityId = qvm.EntityId,
                Name = qvm.Name,
                NameCp = qvm.NameCP,
                NameEs = qvm.NameES,
                Address = qvm.Address,
                Phone = qvm.Phone,
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
                    Address = r.Address,
                    Phone = r.Phone,
                    Id = r.Id,
                    EntityId = r.EntityId,
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

        public void AddOrUpdateQAccess(long id, string hostids, string providerids, string reporterids, string queueadminids)
        {
            _queues.AddOrUpdateQAccess(id, hostids, providerids, reporterids, queueadminids, _stampuser);
        }

        public IList<(string, string)> GetValidContactTypes()
        {
            IList<(string, string)> items = _queues.GetValidContactTypes();            
            return items;
        }

        public IList<(string, string)> GetValidRefCriterias()
        {        
            IList<(string, string)> items = _queues.GetValidRefCriterias();            
            return items;
        }
    }
}
