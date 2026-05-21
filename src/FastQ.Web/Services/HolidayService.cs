using System;
using System.Collections.Generic;
using System.Linq;
using FastQ.Data.Db;
using FastQ.Data.Repositories;
using FastQ.Web.Models.Admin;

namespace FastQ.Web.Services
{
    public class HolidayService
    {
        private readonly IHolidayRepository _Holidays;
        private readonly string _stampUser = new AuthService().GetLoggedInWindowsUser();
        public HolidayService()
           : this(
               DbRepositoryFactory.CreateHolidayRepository())
        {
        }
 
        public HolidayService(IHolidayRepository Holidays)
        {
            _Holidays = Holidays;
        }
        public IList<Data.Entities.Holiday> ListHolidays()
        {
            //return TransformToModelList();
            return _Holidays.ListAll();
        }

        public Data.Entities.Holiday GetHoliday(DateTime day)
        {
            try
            {
                var holiday = _Holidays.Get(day);
                return holiday;
            }
            catch
            {
                return null;
            }
        }

        public void AddOrUpdateHoliday(Data.Entities.Holiday hvm)
        {
            _Holidays.AddOrUpdateHoliday(hvm, _stampUser);
        }

        public void Delete(string day)
        {
            if (DateTime.TryParse(day, out var dt))
                _Holidays.Delete(dt,  _stampUser);
        }
    }
}
