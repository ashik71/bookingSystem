# API versioning · code style & analyzers

*Two optional Phase 1 items. Both are cheap now and expensive to retrofit.*

**Phase:** ROADMAP Phase 1 — marked 🆕💡 (nice-to-have)
**Target framework:** .NET 10 (ADR-0003)

> **Why one lesson for two topics:** each is small, and both are "do it before
> there is anything to migrate" decisions. Versioning after you have consumers is a
> breaking change; enabling analyzers after 50k lines means a thousand warnings
> nobody fixes.

**Contents**

### Part A — API versioning
1. [When you actually need it](#1-when-you-actually-need-it)
2. [The four strategies](#2-the-four-strategies)
3. [Implementation](#3-implementation)
4. [What counts as breaking](#4-what-counts-as-breaking)

### Part B — Code style & analyzers
5. [`.editorconfig`](#5-editorconfig)
6. [Analyzers and warnings-as-errors](#6-analyzers-and-warnings-as-errors)
7. [Nullable reference types](#7-nullable-reference-types-as-a-correctness-tool)

8. [**Interview question bank**](#8-interview-question-bank)
9. [Exercises](#9-exercises)

---

## 0. Ten-second recall

| Thing | The one sentence that matters |
|---|---|
| Version from day one | Adding `/v1` later is itself a breaking change |
| URL path versioning | Most visible, most cacheable, pollutes every route. Usual default |
| Header versioning | Clean URLs, invisible in a browser, harder to test by hand |
| Media type versioning | Most RESTful, least used in practice |
| Breaking change | Removing a field, changing a type, tightening validation |
| Adding an optional field | **Not** breaking — tolerant readers |
| Version the contract | Not the implementation. One codebase can serve two versions |
| `.editorconfig` | Settings travel with the repo, IDE-independent |
| `TreatWarningsAsErrors` | The only setting that stops warning rot |
| Warning rot | 400 warnings means zero warnings are read |
| NRT | A correctness tool, not style — it feeds validation and OpenAPI |
| Enable analyzers early | Retrofitting onto a large codebase produces noise nobody fixes |

---

# Part A — API versioning

## 1. When you actually need it

Versioning exists for one reason: **you cannot redeploy your clients.** A mobile app
on someone's phone, a partner integration, a third-party script — those keep calling
the old shape whether you like it or not.

**You need it when:** clients are outside your deployment (mobile, partners,
public API), or the API is a product in its own right.

**You may not when:** the only consumer is your own SPA deployed together with the
backend. Then you can change both at once and versioning is ceremony — though
during a rolling deploy the old SPA briefly talks to the new API, which is the
sharp edge people forget.

**For this project:** there will be a mobile-shaped consumer and the UI deploys
separately, so URL versioning from the start is cheap insurance. The decisive
argument is that **adding versioning later is itself a breaking change** — every
existing URL moves.

---

## 2. The four strategies

| Strategy | Example | Strengths | Weaknesses |
|---|---|---|---|
| **URL path** | `/api/v1/bookings` | Obvious, easy to route, cacheable, testable in a browser | Pollutes every URL; purists object that the resource did not change |
| **Query string** | `/api/bookings?api-version=1.0` | Easy to default, no route changes | Easy to omit; messy with other params |
| **Custom header** | `X-Api-Version: 1.0` | Clean URLs | Invisible in a browser, harder to test, easy to forget in caching |
| **Media type** | `Accept: application/vnd.slotbook.v2+json` | Most RESTful — versions the *representation* | Rare, poor tooling support, confuses consumers |

**Recommendation for this project: URL path.** Discoverability and cacheability
beat theoretical purity, and a URL you can paste into a browser is worth a lot in
support conversations. Say this out loud in an interview — the honest reasoning
matters more than the choice.

---

## 3. Implementation

`Asp.Versioning.Mvc` (formerly `Microsoft.AspNetCore.Mvc.Versioning`):

```csharp
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;        // advertises api-supported-versions header
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;   // makes OpenAPI docs per version
});
```

```csharp
[ApiController]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class BookingsController : ControllerBase
{
    [HttpGet, MapToApiVersion("1.0")]
    public Task<ActionResult<BookingV1>> GetV1(…);

    [HttpGet, MapToApiVersion("2.0")]
    public Task<ActionResult<BookingV2>> GetV2(…);
}
```

`ReportApiVersions` is worth enabling — responses advertise supported and deprecated
versions, so clients can detect their own obsolescence.

**Version the contract, not the implementation.** Two versions usually map to one
use case with different response shapes. Duplicating the whole stack per version is
how versioning becomes unmaintainable; the split belongs at the boundary, in
mapping.

**Deprecation needs a policy, not just an attribute.** `[ApiVersion("1.0",
Deprecated = true)]` marks it; you still need a sunset date, communication, and
usage telemetry to know whether anyone is still on it. Removing a version you cannot
measure is guesswork.

---

## 4. What counts as breaking

| Change | Breaking? |
|---|---|
| Adding an optional response field | **No** — tolerant readers should ignore unknowns |
| Adding an optional request field | **No** |
| Adding a required request field | **Yes** |
| Removing or renaming a field | **Yes** |
| Changing a field's type | **Yes** |
| Tightening validation | **Yes** — previously valid requests now fail |
| Loosening validation | No |
| Changing the meaning of a field | **Yes**, and the worst kind — nothing fails loudly |
| Adding a new endpoint | No |
| Changing a status code for an existing case | **Yes** |
| Changing default sort order or pagination size | **Yes in practice** — clients depend on it |

The last two are where teams get caught: nothing in the type system changes, so no
tooling flags it and no test fails. This is why you commit the OpenAPI spec and
diff it in CI ([lesson 06 §3](06-swagger-and-openapi.md#3-making-the-document-accurate)).

---

# Part B — Code style & analyzers

## 5. `.editorconfig`

One file at the repo root; every modern IDE honours it, so style stops being
per-developer and stops appearing in diffs.

```ini
root = true

[*]
indent_style = space
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

[*.cs]
indent_size = 4

# Treat nullable warnings as errors — correctness, not style
dotnet_diagnostic.CS8600.severity = error
dotnet_diagnostic.CS8602.severity = error   # dereference of a possibly-null reference
dotnet_diagnostic.CS8618.severity = error   # non-nullable field uninitialised

# Async correctness
dotnet_diagnostic.CA2007.severity = none    # ConfigureAwait: not needed in ASP.NET Core
dotnet_diagnostic.CA2016.severity = error   # forward CancellationToken

# Style, enforced in build
csharp_style_namespace_declarations = file_scoped:warning
csharp_prefer_braces = true:warning
dotnet_style_require_accessibility_modifiers = always:warning
```

Two choices worth making consciously:

**`CA2007` (`ConfigureAwait(false)`) off** — ASP.NET Core has no synchronisation
context, so it is noise in a web app. Turn it **on** in a shared library that might
be consumed by a framework that does have one.

**Nullable diagnostics as errors** — this is the high-value part of the file. CS8602
catches a null dereference at compile time, and it feeds validation and OpenAPI
([lesson 01 §4](01-web-api-controllers-routing-model-binding-validation.md#4-model-binding-and-validation)).

---

## 6. Analyzers and warnings-as-errors

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>

  <AnalysisLevel>latest-recommended</AnalysisLevel>
  <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

Put this in `Directory.Build.props` at the repo root so every project inherits it
and a new module cannot quietly opt out.

**`TreatWarningsAsErrors` is the one that matters.** Without it warnings accumulate
until there are four hundred and nobody reads any of them — at which point a real
one is invisible. The objection is that it blocks work on an unrelated warning; the
answer is `<NoWarn>` for a specific justified code, or `#pragma warning disable`
with a comment at the specific site. Both are explicit decisions, which is the
point.

**`EnforceCodeStyleInBuild`** makes `.editorconfig` style rules build failures
rather than IDE-only suggestions — otherwise they are advisory and drift.

**Enable this before the codebase grows.** Retrofitting onto 50k lines produces a
wall of warnings, the team sets `TreatWarningsAsErrors=false` "temporarily", and it
is never re-enabled. Right now the codebase is near-empty, which is exactly the
moment this is free.

Worth adding beyond the defaults: **`Microsoft.CodeAnalysis.NetAnalyzers`** (on by
default in modern SDKs) and, when the architecture settles, **architecture tests**
([lesson 05 §4](05-modular-monolith.md#4-enforcing-boundaries)) which are the same
idea applied to dependencies rather than syntax.

---

## 7. Nullable reference types as a correctness tool

NRT is listed under "code style" in the roadmap, which undersells it. It is a
correctness feature with three downstream effects:

1. **Compile-time null safety** — CS8602 catches dereferences the compiler can prove
   unsafe.
2. **Validation** — a non-nullable reference property is implicitly `[Required]` in
   model binding.
3. **OpenAPI** — nullability flows into the schema's `required` list, so generated
   clients get it right.

The discipline that makes it work: **do not use `!` to silence the compiler.** The
null-forgiving operator asserts knowledge the compiler lacks, and each use is a
place where a `NullReferenceException` can still occur. Where you must, comment why.

---

## 8. Interview question bank

**Frequency:** 🟠 common · 🟡 senior rounds. Versioning comes up more than analyzers.

> Grouped by how often the *shape* recurs, not attributed to named employers.

---

### 🟠 Q1. How do you version a REST API?

- **Strong:** Four strategies with trade-offs — URL path (visible, cacheable,
  testable in a browser; pollutes URLs), query string (easy to default, easy to
  omit), custom header (clean URLs, invisible and harder to test), media type (most
  RESTful, rarely used, poor tooling). Recommend URL path for most APIs because
  discoverability and cacheability beat purity, and say that explicitly rather than
  presenting it as the only option.
- **Follow-up:** *"When would you not version at all?"* — When you control every
  client and deploy them together. Even then, note the rolling-deploy window where
  the old client talks to the new API. And the decisive point: adding versioning
  later is itself a breaking change, so the cheap moment is now.

---

### 🟠 Q2. What counts as a breaking change?

- **Strong:** Removing or renaming a field, changing a type, making an optional
  request field required, tightening validation, changing a status code for an
  existing case. **Not** breaking: adding an optional field, adding an endpoint,
  loosening validation — assuming tolerant readers.
- **Follow-up:** *"What is the most dangerous kind?"* — A semantic change where the
  shape stays identical: a field that meant "local time" now means UTC, or a default
  page size changing. No tooling flags it, no test fails, and clients silently
  misbehave. That is the argument for committing the OpenAPI spec and diffing it —
  and for naming fields unambiguously in the first place.

---

### 🟡 Q3. How do you support two versions without duplicating everything?

- **Strong:** Version the **contract**, not the implementation. One use case, two
  response DTOs, mapping at the boundary. Duplicating the whole vertical per version
  doubles the maintenance and guarantees the two drift apart. Keep the divergence at
  the edge, where it actually is.

---

### 🟡 Q4. Do you use warnings as errors? Isn't that annoying?

They want to know whether you have an opinion and can defend it.

- **Strong:** Yes, and the reason is that warnings are only useful if they are read.
  Without enforcement they accumulate until there are hundreds, at which point a
  genuine one is invisible — the warning list stops being a signal. The cost is
  being blocked on something unrelated, and the answer is a targeted `NoWarn` or a
  commented `#pragma` at the site. Both force an explicit decision instead of
  silent accumulation.
- **Follow-up:** *"When would you not enable it?"* — On a large legacy codebase
  where the immediate cost is thousands of warnings. There, enable it on new
  projects via `Directory.Build.props` and leave the legacy ones on the old setting,
  rather than a flag day nobody finishes.

---

### 🟡 Q5. What does enabling nullable reference types actually buy you?

- **Strong:** It is a correctness feature, not style. Compile-time null-safety
  warnings; implicit `[Required]` in model binding, since a non-nullable reference
  property cannot be null; and accurate `required` in the OpenAPI schema so
  generated clients match reality. The discipline is avoiding `!` — each use
  reasserts knowledge the compiler does not have and reintroduces the exception
  you enabled NRT to prevent.

---

### Questions to ask back

- "How do you detect breaking API changes before release?"
- "Is `TreatWarningsAsErrors` on — and if not, how many warnings are there?"
- "Is nullable enabled, and is `!` used much?"

---

## 9. Exercises

1. Your API adds a required field to a request DTO. Breaking? What must you do?
2. Choose a versioning strategy for this project and defend it against one specific
   alternative.
3. A field named `startTime` changes from local time to UTC. What breaks, what
   detects it, and what should it have been called?
4. `TreatWarningsAsErrors` blocks your build on one warning in a generated file.
   Give two correct responses and one wrong one.
5. Where does `Directory.Build.props` go, and why there rather than per-project?
6. `CA2007` (`ConfigureAwait`) — keep it on or off in this project? What would
   change your answer?
7. You inherit a codebase with 1,200 warnings. What is your actual plan?

---

## References

- [ASP.NET API Versioning](https://github.com/dotnet/aspnet-api-versioning)
- [Versioning wiki](https://github.com/dotnet/aspnet-api-versioning/wiki)
- [EditorConfig](https://editorconfig.org/)
- [.NET code analysis overview](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview)
- [Code style rules](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/)
- [Nullable reference types](https://learn.microsoft.com/en-us/dotnet/csharp/nullable-references)
- [Customize your build — `Directory.Build.props`](https://learn.microsoft.com/en-us/visualstudio/msbuild/customize-your-build)
