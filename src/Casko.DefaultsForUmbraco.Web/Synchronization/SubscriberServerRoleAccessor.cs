using Umbraco.Cms.Core.Sync;

namespace Casko.DefaultsForUmbraco.Web.Synchronization;

public class SubscriberServerRoleAccessor : IServerRoleAccessor
{
    public ServerRole CurrentServerRole => ServerRole.Subscriber;
}