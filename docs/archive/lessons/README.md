# Lessons

Teaching references, one file per topic, grouped by ROADMAP phase. Written by the
AI during a session so the material survives the chat.

**Structure:** `docs/lessons/phase-N/NN-topic-name.md` — the number is the topic's
position in that phase's ROADMAP "Learn" list, so the folder reads in roadmap order.

## How these differ from the other docs

| File | Written by | Contains |
|---|---|---|
| `docs/lessons/` | AI | **How a thing works.** Reference, re-readable, interview prep |
| `docs/learning/LEARNINGS.md` | **Developer** | What was genuinely *hard*. Interview raw material |
| `docs/sessions/` | AI | What happened in one session; decisions, gaps exposed |
| `docs/adr/` | **Developer** | Decisions that stay true after the feature ships |

A lesson is reference material, not a record of a decision. If a lesson leads to a
choice that will still be true in six months, that choice belongs in an ADR.

---

## Phase 1 — ASP.NET Core Web API & Architecture

| # | Topic | Covers | Q&A |
|---|---|---|---|
| 01 | [Web API: controllers, routing, model binding, validation](phase-1/01-web-api-controllers-routing-model-binding-validation.md) | | 16 |
| 02 | [Middleware pipeline and error handling](phase-1/02-middleware-pipeline-and-error-handling.md) | | 10 |
| 03 | [Dependency injection](phase-1/03-dependency-injection.md) | *Lamar dropped* | 8 |
| 04 | [Clean / layered architecture](phase-1/04-clean-and-layered-architecture.md) | CURRICULUM **A4** | 7 |
| 05 | [Modular monolith](phase-1/05-modular-monolith.md) | CURRICULUM **A5**, ADR-0002 input | 7 |
| 06 | [Swagger / OpenAPI](phase-1/06-swagger-and-openapi.md) | | 6 |
| 07 | [Health checks](phase-1/07-health-checks.md) | | 6 |
| 08 | [API versioning · code style & analyzers](phase-1/08-api-versioning-and-code-analysis.md) | 🆕💡 optional | 5 |

**Suggested reading order:** 01 → 02 → 03 are the mechanics; 04 → 05 are the design
pair worth doing before Phase 2 touches a database; 06 → 07 → 08 are supporting.

---

## Interview use

Every lesson carries the same three devices:

- **§0 Ten-second recall** — one line per concept, for the night before
- **Interview question bank** — questions with a *weak* answer, a *strong* answer,
  and the **follow-up** the interviewer asks next. The follow-up is the real test
- **Questions to ask back** — these signal seniority more reliably than answers

**Frequency markers:** 🔴 near-certain in a .NET interview · 🟠 common ·
🟡 occasional, usually senior rounds.

> **On sourcing.** Questions are grouped by how often the *shape* recurs in .NET
> hiring loops and by interview type, **not** attributed to named employers.
> Claiming a specific question belongs to a specific company would be invention;
> what is real is that these shapes recur constantly.

---

## Standing conventions

- **.NET 10** throughout (ADR-0003). ROADMAP.md says .NET 8 because it predates the
  move — the lessons note each place the roadmap is stale
- Each lesson ends with **"what this lesson does not settle"**, naming the topic
  that decides each open question, so nothing gets silently assumed
- Cross-references between lessons are live links; follow them rather than
  duplicating content
