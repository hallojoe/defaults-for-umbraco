using Umbraco.Cms.Core.Sync;

namespace Casko.DefaultsForUmbraco.Web.Sync;

public class SchedulingPublisherServerRoleAccessor : IServerRoleAccessor
{
    public ServerRole CurrentServerRole => ServerRole.SchedulingPublisher;
}