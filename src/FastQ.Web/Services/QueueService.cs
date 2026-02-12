using FastQ.Data.Common;
using FastQ.Data.Repositories;
using FastQ.Web.Models.Admin;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Web;
using FastQ.Data.Db;
namespace FastQ.Web.Services
{
    public class QueueService
    {
        private readonly IQueueRepository _Queues;
        private readonly string _stampQueue = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToLower().Replace("ocgov\"", "");
        private Int32 _stampuserentity;
        public QueueService()
           : this(
               DbRepositoryFactory.CreateQueueRepository())
        {
        }
 
        public QueueService(IQueueRepository Queues)
        {
            _Queues = Queues;
        }
        public IList<QueueVM> ListQueues()
        {
            if (HttpContext.Current.Session["fq_this_entity"]  == null  ||
                    !Int32.TryParse(HttpContext.Current.Session["fq_this_entity"].ToString(), out _stampuserentity))
                throw new Exception("Entity is missing for this session");

            string json = @"[{""queue_id"":10001,""name"":""Building Plans Review (Commercial)"",""name_es"":""Revisión de planos de construcción (comerciales)"",""name_cp"":""Revizyon Plan Konstriksyon (Komèsyal)"",""location_id"":1,""activeflag"":""Y"",""emp_only"":"""",""hide_in_kiosk"":"""",""hide_in_monitor"":"""",""lead_time_min"":""P1D"",""lead_time_max"":""P30D"",""has_guidelines"":"""",""has_uploads"":null},
                                {""queue_id"":10011,""name"":""Zoning"",""name_es"":""Zonificación"",""name_cp"":""Zoning"",""location_id"":1,""activeflag"":""Y"",""emp_only"":"""",""hide_in_kiosk"":"""",""hide_in_monitor"":"""",""lead_time_min"":""P3D"",""lead_time_max"":""P360D"",""has_guidelines"":"""",""has_uploads"":null},
                                {""queue_id"":10003,""name"":""Building Plans Review (Residential)"",""name_es"":""Revisión de planos de construcción (residencial)"",""name_cp"":""Revizyon Plan Konstriksyon (Rezidansyèl)"",""location_id"":1,""activeflag"":""Y"",""emp_only"":"""",""hide_in_kiosk"":"""",""hide_in_monitor"":"""",""lead_time_min"":"""",""lead_time_max"":""P60D"",""has_guidelines"":"""",""has_uploads"":null},
                                {""queue_id"":10009,""name"":""Building - Commercial Plans Coordination"",""name_es"":""Edificación - Coordinación de planos comerciales"",""name_cp"":""Kowòdinasyon Plan Komèsyal - Konstriksyon"",""location_id"":1,""activeflag"":""Y"",""emp_only"":"""",""hide_in_kiosk"":"""",""hide_in_monitor"":"""",""lead_time_min"":""P1D"",""lead_time_max"":""P90D"",""has_guidelines"":"""",""has_uploads"":null},
                                {""queue_id"":10005,""name"":""Building Sub-Permitting"",""name_es"":""Permisos de construcción secundarios"",""name_cp"":""Sou-pèmi pou konstwi"",""location_id"":1,""activeflag"":""Y"",""emp_only"":"""",""hide_in_kiosk"":"""",""hide_in_monitor"":"""",""lead_time_min"":""P2D"",""lead_time_max"":""P90D"",""has_guidelines"":"""",""has_uploads"":null},
                                {""queue_id"":10007,""name"":""Building - Licensing & NOC"",""name_es"":""Construcción - Licencias y permisos"",""name_cp"":""Konstriksyon - Lisans "",""location_id"":1,""activeflag"":""Y"",""emp_only"":"""",""hide_in_kiosk"":"""",""hide_in_monitor"":"""",""lead_time_min"":""P1D"",""lead_time_max"":""P90D"",""has_guidelines"":"""",""has_uploads"":null},
                                {""queue_id"":10013,""name"":""Impact Fees & Concurrency"",""name_es"":""Tarifas de impacto y simultaneidad"",""name_cp"":""Frè Enpak ak Konkourans"",""location_id"":1,""activeflag"":""Y"",""emp_only"":"""",""hide_in_kiosk"":"""",""hide_in_monitor"":"""",""lead_time_min"":""P1D"",""lead_time_max"":""P90D"",""has_guidelines"":"""",""has_uploads"":null},
                                {""queue_id"":10015,""name"":""Public Records"",""name_es"":""Registros públicos"",""name_cp"":""Dosye Piblik yo"",""location_id"":1,""activeflag"":""Y"",""emp_only"":"""",""hide_in_kiosk"":"""",""hide_in_monitor"":"""",""lead_time_min"":"""",""lead_time_max"":""P120D"",""has_guidelines"":"""",""has_uploads"":null},
                                {""queue_id"":10017,""name"":""Utilities"",""name_es"":""Servicios públicos"",""name_cp"":""Itilite yo"",""location_id"":1,""activeflag"":""Y"",""emp_only"":"""",""hide_in_kiosk"":"""",""hide_in_monitor"":"""",""lead_time_min"":""P1D"",""lead_time_max"":"""",""has_guidelines"":"""",""has_uploads"":null}]";

            JArray jsonArray = JArray.Parse(json);  
            //List<QueueVM> items = JsonConvert.DeserializeObject<List<QueueVM>>(json);

            List<QueueVM> items = new List<QueueVM>();
            foreach (JObject item in jsonArray)
            {
                // Access values using keys               
                var ovm = new QueueVM
                {
                    Name = item.GetValue("name").ToString(),
                    NameCP = item.GetValue("name_cp").ToString(),
                    NameES = item.GetValue("name_cp").ToString(),
                    Id = Convert.ToInt64(item.GetValue("queue_id").ToString()),
                    ActiveFlag = item.GetValue("activeflag").ToString(),
                    LeadTimeMin = item.GetValue("lead_time_min").ToString(),
                    LeadTimeMax = item.GetValue("lead_time_max").ToString(),
                    EmpOnly = item.GetValue("emp_only").ToString(),
                    HideInKiosk = item.GetValue("hide_in_kiosk").ToString(),
                    HideInMonitor = item.GetValue("hide_in_monitor").ToString()
                };

                items.Add(ovm);
            }
            return items;
            //return TransformToModelList();                
            //return null;


        }

        private IList<QueueScheduleVM> GetSchedules(string json)
        {
            JArray jsonArray = JArray.Parse(json);
            List<QueueScheduleVM> items = new List<QueueScheduleVM>();
            //""services"":[  {""service_id"":10004,""service_name"":""Pick "",""service_name_es"":""Recogida"",""service_name_cp"":""Ranmase/depoze""},
            //                {""service_id"":10005,""service_name"":""Questions: general (Residential)"",""service_name_es"":""Preguntas)"",""service_name_cp"":""Kesyon: syèl)""}]

            //schedules"":[{""schedule_id"":2,""date_begin"":""2026-01-01T00:00:00"",""date_end"":""2026-12-31T00:00:00"",""open_time"":""PT11H"",""close_time"":""PT14H"",""interval_time"":""PT1H"",""weekly_sch"":""24"",""available_resources"":2},
            //             {""schedule_id"":6,""date_begin"":""2026-01-01T00:00:00"",""date_end"":""2026-12-31T00:00:00"",""open_time"":""PT13H"",""close_time"":""PT15H30M"",""interval_time"":""PT1H"",""weekly_sch"":""3"",""available_resources"":1}]

            foreach (JObject item in jsonArray)
            {
                // Access values using keys
                var ovm = new QueueScheduleVM
                {
                     //QueueId  = item.GetValue("lead_time_min").ToString(),
                     Id  = Convert.ToInt64(item.GetValue("schedule_id").ToString()),
                     BeginDate = Convert.ToDateTime(item.GetValue("date_begin")),
                     EndDate  = Convert.ToDateTime(item.GetValue("date_end").ToString()),
                     CloseTime = item.GetValue("close_time").ToString(), 
                     OpenTime  = item.GetValue("open_time").ToString(),
                     Duration  = item.GetValue("interval_time").ToString(),
                     ResourcesAvailable  = Convert.ToInt16(item.GetValue("available_resources").ToString()),
                     WeeklySchedule = item.GetValue("weekly_sch").ToString()                    
                };

                items.Add(ovm);
            }
            return items;
            //return new List<QueueScheduleVM>();        
        
        }

        public QueueVM GetQueue(Int64 queueid)
        {
            //string json = @"[{""queue_id"":10003,""name"":""Building Plans Review (Residential)"",""name_cp"":""Revizyon Plan Konstriksyon (Rezidansyèl)"",""name_es"":""Revisión de planos de construcción (residencial)"",""locname"":""Division of Building Safety"",""address"":""201 S. Rosalind Avenue Orlando, FL 32801"",""phone"":""407-836-5550"",""configOptions"":{""lead_time_max"":""P60D"",""lead_time_min"":"""",""has_uploads"":"""",""has_guidelines"":"""",""emp_only"":"""",""hide_in_monitor"":"""",""hide_in_kiosk"":"""",""activeflag"":""Y""},""contactoptions"":[{""type_key"":""OM"",""type_val"":""Online Meeting"",""type_val_es"":""Reunión en línea"",""type_val_cp"":""Reyinyon sou entènèt""},{""type_key"":""PC"",""type_val"":""Phone Call"",""type_val_es"":""Llamada telefónica"",""type_val_cp"":""Telefòn-Rele""}],""refoptions"":[{""ref_key"":""G"",""ref_val"":""General"",""ref_val_es"":""General"",""ref_val_cp"":""Jeneral""},{""ref_key"":""P"",""ref_val"":""Permit"",""ref_val_es"":""Permiso"",""ref_val_cp"":""Pèmi""}]}, ""services"":[{""service_id"":10004,""service_name"":""Pick up/drop off: existing paper plans only"",""service_name_es"":""Recogida/entrega: solo planos en papel existentes."",""service_name_cp"":""Ranmase/depoze: plan papye ki deja egziste sèlman""},{""service_id"":10005,""service_name"":""Questions: general (Residential)"",""service_name_es"":""Preguntas: generales (Residencial)"",""service_name_cp"":""Kesyon: jeneral (Rezidansyèl)""},{""service_id"":10006,""service_name"":""Questions: online Fasttrack application"",""service_name_es"":""Preguntas: solicitud Fasttrack en línea"",""service_name_cp"":""Kesyon: aplikasyon Fasttrack sou entènèt""},{""service_id"":10007,""service_name"":""Questions: violations"",""service_name_es"":""Preguntas: infracciones"",""service_name_cp"":""Kesyon: vyolasyon""}],""schedules"":[{""schedule_id"":2,""date_begin"":""2026-01-01T00:00:00"",""date_end"":""2026-12-31T00:00:00"",""open_time"":""PT11H"",""close_time"":""PT14H"",""interval_time"":""PT1H"",""weekly_sch"":""24"",""available_resources"":2},{""schedule_id"":6,""date_begin"":""2026-01-01T00:00:00"",""date_end"":""2026-12-31T00:00:00"",""open_time"":""PT13H"",""close_time"":""PT15H30M"",""interval_time"":""PT1H"",""weekly_sch"":""3"",""available_resources"":1}]}]";
            string json = @"{""queue_id"":10003,""name"":""Building Plans Review (Residential)"",""name_cp"":""Revizyon Plan Konstriksyon (Rezidansyèl)"",""name_es"":""Revisión de planos de construcción (residencial)"",""locname"":""Division of Building Safety"",""address"":""201 S. Rosalind Avenue Orlando, FL 32801"",""phone"":""407-836-5550"",""configOptions"":{""lead_time_max"":""P60D"",""lead_time_min"":"""",""has_uploads"":"""",""has_guidelines"":"""",""emp_only"":"""",""hide_in_monitor"":"""",""hide_in_kiosk"":"""",""activeflag"":""Y""},""contactoptions"":[{""type_key"":""OM"",""type_val"":""Online Meeting"",""type_val_es"":""Reunión en línea"",""type_val_cp"":""Reyinyon sou entènèt""},{""type_key"":""PC"",""type_val"":""Phone Call"",""type_val_es"":""Llamada telefónica"",""type_val_cp"":""Telefòn-Rele""}],""refoptions"":[{""ref_key"":""G"",""ref_val"":""General"",""ref_val_es"":""General"",""ref_val_cp"":""Jeneral""},{""ref_key"":""P"",""ref_val"":""Permit"",""ref_val_es"":""Permiso"",""ref_val_cp"":""Pèmi""}],""schedules"":[{""schedule_id"":2,""date_begin"":""2026-01-01T00:00:00"",""date_end"":""2026-12-31T00:00:00"",""open_time"":""PT11H"",""close_time"":""PT14H"",""interval_time"":""PT1H"",""weekly_sch"":""24"",""available_resources"":2},{""schedule_id"":6,""date_begin"":""2026-01-01T00:00:00"",""date_end"":""2026-12-31T00:00:00"",""open_time"":""PT13H"",""close_time"":""PT15H30M"",""interval_time"":""PT1H"",""weekly_sch"":""3"",""available_resources"":1}]}";

            //string json  = @"[{""queue_id"":10003,""name"":""Building Plans Review (Residential)"",""name_cp"":""Revizyon Plan Konstriksyon (Rezidansyèl)"",""name_es"":""Revisión de planos de construcción (residencial)"",""locname"":""Division of Building Safety"",""address"":""201 S. Rosalind Avenue Orlando, FL 32801"",""phone"":""407-836-5550"",""lead_time_max"":""P60D"",""lead_time_min"":null,""has_uploads"":null,""has_guidelines"":null,""emp_only"":null,""hide_in_monitor"":null,""hide_in_kiosk"":null,""activeflag"":""Y""}]";

                       
            JObject jo = JObject.Parse(json);

            var model = new QueueVM
            {
                Name = jo["name"].ToString(),
                NameCP = jo["name_cp"].ToString(),
                NameES = jo["name_es"].ToString(),
                Id = Convert.ToInt64(jo["queue_id"].ToString()),
                ActiveFlag = jo["configOptions"]["activeflag"].ToString(),
                LeadTimeMin = jo["configOptions"]["lead_time_min"].ToString(),
                LeadTimeMax = jo["configOptions"]["lead_time_max"].ToString(),
                EmpOnly = jo["configOptions"]["emp_only"].ToString(),
                HideInKiosk = jo["configOptions"]["hide_in_kiosk"].ToString(),
                HideInMonitor = jo["configOptions"]["hide_in_monitor"].ToString(),
                Schedules = GetSchedules(jo["schedules"].ToString())
            };
            //JArray jsonArray = JArray.Parse(json);
            //var model = new QueueVM
            //{
            //    Name = jsonArray[0]["name"].ToString(),
            //    NameCP = jsonArray[0]["name_cp"].ToString(),
            //    NameES = jsonArray[0]["name_es"].ToString(),
            //    Id = Convert.ToInt64(jsonArray[0]["queue_id"].ToString()),
            //    ActiveFlag = JArray.Parse(jsonArray[0]["configOptions"].ToString())[0]["activeflag"].ToString(),
            //    LeadTimeMin = JArray.Parse(jsonArray[0]["configOptions"].ToString())[0]["lead_time_min"].ToString(),
            //    LeadTimeMax = JArray.Parse(jsonArray[0]["configOptions"].ToString())[0]["lead_time_max"].ToString(),
            //    EmpOnly = JArray.Parse(jsonArray[0]["configOptions"].ToString())[0]["emp_only"].ToString(),
            //    HideInKiosk = JArray.Parse(jsonArray[0]["configOptions"].ToString())[0]["hide_in_kiosk"].ToString(),
            //    HideInMonitor = JArray.Parse(jsonArray[0]["configOptions"].ToString())[0]["hide_in_monitor"].ToString(),
            //    Schedules = GetSchedules(jsonArray[0]["schedules"].ToString())
            //};

            return (model);

            //var usr = _Queues.Get(Queueid, _stampQueue);
            //return new QueueVM
            //{
            //    FirstName = user.FirstName,
            //    LastName = user.LastName,
            //    QueueId = user.QueueId,
            //    Title = user.Title,
            //    OtherLanguage = user.Language,
            //    IsActive =  user.ActiveFlag,
            //    IsAdmin = user.AdminFlag,
            //    Email = user.Email
            //};
            //return null;
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
        //    if (string.IsNullOrWhiteSpace(_stampQueue))
        //    {
        //        return new List<QueueVM>();
        //    }

        //    //var rows = _Queues.ListAll(_stampuserentity, _stampQueue);
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

    }
}
