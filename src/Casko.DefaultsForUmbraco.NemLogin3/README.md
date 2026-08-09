# Casko.DefaultsForUmbraco.NemLogin3

Umbraco 17 member and backoffice external login provider for NemLog-in 3. This project adapts the reusable SAML functionality from `Casko.NemLogin3.Web` into an Umbraco member authentication scheme named `UmbracoMembers.NemLogin3` and a backoffice user authentication scheme named `Umbraco.NemLogin3`.

## Responsibility

- Register NemLog-in 3 as an Umbraco member external login provider.
- Register NemLog-in 3 as an Umbraco backoffice external login provider.
- Start SAML login from Umbraco's member external login flow.
- Start SAML login from Umbraco's backoffice external login flow.
- Validate the SAML callback and return an external authentication ticket to Umbraco.
- Map NemLog-in claims into the claims Umbraco needs for member auto-linking and backoffice user linking.
- Auto-link members with configured approval state, member type, member groups, and profile data.
- Allow existing backoffice users to manually link NemLog-in; backoffice auto-linking is disabled by default.
- Expose a metadata endpoint backed by `Casko.NemLogin3.Web`.

This project should contain Umbraco-specific behavior only. Low-level SAML configuration, claim constants, metadata generation, and standalone MVC login behavior belong in `Casko.NemLogin3.Web`.

## Main Entry Point

Use `AddNemLogin3MemberLogin(...)` and/or `AddNemLogin3BackOfficeLogin(...)` during Umbraco builder setup:

```csharp
var umbracoBuilder = builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddDeliveryApi()
    .AddComposers();

umbracoBuilder.AddNemLogin3MemberLogin(builder.Environment);
umbracoBuilder.AddNemLogin3BackOfficeLogin(builder.Environment);
```

The short overload resolves `IWebHostEnvironment` from the service collection, but passing it explicitly is preferred when available.

## Code Map

- `Configuration/UmbracoBuilderExtensions.cs`
  Registers shared NemLog-in SAML services, the Umbraco member external scheme, auto-link options, metadata controller application part, distributed cache for RelayState, and claim mapper.

- `Configuration/NemLogin3MemberLoginOptions.cs`
  Member-provider options read from `NemLogin3:Members`, including scheme name, display name, synthetic email domain, correlation cookie domain, member type alias, groups, approval, and `ExternalOnly`.

- `Configuration/NemLogin3BackOfficeLoginOptions.cs`
  Backoffice-provider options read from `NemLogin3:BackOffice`, including scheme name, display name, callback path, correlation cookie domain, synthetic email domain, auto-linking, approval state, manual linking, culture, and default user groups.

- `Security/NemLogin3AuthenticationHandler.cs`
  Shared SAML `RemoteAuthenticationHandler` base plus the member handler. It creates the signed AuthnRequest, stores Umbraco auth properties server-side in distributed cache, sends compact RelayState, validates SAML responses, maps claims, and returns the external auth ticket.

- `Security/NemLogin3BackOfficeAuthenticationHandler.cs`
  Backoffice handler that reuses the shared SAML flow and maps claims for Umbraco users.

- `Services/NemLogin3MemberClaimsMapper.cs`
  Requires `cprUuid`, maps it to `ClaimTypes.NameIdentifier`, maps full name, creates a synthetic email, and stores profile JSON for auto-link callbacks.

- `Services/NemLogin3BackOfficeClaimsMapper.cs`
  Requires `cprUuid`, maps it to `ClaimTypes.NameIdentifier`, maps full name to `ClaimTypes.Name`, maps a synthetic email/UPN for backoffice auto-linking, and preserves raw NemLog-in claims.

- `Controllers/NemLogin3MetadataController.cs`
  Umbraco-hosted `/Metadata` endpoint using `INemLogin3MetadataService`.

- `wwwroot/App_Plugins/CaskoNemLogin3/umbraco-package.json`
  Backoffice `authProvider` manifest for the login button and manual linking UI.

- `Models/NemLogin3MemberProfile.cs`
  Serializable profile payload persisted on auto-link and external login.

## Configuration Shape

The Umbraco host uses the shared `NemLogin3` and `Saml2` sections, plus member-specific options under `NemLogin3:Members`:

```json
{
  "NemLogin3": {
    "PublicBaseUrl": "https://samlcasko0001.dev.localhost",
    "AssertionConsumerServicePath": "/Auth/AssertionConsumerService",
    "Members": {
      "SchemeName": "NemLogin3",
      "DisplayName": "NemLog-in",
      "SyntheticEmailDomain": "nemlogin.local",
      "CorrelationCookieDomain": ".dev.localhost",
      "AutoLinkExternalAccount": true,
      "DefaultIsApproved": true,
      "ExternalOnly": false,
      "DefaultMemberTypeAlias": "Member",
      "DefaultMemberGroups": [ "NemLogin3" ]
    },
    "BackOffice": {
      "SchemeName": "NemLogin3",
      "DisplayName": "NemLog-in",
      "CallbackPath": "/Auth/AssertionConsumerService",
      "CorrelationCookieDomain": ".dev.localhost",
      "SyntheticEmailDomain": "nemlogin.local",
      "AutoLinkExternalAccount": false,
      "DefaultIsApproved": false,
      "AllowManualLinking": true,
      "DefaultUserGroups": []
    }
  }
}
```

`ExternalOnly=false` creates normal Umbraco members with the configured member type. `ExternalOnly=true` creates lightweight external members, which are not edited like regular member-content records in the backoffice.

## Login Flow

1. A protected page or login partial posts provider `UmbracoMembers.NemLogin3` to Umbraco's member external login controller.
2. Umbraco challenges the registered remote scheme.
3. `NemLogin3AuthenticationHandler` creates a signed SAML AuthnRequest with persistent NameID and requested NSIS LoA.
4. The handler stores Umbraco authentication properties in distributed cache for 15 minutes and sends only a short `ReturnUrl=<state-id>` RelayState to NemLog-in.
5. NemLog-in posts a SAMLResponse to the configured callback path.
6. The handler validates the SAMLResponse with ITfoxtec, restores the Umbraco auth properties, validates correlation, maps claims, and returns an external auth ticket.
7. Umbraco auto-links or signs in the member.

## Notes For Maintainers And Agents

- The public scheme name configured here is `NemLogin3`; Umbraco's member scheme becomes `UmbracoMembers.NemLogin3`.
- The public backoffice scheme name configured here is `NemLogin3`; Umbraco's backoffice provider name becomes `Umbraco.NemLogin3`.
- `cprUuid` is the stable external provider key and is required. Missing `cprUuid` should fail login rather than create a weak link.
- The synthetic member and backoffice emails are derived from CPR UUID and `SyntheticEmailDomain`; they are not real email addresses.
- Backoffice auto-linking and approval both default to `false`. Set `AutoLinkExternalAccount=true` and `DefaultIsApproved=true` only when users created from NemLog-in should be able to sign in immediately.
- Do not add standalone MVC session middleware from `Casko.NemLogin3.Web` here. This package uses `AddNemLogin3Saml(...)` and lets Umbraco own member sessions.
- DevTest4 only permits one `AssertionConsumerService` per SP registration. For two IT-systems, member and backoffice use the same ACS path on different hosts. For one IT-system, member and backoffice share one ACS URL.
- RelayState is stored in `IDistributedCache`. CM/CD environments should use the configured distributed SQL cache; single-node development falls back to distributed-memory cache.
- The Umbraco AuthnRequest sets `AssertionConsumerServiceURL` from the active scheme callback path. With the default single-ACS setup, both member and backoffice requests point to `/Auth/AssertionConsumerService`.
- RelayState is intentionally compact. Avoid putting protected Umbraco auth properties directly into RelayState.
