using FastQ.Data.Db;
using FastQ.Data.Entities;
using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Threading.Tasks;

namespace FastQ.Web.Hubs
{
    public class QueueHub : Hub
    {
        public Task JoinEntity(string entityId)
            => Groups.Add(Context.ConnectionId, $"ent:{entityId}");

        public Task JoinQueue(string queueId)
            => Groups.Add(Context.ConnectionId, $"queue:{queueId}");

        public Task JoinAppointment(string appointmentId)
            => Groups.Add(Context.ConnectionId, $"appt:{appointmentId}");

        public Task JoinNotificationQueues(long[] queueIds)
        {
            var requestedQueueIds = (queueIds ?? new long[0])
                .Where(id => id > 0)
                .Distinct()
                .Take(500)
                .ToList();
            if (requestedQueueIds.Count == 0)
                return Task.FromResult(0);

            var userId = ResolveUserId();
            if (string.IsNullOrWhiteSpace(userId))
                throw new HubException("A logged-in FastQ user is required for notifications.");

            var users = DbRepositoryFactory.CreateUserRepository();
            var user = users.Get(userId, "AUTHSERVICE");
            if (user == null || !user.ActiveFlag)
                throw new HubException("The FastQ user is not active.");

            var permissions = users.GetActionQueuePermissions(userId) ?? new List<UserQueuePermission>();
            var allowedQueueIds = new HashSet<long>(permissions
                .Where(p => p.QueueActiveFlag && (p.ProviderFlag || p.QueueAdminFlag))
                .Select(p => p.QueueId));

            var adminEntityIds = new HashSet<long>((user.BusinessEntities ?? new List<UserEntity>())
                .Where(e => e.ActiveFlag && e.ConfigAdminFlag)
                .Select(e => e.EntityId));
            if (adminEntityIds.Count > 0)
            {
                var queues = DbRepositoryFactory.CreateQueueRepository();
                foreach (var queueId in requestedQueueIds.Where(id => !allowedQueueIds.Contains(id)))
                {
                    var queue = queues.Get(queueId);
                    if (queue != null && queue.ActiveFlag && adminEntityIds.Contains(queue.EntityId))
                        allowedQueueIds.Add(queueId);
                }
            }

            var joins = requestedQueueIds
                .Where(allowedQueueIds.Contains)
                .Select(queueId => Groups.Add(Context.ConnectionId, $"notify:queue:{queueId}"));
            return Task.WhenAll(joins);
        }

        private string ResolveUserId()
        {
            if (HttpContext.Current?.Session?["fq_user"] is User sessionUser
                && !string.IsNullOrWhiteSpace(sessionUser.UserId))
                return sessionUser.UserId.Trim();

            var configuredDebugKey = ConfigurationManager.AppSettings["debugkey"] ?? string.Empty;
            var requestedDebugKey = Context.QueryString["debug"] ?? string.Empty;
            var requestedDebugUserId = Context.QueryString["debuguserid"] ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(configuredDebugKey)
                && string.Equals(requestedDebugKey, configuredDebugKey, StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(requestedDebugUserId)
                && Context.User != null
                && Context.User.IsInRole("ISS LDMS Team"))
                return requestedDebugUserId.Trim();

            var identityName = Context.User?.Identity?.Name ?? string.Empty;
            var slashIndex = identityName.IndexOf("\\", StringComparison.Ordinal);
            return (slashIndex >= 0 ? identityName.Substring(slashIndex + 1) : identityName).Trim();
        }
    }
}
