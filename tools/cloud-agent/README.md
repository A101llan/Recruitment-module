# Cloud Agent / Linux (Mono) development environment

This directory contains the Cursor Cloud Agent environment for running the
otherwise Windows/IIS-only **NanoHireHub** app (ASP.NET MVC 4, .NET Framework
4.0) on Linux.

## How it runs

| Layer            | On Windows (native)        | On Linux (this environment)                     |
| ---------------- | -------------------------- | ----------------------------------------------- |
| CLR / build      | .NET Framework 4.0 / MSBuild | **Mono 6.8** + `xbuild`                        |
| Web host         | IIS / IIS Express          | **fastcgi-mono-server4** behind **nginx** (:5002) |
| Data access      | EntityFramework            | **EntityFramework 6.1.3** (net40)               |
| Database         | SQL Server / LocalDB       | **SQL Server 2022 for Linux**                   |

`http://localhost:5002/Account/Login` is the entry point. Bootstrap login is
`admin` / `Nanosoft#2026!` (override with `HR_ADMIN_PASSWORD`).

## Files

- `install.sh` — durable, idempotent setup: installs Mono, SQL Server + tools,
  nginx; restores NuGet packages; generates dev config; builds; initializes the
  database and applies the schema. Run once (captured in environment builds).
- `start.sh` — per-boot: starts SQL Server, the Mono FastCGI app host, and nginx,
  then waits until the login page returns HTTP 200.
- `generate-configs.sh` — writes the gitignored `HR.Web/Web.config` and
  `HR.Web/Views/Web.config` wired for EF6 + SQL Server for Linux.
- `nginx-hirehub.conf` — nginx reverse-proxy site (templated app root).

## Why EntityFramework 6 on Linux

EF5 depends on the .NET Framework's `System.Data.Entity` SQL provider manifest,
which Mono does not implement (`ProviderIncompatibleException: The provider did
not return a ProviderManifest instance`). EF6 bundles its own
`EntityFramework.SqlServer` provider and runs correctly on Mono. The app was
already written against EF6 semantics (see the EF6 comments in
`HomeController`/`AccountController`) and its git history shipped EF6, so this is
a restoration rather than a new dependency choice. Only one source change was
required: `EntityState` moved namespace between EF5 and EF6
(`System.Data.EntityState` → `System.Data.Entity.EntityState`).

## Override variables

- `MSSQL_SA_PASSWORD` (default `HirehubDev2026x`)
- `HR_DB_NAME` (default `HR_Local`)
- `HR_ADMIN_PASSWORD` (default `Nanosoft#2026!`)
