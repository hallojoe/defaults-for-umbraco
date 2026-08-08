# Casko.NemLogin3.Web

Shared ASP.NET Core support for NemLog-in 3 SAML integration. This project owns the reusable SAML setup, metadata generation, standalone MVC endpoints, and NemLog-in claim constants used by both the standalone demo and Umbraco member login integration.

## Responsibility

- Configure `ITfoxtec.Identity.Saml2` from the `Saml2` and `NemLogin3` configuration sections.
- Load NemLog-in IdP metadata and SP signing/decryption certificate.
- Generate service provider metadata for NemLog-in registration.
- Provide standalone MVC login, callback, and logout endpoints.
- Normalize raw SAML claims into the common claim shape consumed by host applications.

This project should stay Umbraco-agnostic. Umbraco-specific authentication schemes, member auto-linking, member groups, and member profile persistence belong in `Casko.DefaultsForUmbraco.NemLogin3`.

## Main Entry Points

- `NemLogin3WebExtensions.AddNemLogin3Saml(...)`
  Registers reusable SAML configuration, metadata service, HTTP client, and claim transformer. Use this from wrapper packages that do not want the standalone MVC login/session behavior.

- `NemLogin3WebExtensions.AddNemLogin3Web(...)`
  Registers the shared SAML services plus standalone MVC controllers.

- `NemLogin3WebExtensions.UseNemLogin3Web(...)`
  Wires the standalone ASP.NET Core middleware pipeline, including forwarded headers, static files, routing, SAML session support, and authorization.

## Code Map

- `Configuration/NemLogin3Options.cs`
  Options for public SP URLs, endpoint paths, requested NSIS LoA, metadata contact details, and requested attributes.

- `Configuration/NemLogin3ClaimConstants.cs`
  NemLog-in/OIOSAML claim URI constants, including CPR UUID, full name, NSIS LoA, CVR, and organization name.

- `Configuration/NemLogin3WebExtensions.cs`
  DI and middleware extension methods. This is where certificate loading, IdP metadata reading, SAML destinations, accepted issuers, and signature validation certificates are configured.

- `Controllers/AuthController.cs`
  Standalone SAML login, assertion consumer service, and logout controller. It creates signed AuthnRequests, validates SAML responses, transforms claims, and creates a local ASP.NET session.

- `Controllers/MetadataController.cs`
  Standalone `/Metadata` endpoint.

- `Services/NemLogin3MetadataService.cs`
  Builds SP metadata, including ACS, SLO, signing/encryption certificates, NameID format, and requested attributes.

- `Services/DefaultNemLogin3ClaimsTransformer.cs`
  Default hook for normalizing SAML claims. Keep this generic; host-specific member/user mapping belongs outside this project.

## Configuration Shape

The host must provide:

```json
{
  "NemLogin3": {
    "PublicBaseUrl": "https://samlcasko0001.dev.localhost",
    "MetadataPath": "/Metadata",
    "LoginPath": "/Auth/Login",
    "AssertionConsumerServicePath": "/Auth/AssertionConsumerService",
    "RequestedAuthnContext": "https://data.gov.dk/concept/core/nsis/loa/Substantial"
  },
  "Saml2": {
    "IdPMetadataFile": "oiosaml3-idp-devtest4-inttest-25-11-26.xml",
    "Issuer": "https://samlcasko0001.dev.localhost",
    "SigningCertificateFile": "oces3_-test-_systemcertifikat.p12",
    "SigningCertificatePassword": "..."
  }
}
```

`PublicBaseUrl` and `Saml2:Issuer` must match the SP registration at NemLog-in. Hostnames are significant.

## Notes For Maintainers And Agents

- The standalone `AuthController` intentionally does not set `AssertionConsumerServiceURL` on the AuthnRequest. The registered SP metadata declares the ACS.
- `AddNemLogin3Saml(...)` is the reusable integration point. Prefer it over `AddNemLogin3Web(...)` when embedding this in another auth system.
- Do not add Umbraco dependencies here. Keep this package usable by non-Umbraco ASP.NET Core hosts.
- The metadata service signs nothing itself; it emits SP metadata using the configured SAML certificate and ITfoxtec metadata types.
- In DEBUG, the default HTTP client accepts any server certificate to support local dev metadata/certificate flows.

