# NemLog-in 3 Links

Curated links for developers and AI agents working on the NemLog-in 3 SAML integration in this repository.

## NemLog-in

- [NemLog-in: Integrer et nyt it-system](https://www.nemlog-in.dk/integrer-og-administrer-it-systemet/login/integrer-et-nyt-it-system/)
  Step-by-step entry point for integrating a service provider with NemLog-in login.

- [NemLog-in: Miljoer i NemLog-in](https://www.nemlog-in.dk/om-nemlog-in/miljoer-i-nemlog-in/)
  Overview of production, integration test, and DevTest4/pre-production environments.

- [MitID Erhverv: Integration test environment](https://mitid-erhverv.dk/en/advanced-functionalities-in-mitid-erhverv/mitid-erhverv-integration-test-environment/)
  Test environment guidance, including references to metadata and log viewer information.

- [NemLog-in for tjenesteudbydere](https://tu.nemlog-in.dk/)
  Danish service-provider portal for NemLog-in guidance, administration, and connection material.

- [Integration with NemLog-in3 PDF](https://cms.nemlog-in.dk/media/jhmbnulm/integration-with-nemlog-in.pdf)
  Main technical integration document for NemLog-in3 OIOSAML interfaces.

- [Integrationstest ved tilslutning til NemLog-in PDF](https://tu.nemlog-in.dk/media/hebbfxsm/de120321-434c-4227-98ba-a95078b57245.pdf)
  Danish integration test guidance for service providers connecting to NemLog-in3.

- [Integration with NemLog-in for brokers PDF](https://cms.nemlog-in.dk/media/ojiiw1a1/integration-with-nemlog-in-for-brokers.pdf)
  Broker-oriented technical integration document. Useful when comparing direct SP integration with broker setups.

- [Integration with NemLog-in - Local IdP PDF](https://cms.nemlog-in.dk/media/3old3xom/integration-with-nemlog-in-local-idp.pdf)
  Technical document for Local IdP integration with NemLog-in3.

## OIOSAML And Danish Standards

- [Digitaliseringsstyrelsen: OIOSAML profiler](https://digst.dk/it-loesninger/standarder/oiosaml-profiler/)
  Main page for Danish OIOSAML profiles and profile documentation.

- [Digitaliseringsstyrelsen: Aeldre versioner af OIOSAML profiler](https://digst.dk/it-loesninger/standarder/oiosaml-profiler/aeldre-versioner-af-oiosaml-profiler/)
  Includes OIOSAML 3.0.3, the profile exposed by NemLog-in3 for service-provider and broker authentication.

- [OIOSAML Web SSO Profile 3.0.3 PDF](https://digst.dk/media/1gxag1pn/oiosaml-web-sso-profile-303.pdf)
  OIOSAML 3.0.3 Web SSO profile. Relevant for SAML AuthnRequest, metadata, attributes, NameID, and binding expectations.

- [OIOSAML 4.0.0 profile PDF](https://prodstoragehoeringspo.blob.core.windows.net/3ed45e0b-24a5-4418-8018-23139d94f6d6/OIOSAML%204.0.0%20profil%20-%20Endelig%20version%20med%20TC.pdf)
  Newer OIOSAML profile. Relevant when evaluating future NemLog-in/OIOSAML4 support.

- [OIO Identity Based Web Services](https://digst.dk/it-loesninger/standarder/oio-identity-based-web-services-oio-idws/)
  Related OIO IDWS standards for SAML identity tokens in service-to-service scenarios.

## SAML Specifications

- [OASIS SAML 2.0 metadata specification](https://docs.oasis-open.org/security/saml/v2.0/saml-metadata-2.0-os.pdf)
  Defines SAML metadata entities, roles, endpoints, certificates, and attribute-consuming services.

- [OASIS SAML 2.0 protocol schema](http://docs.oasis-open.org/security/saml/v2.0/saml-schema-protocol-2.0.xsd)
  XML schema for SAML protocol messages such as AuthnRequest and Response.

- [OASIS SAML 2.0 assertion schema](http://docs.oasis-open.org/security/saml/v2.0/saml-schema-assertion-2.0.xsd)
  XML schema for SAML assertions, subjects, conditions, and attributes.

## .NET And ITfoxtec

- [ITfoxtec.Identity.Saml2 GitHub repository](https://github.com/ITfoxtec/ITfoxtec.Identity.Saml2)
  Source repository for the SAML library used by `Casko.NemLogin3.Web`.

- [ITfoxtec.Identity.Saml2 NuGet package](https://www.nuget.org/packages/ITfoxtec.Identity.Saml2)
  Package metadata and current published versions.

- [FoxIDs: ITfoxtec Identity SAML 2.0 component](https://www.foxids.com/en-gb/components/identitysaml2)
  Product/documentation page describing supported SAML features.

- [FoxIDs: Connect to NemLog-in with SAML 2.0](https://www.foxids.com/en-gb/docs/auth-method-howto-saml-2.0-nemlogin)
  Practical NemLog-in SAML setup notes. Useful as an external implementation reference.

- [Digitaliseringsstyrelsen OIOSAML.Net GitHub repository](https://github.com/digst/OIOSAML.Net)
  Reference implementation and examples for OIOSAML/NemLog-in-style SAML integration.

## Umbraco Integration Context

- [Umbraco: External login providers](https://docs.umbraco.com/umbraco-cms/run-in-production/security/external-login-providers)
  Umbraco external login provider documentation used by the member provider wrapper.

- [Umbraco: Lightweight external members](https://docs.umbraco.com/umbraco-cms/run-in-production/security/lightweight-external-members)
  Explains `ExternalOnly` member behavior in Umbraco 17.4+.

