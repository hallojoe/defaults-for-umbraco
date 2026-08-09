using System.Text.Json;
using System.Security.Claims;
using Casko.DefaultsForUmbraco.NemLogin3.Models;
using Casko.DefaultsForUmbraco.NemLogin3.Security;
using Casko.DefaultsForUmbraco.NemLogin3.Services;
using Casko.NemLogin3.Web.Configuration;
using Casko.NemLogin3.Web.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.Cms.Api.Management.Security;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Web.Common.Security;
using Umbraco.Extensions;

namespace Casko.DefaultsForUmbraco.NemLogin3.Configuration;

public static class UmbracoBuilderExtensions
{
    public static IUmbracoBuilder AddNemLogin3BackOfficeLogin(
        this IUmbracoBuilder builder,
        Action<NemLogin3BackOfficeLoginOptions>? configure = null)
    {
        var environment = ResolveWebHostEnvironment(builder);
        return builder.AddNemLogin3BackOfficeLogin(environment, configure);
    }

    public static IUmbracoBuilder AddNemLogin3BackOfficeLogin(
        this IUmbracoBuilder builder,
        IWebHostEnvironment environment,
        Action<NemLogin3BackOfficeLoginOptions>? configure = null)
    {
        var loginOptions = new NemLogin3BackOfficeLoginOptions();
        ConfigureFromConfiguration(builder.Config, loginOptions);
        configure?.Invoke(loginOptions);

        builder.Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(loginOptions));
        builder.Services.PostConfigure<NemLogin3Options>(options =>
        {
            if (!string.Equals(options.AssertionConsumerServicePath, loginOptions.CallbackPath, StringComparison.OrdinalIgnoreCase)
                && !options.AdditionalAssertionConsumerServicePaths.Contains(loginOptions.CallbackPath, StringComparer.OrdinalIgnoreCase))
            {
                options.AdditionalAssertionConsumerServicePaths.Add(loginOptions.CallbackPath);
            }
        });
        builder.Services.AddNemLogin3UmbracoServices(builder.Config, environment);
        builder.Services.AddScoped<INemLogin3BackOfficeClaimsMapper, NemLogin3BackOfficeClaimsMapper>();
        builder.Services
            .AddControllersWithViews()
            .AddApplicationPart(typeof(UmbracoBuilderExtensions).Assembly);

        builder.AddBackOfficeExternalLogins(logins =>
        {
            logins.AddBackOfficeLogin(
                backOfficeAuthenticationBuilder =>
                {
                    var schemeName = BackOfficeAuthenticationBuilder.SchemeForBackOffice(loginOptions.SchemeName);
                    ArgumentNullException.ThrowIfNull(schemeName);

                    backOfficeAuthenticationBuilder.AddRemoteScheme<NemLogin3AuthenticationOptions, NemLogin3BackOfficeAuthenticationHandler>(
                        schemeName,
                        loginOptions.DisplayName,
                        options =>
                        {
                            options.CallbackPath = loginOptions.CallbackPath;
                            if (!string.IsNullOrWhiteSpace(loginOptions.CorrelationCookieDomain))
                            {
                                options.CorrelationCookie.Domain = loginOptions.CorrelationCookieDomain;
                            }
                        });
                },
                options =>
                {
                    options.AutoLinkOptions = new ExternalSignInAutoLinkOptions(
                        loginOptions.AutoLinkExternalAccount,
                        loginOptions.DefaultUserGroups.ToArray(),
                        loginOptions.DefaultCulture,
                        loginOptions.AllowManualLinking)
                    {
                        OnAutoLinking = (user, _) =>
                        {
                            user.IsApproved = loginOptions.DefaultIsApproved;
                        }
                    };
                });
        });

        return builder;
    }

    public static IUmbracoBuilder AddNemLogin3MemberLogin(
        this IUmbracoBuilder builder,
        Action<NemLogin3MemberLoginOptions>? configure = null)
    {
        var environment = ResolveWebHostEnvironment(builder);
        return builder.AddNemLogin3MemberLogin(environment, configure);
    }

    public static IUmbracoBuilder AddNemLogin3MemberLogin(
        this IUmbracoBuilder builder,
        IWebHostEnvironment environment,
        Action<NemLogin3MemberLoginOptions>? configure = null)
    {
        var loginOptions = new NemLogin3MemberLoginOptions();
        ConfigureFromConfiguration(builder.Config, loginOptions);
        configure?.Invoke(loginOptions);

        builder.Services.AddSingleton(Microsoft.Extensions.Options.Options.Create(loginOptions));
        builder.Services.AddNemLogin3UmbracoServices(builder.Config, environment);
        builder.Services.AddScoped<INemLogin3MemberClaimsMapper, NemLogin3MemberClaimsMapper>();
        builder.Services
            .AddControllersWithViews()
            .AddApplicationPart(typeof(UmbracoBuilderExtensions).Assembly);

        builder.AddMemberExternalLogins(logins =>
        {
            logins.AddMemberLogin(
                memberAuthenticationBuilder =>
                {
                    memberAuthenticationBuilder.AddRemoteScheme<NemLogin3AuthenticationOptions, NemLogin3AuthenticationHandler>(
                        memberAuthenticationBuilder.SchemeForMembers(loginOptions.SchemeName),
                        loginOptions.DisplayName,
                        options =>
                        {
                            options.CallbackPath = builder.Config["NemLogin3:AssertionConsumerServicePath"] ?? new NemLogin3Options().AssertionConsumerServicePath;
                            if (!string.IsNullOrWhiteSpace(loginOptions.CorrelationCookieDomain))
                            {
                                options.CorrelationCookie.Domain = loginOptions.CorrelationCookieDomain;
                            }
                        });
                },
                options =>
                {
                    options.AutoLinkOptions = new MemberExternalSignInAutoLinkOptions(
                        loginOptions.AutoLinkExternalAccount,
                        loginOptions.DefaultIsApproved,
                        string.IsNullOrWhiteSpace(loginOptions.DefaultMemberTypeAlias) ? Constants.Security.DefaultMemberTypeAlias : loginOptions.DefaultMemberTypeAlias,
                        loginOptions.DefaultCulture,
                        loginOptions.DefaultMemberGroups)
                    {
                        ExternalOnly = loginOptions.ExternalOnly,
                        OnAutoLinking = (member, loginInfo) =>
                        {
                            member.ProfileData = BuildProfileData(loginInfo);
                        },
                        OnExternalLogin = (member, loginInfo) =>
                        {
                            member.ProfileData = BuildProfileData(loginInfo);
                            return true;
                        }
                    };
                });
        });

        return builder;
    }

    private static IServiceCollection AddNemLogin3UmbracoServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        if (services.All(service => service.ServiceType != typeof(IDistributedCache)))
        {
            services.AddDistributedMemoryCache();
        }

        if (services.Any(service => service.ServiceType == typeof(INemLogin3MetadataService)))
        {
            return services;
        }

        services.AddNemLogin3Saml(configuration, environment);
        return services;
    }

    private static void ConfigureFromConfiguration(IConfiguration configuration, NemLogin3MemberLoginOptions options)
    {
        var section = configuration.GetSection(NemLogin3MemberLoginOptions.SectionName);
        options.SchemeName = ReadString(section, nameof(options.SchemeName), options.SchemeName);
        options.DisplayName = ReadString(section, nameof(options.DisplayName), options.DisplayName);
        options.SyntheticEmailDomain = ReadString(section, nameof(options.SyntheticEmailDomain), options.SyntheticEmailDomain);
        options.CorrelationCookieDomain = ReadNullableString(section, nameof(options.CorrelationCookieDomain), options.CorrelationCookieDomain);
        options.DefaultCulture = ReadNullableString(section, nameof(options.DefaultCulture), options.DefaultCulture);
        options.DefaultMemberTypeAlias = ReadString(section, nameof(options.DefaultMemberTypeAlias), options.DefaultMemberTypeAlias);
        options.AutoLinkExternalAccount = ReadBool(section, nameof(options.AutoLinkExternalAccount), options.AutoLinkExternalAccount);
        options.DefaultIsApproved = ReadBool(section, nameof(options.DefaultIsApproved), options.DefaultIsApproved);
        options.ExternalOnly = ReadBool(section, nameof(options.ExternalOnly), options.ExternalOnly);

        var groups = section
            .GetSection(nameof(options.DefaultMemberGroups))
            .GetChildren()
            .Select(child => child.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToList();

        if (groups.Count > 0)
        {
            options.DefaultMemberGroups = groups;
        }
    }

    private static void ConfigureFromConfiguration(IConfiguration configuration, NemLogin3BackOfficeLoginOptions options)
    {
        var section = configuration.GetSection(NemLogin3BackOfficeLoginOptions.SectionName);
        options.SchemeName = ReadString(section, nameof(options.SchemeName), options.SchemeName);
        options.DisplayName = ReadString(section, nameof(options.DisplayName), options.DisplayName);
        options.CallbackPath = ReadString(section, nameof(options.CallbackPath), options.CallbackPath);
        options.CorrelationCookieDomain = ReadNullableString(section, nameof(options.CorrelationCookieDomain), options.CorrelationCookieDomain);
        options.SyntheticEmailDomain = ReadString(section, nameof(options.SyntheticEmailDomain), options.SyntheticEmailDomain);
        options.DefaultCulture = ReadNullableString(section, nameof(options.DefaultCulture), options.DefaultCulture);
        options.AutoLinkExternalAccount = ReadBool(section, nameof(options.AutoLinkExternalAccount), options.AutoLinkExternalAccount);
        options.DefaultIsApproved = ReadBool(section, nameof(options.DefaultIsApproved), options.DefaultIsApproved);
        options.AllowManualLinking = ReadBool(section, nameof(options.AllowManualLinking), options.AllowManualLinking);

        var groups = section
            .GetSection(nameof(options.DefaultUserGroups))
            .GetChildren()
            .Select(child => child.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Cast<string>()
            .ToList();

        if (groups.Count > 0)
        {
            options.DefaultUserGroups = groups;
        }
    }

    private static string ReadString(IConfiguration section, string name, string fallback)
        => string.IsNullOrWhiteSpace(section[name]) ? fallback : section[name]!;

    private static string? ReadNullableString(IConfiguration section, string name, string? fallback)
        => string.IsNullOrWhiteSpace(section[name]) ? fallback : section[name];

    private static bool ReadBool(IConfiguration section, string name, bool fallback)
        => bool.TryParse(section[name], out var value) ? value : fallback;

    private static IWebHostEnvironment ResolveWebHostEnvironment(IUmbracoBuilder builder)
    {
        var environment = builder.Services
            .FirstOrDefault(service => service.ServiceType == typeof(IWebHostEnvironment))
            ?.ImplementationInstance as IWebHostEnvironment;

        if (environment is null)
        {
            throw new InvalidOperationException(
                $"Unable to resolve {nameof(IWebHostEnvironment)} from the Umbraco service collection. " +
                $"Call {nameof(AddNemLogin3MemberLogin)} with an explicit {nameof(IWebHostEnvironment)} instance instead.");
        }

        return environment;
    }

    private static string BuildProfileData(ExternalLoginInfo loginInfo)
    {
        var serializedProfile = loginInfo.Principal.FindFirstValue(NemLogin3MemberLoginConstants.ProfileDataClaimType);
        if (!string.IsNullOrWhiteSpace(serializedProfile))
        {
            return serializedProfile;
        }

        return JsonSerializer.Serialize(new NemLogin3MemberProfile
        {
            Email = loginInfo.Principal.FindFirstValue(ClaimTypes.Email),
            FullName = loginInfo.Principal.FindFirstValue(ClaimTypes.Name),
            CprUuid = loginInfo.Principal.FindFirstValue(ClaimTypes.NameIdentifier),
        }, JsonSerializerOptions.Web);
    }
}
