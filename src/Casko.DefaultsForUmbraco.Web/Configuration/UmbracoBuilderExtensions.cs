using Casko.DefaultsForUmbraco.Web.Sync;
using Umbraco.Cms.Infrastructure.DependencyInjection;

namespace Casko.DefaultsForUmbraco.Web.Configuration;

public static class ServerRolesConfigurationExtensions
{
    public static IUmbracoBuilder AddServerRole(this IUmbracoBuilder builder, string serverRole)
    {
        if (IsServerRole(serverRole, CommonConstants.SubscriberServerRoleName))
        {
            builder.SetServerRegistrar(new SubscriberServerRoleAccessor());
            
            return builder;
        }

        if (IsServerRole(serverRole, CommonConstants.SchedulingPublisherServerRoleName))
        {
            builder.SetServerRegistrar(new SchedulingPublisherServerRoleAccessor());
        
            return builder;
        }
        
        builder.SetServerRegistrar(new SingleServerRoleAccessor());

        return builder;
    }

    private static bool IsServerRole(string claimedServerRole, string serverRole)
        => string.Equals(claimedServerRole, serverRole, StringComparison.OrdinalIgnoreCase);
}