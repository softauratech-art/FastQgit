using System;
using System.Collections.Generic;
using FastQ.Data.Entities;

namespace FastQ.Data.Repositories
{
    public interface IQueueRepository
    {
        Queue Get(long id);
        Queue GetQueueDetails(long id);
        //void Add(Queue queue);
        //void Update(Queue queue);

        long AddOrUpdateQueue(Queue oqueue, string stampuser);
        void Delete(long id, string stampuser);
        IList<Queue> ListByLocation(long entityid);
        //IList<Queue> ListAll();
        IList<Tuple<long, string>> ListServicesByQueue(long queueId);
        IList<Queue> ListByEntity(long? entityid, string stampuser);
        //IList<Queue> ListAll(string stampuser);
        Tuple<string, string, string> GetQueueDetailsJson(long queueId);
        QService GetQService(long id, string stampuser);
        void AddOrUpdateQService(QService qservice, string stampuser);
        void DeleteQService(long serviceid, string stampuser);

        QSchedule GetQSchedule(long id, string stampuser);
        void AddOrUpdateQSchedule(QSchedule qschedule, string stampuser);
        void DeleteQSchedule(long serviceid, string stampuser);

    }
}