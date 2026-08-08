# NemLog-in DevTest4 setup

This guide documents the local NemLog-in 3 demo setup in `src/Casko.DefaultsFor.NemLogin3.Web.UI` and the external DevTest4 steps needed before integration testing.

## External setup

Create a test service provider organization here:

```text
https://testportal.test-devtest4-nemlog-in.dk/TU
```

The test portal creates a service provider organization and a test user for the DevTest4 environment. The user is not a MitID simulator user. When signing in with this user, use the **Test login** tab.

The user has both a private identity and an organization identity. When creating or managing the IT-system, sign in with the organization profile.

Use the official NemLog-in DevTest4 documentation as the source of truth:

```text
https://www.nemlog-in.dk/om-nemlog-in/dokumentation-og-vejledninger/#devtest4
https://www.nemlog-in.dk/om-nemlog-in/miljoer-i-nemlog-in/devtest4-miljoet-tekniske-oplysninger/nyt-it-system-oprettes-i-devtest4-pre-produktionsmiljoet/
```

When the IT-system asks for service provider metadata, upload the metadata from:

```text
https://samlcasko0001.dev.localhost/Metadata
```

## Local website

The demo website project is:

```text
src/Casko.DefaultsFor.NemLogin3.Web.UI
```

The launch profile opens:

```text
https://samlcasko0001.dev.localhost/
```

The metadata endpoint is:

```text
https://samlcasko0001.dev.localhost/Metadata
```

The important settings are in `src/Casko.DefaultsFor.NemLogin3.Web.UI/appsettings.json`.

`NemLogin3` owns the reusable package settings:

| Setting | Purpose |
| ------- | ------- |
| `PublicBaseUrl` | Public SP base URL used in generated metadata, currently `https://samlcasko0001.dev.localhost`. |
| `RequestedAuthnContext` | NemLog-in NSIS LoA requested by `/Auth/Login`. |
| `UseForwardedHeaders` | Enables forwarded header middleware for proxied/local friendly-host setups. |
| `Metadata.ServiceName` | Service name emitted in the SP metadata. |
| `Metadata.Organization` | Organization metadata emitted in the SP metadata. |
| `Metadata.Contact` | Technical contact emitted in the SP metadata. |
| `Metadata.RequestedAttributes` | Requested private-profile attributes emitted in SP metadata. |

`Saml2` owns the ITfoxtec and certificate settings:

| Setting | Purpose |
| ------- | ------- |
| `Issuer` | The SP entity ID, currently `https://samlcasko0001.dev.localhost`. |
| `IdPMetadataFile` | Local DevTest4 INT IdP metadata file. |
| `SigningCertificateFile` | Local OCES3 test system certificate used for SP signing and encryption metadata. |
| `SigningCertificatePassword` | Password for the local test certificate. |

Current local files:

```text
oiosaml3-idp-devtest4-inttest-25-11-26.xml
oces3_-test-_systemcertifikat.p12
```

These files are test/dev assets for DevTest4. Do not treat the checked-in test password pattern as production secret handling.

## Metadata profile

The current metadata is for a private IT-system profile.

Requested attributes:

```text
https://data.gov.dk/model/core/specVersion
https://data.gov.dk/concept/core/nsis/loa
https://data.gov.dk/model/core/eid/cprUuid
https://data.gov.dk/model/core/eid/fullName
https://data.gov.dk/model/core/eid/professional/cvr
https://data.gov.dk/model/core/eid/professional/orgName
```

The metadata must not request:

```text
https://data.gov.dk/model/core/eid/cprNumber
https://data.gov.dk/model/core/eid/privilegesIntermediate
```

The metadata is intentionally unsigned, but includes signing and encryption key descriptors and uses persistent NameID format.

## Verify before upload

Build the project:

```powershell
dotnet build src/Casko.DefaultsFor.NemLogin3.Web.UI/Casko.DefaultsFor.NemLogin3.Web.UI.csproj --no-restore
```

Run the site:

```powershell
dotnet run --project src/Casko.DefaultsFor.NemLogin3.Web.UI
```

Open the metadata endpoint:

```text
https://samlcasko0001.dev.localhost/Metadata
```

Before uploading, confirm that the XML:

- has entity ID `https://samlcasko0001.dev.localhost`
- contains one `SPSSODescriptor`
- contains signing and encryption key descriptors
- uses persistent NameID format
- does not contain `cprNumber`
- does not contain `privilegesIntermediate`
