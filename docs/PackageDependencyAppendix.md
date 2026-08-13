---
layout: default
title: Package Dependency Appendix
nav_order: 6
---

# Appendix: Package Dependency Inventory

**Solution:** Jube (33 .csproj projects, all targeting `net9.0`)
**Scope:** All NuGet package references across the solution, plus all vendored/static third-party client-side assets under `Jube.App/wwwroot` (outside NuGet).

## A. NuGet Packages (server-side, .NET)

- AsyncFixer — 1.6.0 — NuGet (Roslyn analyzer)
- AutoMapper — 15.1.3 — NuGet
- coverlet.collector — 3.1.0 — NuGet (test)
- DnsClient — 1.2.0 — NuGet
- Fastenshtein — 1.0.0.5 — NuGet
- FluentAssertions — 6.12.0 — NuGet (test)
- FluentMigrator — 3.3.2 — NuGet
- FluentMigrator.Extensions.Postgres — 3.3.2 — NuGet
- FluentMigrator.Runner — 3.3.2 — NuGet
- FluentMigrator.Runner.Postgres — 3.3.2 — NuGet
- FluentValidation — 10.3.6 — NuGet
- Isopoh.Cryptography.Argon2 — 1.1.12 — NuGet
- linq2db — 3.6.0 — NuGet
- linq2db.AspNet — 3.6.0 — NuGet
- log4net — 3.3.1 — NuGet
- MessagePack — 3.1.7 — NuGet
- Microsoft.AspNetCore.Authentication.JwtBearer — 9.0.14 — NuGet
- Microsoft.AspNetCore.Authentication.Negotiate — 6.0.8 — NuGet
- Microsoft.AspNetCore.Authentication.OpenIdConnect — 9.0.18 — NuGet
- Microsoft.AspNetCore.Mvc.NewtonsoftJson — 9.0.11 — NuGet
- Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation — 3.1.10 — NuGet
- Microsoft.AspNetCore.SignalR.Common — 6.0.6 — NuGet
- Microsoft.AspNetCore.SignalR.StackExchangeRedis — 9.0.13 — NuGet
- Microsoft.CodeAnalysis — 4.10.0 — NuGet
- Microsoft.CodeAnalysis.Common — 4.10.0 — NuGet
- Microsoft.CodeAnalysis.CSharp — 4.10.0 — NuGet
- Microsoft.CodeAnalysis.CSharp.Workspaces — 4.10.0 — NuGet
- Microsoft.CodeAnalysis.VisualBasic — 4.10.0 — NuGet
- Microsoft.CodeAnalysis.Workspaces.Common — 4.10.0 — NuGet
- Microsoft.Extensions.Caching.Memory — 9.0.0 — NuGet
- Microsoft.Extensions.Logging.Log4Net.AspNetCore — 6.0.0 — NuGet
- Microsoft.Extensions.Primitives — 9.0.10 — NuGet
- Microsoft.NET.Test.Sdk — 16.11.0 — NuGet (test)
- Microsoft.VisualBasic — 10.3.0 — NuGet
- Microsoft.VisualStudio.Azure.Containers.Tools.Targets — 1.10.9 — NuGet (tooling)
- Microsoft.VisualStudio.Threading.Analyzers — 17.14.15 — NuGet (Roslyn analyzer)
- Microsoft.VisualStudio.Web.CodeGeneration.Design — 3.1.5 — NuGet (tooling)
- Newtonsoft.Json — 13.0.4 — NuGet
- Npgsql — 5.0.18 — NuGet
- Npgsql.Json.NET — 5.0.13 — NuGet
- pgsqlparser — 1.0.0 — NuGet
- Polly — 8.6.6 — NuGet
- RabbitMQ.Client — 6.2.1 — NuGet
- StackExchange.Redis — 2.11.8 — NuGet
- Swashbuckle.AspNetCore.Swagger — 6.2.3 — NuGet
- Swashbuckle.AspNetCore.SwaggerGen — 6.2.3 — NuGet
- Swashbuckle.AspNetCore.SwaggerUI — 10.1.7 — NuGet
- System.Buffers — 4.5.1 — NuGet
- System.ComponentModel.Annotations — 4.4.1 — NuGet
- System.ComponentModel.Composition — 5.0.0 — NuGet
- System.Data.DataSetExtensions — 4.5.0 — NuGet
- System.Diagnostics.DiagnosticSource — 4.7.1 — NuGet
- System.Diagnostics.PerformanceCounter — 9.0.10 — NuGet
- System.Numerics.Vectors — 4.5.0 — NuGet
- System.Reflection.Emit.Lightweight — 4.3.0 — NuGet
- System.Runtime — 4.3.0 — NuGet
- System.Runtime.CompilerServices.Unsafe — 6.0.0 — NuGet
- System.Runtime.InteropServices.RuntimeInformation — 4.3.0 — NuGet
- xunit — 2.4.1 — NuGet (test)
- xunit.runner.visualstudio — 2.4.3 — NuGet (test)
- YamlDotNet — 16.3.0 — NuGet

*Note: the Accord.\* projects (Core, Genetic, MachineLearning, Math, Math.Core, Neuro, Statistics) are vendored source forks compiled in-tree, not NuGet references — no `PackageReference` entries exist in them.*

## B. wwwroot Client-Side Dependencies (`Jube.App/wwwroot`) — non-NuGet, vendored static assets

- Kendo UI for jQuery (Progress/Telerik) — 2021.1.224 — Vendored JS/CSS library (commercial) — `kendo/`
- jQuery (bundled inside Kendo) — 1.12.4 — Vendored JS library — `kendo/js/jquery.min.js`
- JSZip (bundled inside Kendo) — 2.6.1 — Vendored JS library — internal to Kendo build, per Kendo NOTICE
- Pako (bundled inside Kendo) — 1.0.6 — Vendored JS library — internal to Kendo build, per Kendo NOTICE
- AngularJS (bundled inside Kendo examples) — 1.7.2 — Vendored JS library — `kendo/examples/` demo content only, not used by the app
- WiX Toolset (bundled inside Kendo, build-time only) — 3.8.1128.0 — Vendored MS-RL component — Kendo installer tooling, not runtime
- Ace Editor — 1.4.13 — Vendored JS library — `js/ace/`
- SignalR JS client — 6.0.4 — Vendored JS library — `js/signalr/signalr.js`
- JSZip (app-level, separate from Kendo's internal copy) — 3.10.0 — Vendored JS library — `js/jszip/jszip.min.js`
- jQuery Bar Rating — version not embedded in the minified source — Vendored JS library (jQuery plugin) — `js/barrating/`
- jQuery QueryBuilder — 2.6.2 — Vendored JS/CSS library — `js/builder/query-builder.standalone.min.js`, `styles/query-builder.default.min.css`
- Bootstrap — 3.3.2 — Vendored CSS library — `styles/bootstrap.min.css`
- normalize.css (bundled in the Bootstrap file) — 3.0.2 — Vendored CSS — header comment in `styles/bootstrap.min.css`

**First-party JS/CSS (not third-party dependencies, excluded above):** `js/BuilderCoder.js`, `js/CaseFilterBuilder.js`, `js/CRUD.js`, `js/ExhaustiveFilterBuilder.js`, `js/Suppression.js`, `js/Tree.js`, the `js/Account`, `js/Administration`, `js/Case`, `js/Model`, `js/RoleAllocation`, `js/Sanction`, `js/Visualisation`, `js/Watcher` directories, and `styles/jube.css` / `styles/site.css`.
