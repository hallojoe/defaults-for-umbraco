using Umbraco.Cms.Core.Sync;

namespace Casko.DefaultsForUmbraco.Web.Sync;

public class SingleServerRoleAccessor : IServerRoleAccessor
{
    public ServerRole CurrentServerRole => ServerRole.Single;
}