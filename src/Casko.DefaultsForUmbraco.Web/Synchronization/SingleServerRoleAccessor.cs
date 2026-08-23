using Umbraco.Cms.Core.Sync;

namespace Casko.DefaultsForUmbraco.Web.Synchronization;

public class SingleServerRoleAccessor : IServerRoleAccessor
{
    public ServerRole CurrentServerRole => ServerRole.Single;
}