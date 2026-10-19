# C# Client SDK Generation from OpenAPI — Research Findings (ticket #14)

Wayfinder research ticket #14 (`Wayfinder R2 — Research: генерация клиентского SDK (C#)`), part of map #4.
Facts only — no decisions. Snapshot date: **2026-10-19**. Every fact carries a source URL.

**Methodology note.** `learn.microsoft.com` was unreachable from the research environment (proxy-level 403 for that domain), so the Microsoft Kiota documentation content was read from Microsoft's own public source repo [`MicrosoftDocs/openapi-docs`](https://github.com/MicrosoftDocs/openapi-docs) (`OpenAPI/kiota/`, the build source of the `learn.microsoft.com/en-us/openapi/kiota/*` pages). Citations below give the canonical Learn URL; the same text exists in the source repo at the matching path (e.g. `OpenAPI/kiota/dependencies.md`). All version numbers were additionally verified against the GitHub Releases API and the NuGet v3 flat-container API.

## Context: what the spec pipeline emits

- The TypeSpec OpenAPI3 emitter is a "library for emitting OpenAPI 3.0 and OpenAPI 3.1"; its `openapi-versions` option type is `"3.0.0" | "3.1.0" | "3.2.0"` with **default `["3.0.0"]`**. <https://github.com/microsoft/typespec/blob/main/packages/openapi3/README.md>
- Consequence relevant to all three tools: OpenAPI **3.0.x input is the safe floor**; 3.1/3.2 support is the differentiator (tables below).

---

## 1. Microsoft Kiota

Docs index: <https://learn.microsoft.com/en-us/openapi/kiota/> · repo: <https://github.com/microsoft/kiota> · runtime libs: <https://github.com/microsoft/kiota-dotnet>

### Version, maturity, activity (2025–2026)

- Latest CLI release **v1.36.1**, published 2026-10-12. <https://github.com/microsoft/kiota/releases/tag/v1.36.1>
- Release policy: SemVer; **minor releases targeted for the first Tuesday of every month**; major releases "as needed"; only the latest major is maintained. <https://learn.microsoft.com/en-us/openapi/kiota/support> (source: <https://github.com/MicrosoftDocs/openapi-docs/blob/main/OpenAPI/kiota/support.md>)
- C# is listed as **stable ("✔") for generation, abstractions, all four serializers, auth, and HTTP** in the project README language table (legend: ✔ = stable). <https://github.com/microsoft/kiota#supported-languages>
- "Stable" is defined as "Kiota provides full functionality for the language and is used to generate production API clients"; primary support channel is GitHub issues. <https://learn.microsoft.com/en-us/openapi/kiota/support>
- Compatibility model: upgrading the tool never changes existing generated code — clients must be regenerated; app rebuild/testing against new tool versions is recommended. <https://learn.microsoft.com/en-us/openapi/kiota/support>

### Supported OpenAPI versions

- Descriptions are parsed with the `Microsoft.OpenApi.NET` library, which "can accept OpenAPI V2 or V3, in JSON or YAML" — string, stream, or text reader. <https://learn.microsoft.com/en-us/openapi/kiota/design>
- Changelog (tool): **"Added support for OpenAPI 3.1"** in **1.24.0 (2025-03-12)**; **"Added support for OpenAPI 3.2.0"** in **1.30.0 (2026-01-26)**; model generation for **3.1 webhooks** in 1.35.0 (2026-09-01). <https://github.com/microsoft/kiota/blob/main/CHANGELOG.md>
- Recent 3.1-related fixes specifically mention outputs "emitted by ASP.NET Core's OpenAPI 3.1 generator" and "ASP.NET Core 10's OpenAPI 3.1 generator" (nullable-`oneOf`, numeric unions). <https://github.com/microsoft/kiota/blob/main/CHANGELOG.md>

### Runtime packages it adds

Per the dependencies doc (C# column): <https://learn.microsoft.com/en-us/openapi/kiota/dependencies>

| Role | Package | Relies on |
|---|---|---|
| Abstractions (required) | `Microsoft.Kiota.Abstractions` | — |
| JSON serialization | `Microsoft.Kiota.Serialization.Json` | **System.Text.Json** |
| Text / Form / Multipart | `Microsoft.Kiota.Serialization.Text` / `.Form` / `.Multipart` | — |
| HTTP request adapter | `Microsoft.Kiota.Http.HttpClientLibrary` | **System.Net.Http** |
| Azure/Entra auth (optional) | `Microsoft.Kiota.Authentication.Azure` | Azure.Identity |
| **Bundle** (one reference covering abstractions + all serializers + HTTP) | `Microsoft.Kiota.Bundle` | — |

- Quickstart/DI tutorials instruct adding exactly one reference: `dotnet add package Microsoft.Kiota.Bundle` (+ `Microsoft.Extensions.Http` for DI). <https://learn.microsoft.com/en-us/openapi/kiota/quickstarts/dotnet>, <https://learn.microsoft.com/en-us/openapi/kiota/tutorials/dotnet-dependency-injection>
- `Microsoft.Kiota.Bundle` **2.1.2** nuspec = Abstractions + Http.HttpClientLibrary + Serialization.{Json,Form,Text,Multipart} 2.1.2. <https://api.nuget.org/v3-flatcontainer/microsoft.kiota.bundle/2.1.2/microsoft.kiota.bundle.nuspec> (NuGet index: <https://www.nuget.org/packages/Microsoft.Kiota.Bundle>)
- The HTTP package's nuspec dependencies for net8.0/net10.0 are only `Microsoft.Kiota.Abstractions` + `Microsoft.Extensions.DependencyInjection.Abstractions` — **no Polly, no RestSharp, no Newtonsoft**. <https://api.nuget.org/v3-flatcontainer/microsoft.kiota.http.httpclientlibrary/2.1.2/microsoft.kiota.http.httpclientlibrary.nuspec>
- Retry/redirect/etc. behavior is implemented as **`DelegatingHandler` middleware** built into the request adapter ("existing middleware already implemented within the HTTP library that includes existing handlers like retry, redirect, and more"; `KiotaClientFactory.CreateDefaultHandlers()`). <https://learn.microsoft.com/en-us/openapi/kiota/middleware>

### Generated-code shape

- Typed client class (`-c PostsClient`) + **request-builder classes per path segment**: the URL is constructed by navigating a hierarchy ("navigating the hierarchy of request builders, aided by the editor's autocomplete"), RFC6570 URL templates, typed path/query parameters and header setters; builders are generated into sub-namespaces mirroring the API path. Example: `await todoClient.ToDos["<todoId>"].GetAsync()`. <https://learn.microsoft.com/en-us/openapi/kiota/request-builders>
- Everything is async (`…Async` methods on the adapter interface `IRequestAdapter.SendAsync/SendCollectionAsync/SendPrimitiveAsync/SendNoContentAsync`). <https://learn.microsoft.com/en-us/openapi/kiota/abstractions>
- Models: generated classes per schema/component ("class/interface" outcomes; components land in a `models` sub-namespace); `allOf` maps to inheritance. <https://learn.microsoft.com/en-us/openapi/kiota/models>
- Status-code → return-type mapping is documented (200/2XX with schema → model class; 204/205 → void; schema-less 200/203/206 → stream), plus runtime handling of undescribed codes. <https://learn.microsoft.com/en-us/openapi/kiota/status-codes>
- Error responses (400–600, 4XX/5XX with object schema) become generated exception types inheriting the abstractions' base API exception; `allOf` error schemas are flattened. <https://learn.microsoft.com/en-us/openapi/kiota/errors>
- Size model: "Generates only the source code necessary by building on a core library" + `--include-path/--exclude-path` to generate for subsets of the description. <https://learn.microsoft.com/en-us/openapi/kiota/overview>, <https://learn.microsoft.com/en-us/openapi/kiota/using>

### Auth model

- Contract: `IAuthenticationProvider.AuthenticateRequestAsync(RequestInformation…)`; bearer via `BaseBearerTokenAuthenticationProvider` + user-supplied `IAccessTokenProvider`; built-in providers: **Anonymous**, **API key** (header or query parameter; "does not perform any encoding of the key or the value"), **Entra ID** (Azure package). <https://learn.microsoft.com/en-us/openapi/kiota/authentication>
- The auth provider is a required constructor parameter of the default request adapter — deliberate design to force an explicit choice. <https://learn.microsoft.com/en-us/openapi/kiota/authentication>
- `HttpClientRequestAdapter(IAuthenticationProvider, IParseNodeFactory? …, HttpClient? httpClient = null, …)` — an externally created/injected `HttpClient` is supported; `KiotaClientFactory.Create(...)` builds the default handler chain. <https://github.com/microsoft/kiota-dotnet/blob/main/src/http/httpClient/HttpClientRequestAdapter.cs>, <https://github.com/microsoft/kiota-dotnet/blob/main/src/http/httpClient/KiotaClientFactory.cs>

### Invocation: CLI / dotnet tool / MSBuild

- **`.NET` global tool**: `dotnet tool install --global Microsoft.OpenApi.Kiota` → `kiota` command. No Java/Node needed. Also: standalone binaries per OS/RID, **Docker** `mcr.microsoft.com/openapi/kiota`, **GitHub Action** `microsoft/setup-kiota` (preview) e.g. `kiota update -o .`, VS Code extension (preview), and an *unofficial* Visual Studio extension ("REST API Client Code Generator"). <https://learn.microsoft.com/en-us/openapi/kiota/install>
- Commands (using.md): `kiota search/download/show/generate/info/update/login/logout`. Generation example: `kiota generate -l CSharp -c PostsClient -n KiotaPosts.Client -d ./posts-api.yml -o ./Client`. <https://learn.microsoft.com/en-us/openapi/kiota/using>, <https://learn.microsoft.com/en-us/openapi/kiota/quickstarts/dotnet>
- A **`kiota-lock.json`** is written into the output directory alongside sources: all generation parameters + description hash; later `kiota generate/update` skip work when nothing changed; "the lock file is meant to be committed to the source control with the generated sources". `kiota info` prints the recommended dependency versions. <https://learn.microsoft.com/en-us/openapi/kiota/using>
- **No official MSBuild task** in the docs; build-pipeline integration is documented via the CLI/GitHub Action (the VS extension is community-maintained and flagged UNOFFICIAL). <https://learn.microsoft.com/en-us/openapi/kiota/install>

### .NET target support (incl. .NET 10)

- .NET quickstart requires **.NET SDK 10.0** and lists valid `TargetFramework`s: netstandard2.0/2.1, net462, **net8.0 or later**; LangVersion ≥ 7.3; Nullable enable recommended. <https://learn.microsoft.com/en-us/openapi/kiota/quickstarts/dotnet>
- Runtime libs v2.0.0 (2026-05-06): **breaking change "net5.0 and net6.0 TFMs dropped; net8.0 and net10.0 added"**; `Microsoft.Kiota.Http.HttpClientLibrary.csproj` on main targets `netstandard2.0;netstandard2.1;net8.0;net10.0;net462;…`. <https://github.com/microsoft/kiota-dotnet/blob/main/CHANGELOG.md>, <https://github.com/microsoft/kiota-dotnet/blob/main/src/http/httpClient/Microsoft.Kiota.Http.HttpClientLibrary.csproj>
- NuGet latest (flat container, 2026-10-19): Abstractions/Bundle/Serialization.Json **2.1.2**; kiota-dotnet CHANGELOG lists 2.1.3–2.1.5 through 2026-10-13. <https://api.nuget.org/v3-flatcontainer/microsoft.kiota.abstractions/index.json>, <https://github.com/microsoft/kiota-dotnet/blob/main/CHANGELOG.md>

### DI / HttpClient fit in Blazor Server

- Official DI tutorial pattern: `builder.Services.AddKiotaHandlers(); … AddHttpClient<GitHubClientFactory>(…).AttachKiotaHandlers(); AddTransient(sp => factory.GetClient())` — a client **factory** registered with `AddHttpClient`, client resolved from DI, `IAuthenticationProvider` created in the factory. Full source in the public samples repo. <https://learn.microsoft.com/en-us/openapi/kiota/tutorials/dotnet-dependency-injection>, <https://github.com/microsoft/kiota-samples/blob/main/get-started/dotnet-dependency-injection/Program.cs>
- Blazor Server facts from ASP.NET Core docs: `HttpClient` is registered **Scoped, not singleton** in Blazor apps; server-side "an HttpClient isn't configured as a service by default"; in interactive server-side apps **the DI scope lasts for the duration of the circuit**, so scoped/transient disposables can live much longer than a component — component-lifetime scoping via `OwningComponentBase` is the documented remedy. <https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/dependency-injection> (source: <https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/blazor/fundamentals/dependency-injection.md>)
- Consequence recorded as fact (not a decision): a per-user/per-circuit API client in Blazor Server is obtained by registering the factory + client as scoped/transient and letting the adapter receive the DI-provided `HttpClient`.

### Common pitfalls (documented)

- By default Kiota **emits extra obsolete backward-compatibility members**; quickstart recommends `--exclude-backward-compatible` "if you want to reduce the size of the generated client" (trade-off: potential source breaks on update). <https://learn.microsoft.com/en-us/openapi/kiota/quickstarts/dotnet>
- Generated code does not roll forward automatically with tool updates — regeneration required (support page). <https://learn.microsoft.com/en-us/openapi/kiota/support>
- API-key provider performs **no encoding** (e.g. manual base64 for Basic) — documented in the auth page. <https://learn.microsoft.com/en-us/openapi/kiota/authentication>
- Redirect handler scrubs only `Authorization`/`Cookie`/`Proxy-Authorization` on cross-origin redirects; **custom API-key headers must be scrubbed by the consumer**. <https://learn.microsoft.com/en-us/openapi/kiota/middleware>
- Tool version (1.x) and runtime-lib version (2.x) are **separate version lines**; keep them aligned via `kiota info`/lock file. <https://learn.microsoft.com/en-us/openapi/kiota/using>

---

## 2. NSwag

Docs: <https://github.com/RicoSuter/NSwag/wiki> · site: <https://nswag.org/> · repo: <https://github.com/RicoSuter/NSwag>

### Version, maturity, activity (2025–2026)

- Latest release **v14.7.1 (2026-04-20)**; prior: 14.7.0 (2026-04-08), 14.6.3 (2025-11-20), 14.6.1/14.6.0 (2025-09), 14.5.0 (2025-08), 14.4.0 (2025-04), 14.3.0 (2025-03) — i.e. regularly released through 2025, most recent release ~6 months before the snapshot date. <https://github.com/RicoSuter/NSwag/releases>
- Project positioning (README): "NSwag is a Swagger/OpenAPI **2.0 and 3.0** toolchain for .NET, .NET Core, Web API, ASP.NET Core, TypeScript… written in C#"; "combines the functionality of Swashbuckle (spec generation) and AutoRest (client generation)"; maintained by Rico Suter + contributors. <https://github.com/RicoSuter/NSwag#readme>
- v14.7.1 is explicitly a **correctness patch**: v14.7.0 bundled an NJsonSchema regression that "silently dropped nullability on `required T?` / `[JsonRequired] T?` DTO properties", so generated TS/C# clients lost null-safety; release notes say "Upgrade directly from v14.6.3 to v14.7.1 — skip v14.7.0". <https://github.com/RicoSuter/NSwag/releases/tag/v14.7.1>

### C# client (proxy) generation

- Generator: `CSharpClientGenerator` (package `NSwag.CodeGeneration.CSharp`) — emits **one client class + DTO classes from the document** ("POCOs or classes implementing INotifyPropertyChanged"; wiki sample method shape `Task MyOperationAsync()` with `ConfigureAwait(false)`). <https://github.com/RicoSuter/NSwag/wiki/CSharpClientGenerator>
- Client is a partial class with `partial void PrepareRequest/ProcessResponse` extension hooks, optional `ClientBaseClass`/`ConfigurationClass`, optional generated interfaces (`NSwagGenerateClientInterfaces`), `GenerateSyncMethods`, exception classes settings. <https://github.com/RicoSuter/NSwag/wiki/CSharpClientGenerator>, <https://github.com/RicoSuter/NSwag/blob/master/src/NSwag.ApiDescription.Client/NSwag.ApiDescription.Client.props>
- **HttpClient injection is the default**: `InjectHttpClient = true` (default in `CSharpClientGeneratorSettings`), `DisposeHttpClient` semantics, overridable `HttpClientType`; "Provide base path via HttpClient: set InjectHttpClient to true, UseBaseUrl to false … place a slash at the end of the BaseAddress". This makes the standard MS typed-client pattern (`AddHttpClient<IMyApi, MyApiClient>()`) directly applicable. <https://github.com/RicoSuter/NSwag/blob/master/src/NSwag.CodeGeneration.CSharp/CSharpClientGeneratorSettings.cs>, <https://github.com/RicoSuter/NSwag/wiki/CSharpClientGenerator>

### Runtime deps (Polly-less vs HttpClient)

- Wiki "Required dependencies for the client" (.NET Standard 1.4+ target): **Newtonsoft.Json, System.Net.Http, System.ComponentModel.Annotations** — **no Polly** in the list. <https://github.com/RicoSuter/NSwag/wiki/CSharpClientGenerator>
- Serializer: NJsonSchema's `CSharpGeneratorSettings` defaults `JsonLibrary` to **NewtonsoftJson**, with the source comment that `'SystemTextJson' is experimental/not complete` (also for the polymorphic serialization style). <https://github.com/RicoSuter/NJsonSchema/blob/master/src/NJsonSchema.CodeGeneration.CSharp/CSharpGeneratorSettings.cs>
- Current `CSharpClientGeneratorSettings` and NJsonSchema `CSharpGeneratorSettings` sources contain no Polly option at all (grep of both files for "Polly" is empty) — the 14.x client runtime is HttpClient-based. <https://github.com/RicoSuter/NSwag/blob/master/src/NSwag.CodeGeneration.CSharp/CSharpClientGeneratorSettings.cs>, <https://github.com/RicoSuter/NJsonSchema/blob/master/src/NJsonSchema.CodeGeneration.CSharp/CSharpGeneratorSettings.cs>
- NSwag.Core (document object model used at generation time) targets `net462;netstandard2.0;net8.0`. <https://github.com/RicoSuter/NSwag/blob/master/src/NSwag.Core/NSwag.Core.csproj>

### OpenAPI 3.1 support status

- The README scopes NSwag to **"OpenAPI 2.0 and 3.0"**; 3.1 is not claimed in README, and no 3.1 support page exists in the wiki index. <https://github.com/RicoSuter/NSwag#readme>
- Open issue #5387 (as of the snapshot): "IFormFile not generating FileParameter when using .NET 10 OpenAPI 3.1 output (format:binary not recognized)" — 3.1 inputs are consumed in practice but with documented gaps; also open #4839 "Generate OpenApi 3.1 compatible Enums?". <https://github.com/RicoSuter/NSwag/issues/5387>, <https://github.com/RicoSuter/NSwag/issues/4839>

### Invocation & toolchain

- **dotnet global tool**: `dotnet tool install --global NSwag.ConsoleCore` (`PackAsTool`, `ToolCommandName=nswag`, console tool multi-targets **net8.0;net9.0;net10.0** — runs on .NET 10). <https://github.com/RicoSuter/NSwag/wiki/CommandLine>, <https://github.com/RicoSuter/NSwag/blob/master/src/NSwag.ConsoleCore/NSwag.ConsoleCore.csproj>
- One-shot command: `nswag openapi2csclient /input:spec.json /classname:MyServiceClient /namespace:My.Namespace /output:MyServiceClient.cs`; or declarative `nswag run nswag.json` (config document). <https://github.com/RicoSuter/NSwag/wiki/CommandLine>
- **MSBuild integration**: `NSwag.MSBuild` package (`$(NSwagExe)` / `$(NSwagExe_Net80)` exec placeholders, wiki sample target; wiki's "Recommended for .NET 8 and later" snippet uses the `NSwagExe_Net80` placeholder — the docs page still illustrates net8.0); `NSwag.ApiDescription.Client` package adds `OpenApiReference`/`CodeGenerator=NSwagCSharp` build-time generation with `NSwag*` MSBuild properties. <https://github.com/RicoSuter/NSwag/wiki/NSwag.MSBuild>, <https://github.com/RicoSuter/NSwag/blob/master/src/NSwag.ApiDescription.Client/NSwag.ApiDescription.Client.props> (NuGet latest 14.7.1: <https://www.nuget.org/packages/NSwag.MSBuild>, <https://www.nuget.org/packages/NSwag.ApiDescription.Client>)
- Other surfaces: NSwagStudio Windows GUI (MSI in releases), NPM package wrapper, Mono/.NET Core CLI on Mac/Linux. <https://github.com/RicoSuter/NSwag#readme>, <https://github.com/RicoSuter/NSwag/releases>
- No Java required; everything is .NET-native.

### Common pitfalls (documented)

- Newtonsoft.Json is the required default serializer of generated clients; STJ is labeled experimental (source comments above). <https://github.com/RicoSuter/NSwag/wiki/CSharpClientGenerator>
- Nullability regression shipped in 14.7.0 and was fixed in 14.7.1 — a live example of spec-interpretation bugs reaching client DTO code; pin versions accordingly. <https://github.com/RicoSuter/NSwag/releases/tag/v14.7.1>
- BasePath semantics are order-sensitive (trailing slash in `BaseAddress` when `InjectHttpClient`+`UseBaseUrl=false`). <https://github.com/RicoSuter/NSwag/wiki/CSharpClientGenerator>
- 3.1 input gaps (format:binary/enum) remain open. <https://github.com/RicoSuter/NSwag/issues/5387>

---

## 3. OpenAPI Generator (OpenAPITools)

Docs: <https://openapi-generator.tech/docs/generators> · repo: <https://github.com/OpenAPITools/openapi-generator>

### Version, maturity, activity (2025–2026)

- Latest stable **v7.26.0 (2026-10-06)**, "comes with 240+ enhancements and bug fixes" (releases are frequent; CLI jar attached per release). <https://github.com/OpenAPITools/openapi-generator/releases/tag/v7.26.0>
- Project scale (README): "600k weekly downloads via NPM CLI wrapper, 30M downloads via openapi-generator-cli docker image". <https://github.com/OpenAPITools/openapi-generator/blob/v7.26.0/README.md>

### Supported OpenAPI versions

- README: **"OpenAPI Spec compatibility: 1.0, 1.1, 1.2, 2.0, 3.0, 3.1 (beta support)"** — 3.1 explicitly still labeled beta in the project's own wording; the README also lists "v3.1" among "new features … we will continue this approach to deliver", i.e. incremental support. <https://github.com/OpenAPITools/openapi-generator/blob/v7.26.0/README.md>

### C# client generator (`csharp`) & `aspnetcore`

- `csharp` generator metadata (v7.26.0 docs): **generator type CLIENT, stability STABLE**, helpTxt "Generates a C# client library (.NET Standard, .NET Core)". <https://github.com/OpenAPITools/openapi-generator/blob/v7.26.0/docs/generators/csharp.md>
- `aspnetcore` generator is **generator type SERVER** ("Generates an ASP.NET Core Web API server", stability STABLE, `aspnetCoreVersion` default **10.0**) — it is a contract-first server generator, not a client SDK generator; v7.26.0 release notes: "[aspnetcore] Add .NET 10 support for aspnetcore #25124". <https://github.com/OpenAPITools/openapi-generator/blob/v7.26.0/docs/generators/aspnetcore.md>, <https://github.com/OpenAPITools/openapi-generator/releases/tag/v7.26.0>
- `.NET` target support (`csharp`): `targetFramework` option values include `netstandard1.3…2.1`, `net47`, `net48`, `net8.0`, `net9.0`, `net10.0` — **default is `net10.0`**. <https://github.com/OpenAPITools/openapi-generator/blob/v7.26.0/docs/generators/csharp.md>
- HTTP library (`library` option): default **`generichost`** = "HttpClient, Generic Host integration, and System.Text.Json"; `httpclient` = "HttpClient and Newtonsoft **(Experimental. Subject to breaking changes without notice.)**"; `restsharp`; `unityWebRequest`. <https://github.com/OpenAPITools/openapi-generator/blob/v7.26.0/docs/generators/csharp.md>
- Output is a **full library project**: options `optionalProjectFile` (generate csproj, default true), `optionalAssemblyInfo` (default true), plus test templates (`api_test`, `DependencyInjectionTests`) and DI templates (`IServiceCollectionExtensions`, `IHttpClientBuilderExtensions`, `HostConfiguration`) in the `generichost` template set; `nullableReferenceTypes` default false. <https://github.com/OpenAPITools/openapi-generator/blob/v7.26.0/docs/generators/csharp.md>, <https://github.com/OpenAPITools/openapi-generator/tree/v7.26.0/modules/openapi-generator/src/main/resources/csharp/libraries/generichost>
- Security per the same feature matrix: BasicAuth ✓, ApiKey ✓, OAuth2_Implicit ✓, OAuth2_ClientCredentials ✓, OAuth2_Password ✗, OAuth2_AuthorizationCode ✗; token templates (`BearerToken`, `ApiKeyToken`, `BasicToken`, `OAuthToken`) exist for `generichost`. <https://github.com/OpenAPITools/openapi-generator/blob/v7.26.0/docs/generators/csharp.md>

### Typical quality/size caveats (as documented, not opinion)

- The v7.26.0 `csharp` feature matrix marks **`allOf` ✗, `anyOf` ✗, `oneOf` ✗, `not` ✗, `Union` ✗** and also `ParameterStyling` ✗, `MultiServer` ✗, `ParameterizedServer` ✗, `Callbacks` ✗, `LinkObjects` ✗ — composition of the 3.x schema features is not supported by this client generator per its own matrix (note: release notes for 7.26.0 still contain "[csharp] Fix oneOf/anyOf response deserialization in httpclient library #25080", i.e. these areas are actively churning). <https://github.com/OpenAPITools/openapi-generator/blob/v7.26.0/docs/generators/csharp.md>, <https://github.com/OpenAPITools/openapi-generator/releases/tag/v7.26.0>
- Code size: generation produces an entire solution-shaped package (own csproj, AssemblyInfo, client config, API classes, models, tests) rather than a thin client set — options above; `disallowAdditionalPropertiesIfNotPresent` documents a spec-compliance default the project itself flags as legacy ("keep the old (incorrect) behaviour"). <https://github.com/OpenAPITools/openapi-generator/blob/v7.26.0/docs/generators/csharp.md>

### Toolchain

- **Java runtime required: "Java 11 runtime at a minimum"** (jar from Maven Central; `java -jar openapi-generator-cli.jar generate -g csharp …`). <https://github.com/OpenAPITools/openapi-generator/blob/v7.26.0/docs/installation.md>
- Alternatives when no JVM: official **Docker image** (`openapi-generator-cli`, "can be used as an alternative to installing via homebrew, or for developers who are unable to install Java"), **NPM wrapper** `@openapitools/openapi-generator-cli` (Node, which itself shells out to the jar), PyPI/jdk4py. **No .NET tool exists for it.** <https://github.com/OpenAPITools/openapi-generator/blob/v7.26.0/docs/installation.md>

---

## Facts table (as verified 2026-10-19)

| | **Kiota** | **NSwag** | **OpenAPI Generator** |
|---|---|---|---|
| Latest release | CLI **1.36.1** (2026-10-12); runtime libs 2.1.2–2.1.5 | **14.7.1** (2026-04-20) | **7.26.0** (2026-10-06) |
| Cadence / activity | minor monthly (first Tue); GitHub primary support | irregular (Apr→Nov 2025→Apr 2026) | high volume ("240+ enhancements" per release) |
| OpenAPI input | 2.x + 3.0 (Microsoft.OpenApi.NET); 3.1 since tool 1.24.0 (2025-03); 3.2.0 since 1.30.0 (2026-01) | README: **2.0 and 3.0**; 3.1 gaps open (#5387, #4839) | 2.0/3.0; **3.1 "beta support"** (README) |
| .NET 10 | runtime libs **target net10.0** (2.0.0 dropped net5/6); quickstart: SDK 10.0 | tool TFMs net8/9/10; libs netstandard2.0/net8.0 | `csharp` `targetFramework` **default net10.0**; aspnetcore got .NET 10 in 7.26.0 |
| Runtime deps added | `Microsoft.Kiota.Bundle`: Abstractions + HttpClientLibrary + 4 serializers (STJ-based JSON); DI.Abstractions; **no Polly/Newtonsoft/RestSharp** | **Newtonsoft.Json** + System.Net.Http + System.ComponentModel.Annotations; STJ "experimental" | default `generichost`: HttpClient + System.Text.Json; `httpclient` (Newtonsoft) "Experimental"; `restsharp` option |
| Client shape | typed client + request builders per path, fluent nav, async, typed params | one partial client class + DTOs, per-operation async methods, interfaces optional | whole library project (csproj/tests/config), API-per-tag classes + models |
| Auth | IAuthenticationProvider (+bearer/api-key/anonymous/Entra providers) | hooks (`PrepareRequest`) / base class / config class | Basic/ApiKey/OAuth2(ClientCredentials, Implicit) per feature matrix; token templates |
| DI/HttpClient (Blazor Server) | official DI tutorial: `AddKiotaHandlers`/`AttachKiotaHandlers` + factory; adapter accepts `HttpClient` | `InjectHttpClient=true` default → typed-client registration; base path via injected client | `generichost` emits `IServiceCollection`/`IHttpClientBuilder` extensions + `HostConfiguration` |
| Invocation | `dotnet tool install --global Microsoft.OpenApi.Kiota`; binaries/Docker/GitHub Action (preview); lock file + `kiota update`; **no MSBuild task** | `dotnet tool install --global NSwag.ConsoleCore`; `nswag.json`; **NSwag.MSBuild / ApiDescription.Client**; Windows GUI | `java -jar …jar` / Docker / NPM wrapper; Gradle/Maven plugins; **needs JVM 11+** |
| Size knobs | `--include-path/--exclude-path`, `--exclude-backward-compatible` | one output file; interface/DTO toggles | full project incl. csproj/tests/AssemblyInfo; pruning left to user |

## Implications for this repo

(Recorded facts and constraints only — decisions belong to the decision ticket.)

1. **Spec-version coupling is the first gate.** The planned pipeline is TypeSpec → OpenAPI. The TypeSpec emitter's default output is 3.0.0 with 3.1.0/3.2.0 selectable (`openapi-versions`) <https://github.com/microsoft/typespec/blob/main/packages/openapi3/README.md>. All three tools consume 3.0.x; native 3.1 support is documented only by Kiota (since 1.24.0), while NSwag's README doesn't claim 3.1 and has open 3.1 gaps, and OpenAPI Generator labels 3.1 "beta" with `oneOf/anyOf/allOf/not` marked ✗ in the `csharp` client feature matrix (v7.26.0).
2. **Toolchain weight differs.** Kiota and NSwag are both `dotnet tool` installs and fit the repo's .NET-only tooling note (`dotnet format` is the only lint, AGENTS.md "Постоянные ограничения"); OpenAPI Generator requires a JVM (≥11) or Docker/NPM in CI and dev boxes — an extra runtime for `.github/workflows/ci.yml` runners.
3. **.NET 10 status is settled for all three** (Kiota libs target net10.0; NSwag tool ships a net10.0 TFM; openapi-generator `csharp` defaults to targetFramework net10.0) — verified links above; the repo's `Directory.Build.props` net10.0 is not a blocker for any option.
4. **Blazor Server DI mechanics:** the Blazor docs fix `HttpClient` lifetime as scoped-per-circuit and warn that interactive-server DI scopes last for the circuit duration <https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/dependency-injection>. NSwag's `InjectHttpClient=true` default accepts that injected client directly (typed-client style); Kiota routes the injected `HttpClient` through the request adapter/factory (official tutorial); `generichost` OAG output generates its own DI extensions. All three can live with the repo's existing `src/CallCalendar.Web` structure; generated sources by default participate in project compilation and would be covered by the `dotnet format --verify-no-changes` CI gate (an established fact of this repo's checks, worth noting before adopting a generator).
5. **Runtime-package footprint:** Kiota adds 6 first-party packages (or the single `Microsoft.Kiota.Bundle` meta-package, STJ-only, no third-party HTTP libs); NSwag adds Newtonsoft.Json to the client's runtime graph (its serializer default; STJ alternative is self-described "experimental/not complete"); OAG `generichost` adds Microsoft.Extensions hosting/Options machinery and owns its own configuration/client layer.
6. **Auth-model reach:** current plan docs don't fix an auth scheme; facts: Kiota has no built-in cookie/session provider (providers are bearer/api-key/anonymous/Entra); NSwag relies on the shared `HttpClient`/handler pipeline and request hooks for arbitrary headers/cookies; OAG's matrix covers Basic/ApiKey/OAuth2-flows. Ticket #4 (auth) is downstream context for #14 — noted, not decided.

## Source inventory (primary)

- Kiota docs (Learn; content mirrored at MicrosoftDocs/openapi-docs): overview <https://learn.microsoft.com/en-us/openapi/kiota/>, install <https://learn.microsoft.com/en-us/openapi/kiota/install>, using <https://learn.microsoft.com/en-us/openapi/kiota/using>, dependencies <https://learn.microsoft.com/en-us/openapi/kiota/dependencies>, authentication <https://learn.microsoft.com/en-us/openapi/kiota/authentication>, middleware <https://learn.microsoft.com/en-us/openapi/kiota/middleware>, request-builders <https://learn.microsoft.com/en-us/openapi/kiota/request-builders>, models <https://learn.microsoft.com/en-us/openapi/kiota/models>, abstractions <https://learn.microsoft.com/en-us/openapi/kiota/abstractions>, status-codes <https://learn.microsoft.com/en-us/openapi/kiota/status-codes>, errors <https://learn.microsoft.com/en-us/openapi/kiota/errors>, design <https://learn.microsoft.com/en-us/openapi/kiota/design>, quickstart .NET <https://learn.microsoft.com/en-us/openapi/kiota/quickstarts/dotnet>, DI tutorial <https://learn.microsoft.com/en-us/openapi/kiota/tutorials/dotnet-dependency-injection>, support <https://learn.microsoft.com/en-us/openapi/kiota/support>
- Kiota GitHub: releases <https://github.com/microsoft/kiota/releases> · CHANGELOG <https://github.com/microsoft/kiota/blob/main/CHANGELOG.md> · runtime libs <https://github.com/microsoft/kiota-dotnet> (CHANGELOG, csproj) · NuGet <https://www.nuget.org/packages/Microsoft.Kiota.Bundle> (nuspecs via flat container) · DI sample <https://github.com/microsoft/kiota-samples/blob/main/get-started/dotnet-dependency-injection/Program.cs>
- NSwag: releases <https://github.com/RicoSuter/NSwag/releases> · README <https://github.com/RicoSuter/NSwag#readme> · wiki CommandLine / CSharpClientGenerator / NSwag.MSBuild <https://github.com/RicoSuter/NSwag/wiki> · settings source <https://github.com/RicoSuter/NSwag/blob/master/src/NSwag.CodeGeneration.CSharp/CSharpClientGeneratorSettings.cs> · NJsonSchema settings source <https://github.com/RicoSuter/NJsonSchema/blob/master/src/NJsonSchema.CodeGeneration.CSharp/CSharpGeneratorSettings.cs> · issues #5387, #4839
- OpenAPI Generator: releases <https://github.com/OpenAPITools/openapi-generator/releases/tag/v7.26.0> · docs/generators/csharp.md & aspnetcore.md & installation.md at tag v7.26.0 <https://github.com/OpenAPITools/openapi-generator/tree/v7.26.0/docs> · README <https://github.com/OpenAPITools/openapi-generator/blob/v7.26.0/README.md>
- ASP.NET Core docs (Blazor DI): <https://learn.microsoft.com/en-us/aspnet/core/blazor/fundamentals/dependency-injection>
- TypeSpec: <https://github.com/microsoft/typespec/blob/main/packages/openapi3/README.md>
