using Umbraco.Cms.Core.Sync;

namespace Casko.DefaultsForUmbraco.Common.Sync;

public class SubscriberServerRoleAccessor : IServerRoleAccessor
{
    public ServerRole CurrentServerRole => ServerRole.Subscriber;
}