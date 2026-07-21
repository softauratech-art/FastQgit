using System;
using System.Collections.Generic;
using FastQ.Data.Entities;

namespace FastQ.Data.Repositories
{
    public interface IHolidayRepository
    {
        public Holiday Get(DateTime day);
        public IList<Holiday> ListAll();
        public void AddOrUpdateHoliday(Holiday oholiday, string stampuser);
        public void Delete(DateTime day, string stampuser);
    }
}