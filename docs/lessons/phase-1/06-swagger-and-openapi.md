# Swagger / OpenAPI documentation

*The contract your API publishes, and why generated docs drift from reality.*

**Phase:** ROADMAP Phase 1 — ASP.NET Core Web API & Architecture
**Target framework:** .NET 10 (ADR-0003)

> **ROADMAP links the Swashbuckle tutorial.** As of .NET 9+, the template uses
> **`Microsoft.AspNetCore.OpenApi`** (`AddOpenApi()` / `MapOpenApi()`) — which is
> what this project's `Program.cs` already calls. Swashbuckle is no longer the
> default. §1 covers the distinction, which is itself an interview question now.

**Contents**

0. [Ten-second recall](#0-ten-second-recall)
1. [OpenAPI vs Swagger vs Swashbuckle](#1-openapi-vs-swagger-vs-swashbuckle)
2. [Why the generated document is usually wrong](#2-why-the-generated-document-is-usually-wrong)
3. [Making the document accurate](#3-making-the-document-accurate)
4. [Design-first vs code-first](#4-design-first-vs-code-first)
5. [Security considerations](#5-security-considerations)
6. [OpenAPI in a modular monolith](#6-openapi-in-a-modular-monolith)
7. [**Interview question bank**](#7-interview-question-bank)
8. [Exercises](#8-exercises)
9. [What this lesson does not settle](#9-what-this-lesson-does-not-settle)

---

## 0. Ten-second recall

| Thing | The one sentence that matters |
|---|---|
| OpenAPI | The **specification**. Swagger is the tooling around it |
| .NET 9+ default | `Microsoft.AspNetCore.OpenApi`, not Swashbuckle |
| `AddOpenApi()` | Generates the document; `MapOpenApi()` serves it at `/openapi/v1.json` |
| No UI by default | The built-in package emits JSON only — Scalar or Swagger UI renders it |
| Default document | Claims every endpoint returns 200 and nothing else. It is a lie |
| `[ProducesResponseType]` | How the document learns about 400/404/409 |
| `ActionResult<T>` | Keeps the response type visible; `IActionResult` erases it |
| Generated docs drift | Nothing fails when the document is wrong — no test asserts it |
| Exposing in Production | A decision, not a default. It is an API map for an attacker |
| Breaking change detection | Diff the committed spec in CI — the only mechanism that actually works |

---

## 1. OpenAPI vs Swagger vs Swashbuckle

Precision here is worth a surprising amount in interviews, because most people use
the words interchangeably.

| Term | What it actually is |
|---|---|
| **OpenAPI** | The specification — a standard JSON/YAML description of an HTTP API. Formerly "Swagger Specification", renamed at 3.0 |
| **Swagger** | SmartBear's tooling family — Swagger UI, Swagger Editor, Swagger Codegen |
| **Swashbuckle** | A .NET library that generates an OpenAPI document and bundles Swagger UI |
| **NSwag** | Alternative .NET library; also generates **clients** |
| **`Microsoft.AspNetCore.OpenApi`** | Microsoft's first-party generator, the .NET 9+ template default |

```csharp
builder.Services.AddOpenApi();        // already in this project's Program.cs

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                 // serves /openapi/v1.json — JSON only, no UI
}
```

**The gotcha:** the first-party package emits the document but ships **no UI**. The
template looks like it lost Swagger UI, and it did. Add a renderer separately —
Scalar (`app.MapScalarApiReference()`) is the common choice now, or Swagger UI via
Swashbuckle if you prefer the familiar interface.

**Choosing:** first-party for a plain API; Swashbuckle if you depend on its filter
ecosystem; NSwag if you want generated typed clients — relevant here, since the
Angular front end can consume a generated TypeScript client instead of hand-written
HTTP services.

---

## 2. Why the generated document is usually wrong

This is the part worth internalising. **Generation is not verification.** The
generator reports what it can infer from signatures, and signatures do not express
most of your contract.

A default document typically claims:

- every endpoint returns **200 and only 200** — no 400, 404, 409, 401
- every response body is the action's return type, even when errors return
  `ProblemDetails`
- every string is unconstrained — no max length, no format
- nothing about authentication requirements
- nothing about which fields are nullable, if you are careless with NRT

Published, that document generates client SDKs that have no error handling for
states your API returns daily.

**Why it stays wrong:** nothing fails. Tests pass, the build is green, and the
document is silently inaccurate until a consumer hits a 409 their generated client
cannot represent. Unlike code, a wrong spec has no feedback loop — unless you build
one (§3).

---

## 3. Making the document accurate

### Declare every response

```csharp
[HttpPost]
[ProducesResponseType<BookingResponse>(StatusCodes.Status201Created)]
[ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
public async Task<ActionResult<BookingResponse>> Create(
    CreateBookingRequest request, CancellationToken ct)
```

The 409 matters in this domain — a slot taken concurrently is an *expected* outcome,
and a client that does not handle it will show the user a generic failure.

### Use `ActionResult<T>`

`IActionResult` erases the response type, so the generator cannot infer a schema.
See [lesson 01 §5](01-web-api-controllers-routing-model-binding-validation.md#5-action-results).

### Let validation attributes describe constraints

`[MaxLength(200)]`, `[Range]`, `[RegularExpression]` all flow into the schema. Nullable
reference types flow into `required` — another reason NRT discipline matters
([lesson 01 §4](01-web-api-controllers-routing-model-binding-validation.md#4-model-binding-and-validation)).

### XML comments

```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
```

Summaries become descriptions. Worth it for a public API, optional internally.

### Transformers (first-party equivalent of Swashbuckle filters)

```csharp
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info.Title = "SlotBook API";
        return Task.CompletedTask;
    });
});
```

Document, operation and schema transformers cover adding security schemes, global
headers, and examples.

### Close the loop: commit the spec and diff it in CI

The only mechanism that reliably prevents drift and accidental breaking changes:

1. Generate the document at build time (`dotnet tool install Microsoft.Extensions.ApiDescription.Server`)
2. Commit it
3. In CI, regenerate and diff — a change to the committed spec must be deliberate
4. Optionally run a breaking-change detector (`oasdiff`) and fail the build on one

This converts "the docs drifted" into a failing build, which is the only thing that
actually works.

---

## 4. Design-first vs code-first

| | Code-first | Design-first |
|---|---|---|
| Source of truth | The C# code | The OpenAPI document |
| Flow | Code → generated spec | Spec → generated server stubs + clients |
| Strength | Nothing to keep in sync; fast | Contract agreed before implementation; parallel front/back work |
| Weakness | Spec is an afterthought and drifts | Tooling friction; regeneration discipline |

**Code-first** suits a solo developer owning both ends — which is this project.
**Design-first** earns its place when separate teams build client and server, or
when the contract is a deliverable negotiated with a third party.

A reasonable middle ground, and a good interview answer: code-first generation,
but the spec is **committed and diffed**, so it functions as a contract even though
it is derived.

---

## 5. Security considerations

### Should the document be public?

`MapOpenApi()` inside `IsDevelopment()` — as this project's `Program.cs` has it — is
the safe default. It is also a decision worth making consciously rather than
inheriting from the template.

**Against exposing it:** it is a complete map of your attack surface — every
endpoint, parameter, and type. It reveals internal endpoints you forgot were
reachable, and admin routes you assumed were obscure.

**For exposing it:** a genuinely public API needs discoverable documentation, and
security through obscurity is not security. If your API is safe only because
nobody has the route list, it is not safe.

**The middle path:** expose in Production only for genuinely public endpoints, with
internal/admin operations excluded from the document; or require authentication to
reach it.

### Do not leak internals into the schema

If a response type is a domain entity, the schema publishes your database shape —
including columns clients should never know about. This is another argument for
response DTOs ([lesson 04 §6](04-clean-and-layered-architecture.md#6-common-mistakes)).

### Document the security scheme

Declare bearer auth so the UI can exercise protected endpoints and so generated
clients know to send a token. Undocumented auth makes the spec useless for client
generation.

---

## 6. OpenAPI in a modular monolith

Two questions that follow from [lesson 05](05-modular-monolith.md):

**One document or several?** One per API surface is usually right — the document
describes what the *host* exposes, and consumers do not care about your internal
module boundaries. Multiple documents make sense when audiences genuinely differ:
a public client API and an internal admin API with different auth and exposure.
`AddOpenApi("public")` / `AddOpenApi("admin")` registers named documents.

**Where do schemas live?** If controllers are in the host, response DTOs live in
the host and modules stay HTTP-free. If controllers are in modules, each module
owns its DTOs — which keeps features together but means the module now has an HTTP
contract as well as a module contract. Same trade-off as controller placement; keep
the answer consistent with it.

---

## 7. Interview question bank

**Frequency:** 🔴 near-certain · 🟠 common · 🟡 senior rounds.

> Grouped by how often the *shape* recurs in .NET hiring loops, not attributed to
> named employers.

---

### 🔴 Q1. What is the difference between OpenAPI and Swagger?

Common opener. Most candidates conflate them.

- **Strong:** OpenAPI is the specification — a standard description format for HTTP
  APIs, called the Swagger Specification until 3.0. Swagger is SmartBear's tooling
  around it: Swagger UI, Editor, Codegen. Swashbuckle is a .NET library that
  generates an OpenAPI document and bundles Swagger UI.
- **Follow-up:** *"What changed in .NET 9?"* — The template moved to the
  first-party `Microsoft.AspNetCore.OpenApi` with `AddOpenApi()`/`MapOpenApi()`,
  and Swashbuckle is no longer included. The practical catch: the first-party
  package serves JSON only, no UI, so you add Scalar or Swagger UI separately.

---

### 🟠 Q2. Your generated documentation says every endpoint returns 200. Why, and how do you fix it?

- **Strong:** Because the generator infers from signatures, and a signature does not
  express error outcomes. Fix with `[ProducesResponseType]` for each real status,
  `ActionResult<T>` rather than `IActionResult` so the success type survives, and
  validation attributes so constraints reach the schema.
- **Follow-up:** *"What is the risk of leaving it?"* — Generated clients have no
  representation for states your API returns routinely. In this domain a 409 on a
  concurrently-booked slot is normal, so a client without it shows users a generic
  error for an expected outcome.

---

### 🟠 Q3. Design-first or code-first?

- **Strong:** Code-first when one team or person owns both ends — the spec is
  derived so it cannot drift from the code's shape. Design-first when separate
  teams build client and server in parallel, or when the contract is negotiated
  externally, because the contract must exist before either side is written.
- **Follow-up:** *"How do you stop a code-first spec from drifting?"* — Generate it
  at build time, **commit it**, and diff in CI so any change is deliberate. Add a
  breaking-change detector like `oasdiff` to fail the build. Without that loop
  nothing ever tells you the document is wrong.

---

### 🟡 Q4. Should Swagger be enabled in Production?

A judgement question — they want the trade-off, not a rule.

- **Strong:** Default to Development-only, but decide consciously. Against: the
  document is a complete map of your attack surface and often reveals endpoints you
  forgot were reachable. For: a genuinely public API needs discoverable docs, and
  obscurity is not a security control — if your API is only safe because the routes
  are unknown, it is not safe. Middle path: expose only the public surface, exclude
  internal and admin operations, or put it behind authentication.

---

### 🟡 Q5. How do you version an API in OpenAPI terms?

- **Strong:** A document per version (`/openapi/v1.json`, `/openapi/v2.json`), with
  `Asp.Versioning` supplying the version set. The spec describes one version's
  contract; mixing versions in one document makes it ambiguous for client
  generation.
- **Follow-up:** *"What counts as a breaking change to a spec?"* — Removing a field
  or endpoint, changing a type, making an optional field required, tightening
  validation, changing a status code's meaning. Adding an optional field is not
  breaking. Tooling can assert this mechanically, which is the point of committing
  the spec.

---

### 🟡 Q6. How would you generate a TypeScript client for your Angular app?

- **Strong:** NSwag or `openapi-generator` against the committed spec, run in CI so
  the client regenerates when the contract changes. The real benefit is that a
  backend contract change becomes a **compile error in the front end** rather than a
  runtime failure found in QA. The cost is generated code in the repo and sensitivity
  to spec quality — which is another reason the spec must be accurate.

---

### Questions to ask back

- "Is your OpenAPI document committed and diffed, or generated and forgotten?"
- "Do your generated clients handle every status your API returns?"
- "Is the spec exposed in Production, and was that deliberate?"

---

## 8. Exercises

1. Your endpoint returns 201, 400, 401 and 409. Write the attributes that make the
   document accurate.
2. An action returns `IActionResult`. What does the schema say, and why?
3. Describe a CI setup that fails the build when someone makes a breaking API change.
4. A response DTO is replaced with the domain entity. Name two problems this causes
   in the published schema.
5. You have a public client API and an internal admin API in one host. How do you
   document them, and what do you expose where?
6. Angular consumes a generated client. A backend field is renamed. Where does the
   failure surface — and where would it have surfaced without generation?
7. At what point is design-first worth the friction for *this* project?

---

## 9. What this lesson does not settle

| Question | Decided in |
|---|---|
| Whether to expose the spec in Production | Open — decide with C7 (API hardening) |
| API versioning scheme | Lesson 07 (optional topic) |
| Controller placement, hence DTO ownership | **A4 / A5** |
| Whether Angular uses a generated client | Open — UI is deliberately thin |

---

## References

- [Generate OpenAPI documents (.NET)](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/aspnetcore-openapi)
- [OpenAPI Specification](https://spec.openapis.org/oas/latest.html)
- [Swashbuckle](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)
- [NSwag](https://github.com/RicoSuter/NSwag)
- [Scalar (API reference UI)](https://github.com/scalar/scalar)
- [oasdiff — breaking change detection](https://github.com/Tufin/oasdiff)
