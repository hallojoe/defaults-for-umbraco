using Umbraco.Cms.Core.Sync;

namespace Casko.DefaultsForUmbraco.Web.Sync;

public class SubscriberServerRoleAccessor : IServerRoleAccessor
{
    public ServerRole CurrentServerRole => ServerRole.Subscriber;
}