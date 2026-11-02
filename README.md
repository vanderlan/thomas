# **Thomas Api**

[![Build and Test](https://github.com/vanderlan/Thomas-Api/actions/workflows/build.yml/badge.svg)](https://github.com/vanderlan/Thomas-Api/actions/workflows/build.yml)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=vanderlan_Thomas-Api&metric=coverage)](https://sonarcloud.io/summary/overall?id=vanderlan_Thomas-Api) 
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=vanderlan_Thomas-Api&metric=alert_status)](https://sonarcloud.io/summary/overall?id=vanderlan_Thomas-Api)
[![Technical Debt](https://sonarcloud.io/api/project_badges/measure?project=vanderlan_Thomas-Api&metric=sqale_index)](https://sonarcloud.io/summary/overall?id=vanderlan_Thomas-Api)
[![GitHub release](https://img.shields.io/github/release/vanderlan/Thomas-Api.svg)](https://github.com/vanderlan/Thomas-Api/releases) 

**About this Project**

*A .NET Web Api (v10.0) + Angular (v22) application that watches the errors of a portfolio of projects.*

Thomas keeps a **portfolio of projects** and, for each one of them, collects the application errors
reported to **NewRelic**, storing them in an **errors knowledge base** that is kept up to date by a
background job. In the near future that knowledge base will be used to decide whether a **Pull Request**
must be opened on the **GitHub** repository of the project to fix an error.

**Current scope**

+ Projects portfolio API (CRUD): name, description, NewRelic id, GitHub repository, GitHub keys and the priority branch for the PRs;
+ NewRelic integration (NerdGraph / NRQL) that reads the `TransactionError` events of an application;
+ Error log sync job: runs every 5 minutes, stores the errors found, compares them with the ones already known and keeps the knowledge base up to date;
+ Errors knowledge base API: list, filter, inspect (with stack trace) and resolve errors;
+ Web UI (Angular 22, dark mode by default) covering all of the above: sign in, projects portfolio, manual sync, knowledge base browsing and resolution;
+ Users, authentication (JWT + refresh token) and the template infrastructure described below.

**Not implemented yet (by design)**

+ GitHub integration: the repository and the keys are only stored. The decision of opening a PR and the PR creation itself will be plugged on the `ProjectErrorsSynchronizedNotification` handler.

# **Repository layout**

	api/        -> the .NET 10 Web API (solution, projects, tests, Dockerfile)
	frontend/   -> the Angular 22 web application (Dockerfile + nginx config)
	.github/    -> CI/CD workflows (API and frontend jobs)
	docker-compose.yml, README.md, .gitignore, .editorconfig, .prettierrc -> shared files kept at the root

# **How it works**

```
                 every 5 minutes                          NerdGraph (NRQL)
 ErrorLogSyncWorker ───► SyncAllProjectsErrors ───► SyncProjectErrors ───► NewRelic
        (job)                 (per project)                 │
                                                            ▼
                                              group by fingerprint ──► compare with the
                                              (class + normalized msg)  ErrorLog collection
                                                            │
                                     ┌──────────────────────┴──────────────────────┐
                                     ▼                                             ▼
                           unknown: insert as "New"                 known: occurrences += n, last
                                                                    sample/seen updated, status →
                                                                    "Recurring" (or "Reopened" when
                                                                    it was "Resolved")
```

1. The **`ErrorLogSyncWorker`** (`BackgroundService`) runs at a fixed interval (`ErrorLogSync:IntervalMinutes`, 5 by default). The executions never overlap and a failure in one project does not stop the others;
2. For every project, the job collects from NewRelic the errors that happened **since the last synchronization** of that project (`Project.LastErrorSyncAt`). The first execution collects the `ErrorLogSync:InitialLookbackMinutes` window;
3. Every occurrence receives a **fingerprint** (SHA-512 of the error class + the message without its volatile parts: GUIDs, hexadecimals, numbers, casing and whitespace), so `Order 123 not found` and `Order 456 not found` are the same error;
4. The occurrences are grouped by fingerprint and compared with the **`ErrorLog`** collection of the project:
	+ unknown fingerprint → a new entry with status `New`, the amount of occurrences, first/last seen dates and the most recent sample (message, stack trace, transaction);
	+ known fingerprint → the entry is updated: occurrences are summed, the most recent sample and `LastSeenAt` are replaced and the status becomes `Recurring` (or `Reopened` if it had been `Resolved`);
5. The project `LastErrorSyncAt` is moved to the end of the window and everything is committed by a single unit of work;
6. A `ProjectErrorsSynchronizedNotification` is published with the summary of the synchronization (extension point for the PR decision).

The same synchronization can be triggered on demand with `POST /Projects/{id}/Errors/Sync`.

**Error statuses**

| Status      | Meaning                                                          |
|-------------|------------------------------------------------------------------|
| `New`       | Seen by the job for the first time                               |
| `Recurring` | Seen again after its registration                                |
| `Resolved`  | Marked as resolved through `PATCH /ErrorLogs/{id}/Resolve`       |
| `Reopened`  | Happened again after being resolved                              |

# **API**

All the endpoints require a JWT (`POST /Auth/Login`). Default user: `adm@thomas.com` / `123`.

**Projects** *(admin)*

| Method   | Route                          | Description                                              |
|----------|--------------------------------|----------------------------------------------------------|
| `GET`    | `/Projects?Query=&Page=&Quantity=` | Paginated list (filter by name)                      |
| `GET`    | `/Projects/{id}`               | Project details (the GitHub keys are never returned)     |
| `POST`   | `/Projects`                    | Creates a project                                        |
| `PUT`    | `/Projects/{id}`               | Updates a project (`gitHubKeys` is optional: when omitted the current keys are kept) |
| `DELETE` | `/Projects/{id}`               | Removes the project and its knowledge base               |
| `POST`   | `/Projects/{id}/Errors/Sync`   | Runs the error synchronization of the project right away |

	POST /Projects
	{
	  "name": "Billing API",
	  "description": "Invoices and payments",
	  "newRelicId": "MTIzNDU2fEFQTXxBUFBMSUNBVElPTnw3ODk",   // entity GUID or the numeric APM application id
	  "gitHubRepository": "my-org/billing-api",
	  "gitHubKeys": { "accessToken": "ghp_...", "privateKey": null },
	  "priorityBranch": "main"
	}

**Error logs (knowledge base)** *(admin and operator)*

| Method  | Route                                                       | Description                                        |
|---------|-------------------------------------------------------------|----------------------------------------------------|
| `GET`   | `/ErrorLogs?ProjectId=&Status=&Query=&Page=&Quantity=`      | Paginated list ordered by the most recent occurrence. `Query` searches the class and the message |
| `GET`   | `/ErrorLogs/{id}`                                           | Error details, including the stack trace           |
| `PATCH` | `/ErrorLogs/{id}/Resolve`                                   | Marks the error as resolved *(admin)*              |

# **Configuration**

	"ConnectionStrings": {
		"MongoDb": "mongodb://mongo:CHANGE_ME_Passw0rd!@localhost:27017/?authSource=admin"
	},
	"MongoDb": {
		"DatabaseName": "ThomasDev"
	},
	"NewRelic": {
		"ApiKey": "NRAK-...",                                   // User API key with NRQL permission
		"AccountId": 1234567,                                  // account that owns the monitored applications
		"GraphQlEndpoint": "https://api.newrelic.com/graphql", // https://api.eu.newrelic.com/graphql for EU accounts
		"TimeoutSeconds": 30,
		"MaxResults": 2000,                                    // NRQL limit of occurrences per execution
		"IncludeExpectedErrors": false                         // ignore the errors marked as expected on NewRelic
	},
	"ErrorLogSync": {
		"Enabled": true,               // turn the job off (the manual sync endpoint keeps working)
		"IntervalMinutes": 5,
		"InitialLookbackMinutes": 5    // window collected on the first sync of a project
	}

Secrets (NewRelic API key, JWT key, connection string) should be provided through environment variables
(`NewRelic__ApiKey`, ...) or user secrets, never committed. See `docker-compose.yml`.

**Error responses**

Business exceptions extend `BusinessException` and are translated by the `ExceptionMiddleware` into
`{ title, notificationType, errors[] }` payloads with the matching status code: `NotFoundException` → 404,
`ConflictException` → 409, `UnauthorizedUserException` → 401, `IntegrationException` (e.g. `NewRelicApiException`,
when NewRelic is unreachable, rejects the request or returns an unexpected payload) → **502 Bad Gateway**, any other
`BusinessException` → 400. Only unexpected exceptions produce a 500.

**Notes and limitations**

+ The job is meant to run on a single instance of the API. When scaling out, enable it (`ErrorLogSync:Enabled`) on one instance only;
+ NewRelic ingestion has a small delay; occurrences ingested after the window was collected are not retried;
+ The GitHub keys are stored as they are received. Encrypting them at rest is a pre-requisite of the GitHub integration.

**Libraries**

+ MongoDB Driver *(MongoDb)*
	+ BsonClassMap mappings;
	+ CreatedAt and UpdatedAt by default;
	+ Indexes and default data created on startup;
	+ Pagination.

+ MediaR
+ Swagger
+ Fluent Validation
+ Authentication and Authorization
	+ JWT Token;
	+ Claims and profiles configuration;
	+ Personalized decorators;
	+ Refresh Token.

+ Serilog
+ Bogus

+ Angular 22 *(frontend)*
	+ Standalone components, signals and zoneless change detection;
	+ Router with lazy routes, guards and component input binding;
	+ HttpClient with functional interceptors (bearer token + refresh token rotation);
	+ Reactive Forms;
	+ Vitest + jsdom unit tests;
	+ Prettier.

**Configurations and Patterns**

	1. Business Exceptions;
	2. Exception Middleware;
	3. Repository Pattern;
	4. Faker Objects;
	5. Unit Of Work;
	6. Base Repository;
	7. Environments configuration;
	8. CORS Configuration;
	9. Docker and Docker-Compose;
	10. API Versioning Configuration (by x-api-version header attribute);
	11. Globalization;
	12. Real database on CI Tests;
	13. Mediator + CQRS + Notification Pattern;
	14. Logs with Correlation Id;
	15. Document (MongoDb) database;
	16. Background job (hosted service) with scoped executions;
	17. External integration behind a domain contract (`INewRelicErrorProvider`), replaced by a fake on the API tests;
	18. Angular 22 UI: standalone components, signals, zoneless change detection, lazy routes, dark/light theme.

**CI & CD**

	1. Unit, Integration and Api Tests (API) + unit tests (frontend, Vitest);
	2. Continuous Integration (GitHub CI, one job per application);
	3. Continuous Delivery (GitHub CI);
	4. DockerHub Integration;
	5. Sonar Cloud Integration.

# **Frontend (Angular 22)**

The `frontend/` folder contains the web UI: a standalone, zoneless, signals based Angular 22 application
(Vitest for unit tests). **Dark mode is the default**; the theme toggle (sidebar / sign in page) persists the
preference and the light theme is applied through the `data-theme` attribute of `<html>`.

**Screens**

| Route                  | Description                                                                                   |
|------------------------|-----------------------------------------------------------------------------------------------|
| `/login`               | Sign in (`POST /Auth/Login`). The session (JWT + refresh token) is kept on `localStorage`    |
| `/projects`            | Portfolio: search, pagination, **Sync errors** (`POST /Projects/{id}/Errors/Sync`), edit, delete |
| `/projects/new`        | Create a project (`POST /Projects`)                                                           |
| `/projects/:id`        | Project details + its knowledge base + manual sync summary                                   |
| `/projects/:id/edit`   | Update a project (`PUT /Projects/{id}`); blank keys keep the current ones                     |
| `/errors`              | Knowledge base (`GET /ErrorLogs`): filter by project, status and text                         |
| `/errors/:id`          | Error details with the stack trace and **Mark as resolved** (`PATCH /ErrorLogs/{id}/Resolve`) |

The actions restricted to administrators on the API (create/edit/delete/sync projects, resolve errors) are only
rendered for users with the `admin` role (read from the JWT claims).

**How it talks to the API**

+ Every request goes to the relative `/api` path (`API_BASE_URL` token), so there is no CORS/host configuration on the app:
	+ `ng serve` proxies `/api/*` to `http://localhost:5100/*` (`frontend/proxy.conf.json`);
	+ in Docker, nginx proxies `/api/*` to the `api` service (`frontend/nginx.conf`).
+ An HTTP interceptor adds the bearer token; on `401` it rotates the refresh token once (`POST /Auth/RefreshToken`)
  and retries the request, otherwise it signs the user out.

**Structure**

	frontend/src/app/
	  core/        -> models, API clients (AuthApi, ProjectsApi, ErrorLogsApi), auth (session, interceptor, guards), theme, toasts
	  shared/      -> pagination, status badge, confirm dialog, toasts, timeAgo pipe
	  features/    -> login, shell (layout), projects (list/form/detail), error-logs (list/detail)

# **Solution structure (API)**

	api/src/
	  GeminiCode.Thomas.Api                   -> controllers, workers (ErrorLogSyncWorker), configuration
	  GeminiCode.Thomas.Application.Core      -> use cases (Users, Auth, Projects, ErrorLogs), validators, notifications
	  GeminiCode.Thomas.Domain.Core           -> entities (Project, ErrorLog...), repositories contracts, INewRelicErrorProvider, fingerprint service
	  GeminiCode.Thomas.Infra.Data            -> MongoDb context, repositories, unit of work, indexes
	  GeminiCode.Thomas.Infra.NewRelic        -> NerdGraph client (NRQL over TransactionError events)
	  GeminiCode.Thomas.Crosscutting.Ioc      -> dependency injection
	  GeminiCode.Thomas.Crosscutting.Resources-> localized messages (en-US, pt-BR)
	api/tests/
	  GeminiCode.Thomas.Test                  -> unit tests (mocked MongoDb driver, NewRelic HTTP handler) and API tests (MongoDb container + fake NewRelic)

# **Running**

**Everything with Docker** *(frontend + API + MongoDb)*

	docker-compose up --build

	# UI:      http://localhost:4200   (adm@thomas.com / 123)
	# API:     http://localhost:5010/swagger
	# MongoDb: localhost:27017

Set the NewRelic credentials on the `api` service environment (`NewRelic__ApiKey`, `NewRelic__AccountId`) before
starting, or use the manual sync with the job disabled (`ErrorLogSync__Enabled=false`).

**API locally** *(requires .NET 10 SDK and a MongoDb, e.g. `docker-compose up mongodb`)*

	cd api
	dotnet restore Thomas.sln
	dotnet run --project src/GeminiCode.Thomas.Api     # http://localhost:5100 (swagger at /swagger)

	# tests (the API tests start a throwaway MongoDb container, Docker is required)
	dotnet test Thomas.sln

**Frontend locally** *(requires Node.js 24 LTS or newer, see `engines` on `frontend/package.json`)*

	cd frontend
	npm install
	npm start                 # http://localhost:4200, proxying /api to the API on http://localhost:5100

	npm run build             # production bundle on frontend/dist/frontend/browser
	npm test                  # unit tests (Vitest + jsdom); watch mode on interactive terminals
	npm run test:ci           # single run, used by the CI
	npm run format            # prettier (configuration on the root .prettierrc)

If the API runs on another address, change the `target` of `frontend/proxy.conf.json`.

# **Create your Project based on the Thomas Api Template**

**Install template and create your project**

	cd api
	dotnet new install .
	dotnet new thomas-api --companyName "MyGeminiCode" --output "MyProjectName" --systemDatabase "MongoDb"

# **MongoDb**

The Entity Framework infrastructure is replaced by the MongoDb implementation placed on
`GeminiCode.Thomas.Infra.Data/Mongo`. The domain contracts (`IUnitOfWork`, `IUserRepository`,
`IRefreshTokenRepository`, `IProjectRepository`, `IErrorLogRepository`) are the same, so the application layer does not change.

**Configuration**

	"ConnectionStrings": {
		"MongoDb": "mongodb://mongo:CHANGE_ME_Passw0rd!@localhost:27017/?authSource=admin"
	},
	"MongoDb": {
		"DatabaseName": "ThomasDev"
	}

The database name can also be informed on the connection string, e.g. `mongodb://localhost:27017/ThomasDev`.

**How it works**

	1. The MongoClient is registered as a singleton (connection pool) and each unit of work owns its own context;
	2. Writes are buffered by the MongoDataContext and only applied on CommitAsync (including filter based removals, e.g. the knowledge base of a deleted project);
	3. PublicId is mapped as the document _id, and CreatedAt/LastUpdated are filled on commit;
	4. Collections use the entity name (User, RefreshToken, Project, ErrorLog);
	5. There are no migrations: the indexes (unique project name, unique fingerprint per project...) and the default admin user are created on startup (idempotent);
	6. Integration tests run against a throwaway MongoDb container, each fixture using its own database;
	7. The MongoDb data layer is unit tested with a mocked driver (tests/GeminiCode.Thomas.Test/Unit/Infra/Mongo), no server needed.

*Owner:* https://github.com/vanderlan
