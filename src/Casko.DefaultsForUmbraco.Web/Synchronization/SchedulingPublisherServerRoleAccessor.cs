using Umbraco.Cms.Core.Sync;

namespace Casko.DefaultsForUmbraco.Web.Synchronization;

public class SchedulingPublisherServerRoleAccessor : IServerRoleAccessor
{
    public ServerRole CurrentServerRole => ServerRole.SchedulingPublisher;
}