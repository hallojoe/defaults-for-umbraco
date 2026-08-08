# Casko.DefaultsForUmbraco.NemLogin3

Umbraco 17 member external login provider for NemLog-in 3. This project adapts the reusable SAML functionality from `Casko.NemLogin3.Web` into an Umbraco member authentication scheme named `UmbracoMembers.NemLogin3`.

## Responsibility

- Register NemLog-in 3 as an Umbraco member external login provider.
- Start SAML login from Umbraco's member external login flow.
- Validate the SAML callback and return an external authentication ticket to Umbraco.
- Map NemLog-in claims into the claims Umbraco needs for member auto-linking.
- Auto-link members with configured approval state, member type, member groups, and profile data.
- Expose a metadata endpoint backed by `Casko.NemLogin3.Web`.

This project should contain Umbraco-specific behavior only. Low-level SAML configuration, claim constants, metadata generation, and standalone MVC login behavior belong in `Casko.NemLogin3.Web`.

## Main Entry Point

Use `AddNemLogin3MemberLogin(...)` during Umbraco builder setup:

```csharp
var umbracoBuilder = builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddDeliveryApi()
    .AddComposers();

umbracoBuilder.AddNemLogin3MemberLogin(builder.Environment);
```

The short overload resolves `IWebHostEnvironment` from the service collection, but passing it explicitly is preferred when available.

## Code Map

- `Configuration/UmbracoBuilderExtensions.cs`
  Registers shared NemLog-in SAML services, the Umbraco member external scheme, auto-link options, metadata controller application part, memory cache for RelayState, and claim mapper.

- `Configuration/NemLogin3MemberLoginOptions.cs`
  Member-provider options read from `NemLogin3:Members`, including scheme name, display name, synthetic email domain, correlation cookie domain, member type alias, groups, approval, and `ExternalOnly`.

- `Security/NemLogin3AuthenticationHandler.cs`
  Custom `RemoteAuthenticationHandler` for SAML. It creates the signed AuthnRequest, stores Umbraco auth properties server-side, sends compact RelayState, validates SAML responses, maps claims, and returns the external auth ticket.

- `Services/NemLogin3MemberClaimsMapper.cs`
  Requires `cprUuid`, maps it to `ClaimTypes.NameIdentifier`, maps full name, creates a synthetic email, and stores profile JSON for auto-link callbacks.

- `Controllers/NemLogin3MetadataController.cs`
  Umbraco-hosted `/Metadata` endpoint using `INemLogin3MetadataService`.

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
    }
  }
}
```

`ExternalOnly=false` creates normal Umbraco members with the configured member type. `ExternalOnly=true` creates lightweight external members, which are not edited like regular member-content records in the backoffice.

## Login Flow

1. A protected page or login partial posts provider `UmbracoMembers.NemLogin3` to Umbraco's member external login controller.
2. Umbraco challenges the registered remote scheme.
3. `NemLogin3AuthenticationHandler` creates a signed SAML AuthnRequest with persistent NameID and requested NSIS LoA.
4. The handler stores Umbraco authentication properties in memory for 15 minutes and sends only a short `ReturnUrl=<state-id>` RelayState to NemLog-in.
5. NemLog-in posts a SAMLResponse to the configured callback path.
6. The handler validates the SAMLResponse with ITfoxtec, restores the Umbraco auth properties, validates correlation, maps claims, and returns an external auth ticket.
7. Umbraco auto-links or signs in the member.

## Notes For Maintainers And Agents

- The public scheme name configured here is `NemLogin3`; Umbraco's member scheme becomes `UmbracoMembers.NemLogin3`.
- `cprUuid` is the stable external provider key and is required. Missing `cprUuid` should fail login rather than create a weak link.
- The synthetic email is derived from CPR UUID and `SyntheticEmailDomain`; it is not a real email address.
- Do not add standalone MVC session middleware from `Casko.NemLogin3.Web` here. This package uses `AddNemLogin3Saml(...)` and lets Umbraco own member sessions.
- The AuthnRequest intentionally omits explicit `AssertionConsumerServiceURL`, matching the working standalone NemLog-in integration and relying on registered SP metadata.
- RelayState is intentionally compact. Avoid putting protected Umbraco auth properties directly into RelayState.

