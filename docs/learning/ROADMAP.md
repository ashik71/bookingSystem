# Booking System Learning Roadmap
### .NET 8 · Angular · SQL Server · NHibernate · Redis · RabbitMQ · Kafka · MongoDB · Docker · AWS

**Goal:** Learn every technology in the Ding Principal/Senior .NET Engineer job post by building a real appointment booking system, step by step.

**How to use this file:** Each phase has two parts:
1. **Learn:** study the topics using the resources.
2. **Build:** apply what you learned to the app before moving on.

Tick the checkboxes (`- [x]`) as you finish things. Don't skip the Build part; that's where the real learning happens.

**Project note:** Use a generic name (for example, *SlotBook – Consultation Booking System*) and your own design. Don't reuse your client's code, name, or branding.

---

## 🔖 Legend (What the Marks Mean)

| Mark | Meaning |
|---|---|
| 🆕 | **Added in Update 2** (items that were not in the first version of this roadmap) |
| ⭐ | **High priority** addition: fills a real gap, do it |
| 💡 | **Nice-to-have** extra: do it if you have time |
| ⏱ | **Estimated time** for the phase |

So `🆕⭐` means "new item, important" and `🆕💡` means "new item, optional extra".

**Time estimates assume about 10–12 hours per week** (part-time alongside work). If you can give more hours, phases will go faster. Total: roughly **7–9 months**.

---

## Progress Tracker

| Phase | Topic | ⏱ Time | Status |
|---|---|---|---|
| 0 | Setup & Fundamentals | 1 week | ⬜ |
| 1 | ASP.NET Core Web API & Architecture | 1–2 weeks | ⬜ |
| 2 | SQL Server & NHibernate | 2 weeks | ⬜ |
| 3 | Authentication, Security & Angular Basics | 2–3 weeks | ⬜ |
| 4 | Practitioners & Scheduling | 2 weeks | ⬜ |
| 5 | Booking Flow, Concurrency & Testing | 2–3 weeks | ⬜ |
| 6 | Redis Caching, Slot Locks & Load Testing | 1–2 weeks | ⬜ |
| 7 | RabbitMQ, Background Worker & Outbox | 2 weeks | ⬜ |
| 8 | MongoDB (Reviews & Logs) | 1 week | ⬜ |
| 9 | Payments (Optional) | 1–2 weeks | ⬜ |
| 🆕 9.5 | Localization: Bangla, English, Arabic (RTL) | 1–2 weeks | ⬜ |
| 10 | Docker, CI/CD, Code Quality & Logging | 2 weeks | ⬜ |
| 11 | Kafka Events | 1–2 weeks | ⬜ |
| 12 | Microservices & Observability | 3–4 weeks | ⬜ |
| 13 | AWS Deployment | 2 weeks | ⬜ |
| 14 | Multi-Tenancy | 2–3 weeks | ⬜ |
| 15 | Legacy .NET Framework 4.8 Migration (Optional) | 1 week | ⬜ |

Change ⬜ to ✅ when a phase is done. Write your actual finish date next to it to see how your estimates compare.

---

## Phase 0 — Setup & Fundamentals
⏱ **1 week**

### Learn
- [ ] Git basics: branches, commits, pull requests
  - https://git-scm.com/book/en/v2
- [ ] Modern C# refresh: async/await, LINQ, records, pattern matching, nullable types
  - https://learn.microsoft.com/en-us/dotnet/csharp/
  - https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-8/overview
- [ ] TypeScript basics (needed for Angular)
  - https://www.typescriptlang.org/docs/handbook/intro.html
- [ ] Docker basics: images, containers, volumes
  - https://docs.docker.com/get-started/
- [ ] IDE: Rider or Visual Studio 2022 shortcuts, debugging, refactoring
  - https://www.jetbrains.com/help/rider/

### Build
- [ ] Create a GitHub repo (private for now)
- [ ] Create `BookingSystem.sln` with an empty `BookingSystem.Api` project (.NET 8)
- [ ] Create the Angular project `booking-web` using the Angular CLI
- [ ] Run SQL Server in Docker and connect with Azure Data Studio or SSMS
  - https://learn.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker
- [ ] Commit: "Initial project setup"

---

## Phase 1 — ASP.NET Core Web API & Architecture
⏱ **1–2 weeks**

### Learn
- [ ] ASP.NET Core Web API: controllers, routing, model binding, validation
  - https://learn.microsoft.com/en-us/aspnet/core/web-api/
- [ ] Middleware pipeline and error handling
  - https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/
- [ ] Dependency injection concepts, then Lamar (used by Ding)
  - https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection
  - https://jasperfx.github.io/lamar/
- [ ] Clean / layered architecture
  - https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures
- [ ] Modular monolith idea (study the structure; don't copy it all)
  - https://github.com/kgrzybek/modular-monolith-with-ddd
- [ ] Swagger / OpenAPI documentation
  - https://learn.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle
- [ ] Health checks
  - https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks
- [ ] 🆕💡 API versioning (`/api/v1/...`)
  - https://github.com/dotnet/aspnet-api-versioning
- [ ] 🆕💡 Code style and analyzers: `.editorconfig`, .NET code analysis
  - https://editorconfig.org/
  - https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview

### Build
- [ ] Set up the folder structure: `Api/`, `BuildingBlocks/`, `Modules/` (Users, Practitioners, Scheduling, Bookings, Notifications)
- [ ] Each module gets `Domain / Application / Infrastructure / Contracts` folders or projects
- [ ] Replace default DI with Lamar in `Program.cs`
- [ ] Add global error-handling middleware that returns clean JSON errors
- [ ] Add Swagger and a `/health` endpoint
- [ ] 🆕💡 Add API versioning so all routes start with `/api/v1/`
- [ ] 🆕💡 Add `.editorconfig` and enable .NET analyzers (treat important warnings as errors)
- [ ] Commit: "API skeleton with modular structure"

---

## Phase 2 — SQL Server & NHibernate
⏱ **2 weeks**

### Learn
- [ ] SQL fundamentals: joins, indexes, transactions, isolation levels
  - https://learn.microsoft.com/en-us/sql/sql-server/
  - https://use-the-index-luke.com/
- [ ] NHibernate: session factory, sessions, mappings, HQL/LINQ queries, lazy loading, N+1 problem
  - https://nhibernate.info/doc/
- [ ] Repository and Unit of Work patterns
  - https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/infrastructure-persistence-layer-design
- [ ] Database migrations (FluentMigrator works well with NHibernate)
  - https://fluentmigrator.github.io/

### Build
- [ ] Set up NHibernate session factory in `BuildingBlocks.Persistence`
- [ ] Register a session per HTTP request with Lamar
- [ ] Create tables with migrations: **Users**, **Practitioners**
- [ ] Write NHibernate mappings for both entities
- [ ] Create basic CRUD endpoints for practitioners (admin only later)
- [ ] Commit: "Database and NHibernate setup"

---

## Phase 3 — Authentication, Security & Angular Basics
⏱ **2–3 weeks**

### Learn
- [ ] Password hashing and JWT authentication in ASP.NET Core
  - https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication
  - https://jwt.io/introduction
- [ ] Role-based authorization
  - https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles
- [ ] 🆕⭐ OWASP Top 10: the most common web security risks (injection, broken access control, etc.)
  - https://owasp.org/www-project-top-ten/
  - https://cheatsheetseries.owasp.org/
- [ ] 🆕⭐ CORS in ASP.NET Core (allow only your Angular app's origin)
  - https://learn.microsoft.com/en-us/aspnet/core/security/cors
- [ ] 🆕⭐ Keeping secrets out of code (User Secrets in development, environment variables later)
  - https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets
- [ ] 🆕⭐ Angular security (XSS protection, sanitization)
  - https://angular.dev/best-practices/security
- [ ] Angular fundamentals: components, standalone components, templates, signals
  - https://angular.dev/tutorials/learn-angular
  - https://angular.dev/guide/signals
- [ ] Angular routing, lazy loading, route guards
  - https://angular.dev/guide/routing
- [ ] HttpClient and interceptors
  - https://angular.dev/guide/http
- [ ] Reactive forms and validation
  - https://angular.dev/guide/forms/reactive-forms
- [ ] RxJS basics (Observables, pipe, map, switchMap)
  - https://rxjs.dev/guide/overview

### Build
- [ ] Backend: `POST /api/v1/auth/register`, `POST /api/v1/auth/login` returning a JWT
- [ ] Roles: User, Practitioner, Admin
- [ ] Angular: `core/auth` with `AuthService`, `auth.interceptor.ts`, `auth.guard.ts`, `role.guard.ts`
- [ ] Angular: Login and Register pages with reactive forms
- [ ] Protect a test page so only logged-in users can see it
- [ ] 🆕⭐ Configure CORS to allow only `http://localhost:4200` (and your real domain later)
- [ ] 🆕⭐ Move the JWT signing key and connection strings into User Secrets (nothing secret in `appsettings.json` or Git)
- [ ] 🆕⭐ Access control check: a user can only see and cancel **their own** bookings (test by changing IDs in requests)
- [ ] Commit: "Authentication end to end"

---

## Phase 4 — Practitioners & Scheduling
⏱ **2 weeks**

### Learn
- [ ] Working with dates, times and time zones in .NET (`DateTimeOffset`, `TimeZoneInfo`)
  - https://learn.microsoft.com/en-us/dotnet/standard/datetime/choosing-between-datetime
- [ ] Angular Material: cards, date picker, tables, dialogs
  - https://material.angular.io/
- [ ] .NET background services (for generating slots on a schedule)
  - https://learn.microsoft.com/en-us/dotnet/core/extensions/workers
- [ ] 🆕💡 Pagination and filtering in REST APIs
  - https://github.com/microsoft/api-guidelines

### Build
- [ ] Tables: **WeeklyAvailability**, **TimeSlots** (with `Status` and `RowVersion`)
- [ ] Practitioner portal: set weekly schedule (for example, Saturday 10am–2pm)
- [ ] Slot generator: create TimeSlots for the next 4 weeks from WeeklyAvailability
- [ ] Public API: list practitioners, list available slots for a practitioner by date
- [ ] 🆕💡 Paginate the practitioner list (`?page=1&pageSize=20`) and return total count
- [ ] Angular: practitioner list page, practitioner detail page with a slot picker
- [ ] Commit: "Practitioners and scheduling"

---

## Phase 5 — Booking Flow, Concurrency & Testing
⏱ **2–3 weeks**

### Learn
- [ ] Optimistic concurrency and versioning in NHibernate (the `<version>` mapping)
  - https://nhibernate.info/doc/
- [ ] Unique filtered indexes in SQL Server
  - https://learn.microsoft.com/en-us/sql/relational-databases/indexes/create-filtered-indexes
- [ ] Unit testing with xUnit
  - https://xunit.net/
  - https://learn.microsoft.com/en-us/dotnet/core/testing/
- [ ] Integration testing ASP.NET Core
  - https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests
- [ ] Testcontainers (real SQL Server in tests)
  - https://dotnet.testcontainers.org/
- [ ] 🆕⭐ Rate limiting in ASP.NET Core (protect login and booking endpoints from abuse)
  - https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit
- [ ] 🆕⭐ Angular testing: components and services
  - https://angular.dev/guide/testing

### Build
- [ ] Table: **Bookings**
- [ ] Endpoints: book a slot, cancel a booking, list my bookings
- [ ] Double-booking protection, part 1: `RowVersion` on TimeSlots
- [ ] Double-booking protection, part 2: unique filtered index on Bookings (TimeSlotId) where Status ≠ Cancelled
- [ ] Angular: booking confirmation page and My Bookings page (upcoming/past, cancel)
- [ ] Unit tests for booking rules (can't book past slots, can't cancel after start time, etc.)
- [ ] Integration test: two users book the same slot at the same time → only one succeeds
- [ ] 🆕⭐ Rate limit: max 5 login attempts per minute per IP; max booking attempts per user
- [ ] 🆕⭐ Angular unit tests for `AuthService`, the booking service, and the slot picker component
- [ ] Commit: "Booking flow with concurrency protection"

**🎯 Milestone: You now have a working booking app (MVP).**

---

## Phase 6 — Redis Caching, Slot Locks & Load Testing
⏱ **1–2 weeks**

### Learn
- [ ] Redis data types, expiry (TTL), cache-aside pattern
  - https://redis.io/docs/latest/
- [ ] StackExchange.Redis client for .NET
  - https://stackexchange.github.io/StackExchange.Redis/
- [ ] Distributed caching in ASP.NET Core
  - https://learn.microsoft.com/en-us/aspnet/core/performance/caching/distributed
- [ ] Distributed locks with Redis (SET NX with expiry, and the Redlock idea)
- [ ] 🆕💡 Load testing with k6
  - https://grafana.com/docs/k6/latest/

### Build
- [ ] Add Redis to `docker-compose.yml`
- [ ] Create `BuildingBlocks.Caching` wrapper
- [ ] Cache available slots per practitioner per day; clear cache when a slot is booked
- [ ] Slot hold: lock a slot in Redis for ~5 minutes when the user starts checkout
- [ ] Measure response time before and after caching (write the numbers down for interviews)
- [ ] 🆕💡 k6 test: 200 virtual users try to book the same 10 slots → confirm zero double bookings
- [ ] 🆕💡 k6 test: slot listing endpoint with and without Redis → record requests/second
- [ ] Commit: "Redis caching and slot holds"

---

## Phase 7 — RabbitMQ, Background Worker & Outbox
⏱ **2 weeks**

### Learn
- [ ] RabbitMQ concepts: exchanges, queues, routing keys, acknowledgements
  - https://www.rabbitmq.com/tutorials
  - https://www.rabbitmq.com/tutorials/tutorial-one-dotnet
- [ ] Retries and dead-letter queues
  - https://www.rabbitmq.com/docs/dlx
- [ ] Transactional Outbox pattern
  - https://microservices.io/patterns/data/transactional-outbox.html
- [ ] Idempotent consumers (handle the same message twice safely)
  - https://microservices.io/patterns/communication-style/idempotent-consumer.html

### Build
- [ ] Add RabbitMQ to `docker-compose.yml`
- [ ] Table: **OutboxMessages**
- [ ] Save `BookingConfirmed` event to the outbox in the same transaction as the booking
- [ ] Outbox publisher: reads unprocessed messages and publishes them to RabbitMQ
- [ ] Create `BookingSystem.Worker` project: consumes events and sends emails (use a test SMTP like Mailpit)
- [ ] Reminder job: send a reminder 24 hours before an appointment
- [ ] Dead-letter queue for failed messages
- [ ] Commit: "Notifications via RabbitMQ with outbox"

---

## Phase 8 — MongoDB (Reviews & Logs)
⏱ **1 week**

### Learn
- [ ] Document modeling: when to embed vs reference
  - https://www.mongodb.com/docs/manual/data-modeling/
- [ ] MongoDB C# driver
  - https://www.mongodb.com/docs/drivers/csharp/current/
- [ ] Free MongoDB University courses
  - https://learn.mongodb.com/

### Build
- [ ] Add MongoDB to `docker-compose.yml`
- [ ] Reviews module: users can review a practitioner after a completed booking
- [ ] Show average rating on practitioner pages in Angular
- [ ] Activity logs collection (login, booking, cancellation)
- [ ] 🆕💡 Paginate the reviews list
- [ ] Commit: "Reviews and activity logs in MongoDB"

---

## Phase 9 — Payments (Optional)
⏱ **1–2 weeks**

### Learn
- [ ] Payment gateway flow: initiate → redirect → callback/IPN → verify
  - SSLCommerz: https://developer.sslcommerz.com/
  - bKash: https://developer.bka.sh/
- [ ] Idempotency for payments
  - https://stripe.com/blog/idempotency

### Build
- [ ] Table: **Payments**
- [ ] Sandbox payment flow: booking stays `Pending` until payment is verified
- [ ] Release the Redis slot hold if payment fails or times out
- [ ] 🆕⭐ Always verify payment on the server with the gateway; never trust the status sent from the browser
- [ ] Commit: "Sandbox payments"

---

## 🆕 Phase 9.5 — Localization: Bangla, English, Arabic (RTL)
⏱ **1–2 weeks** · 🆕💡 Whole phase is a nice-to-have extra

### Learn
- [ ] 🆕💡 Angular internationalization (i18n)
  - https://angular.dev/guide/i18n
  - Runtime language switching alternative: https://github.com/ngx-translate/core
- [ ] 🆕💡 Right-to-left (RTL) layout with Angular CDK Bidi
  - https://material.angular.io/cdk/bidi/overview
- [ ] 🆕💡 Localization in ASP.NET Core (translated error and email messages)
  - https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization

### Build
- [ ] 🆕💡 Language switcher in Angular: English, বাংলা, العربية
- [ ] 🆕💡 Arabic switches the whole layout to right-to-left (`dir="rtl"`)
- [ ] 🆕💡 Dates and numbers shown in the selected language's format
- [ ] 🆕💡 Backend: emails and API error messages in the user's language
- [ ] 🆕💡 Save the user's preferred language in their profile
- [ ] Commit: "Multi-language support with RTL"

---

## Phase 10 — Docker, CI/CD, Code Quality & Logging
⏱ **2 weeks**

### Learn
- [ ] Dockerfiles for .NET (multi-stage builds)
  - https://learn.microsoft.com/en-us/dotnet/core/docker/build-container
- [ ] Docker Compose
  - https://docs.docker.com/compose/
- [ ] Angular production build served by Nginx in Docker
  - https://angular.dev/tools/cli/deployment
- [ ] GitHub Actions CI
  - https://docs.github.com/en/actions
- [ ] Structured logging with Serilog
  - https://serilog.net/
- [ ] 🆕⭐ End-to-end testing with Playwright
  - https://playwright.dev/docs/intro
- [ ] 🆕💡 SonarCloud (free code quality and security scanning for public repos)
  - https://sonarcloud.io/
- [ ] 🆕⭐ GitHub Actions secrets (keep keys out of your pipeline files)
  - https://docs.github.com/en/actions/security-for-github-actions/security-guides/using-secrets-in-github-actions

### Build
- [ ] Dockerfiles for Api, Worker, and Angular
- [ ] One `docker compose up` starts the whole system
- [ ] GitHub Actions: build + run tests on every push
- [ ] Serilog with request logging and correlation IDs
- [ ] 🆕⭐ Playwright tests for the full flow: register → log in → pick slot → book → see it in My Bookings → cancel
- [ ] 🆕⭐ Run Angular unit tests and Playwright tests in GitHub Actions too
- [ ] 🆕💡 Add SonarCloud scan to the pipeline and fix the issues it finds
- [ ] 🆕⭐ Store all pipeline secrets in GitHub Secrets
- [ ] Write a good README with architecture diagram and setup steps
- [ ] Commit: "Fully containerized with CI"

**🎯 Milestone: Portfolio-ready project. You can show this to Ding.**

---

## Phase 11 — Kafka Events
⏱ **1–2 weeks**

### Learn
- [ ] Kafka concepts: topics, partitions, consumer groups, offsets, message keys
  - https://kafka.apache.org/intro
- [ ] Kafka with .NET (Confluent client)
  - https://developer.confluent.io/get-started/dotnet/
- [ ] RabbitMQ vs Kafka: when to use each (be ready to explain this in interviews)

### Build
- [ ] Add Kafka to `docker-compose.yml`
- [ ] Publish domain events to Kafka: `BookingCreated`, `BookingCancelled`, `PaymentCompleted`
- [ ] Simple analytics consumer: bookings per day, cancellation rate
- [ ] Keep RabbitMQ for task-style jobs (emails), Kafka for event streams
- [ ] Commit: "Kafka event streaming"

---

## Phase 12 — Microservices & Observability
⏱ **3–4 weeks**

### Learn
- [ ] Microservices architecture guide (Microsoft)
  - https://learn.microsoft.com/en-us/dotnet/architecture/microservices/
- [ ] Strangler Fig pattern
  - https://martinfowler.com/bliki/StranglerFigApplication.html
- [ ] Architecture tests with NetArchTest
  - https://github.com/BenMorris/NetArchTest
- [ ] API gateway with YARP
  - https://github.com/dotnet/yarp
- [ ] Distributed tracing with OpenTelemetry
  - https://opentelemetry.io/docs/languages/dotnet/
  - https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-with-otel
- [ ] Saga pattern for multi-step workflows
  - https://microservices.io/patterns/data/saga.html
- [ ] 🆕💡 Prometheus (collect metrics)
  - https://prometheus.io/docs/introduction/overview/
- [ ] 🆕💡 Grafana (dashboards)
  - https://grafana.com/docs/grafana/latest/getting-started/

### Build
- [ ] Add architecture tests first: modules can only talk through `Contracts`
- [ ] Extract **Notifications** into its own service (easiest, event-driven only)
- [ ] Extract **Reviews** into its own service (already uses its own MongoDB)
- [ ] Add YARP gateway in front of the monolith and new services
- [ ] Turn `BuildingBlocks` into shared NuGet packages
- [ ] Add OpenTelemetry tracing across services
- [ ] 🆕💡 Add Prometheus and Grafana to `docker-compose.yml`
- [ ] 🆕💡 Grafana dashboard showing: requests per second, error rate, response time, RabbitMQ queue depth, bookings per hour
- [ ] 🆕💡 Take a screenshot of the dashboard for your README and CV
- [ ] (Advanced) Extract **Bookings** with its own database
- [ ] Commit: "First microservices extracted"

---

## Phase 13 — AWS Deployment
⏱ **2 weeks**

### Learn
- [ ] AWS basics: IAM, VPC, EC2, security groups
  - https://aws.amazon.com/free/
  - https://skillbuilder.aws/
- [ ] .NET on AWS
  - https://aws.amazon.com/developer/language/net/
- [ ] Amazon ECS (running Docker containers)
  - https://docs.aws.amazon.com/ecs/
- [ ] Amazon RDS (SQL Server), S3, CloudWatch
- [ ] 🆕⭐ AWS Secrets Manager (production secrets)
  - https://docs.aws.amazon.com/secretsmanager/

### Build
- [ ] Push Docker images to Amazon ECR
- [ ] Deploy services on ECS (or a single EC2 with Docker Compose to stay in free tier)
- [ ] Database on RDS, profile images on S3, logs in CloudWatch
- [ ] 🆕⭐ Production secrets in AWS Secrets Manager; HTTPS only
- [ ] Set up budget alerts so you don't get unexpected bills
- [ ] Add the live link to your CV and README
- [ ] Commit: "Deployed on AWS"

---

## Phase 14 — Multi-Tenancy
⏱ **2–3 weeks**

### Learn
- [ ] Multi-tenant strategies: shared DB, schema per tenant, DB per tenant
  - https://learn.microsoft.com/en-us/azure/architecture/guide/multitenant/considerations/tenancy-models
- [ ] Multi-tenant data isolation patterns
  - https://learn.microsoft.com/en-us/azure/azure-sql/database/saas-tenancy-app-design-patterns
- [ ] NHibernate filters (automatic `WHERE TenantId = ...`)
  - https://nhibernate.info/doc/

### Build
- [ ] Table: **Tenants**; add `TenantId` to all tenant-owned tables
- [ ] Table: **TenantMemberships** (UserId, TenantId, Role)
- [ ] `BuildingBlocks.MultiTenancy`: `ITenantContext`, `TenantEntity`, NHibernate filter + interceptor
- [ ] `TenantResolutionMiddleware`: subdomain → JWT claim → header
- [ ] Redis keys, RabbitMQ headers, Kafka keys, MongoDB documents, and S3 paths all include TenantId
- [ ] Angular: tenant config loader, branding with CSS variables, tenant timezone pipe
- [ ] Angular: Tenant Admin and Super Admin areas
- [ ] **Tenant isolation tests:** Tenant A cannot read, update or book Tenant B's data
- [ ] 🆕⭐ Rate limits per tenant, so one busy tenant can't slow down the others
- [ ] 🆕💡 Grafana dashboard filtered by tenant
- [ ] Commit: "Multi-tenancy"

---

## Phase 15 — Legacy .NET Framework 4.8 Migration (Optional)
⏱ **1 week**

### Learn
- [ ] Differences between .NET Framework and modern .NET
  - https://learn.microsoft.com/en-us/dotnet/core/porting/
- [ ] .NET Upgrade Assistant
  - https://learn.microsoft.com/en-us/dotnet/core/porting/upgrade-assistant-overview

### Build
- [ ] Write a small .NET Framework 4.8 class library (for example, an old-style report generator)
- [ ] Use it from the app, then migrate it to .NET 8
- [ ] Document what broke and how you fixed it (great interview story)

---

## Interview Preparation (Do Alongside the Phases)

- [ ] System design practice
  - https://github.com/donnemartin/system-design-primer
- [ ] Senior/Principal engineer expectations
  - https://staffeng.com/
- [ ] Kanban basics (Ding uses Kanban)
  - https://www.atlassian.com/agile/kanban
- [ ] Prepare stories from this project, each with problem → approach → result:
  - [ ] Preventing double bookings (Redis lock + RowVersion + unique index)
  - [ ] Reliable notifications (outbox + RabbitMQ + dead-letter queue)
  - [ ] Splitting the monolith (what you extracted first and why)
  - [ ] 🆕⭐ Load test results: "200 concurrent users, zero double bookings, X requests/second with Redis"
  - [ ] 🆕⭐ Security decisions: rate limiting, access control checks, secrets management
- [ ] Keep a `LEARNINGS.md` in your repo: write 2–3 lines after each phase about what was hard

---

## Final Architecture (After Phase 13)

```
Angular (Nginx) — English / বাংলা / العربية (RTL)
      │
      ▼
YARP API Gateway (rate limiting)
      │
      ├──► Booking Monolith (Users, Practitioners, Scheduling, Bookings, Payments)
      │        ├── SQL Server (NHibernate)
      │        ├── Redis (cache + slot locks)
      │        └── Outbox ──► RabbitMQ / Kafka
      │
      ├──► Notification Service ◄── RabbitMQ
      ├──► Reviews Service ──► MongoDB
      └──► Analytics Consumer ◄── Kafka

Observability: OpenTelemetry → Prometheus → Grafana
All running in Docker, deployed on AWS (ECS, RDS, S3, CloudWatch, Secrets Manager)
CI: GitHub Actions → unit tests, Playwright E2E, SonarCloud
```

---

## 📝 Change Log

- **Version 1:** Original 15-phase roadmap.
- **Version 2 (🆕):** Added time estimates (⏱) for every phase, security (OWASP, CORS, secrets, rate limiting), Angular unit tests and Playwright E2E tests, k6 load testing, code quality (`.editorconfig`, analyzers, SonarCloud), API versioning and pagination, new Phase 9.5 for localization (Bangla, English, Arabic RTL), and Prometheus + Grafana observability.
