using Umbraco.Cms.Core.Sync;

namespace Casko.DefaultsForUmbraco.Common.Sync;

public class SchedulingPublisherServerRoleAccessor : IServerRoleAccessor
{
    public ServerRole CurrentServerRole => ServerRole.SchedulingPublisher;
}