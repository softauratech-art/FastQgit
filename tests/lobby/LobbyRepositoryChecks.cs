using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FastQ.Data.Db;
using FastQ.Data.Entities;
using FastQ.Data.Repositories;

// Exercise the real display repository against the existing repository contracts.
public class RepositoryProxy : DispatchProxy
{
    public Func<string, object[], object> Call;
    protected override object Invoke(MethodInfo method, object[] args) => Call(method.Name, args);
    public static T Create<T>(Func<string, object[], object> call) where T : class
    {
        var proxy = DispatchProxy.Create<T, RepositoryProxy>();
        ((RepositoryProxy)(object)proxy).Call = call;
        return proxy;
    }
}

static class LobbyRepositoryChecks
{
    static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        Console.WriteLine("PASS: " + message);
    }
    public static void Run()
    {
        var day = new DateTime(2026, 11, 1); // 02:00 UTC Nov 2 is still Nov 1 Eastern.
        var now = new DateTime(2026, 11, 2, 2, 0, 0, DateTimeKind.Utc);
        var membership = new UserEntity { EntityId = 1 };
        var permissions = Enumerable.Range(1, 4).Select(i => new UserQueuePermission {
            EntityId = 1, QueueId = i, QueueActiveFlag = true, LobbyFlag = true }).ToList();
        permissions.Add(new UserQueuePermission { EntityId = 1, QueueId = 5, QueueActiveFlag = true, ReporterFlag = true });
        var account = new User { BusinessEntities = new List<UserEntity> { membership }, Queues = permissions };
        var allQueues = Enumerable.Range(1, 5).Select(i => new Queue {
            Id = i, EntityId = 1, Name = "Queue " + i, HideInMonitor = i == 3, ActiveFlag = i != 4 }).ToList();
        ProviderAppointmentData Row(long queue, string name, AppointmentStatus status, int hour = 10) =>
            new ProviderAppointmentData { QueueId = queue, CustomerName = name, Status = status,
                ScheduledFor = day.AddHours(hour), ScheduledStartTime = day.AddHours(hour) };
        var appts = new List<ProviderAppointmentData> {
            Row(1, "Later", AppointmentStatus.Arrived, 11),
            Row(1, "Serving", AppointmentStatus.InService),
            Row(1, "No time", AppointmentStatus.Arrived, 0),
            Row(1, "Done", AppointmentStatus.Completed),
            Row(1, "Cancelled", AppointmentStatus.Cancelled),
            Row(1, "Unchecked", AppointmentStatus.Scheduled),
            Row(3, "Hidden", AppointmentStatus.Arrived),
            Row(4, "Inactive", AppointmentStatus.Arrived),
            Row(5, "Reporter only", AppointmentStatus.Arrived)
        };
        appts[2].ScheduledStartTime = null;
        var tomorrow = Row(1, "Tomorrow", AppointmentStatus.Arrived);
        tomorrow.ScheduledFor = day.AddDays(1); appts.Add(tomorrow);
        var walkin = Row(1, "Walk-in", AppointmentStatus.Arrived, 15);
        walkin.CreatedOn = day.AddHours(9).AddMinutes(15);
        var reads = 0;
        var users = RepositoryProxy.Create<IUserRepository>((method, args) => {
            Check(method == "Get" && (string)args[0] == "kiosk" && (string)args[1] == "AUTHSERVICE", "Refresh uses existing user lookup");
            return account;
        });
        var queues = RepositoryProxy.Create<IQueueRepository>((method, args) => {
            Check(method == "ListByEntity" && (long)args[0] == 1 && (string)args[1] == "kiosk", "Queues use existing user-scoped lookup");
            return allQueues;
        });
        var appointments = RepositoryProxy.Create<IAppointmentRepository>((method, args) => {
            reads++;
            Check((DateTime)args[2] == day && (DateTime)args[3] == day,
                "Eastern date boundary and inclusive procedure date range");
            return method == "ListForUser" ? appts : new List<ProviderAppointmentData> { walkin };
        });
        var repo = new DbLobbyRepository(appointments, queues, users);
        var result = repo.Today("kiosk", 1, now);
        Check(reads == 2, "Display reuses appointment and walk-in reads");
        Check(result.Count == 5 && result.All(e => e.QueueId == 1 || e.QueueId == 2),
            "Only assigned active visible queues and arrived/in-service entries appear");
        Check(result.Any(e => e.QueueId == 2 && e.Name == null), "Empty assigned queue remains visible");
        Check(result.Single(e => e.Name == "Walk-in").Time == "09:15 AM", "Walk-in displays arrival time");
        Check(result.Single(e => e.Name == "No time").Time == "Not scheduled", "Missing appointment time stays unscheduled");
        Check(result.Single(e => e.Name == "Serving").Status == "IN PROGRESS", "Serving status preserved");
        Check(result.Take(4).Select(e => e.Name).SequenceEqual(new[] { "No time", "Walk-in", "Serving", "Later" }),
            "Appointments and walk-ins share chronological ordering");
        membership.ConfigAdminFlag = true;
        Check(repo.Today("kiosk", 1, now).Any(e => e.QueueId == 5), "Super admin can preview all visible active queues");
        membership.ConfigAdminFlag = false;
        void Denied(string reason)
        {
            int before = reads;
            try { repo.Today("kiosk", 1, now); throw new Exception("Expected denial: " + reason); }
            catch (UnauthorizedAccessException) { Check(before == reads, reason); }
        }
        account.ActiveFlag = false; Denied("Inactive account denied before reading appointments"); account.ActiveFlag = true;
        membership.ActiveFlag = false; Denied("Inactive entity membership denied"); membership.ActiveFlag = true;
        foreach (var p in permissions) p.LobbyFlag = false;
        Denied("Revoked Lobby denied immediately despite remaining Reporter role");
    }
}

namespace FastQ.Data.Db
{
    // Production factory is Oracle-backed; tests always inject the existing contracts.
    static class DbRepositoryFactory
    {
        public static IAppointmentRepository CreateAppointmentRepository() => throw new NotSupportedException();
        public static IQueueRepository CreateQueueRepository() => throw new NotSupportedException();
        public static IUserRepository CreateUserRepository() => throw new NotSupportedException();
    }
}
