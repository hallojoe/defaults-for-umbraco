using Umbraco.Cms.Core.Sync;

namespace Casko.DefaultsForUmbraco.Common.Sync;

public class SingleServerRoleAccessor : IServerRoleAccessor
{
    public ServerRole CurrentServerRole => ServerRole.Single;
}