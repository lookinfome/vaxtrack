# VaxTrack

VaxTrack is a two-dose vaccination management platform. It tracks a beneficiary's
vaccination journey end-to-end — registration, hospital discovery, dose booking and
approval, and a verifiable completion certificate — while giving each hospital
self-service control over its own slot capacity and giving a platform operator full
oversight of hospitals and users, with a complete audit trail on every action.

**Current version:** v2.0.0 (backend: ASP.NET Core / .NET 10, frontend: Angular 20)

New to the codebase? Read this file top to bottom once, then jump into
[Documentation](#documentation) for the full specs. Everything here is scoped to
what you need to get productive — the linked documents carry the depth.

---

## Table of Contents

- [Features](#features)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Roles & Access Model](#roles--access-model)
- [API Overview](#api-overview)
- [Getting Started](#getting-started)
- [Documentation](#documentation)
- [Versioning](#versioning)

---

## Features

- **Two-dose booking lifecycle** — book, approve, reject, cancel, edit, and rebook a
  dose, with slot capacity reserved and released automatically.
- **Three-tier role model** — normal user, hospital-admin (scoped to one hospital),
  and platform admin, enforced server-side on every request.
- **Hospital & user lifecycle management** — disable, reactivate, and (for hospitals)
  a two-party unregister/authorize flow, all with mandatory reasons and full audit
  trails.
- **Self-service flows** — forgot/reset password, disabled-account reactivation
  requests, and hospital-admin role applications, each with an admin approval queue.
- **In-app notifications** — fired on every approval, rejection, and status change.
- **Vaccination certificates** — downloadable and publicly verifiable via a
  bookingId link, without exposing personal contact details.
- **Full audit trails** — every state-changing action on a booking, hospital, or
  user is permanently logged with actor, action, and reason.

## Tech Stack

| Layer          | Technology                                                |
|----------------|------------------------------------------------------------|
| Backend        | ASP.NET Core Web API (.NET 10), C# 13                      |
| Data Access    | Entity Framework Core 10 over Microsoft SQL Server          |
| Authentication | JWT Bearer (HMAC-SHA256) with server-side revocation        |
| Password Hash  | BCrypt.Net-Next                                             |
| Logging        | Serilog (console + rolling file sinks)                      |
| Frontend       | Angular 20 (standalone components) + TailwindCSS            |
| API Docs       | ASP.NET Core OpenAPI, plus Bruno-compatible API contracts    |

If any of these are new to you (say, JWT or EF Core migrations), the
[Technical Specification Document](#documentation) explains each one in the context
of how VaxTrack actually uses it — no need to go hunting for generic tutorials first.

## Project Structure

```
vaxtrack/
├── v2_backend/v2.0.0/        # Current backend (ASP.NET Core Web API)
│   ├── Controllers/          # Auth, User, Hospital, Booking, Notification, UserRoleMapping
│   ├── Services/             # Business logic, ownership & role checks
│   ├── Repositories/         # EF Core data access
│   ├── Dtos/ Models/         # Request/response contracts & entity models
│   └── Migrations/           # EF Core migrations
├── v2_frontend/v2.0.0/       # Current frontend (Angular 20 + TailwindCSS)
│   └── src/app/
│       ├── core/             # Models, services, interceptors
│       ├── features/         # Auth, booking, hospital, user, admin, support
│       └── shared/           # Guards, shared components
├── v1_backend/                # Earlier backend iterations (v1.0.0 – v1.3.0), archived
├── Documents/v2_docs/
│   ├── vaxtrack - api contracts/       # Bruno-compatible request collections, per module
│   ├── vaxtrack - sql queries/         # Seed data, read/truncate reference scripts
│   ├── vaxtrack - daily progress/      # Dated engineering change logs
│   └── vaxtrack - support documents/   # Functional Spec, Technical Spec, Test Cases, Project Review
└── README.md
```

The backend follows a standard Controller → Service → Repository layering — request
validation and routing in Controllers, business rules and authorization checks in
Services, EF Core data access in Repositories. The frontend groups by feature
(`features/booking`, `features/hospital`, etc.) rather than by file type, so
everything for one screen or flow lives together.

## Roles & Access Model

| Role              | Scope                                                                                    |
|-------------------|--------------------------------------------------------------------------------------------|
| **Normal User**   | Full control over their own profile and booking; no visibility into other users' data.     |
| **Hospital Admin**| A normal user additionally scoped to exactly one hospital — can manage that hospital's contact info, slot capacity, and dose approvals only. |
| **Platform Admin**| Unconditional access across every module — user/hospital lifecycle, all bookings, role assignments, and pending-request queues. |

Every role is enforced server-side, not just hidden in the UI — the same rules apply
whether a request comes from the Angular app or a raw API call. Full role
definitions, the permission matrix, and each endpoint's authorization requirement
are in the
[Functional Specification Document](Documents/v2_docs/vaxtrack%20-%20support%20documents/01-Functional-Specification-Document.docx).

## API Overview

The API is organised into six modules, each with its own controller:

| Module           | Base Route                          | Responsibility                                            |
|-------------------|--------------------------------------|-------------------------------------------------------------|
| Auth              | `/api/vaxtrack/v1/auth`              | Login, logout, password recovery, reactivation requests     |
| User              | `/api/vaxtrack/v1/user`              | Registration, profile, and the full user lifecycle          |
| Hospital          | `/api/vaxtrack/v1/hospital`          | Hospital directory, slot capacity, disable/reactivate/unregister |
| Booking           | `/api/vaxtrack/v1/booking`           | Dose booking, approval, cancellation, certificates           |
| UserRoleMapping   | `/api/vaxtrack/v1/userrolemapping`   | Scoped role assignment and hospital-admin applications        |
| Notification      | `/api/vaxtrack/v1/notification`      | In-app notification delivery and read state                   |

Ready-to-import request collections for every endpoint (Bruno / Open Collection
format) live in `Documents/v2_docs/vaxtrack - api contracts/`, one folder per
module. Request/response payloads, status codes, and error contracts for each
endpoint are detailed in the
[Technical Specification Document](Documents/v2_docs/vaxtrack%20-%20support%20documents/02-Technical-Specification-Document.docx).

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) and npm
- SQL Server (local instance or container) — a database named `vaxtrack_sqlserver`

### 1. Backend setup

```bash
cd v2_backend/v2.0.0

# Update the connection string in appsettings.json (or appsettings.Development.json)
# to point at your own SQL Server instance.

dotnet restore
dotnet ef database update      # applies EF Core migrations
dotnet run
```

The API starts at `http://localhost:5119` (see `Properties/launchSettings.json`).
OpenAPI schema is available at `/openapi/v1.json` in Development.

Seed the database with a platform admin, hospital admins, and sample hospitals using
the scripts in `Documents/v2_docs/vaxtrack - sql queries/` (`Truncate Commands.sql`
then `Seed Data.sql`). Default seeded credentials and IDs are documented at the top
of `Seed Data.sql`.

### 2. Frontend setup

```bash
cd v2_frontend/v2.0.0
npm install
npm start
```

The app starts at `http://localhost:4200` and proxies `/api` and `/uploads` requests
to the backend at `http://localhost:5119` (see `proxy.conf.json`).

### 3. Try it out

Log in with a seeded account (see the Seed Data script), or register a new user, then
book a vaccination slot at one of the seeded hospitals.

## Documentation

All project documentation lives under `Documents/v2_docs/`. The four core
deliverables — pick based on what you're trying to do:

| Document | Use it to | Audience |
|----------|-----------|----------|
| [Functional Specification Document](Documents/v2_docs/vaxtrack%20-%20support%20documents/01-Functional-Specification-Document.docx) | Understand *what* the app does — business requirements, roles, workflows, permission matrix, system diagrams | Business stakeholders, all developers |
| [Technical Specification Document](Documents/v2_docs/vaxtrack%20-%20support%20documents/02-Technical-Specification-Document.docx) | Understand *how* it's built — architecture, HLD/LLD, every module's frontend + backend implementation detail | Developers (any experience level), QA, tech leads |
| [Test Case Sheet](Documents/v2_docs/vaxtrack%20-%20support%20documents/VaxTrack_Test_Case_Sheet.xlsx) | Look up or execute QA test cases across every module and feature | QA, developers, tech/team leads |
| [Project Development Review Document](Documents/v2_docs/vaxtrack%20-%20support%20documents/04-Project-Development-Review-Document.docx) | Prep for or run a deep architecture/design review — structured Q&A on every part of the build | Tech leads, project delivery managers, senior developers |

Other reference material:

- **API Contracts** — `vaxtrack - api contracts/`: importable request collections per module.
- **SQL Reference** — `vaxtrack - sql queries/`: seed data, read, and truncate scripts.
- **Daily Progress** — `vaxtrack - daily progress/`: dated engineering change logs.

## Versioning

The repository retains earlier iterations of the backend (`v1_backend/v1.0.0`
through `v1.3.0`) for history; all active development targets `v2_backend/v2.0.0`
and `v2_frontend/v2.0.0`.
