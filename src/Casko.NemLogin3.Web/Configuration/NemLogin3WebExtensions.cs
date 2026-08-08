using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Casko.NemLogin3.Web.Controllers;
using Casko.NemLogin3.Web.Services;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.MvcCore;
using ITfoxtec.Identity.Saml2.MvcCore.Configuration;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;
using ITfoxtec.Identity.Saml2.Util;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;

namespace Casko.NemLogin3.Web.Configuration;

public static class NemLogin3WebExtensions
{
    public static IServiceCollection AddNemLogin3Web(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        IdentityModelEventSource.ShowPII = environment.IsDevelopment();

        services
            .AddOptions<NemLogin3Options>()
            .Bind(configuration.GetSection(NemLogin3Options.SectionName));

        services.ConfigureForwardedHeaders();
        services.ConfigureSaml2(configuration, environment);

        services.AddSaml2(slidingExpiration: true);
        services.AddNemLogin3HttpClient();
        services.AddScoped<INemLogin3MetadataService, NemLogin3MetadataService>();
        services.AddScoped<INemLogin3ClaimsTransformer, DefaultNemLogin3ClaimsTransformer>();
        services
            .AddControllersWithViews()
            .AddApplicationPart(typeof(AuthController).Assembly);

        return services;
    }

    public static WebApplication UseNemLogin3Web(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<NemLogin3Options>>().Value;
        if (options.UseForwardedHeaders)
        {
            app.UseForwardedHeaders();
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseSaml2();
        app.UseAuthorization();

        return app;
    }

    private static void ConfigureForwardedHeaders(this IServiceCollection services)
    {
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedHost |
                ForwardedHeaders.XForwardedProto;

            options.KnownProxies.Add(IPAddress.Loopback);
            options.KnownProxies.Add(IPAddress.IPv6Loopback);
        });
    }

    private static void ConfigureSaml2(this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.BindConfig<Saml2Configuration>(configuration, "Saml2", (_, saml2Configuration) =>
        {
            saml2Configuration.SigningCertificate = CreateSigningCertificate(configuration, environment, saml2Configuration);

            if (saml2Configuration.SigningCertificate.GetSamlPrivateKey(saml2Configuration.SignatureAlgorithm) is null)
            {
                throw new InvalidOperationException($"The SP signing certificate does not support the configured SignatureAlgorithm '{saml2Configuration.SignatureAlgorithm}'.");
            }

            saml2Configuration.AllowedAudienceUris.Add(saml2Configuration.Issuer);
            ConfigureIdentityProviderMetadata(configuration, environment, saml2Configuration);

            return saml2Configuration;
        });
    }

    private static void AddNemLogin3HttpClient(this IServiceCollection services)
    {
        services.AddHttpClient();

#if DEBUG
        services.AddHttpClient(Options.DefaultName)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            });
#endif
    }

    private static void ConfigureIdentityProviderMetadata(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        Saml2Configuration saml2Configuration)
    {
        var idPMetadataFile = configuration["Saml2:IdPMetadataFile"];
        if (string.IsNullOrWhiteSpace(idPMetadataFile))
        {
            throw new InvalidOperationException("Saml2:IdPMetadataFile is required.");
        }

        var entityDescriptor = new EntityDescriptor();
        entityDescriptor.ReadIdPSsoDescriptorFromFile(environment.MapToPhysicalFilePath(idPMetadataFile));

        if (entityDescriptor.IdPSsoDescriptor is null)
        {
            throw new InvalidOperationException($"IdPSsoDescriptor not loaded from metadata file '{idPMetadataFile}'.");
        }

        saml2Configuration.AllowedIssuer = entityDescriptor.EntityId;
        saml2Configuration.SingleSignOnDestination = entityDescriptor.IdPSsoDescriptor.SingleSignOnServices.First().Location;
        saml2Configuration.SingleLogoutDestination = entityDescriptor.IdPSsoDescriptor.SingleLogoutServices.First().Location;

        foreach (var signingCertificate in entityDescriptor.IdPSsoDescriptor.SigningCertificates)
        {
            if (signingCertificate.IsValidLocalTime())
            {
                saml2Configuration.SignatureValidationCertificates.Add(signingCertificate);
            }
        }

        if (saml2Configuration.SignatureValidationCertificates.Count <= 0)
        {
            throw new InvalidOperationException("The IdP signing certificates has expired.");
        }

        if (entityDescriptor.IdPSsoDescriptor.WantAuthnRequestsSigned.HasValue)
        {
            saml2Configuration.SignAuthnRequest = entityDescriptor.IdPSsoDescriptor.WantAuthnRequestsSigned.Value;
        }
    }

    private static X509Certificate2 CreateSigningCertificate(
        IConfiguration configuration,
        IWebHostEnvironment environment,
        Saml2Configuration saml2Configuration)
    {
        if (configuration.GetValue<bool>("Saml2:UseEcdsaSigningCertificate"))
        {
            return CreateEcdsaSigningCertificate(saml2Configuration);
        }

        var signingCertificateFile = configuration["Saml2:SigningCertificateFile"];
        if (string.IsNullOrWhiteSpace(signingCertificateFile))
        {
            throw new InvalidOperationException("Saml2:SigningCertificateFile is required.");
        }

        var signingCertificate = CertificateUtil.Load(
            environment.MapToPhysicalFilePath(signingCertificateFile),
            configuration["Saml2:SigningCertificatePassword"]);

        saml2Configuration.DecryptionCertificates.Add(signingCertificate);

        return signingCertificate;
    }

    private static X509Certificate2 CreateEcdsaSigningCertificate(Saml2Configuration saml2Configuration)
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest($"CN={saml2Configuration.Issuer}", ecdsa, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, critical: true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, critical: false));
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(365));
    }
}
