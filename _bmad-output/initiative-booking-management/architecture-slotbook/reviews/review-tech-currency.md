# Review — tech currency (architecture-slotbook)

- **Lens:** every committed decision web-researched or reality-checked, not asserted from training data.
- **Reviewed:** `architecture-slotbook.md` (Stack table, AD-1, AD-2, AD-10, AD-11, AD-18), `.memlog.md`, `global.json`, `src/frontend/package.json`, `Directory.Packages.props`, `src/backend/**/*.csproj`.
- **Date of checks:** 2026-10-10.
- **Status:** complete.

## Repo pins observed

- `global.json`: SDK `10.0.100`, `rollForward: latestFeature`, MTP runner.
- `Directory.Packages.props`: Microsoft.AspNetCore.Mvc.Testing 10.0.12, Shouldly 4.3.0, xunit.v3 4.0.1; analyzers Meziantou 3.0.296, VS Threading 18.7.23, StyleCop 1.2.0-beta.556.
- `src/frontend/package.json`: Angular ^22.2.0, CLI ^22.2.2, TypeScript ~6.0.2, Vitest ^5.0.0, npm 11.19.0, node >=24.15.0.
- No MongoDB.Driver, MassTransit, Aspire, FluentValidation, NetArchTest or Transloco pins exist yet (spine-only).

## Items

### T-1 .NET 10 SDK — confirmed (low)

- **Spine:** Stack ".NET SDK 10.0.100, rollForward latestFeature"; `global.json` matches.
- **Found:** .NET 10 is in the active support phase. The latest runtime is **10.0.12** (2026-09-08). The latest SDKs are **10.0.401** (feature band 4xx) and 10.0.112. The next Patch Tuesday is 2026-10-13. `Microsoft.AspNetCore.Mvc.Testing 10.0.12` in `Directory.Packages.props` matches the current runtime patch.
- **Evidence:** https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json
- **Verdict:** confirmed. `10.0.100` + `latestFeature` is a floor, not a pin: it picks up whichever 10.0.x SDK is installed (100 through 401). That works, but different machines can then build with different SDKs.
- **Spine change (low):** in the Stack row, write "10.0.100 minimum (global.json floor, rollForward latestFeature); current SDK 10.0.401 / runtime 10.0.12 as of 2026-10-10". Name the sandbox image's SDK tag in AD-18/D9 so that CI and the sandbox build on the same band.

### T-2 MongoDB.Driver 3.x — confirmed

- **Spine:** 3.12.0. Memlog `(version)` entry: 3.12.0, published 2026-09-17.
- **Found:** **3.12.0** is still the latest on nuget.org (published 2026-09-17T22:35Z), Apache-2.0, targets net6.0/netstandard2.1 (runs on net10.0).
- **Evidence:** https://www.nuget.org/packages/MongoDB.Driver/3.12.0 (flat-container index `https://api.nuget.org/v3-flatcontainer/mongodb.driver/index.json`)
- **Spine change:** none.

### T-3 MassTransit 8.5.x — confirmed, the memlog patch is one behind (low)

- **Spine:** "8.5.x (latest 8.5 patch at pin time)". The memlog says 8.5.10 was seen and that MassTransit.MongoDb 8.5.8 depends on MongoDB.Driver >= 3.6.0.
- **Found:** the latest stable v8 is **8.5.11** (2026-09-30), Apache-2.0, with a `net10.0` target. 8.5.12-develop builds exist, so v8 is still being patched. MassTransit, MassTransit.RabbitMQ and MassTransit.MongoDb all share the version line.
- **Evidence:** https://www.nuget.org/packages/MassTransit/8.5.11 ; https://www.nuget.org/packages/MassTransit.MongoDb/8.5.11
- **Spine change (low):** none required, because the "x" wording absorbs it. Update the memlog `(version)` entry to 8.5.11.

### T-4 MassTransit.MongoDb 8.x transactional bus outbox + consumer inbox with MongoDB.Driver 3.x — confirmed (reality-checked against the package)

- **Spine:** AD-10 / D5(b), MassTransit's MongoDB transactional outbox (bus outbox) + consumer inbox. The unit of work starts its transaction on MassTransit's MongoDB session.
- **Found:** I inspected the `MassTransit.MongoDb 8.5.11` nupkg (net10.0 lib and its XML docs). It contains:
  - `AddMongoDbOutbox(IBusRegistrationConfigurator, ...)` and `IMongoDbOutboxConfigurator.UseBusOutbox(...)` with `BusOutboxDeliveryService`;
  - `UseMongoDbOutbox(IReceiveEndpointConfigurator, ...)`, the consumer outbox and inbox;
  - `InboxState`, `OutboxState` and `OutboxMessage` documents, plus `InboxCleanupService` / `DisableInboxCleanupService`, `DuplicateDetectionWindow` and `QueryDelay`;
  - `MongoDbContext.Session` (`IClientSessionHandle`) and `StartTransaction`, which is the session hook the unit of work needs.

  The nuspec for **8.5.11 depends on MongoDB.Driver >= 3.12.0** on every target, so the pair 8.5.11 + 3.12.0 is consistent. Note that older 8.5.x patches carried lower floors (8.5.8 had >= 3.6.0).
- **Evidence:** https://www.nuget.org/packages/MassTransit.MongoDb/8.5.11 (nuspec dependency group net10.0 → MongoDB.Driver 3.12.0). The public docs page for MongoDB persistence now redirects to `masstransit.massient.com`, and its old path returns 404. The official outbox docs only show the EF Core sample: https://masstransit-project.com/advanced/transactional-outbox.html
- **Caveats to carry into epic 1 (medium):**
  1. The bus outbox needs a replica set, which Atlas M0 and the sandbox single-node replica set both provide.
  2. The spine's own unit of work must use **the same `MongoDbContext` session** that MassTransit opens. There is no first-party doc page showing that pattern with a custom repository layer, so epic 1 must prove it with an integration test: an aggregate write plus a publish, then abort, and check that neither persisted.
- **Spine change (medium):** in AD-10, add "MassTransit.MongoDb and MongoDB.Driver are pinned as a pair; the MassTransit.MongoDb nuspec floor on MongoDB.Driver must be ≤ the pinned driver (8.5.11 → 3.12.0)". Also add to the epic 1 proof list (memlog `(question)` line 22): "(d) aggregate write + bus-outbox publish commit and abort together on one `MongoDbContext` session".

### T-5 MassTransit v9 licensing — confirmed

- **Spine:** v9 is commercial and breaks NFR-16; v8 stays Apache 2.0.
- **Found:** **MassTransit 9.2.3** (2026-10-02) has the nuspec license `https://massient.com/license` (non-OSS). 8.5.11 is Apache-2.0. The project site now redirects to `masstransit.massient.com`, the "v9 announcement" page names Massient as the company behind the commercial v9, and there is a "Configure the license key" page. **No official v8 end-of-support date was found.** The end-2026 date in the spine and memlog comes only from an unofficial fork announcement.
- **Evidence:** https://www.nuget.org/packages/MassTransit/9.2.3 ; https://masstransit.massient.com/introduction/v9-announcement ; https://github.com/elsa-workflows/elsa-core/discussions/6583
- **Verdict:** confirmed (licensing); unverifiable (the v8 end-of-support date).
- **Spine change (low):** in the "MassTransit after v8 support ends" deferred row, mark the end-2026 date "[unofficial — no vendor statement found 2026-10-10]". Keep "Revisit when: end of 2026, or the first sale".

### T-6 FluentValidation — outdated minor (low)

- **Spine:** "12.1.x (Apache 2.0)". Memlog: "12.1.x, confirm at pin time".
- **Found:** **12.2.0** was published **2026-10-10** (today). It is Apache-2.0 and targets net8.0 (runs on net10.0). 12.1.1 is the previous version.
- **Evidence:** https://www.nuget.org/packages/FluentValidation/12.2.0
- **Spine change (low):** in the Stack table, change `FluentValidation | 12.1.x (Apache 2.0)` → `FluentValidation | 12.x, latest at pin time (12.2.0 on 2026-10-10; Apache 2.0)`.

### T-7 NetArchTest.Rules 1.3.x — outdated / stale (high)

- **Spine:** AD-2 fitness tests "(NetArchTest)"; Stack `NetArchTest.Rules | 1.3.x`. No memlog `(version)` entry, so this was never web-checked.
- **Found:** **NetArchTest.Rules 1.3.2** is the latest. It was published **2021-05-23**, targets only netstandard2.0, and has no release in over five years. It predates .NET 6+ metadata changes, and nothing confirms it was tested on .NET 10. Live alternatives:
  - **NetArchTest.eNhancedEdition 1.4.5** (MIT, 2025-06-04): an API-compatible fork that is still maintained.
  - **TngTech.ArchUnitNET 0.13.4** (Apache-2.0, **2026-08-20**), with a `TngTech.ArchUnitNET.xUnitV3` adapter (0.13.4). That adapter matches ADR-0006's xUnit v3 directly.
- **Evidence:** https://www.nuget.org/packages/NetArchTest.Rules/1.3.2 ; https://www.nuget.org/packages/NetArchTest.eNhancedEdition ; https://www.nuget.org/packages/TngTech.ArchUnitNET/0.13.4 ; https://www.nuget.org/packages/TngTech.ArchUnitNET.xUnitV3
- **Why high:** AD-2, AD-3 and AD-5 hang several build-failing fitness rules on this library, and the sandbox agent will pin what the Stack table says.
- **Spine change (high):** replace the Stack row with `ArchUnitNET (TngTech.ArchUnitNET + TngTech.ArchUnitNET.xUnitV3) | 0.13.x (Apache-2.0)`. As a lighter fallback, use `NetArchTest.eNhancedEdition | 1.4.x (MIT)`. Change AD-2's "Fitness tests (NetArchTest)" to "Fitness tests (ArchUnitNET)". Add a memlog `(version)` entry. The project-reference rule in AD-3 stays a plain MSBuild/XML-reading test either way.

### T-8 Transloco (@jsverse/transloco) — outdated major (medium)

- **Spine:** Stack `7.6.1 (Angular 22 peer support checked in epic 2)`; D8(b) "verify Angular 22 peer support at epic 2".
- **Found:** the latest is **8.4.0** (2026-06-13), MIT, peer `@angular/core >=16.0.0` and `rxjs >=6.0.0`. 7.6.1 is from **2025-03-24** and is one major behind. 8.0.0 was released 2025-09-14, and a `next` tag points to 9.0.0-alpha.4. The open-ended peer range (>=16) means **npm accepts Angular 22.2.2** (the current Angular, 2026-10-08) without a peer conflict. Runtime compatibility with zoneless and signals is not proven by the peer range alone. For comparison, `@ngx-translate/core` 18.0.0 (2026-06-09) is also alive.
- **Evidence:** https://www.npmjs.com/package/@jsverse/transloco (registry JSON `https://registry.npmjs.org/@jsverse/transloco`)
- **Verdict:** outdated pin. The peer question is already answered, so it need not wait for epic 2.
- **Spine change (medium):** change `Transloco | 7.6.1 (Angular 22 peer support checked in epic 2)` → `Transloco (@jsverse/transloco) | 8.x (8.4.0 on 2026-10-10; peer @angular/core >=16 accepts 22.2)`. In D8(b), replace "verify Angular 22 peer support at epic 2" with "epic 2 proves Transloco under zoneless change detection; fallback @angular/localize".

### T-9 Angular / TypeScript / Vitest frontend pins — confirmed

- **Spine:** Angular 22.2, TypeScript 6.0, Vitest 5.x; repo `^22.2.0`, CLI `^22.2.2`.
- **Found:** `@angular/core` latest is **22.2.2** (2026-10-08). The `next` tag is 22.3.0-next.1. The repo's ranges resolve to the current release.
- **Evidence:** https://www.npmjs.com/package/@angular/core
- **Spine change:** none.

### T-10 Aspire 13.x and hosting the Angular app — outdated minor; the API shape needs naming (medium)

- **Spine:** Stack `Aspire (AppHost, ServiceDefaults, JavaScript app hosting) | 13.4`; AD-18 "AppHost runs the API, Angular, a MongoDB replica set and RabbitMQ". No memlog `(version)` entry.
- **Found:**
  - The latest Aspire is **13.6.1** (Aspire.Hosting.AppHost, Aspire.AppHost.Sdk, Aspire.Hosting.JavaScript, Aspire.Hosting.MongoDB and Aspire.Hosting.RabbitMQ all at 13.6.1; Aspire.Hosting.JavaScript 13.6.1 published 2026-10-07; MIT). 13.4.6 dates from 2026-06-19, so the spine is two minors behind.
  - In Aspire 13.0, `Aspire.Hosting.NodeJs` was renamed `Aspire.Hosting.JavaScript`, and `AddNpmApp` was **removed** in favour of `AddJavaScriptApp` / `AddViteApp`.
  - The official JavaScript integration page shows Angular under **`AddViteApp(..., runScriptName: "dev")`**, followed by `PublishAsStaticWebsite` and `WithExternalHttpEndpoints`, with a `proxy.conf.js` that reads `API_HTTPS` / `API_HTTP`. It warns not to call `WithHttpEndpoint()` on a Vite resource, because `AddViteApp` already registers `http` with `PORT`.
  - So, yes: Aspire 13 can host the Angular app. The repo's `package.json` has no `dev` script yet (only `start`), so the Angular dev server must honour `PORT` (for example `ng serve --port $PORT`).
- **Evidence:** https://www.nuget.org/packages/Aspire.Hosting.JavaScript/13.6.1 ; https://aspire.dev/integrations/frameworks/javascript/ ; https://devblogs.microsoft.com/aspire/aspire13/
- **Risk:** the sandbox agent writes `AddNpmApp` from training data (removed in 13), or fights the port wiring.
- **Spine change (medium):**
  - Stack row → `Aspire (AppHost, ServiceDefaults, Aspire.Hosting.JavaScript/MongoDB/RabbitMQ) | 13.x, latest at pin time (13.6.1 on 2026-10-10)`.
  - Add to AD-18 Local: "Angular is an `AddViteApp` resource running the `dev` script (`ng serve` bound to `PORT`) with an `/api` dev proxy to the API's endpoint; `AddNpmApp` no longer exists". Add a memlog `(version)` entry.

### T-11 Render free tier — confirmed for spin-down and hours; the /api rewrite and the .NET runtime were not reality-checked (high)

- **Spine:** AD-1 (750 h, one always-on service ~744 h); AD-11 (an external monitor every 5 min keeps the service awake); AD-18 ("The static site rewrites `/api/*` to the web service"); AD-12 (SameSite=Strict session cookies that rely on one origin).
- **Confirmed** (render.com/docs/free, fetched 2026-10-10):
  - spin-down after **15 minutes without inbound traffic** (WebSocket messages count), about one minute to spin up;
  - **750 free instance hours per workspace per calendar month** (spun-down time is not counted);
  - free tier only for web services, static sites, Postgres and Key Value;
  - SMTP ports 25/465/587 blocked;
  - single instance, restartable at any time, ephemeral filesystem;
  - free services draw on the workspace's included outbound bandwidth and pipeline minutes; without a payment method an overage **suspends the service for the rest of the month**;
  - suspension is possible for unusually high outbound traffic.

  A 5-minute ping keeps the service up about 744 h a month, which is under 750. That holds for one service only, as AD-1 says.
- **Not confirmed (high):**
  1. **Rewrites to a full URL:** the docs say only "Destination can be either a path or a full, publicly accessible URL" and "Does not redirect the browser". They say nothing about POST bodies, request headers (the AD-5 `X-Tenant`, antiforgery tokens), `Set-Cookie`/`Cookie` pass-through, timeouts, or WebSockets. Community threads report 502s for internal targets and noticeably higher latency through the rewrite. `onrender.com` is on the Public Suffix List, so the fallback of calling `api-x.onrender.com` from `web-x.onrender.com` is **cross-site**: SameSite=Strict cookies would not be sent. The fallback therefore needs a custom domain (for example `app.` and `api.` under one registrable domain) or a single web service that also serves the Angular build.
  2. **.NET on Render:** Render has **no native .NET runtime**. Its docs list .NET as a reason to use Docker. The API must be a **Docker** web service, and the spine never says so. The docs also don't explicitly confirm Docker on free instances, though the Docker page mentions limits "on free instance types".
- **Evidence:** https://render.com/docs/free ; https://render.com/docs/redirects-rewrites ; https://render.com/docs/native-environments ; https://render.com/docs/docker ; https://community.render.com/t/setting-cookies-onrender-com/7886 ; https://community.render.com/t/rewrite-rules-that-target-internal-urls/2940
- **Spine change (high):**
  - AD-18: add "The API deploys to Render as a Docker web service (`runtime: docker` in `render.yaml`, multi-stage image on the .NET 10 SDK/ASP.NET runtime images); Render has no native .NET runtime".
  - AD-18: replace "The static site rewrites `/api/*` to the web service" with "The static site rewrites `/api/*` to the web service's public URL. **Epic 1 proves** through the rewrite: POST with a body, the `X-Tenant` and antiforgery headers, `Set-Cookie` on response and `Cookie` on request, and added latency. **If any fails**, fallback A: one custom domain with `app.`/`api.` subdomains (same-site, SameSite=Strict holds); fallback B: the API web service also serves the Angular build (gives up ADR-0006 deploy independence — needs an ADR)."
  - The memlog `(question)` on line 22 already lists (b). Promote it into the spine; the spine currently carries only the timer proof (AD-11).

### T-12 MongoDB Atlas Free (M0) — limits confirmed; two gaps (medium)

- **Spine:** 100 ops/s budget (AD-12 caches, deferred "single conditional update" row); 3-node replica set with transactions (deployment diagram "transactions, majority"); MongoDB 8.0; the append-only audit role deferred to epic 2.
- **Confirmed** (Free Cluster Limits page, 2026-10-10):
  - **100 operations per second**; throttling with a one-second cooldown when exceeded;
  - 0.5 GB storage, 500 connections, 100 databases / 500 collections;
  - MongoDB **8.0** (not user-upgradable); no backups (mongodump/mongorestore, not on admin);
  - pause after 30 days with zero connections; 32 MB in-memory sort; 50 aggregation stages; namespace 95 bytes (database name 38 bytes).
- **New, not in memlog (medium):** **data transfer is capped at 10 GB in and 10 GB out per rolling seven-day period**, also throttled when exceeded. The spine's per-request caches and the 5-minute ping don't threaten it, but reconciliation and fan-out jobs should be sized against it.
- **Transactions — unverifiable from docs:** neither the limits page nor the unsupported-commands page mentions `startTransaction`, `commitTransaction` or `abortTransaction`. MongoDB's own older statement says replica-set ACID transactions are available on the free tier, and only sharded/distributed ones are not. The epic 1 proof (memlog line 22 (c)) is still needed.
- **Custom roles — partially resolved:** `createRole`, `updateRole` and `grantRolesToUser` are **unsupported as database commands on Free clusters**. Custom roles can still be made through the Atlas UI, CLI or Admin API (the docs note "Changes to custom roles might take up to 30 seconds to deploy in Free clusters"), and they support Collection Actions scoped to a named collection. Whether `insert` + `find` alone are in the custom-role action subset is not listed on the page. The deferred row ("collection-scoped insert+find role") can therefore likely be met **only through Atlas UI/CLI/API, not from app startup code**.
- **Evidence:** https://www.mongodb.com/docs/atlas/reference/free-shared-limitations/ ; https://www.mongodb.com/docs/atlas/unsupported-commands/ ; https://www.mongodb.com/docs/atlas/security-add-mongodb-roles/
- **Spine change (medium):**
  - Add an "Atlas Free limits relied on" line under AD-18 or the Stack: "100 ops/s; 10 GB in / 10 GB out per rolling 7 days; 0.5 GB; 500 connections; MongoDB 8.0; no backups".
  - In the deferred "append-only audit role" row, add: "Role must be created via Atlas CLI/Admin API (declared alongside `render.yaml`), since `createRole` is unsupported on Free clusters".
  - Add "Atlas Free multi-document transactions" to the epic 1 proof list in the spine.

### T-13 CloudAMQP Little Lemur (free) — confirmed

- **Spine:** AD-18 demo environment; Stack "RabbitMQ (CloudAMQP Little Lemur) provider-managed 3.x/4.x; confirmed in epic 1"; AD-10 "There are no broker delayed messages".
- **Found** (plans page, 2026-10-10):
  - Little Lemur: **1 M messages/month, 20 connections, max 100 queues, max 10,000 queued messages**, queues removed after 28 days idle;
  - shared multitenant server, separated by vhost;
  - **plugins are not available on shared plans** (dedicated RabbitMQ only), so the delayed-message exchange is unavailable;
  - new and not in memlog: **free instances with no published or consumed messages for two months are deleted** after a warning email;
  - the RabbitMQ version is not stated.
- **Evidence:** https://www.cloudamqp.com/plans.html
- **Verdict:** confirmed. AD-10's "no broker delayed messages" and in-memory retries are exactly right for this plan. This removes the memlog's "delayed-message plugin unconfirmed" open point: it is unavailable.
- **Spine change (low):** add to AD-10 "(CloudAMQP shared plans have no plugins, so MassTransit `UseDelayedRedelivery`/delayed scheduling must not be used)". Note the 1 M/month quota next to the Grafana/Atlas limits. MassTransit counts publish + deliver, and the outbox and fan-out of announcements (FR-33) consume it.

### T-14 Grafana Cloud Free — confirmed

- **Spine:** AD-17 OTLP to Grafana Cloud Free. Memlog: "third-party summaries 2026, confirm at setup".
- **Found** (official pricing page, 2026-10-10):
  - **10k active metric series/month, 50 GB logs, 50 GB traces, 50 GB profiles per month, 14-day retention, 3 active users**;
  - Application Observability "native OpenTelemetry and Prometheus support";
  - also **Synthetics: 100k API test executions/month free**, which is relevant to T-15.
- **Evidence:** https://grafana.com/pricing/
- **Verdict:** confirmed from the vendor page; the memlog's third-party figures were right.
- **Spine change:** none. Optionally upgrade the memlog `(constraint)` source to "grafana.com/pricing, 2026-10-10".

### T-15 Free uptime monitor every 5 minutes — confirmed technically; licence risk (medium)

- **Spine:** AD-11 "A free external uptime monitor calls `/health` every 5 minutes, which keeps the service awake". The deployment diagram names it "Free uptime monitor". Not recorded in memlog.
- **Found:**
  - The UptimeRobot Free plan offers **50 monitors at a 5-minute interval**, with HTTP monitors and 3-month retention, "Good for hobby and non-profit projects".
  - Several 2025–2026 secondary sources report that UptimeRobot's **Terms limit the free plan to non-commercial/personal use since late 2024** (business and revenue-generating sites excluded). I could not read that clause in the primary terms in this run.
  - SlotBook is a demo today, but the clinic launch makes it commercial.
  - A 5-minute check is inside Render's 15-minute idle window (three chances per window), so the mechanism is sound.
  - Grafana Cloud Free, already in the stack, includes Synthetics with **100k API test executions/month**. One probe every 5 minutes is about 8,640 a month.
- **Evidence:** https://uptimerobot.com/pricing/ ; https://community.mainwp.com/t/advanced-uptime-monitor-only-for-private-use/9866 ; https://notifier.so/guides/uptimerobot-pricing-2026/ ; https://grafana.com/pricing/
- **Verdict:** confirmed (5-minute checks on free); unverifiable (UptimeRobot's commercial-use clause, from secondary sources only).
- **Spine change (medium):** in AD-11, change "A free external uptime monitor" → "**Grafana Cloud Synthetics** (free: 100k API checks/month) calls `/health` every 5 minutes from one probe. That keeps the service awake and alerts on downtime. Fallback: any free monitor whose terms allow commercial use." Update the deployment diagram label. Add a memlog `(constraint)` entry.

## Cross-cutting finding

### X-1 Stack rows written as "checked later" instead of checked now (medium)

Three rows defer a check that a registry lookup answers today:

- Transloco: "peer support checked in epic 2"; the answer is already yes, >=16 (T-8).
- RabbitMQ: "confirmed in epic 1".
- MassTransit: "latest 8.5 patch at pin time".

Two rows carry no check at all: NetArchTest (T-7) and Aspire 13.4 (T-10). The sandbox agent pins whatever the Stack table says, so a stale cell becomes a stale dependency.

- **Spine change (medium):** add a "Checked" column to the Stack table (date + source per row), and a one-line rule above it: "Rows are floors verified on the date shown. The implementing story pins the latest patch and updates the date."

## Summary

| ID | Item | Verdict | Tier | Spine change |
| --- | --- | --- | --- | --- |
| T-1 | .NET 10 SDK (10.0.401 / runtime 10.0.12) | confirmed | low | Label 10.0.100 as floor; name sandbox SDK image |
| T-2 | MongoDB.Driver 3.12.0 | confirmed | — | none |
| T-3 | MassTransit 8.5.11 | confirmed | low | memlog → 8.5.11 |
| T-4 | MassTransit.MongoDb bus outbox + inbox on Driver 3.x | confirmed (package inspected) | medium | Pin as a pair; add the epic 1 shared-session proof |
| T-5 | MassTransit v9 commercial; v8 EOS date | confirmed / EOS unverifiable | low | Mark end-2026 as unofficial |
| T-6 | FluentValidation | outdated (12.2.0 today) | low | 12.x latest at pin |
| T-7 | NetArchTest.Rules 1.3.x | outdated (last release 2021) | **high** | Switch to ArchUnitNET 0.13.x + xUnitV3 (or the NetArchTest.eNhancedEdition fork) |
| T-8 | Transloco 7.6.1 | outdated (8.4.0) | medium | 8.x; peer >=16 already accepts Angular 22 |
| T-9 | Angular 22.2 / TS 6.0 / Vitest 5 | confirmed | — | none |
| T-10 | Aspire 13.4 / Angular hosting | outdated (13.6.1) | medium | 13.x latest; `AddViteApp` + PORT; `AddNpmApp` removed |
| T-11 | Render free: rewrite + .NET runtime | partly unverified | **high** | Docker web service; rewrite proof + custom-domain fallback (onrender.com is a public suffix) |
| T-12 | Atlas Free limits | confirmed + gaps | medium | Add 10 GB/7-day transfer; `createRole` unsupported (use the Atlas API); transactions proof |
| T-13 | CloudAMQP Little Lemur | confirmed | low | No plugins, so no delayed redelivery; 2-month-idle deletion |
| T-14 | Grafana Cloud Free | confirmed | — | none |
| T-15 | Free 5-minute uptime monitor | confirmed / licence unverifiable | medium | Use Grafana Synthetics |
| X-1 | Stack rows deferring checks | — | medium | Add a "Checked" column |

**Overall verdict:** pass with changes. No settled decision is contradicted. Two high items must be fixed before the spine binds epic 1: the NetArchTest staleness, and the unproven Render rewrite plus the missing Docker runtime.
