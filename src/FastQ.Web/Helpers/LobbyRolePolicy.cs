using System.Collections.Generic;
using System.Linq;
using FastQ.Data.Entities;

namespace FastQ.Web.Helpers
{
    public static class LobbyRolePolicy
    {
        // entityId 0 examines all active memberships before entity selection.
        public static bool IsLobbyOnly(User user, long entityId)
        {
            if (user == null || !user.ActiveFlag) return false;
            var entities = (user.BusinessEntities ?? new List<UserEntity>())
                .Where(e => e.ActiveFlag && (entityId == 0 || e.EntityId == entityId)).ToList();
            if (entities.Any(e => e.ConfigAdminFlag)) return false;
            var queues = (user.Queues ?? new List<UserQueuePermission>())
                .Where(q => q.QueueActiveFlag && entities.Any(e => e.EntityId == q.EntityId)).ToList();
            return queues.Any(q => q.LobbyFlag)
                && !queues.Any(q => q.HostFlag || q.ProviderFlag || q.ReporterFlag || q.QueueAdminFlag);
        }
    }
}
