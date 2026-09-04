---
layout: default
title: Service Layer & DTO Model
nav_order: 12
parent: Concepts
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from
Jube's developer — real sovereignty, zero vendor lock-in.

# Service Layer & DTO Model

[Validation Patterns](../SoftwareValidations/index.html) describes Jube's original vertical slice: `Controller >
Repository > Data Context (ORM) > Database`, with the DTO existing solely as the request/response serialisation contract
for a single MVC controller. Jube is migrating this, endpoint by endpoint, to an additional layer:
`Endpoint (Minimal API) > Service > Repository > Data Context > Database`.

The service layer is not just the controller's logic moved sideways. The DTO in front of it is doing considerably more
work than it used to — it is the single declarative source that a generic, entity-agnostic UI and an Agentic AI tool
host both read off directly, so that as little as possible of an entity's grouping, layout, validation, permission shape
and AI-tool metadata has to be hand-written per entity ever again. This page explains that model. It is new — as of this
writing `EntityAnalysisModel` is the first area migrated to it, with the rest of the platform following controller by
controller.

## Where the pieces live

| Project              | Holds                                                                                                                                                                                                                                                     |
|----------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Jube.Dto`           | The DTOs themselves, the `Jube.Dto.Forms` declarative form attributes, and the `Jube.Dto.Interfaces` marker interfaces described below.                                                                                                                   |
| `Jube.Validations`   | One `AbstractValidator<TDto>` per DTO (FluentValidation) — see [Validation Patterns](../SoftwareValidations/index.html) for how this fits the request pipeline.                                                                                           |
| `Jube.Service`       | One `<Area>Service` per area (permission checks, mapping, repository calls, `OperationScope` — see [Logging](../Logging/index.html)), the `Jube.Service.Agent` attributes, and the `ServiceToolCatalogue` that lists every operation exposed to an agent. |
| `Jube.App/Endpoints` | A thin Minimal API endpoint group per area. It has no business logic — it resolves identity from the authenticated principal, calls the service, and maps typed exceptions to the frozen HTTP status codes.                                               |

Why a DTO can't just live in `Jube.Service`: the service constructs its validator, and the validator needs the DTO.
Putting the DTO in its own project (`Jube.Dto`) with no dependency on either avoids a circular reference between
`Jube.Service` and `Jube.Validations`.

## The DTO as the single declarative contract

Every field on a migrated DTO carries a `[Description]` (also used verbatim as the agent-tool argument description — see
below) plus form metadata describing how it is grouped, laid out, and behaves in the UI. Class-level attributes describe
the entity as a whole. From `Jube.Dto/EntityAnalysisModel/EntityAnalysisModelDto.cs`:

```csharp
[FormEndpoint("EntityAnalysisModel")]
[FormKeys(Id = nameof(Id), NaturalKey = nameof(Name))]
[LockField(nameof(Locked))]
[FormGroup("Identity", Order = 10)]
[FormGroup("Entry & Reference Date", Order = 20)]
[FormGroup("Cache", Order = 30, Collapsed = true)]
// ... one FormGroup per section ...
public class EntityAnalysisModelDto : IUpdated, IActivatable, ILockable, IGuidIdentified
{
    [Description("Display name of the model. Unique within the tenant (case-insensitive).")]
    [FormField(Group = "Identity", Order = 10)]
    [ListColumn(Order = 10, Title = "Name")]
    public string Name { get; init; }

    [Description("JSONPath specifying the location of the reference date in the HTTP POST body. Required " +
                 "unless Reference Date Payload Location is Now.")]
    [FormField(Group = "Entry & Reference Date", Order = 50)]
    [VisibleWhen(nameof(ReferenceDatePayloadLocationTypeId), (byte)1)]
    [RequiredWhen(nameof(ReferenceDatePayloadLocationTypeId), (byte)1)]
    public string ReferenceDateXPath { get; set; }

    // ...
}
```

Nothing here is hand-transcribed into a Razor page or a bespoke `CRUD.js` file per entity. It is metadata a single
generic UI component reads at runtime to render the form, and a single agent-tool host reads at startup to build the
tool's parameter schema — the same attributes serve both consumers.

**What is a faithful transcription of the legacy behaviour, and what is a redesign.** Field behaviour (`ReadOnly`,
`VisibleWhen`/`EnabledWhen`/`RequiredWhen`, `Lookup`, `ListColumn`) and the entity's keys/endpoint (`FormKeys`,
`FormEndpoint`, `LockField`) are a contract — carried over exactly from the page and `CRUD.js` they replace. Grouping,
ordering and labelling (`FormGroup`, and the `Group`/`Order`/`Label` on each `FormField`) are **not** — this is a UI
redesign in progress, and groups are chosen for task-based sense (AML/transaction-monitoring norms, `docs/` context)
rather than copied from the legacy fieldset layout.

## The Forms attribute set

Defined in `Jube.Dto/Forms/` — one type per file. All are opt-in and additive; a DTO uses only the ones its shape needs.

| Attribute                                                                           | Target               | Purpose                                                                                                                                            |
|-------------------------------------------------------------------------------------|----------------------|----------------------------------------------------------------------------------------------------------------------------------------------------|
| `[FormEndpoint(area)]`                                                              | class                | The API area the generic form talks to (`/api/<area>`).                                                                                            |
| `[FormKeys]`                                                                        | class                | Names the `Id` property, an optional `Parent` (for tree-shaped entities), and an optional `NaturalKey` used for duplicate-name checks and display. |
| `[LockField(property)]`                                                             | class, repeatable    | Names the boolean property that, when true, disables editing — pairs with `ILockable`.                                                             |
| `[FormGroup(path)]`                                                                 | class, repeatable    | Declares one section of the form: `Order`, optional `Label`, `Collapsed`, and a section-level `Widget` override.                                   |
| `[FormField]`                                                                       | property             | Places a field in a `Group` at an `Order`, with an optional `Label`/`Widget`/`Placeholder`/`Help`, and `ReadOnly`.                                 |
| `[VisibleWhen(property, values...)]` / `[EnabledWhen(...)]` / `[RequiredWhen(...)]` | property, repeatable | Conditional show/enable/require, keyed off another property's value(s); `Op` (`ConditionOp`) and `MatchMode` control the comparison.               |
| `[Lookup(source)]`                                                                  | property             | Declares the field as a reference picker against another area's rows (`TextField`/`ValueField`, optional `ParentField` for cascading lookups).     |
| `[ListColumn]`                                                                      | property             | Declares the field as a column in the entity's list/grid view (`Order`, `Title`, `Sortable`, `Filterable`, `Hidden`).                              |
| `[NewDefault(value)]`                                                               | property             | The value a new (not-yet-saved) row starts with in the form.                                                                                       |

## The marker interfaces

Defined in `Jube.Dto/Interfaces/` — small, composable shape contracts a DTO implements only when it genuinely has that
shape, so the generic UI can key generic behaviour (an audit-trail panel, a lock toggle, a tree view, a rule builder)
off the interface rather than off per-entity special-casing. They are deliberately never bundled onto one
"do everything" interface, and a new one is only added once a second real migrated DTO needs the same shape — not
speculatively.

| Interface          | Members                                                                                                   | Backs                                                                                                                                                            |
|--------------------|-----------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `IUpdated`         | `Id`, `CreatedUser`, `CreatedDate`, `UpdatedUser`, `UpdatedDate`, `Version`, `DeletedUser`, `DeletedDate` | The generic audit-trail display and optimistic-concurrency handling.                                                                                             |
| `IActivatable`     | `Active`                                                                                                  | The generic active/inactive toggle.                                                                                                                              |
| `ILockable`        | `Locked`                                                                                                  | The generic lock toggle — pairs with the class-level `[LockField]`.                                                                                              |
| `IGuidIdentified`  | `Guid`                                                                                                    | Entities addressed externally by a stable GUID (for example a model's invocation endpoint).                                                                      |
| `ITreeChild`       | *(marker only, no members)*                                                                               | Entities with a parent/child hierarchy; the actual parent-FK property name is area-specific and comes from `[FormKeys(Parent = ...)]` rather than the interface. |
| `IRuleBuilderJson` | `Json`, `BuilderRuleScript`                                                                               | The jQuery QueryBuilder shape used by every rule-authoring field across the platform (Abstraction Rules, Activation Rules, and others).                          |

`EntityAnalysisModelDto` currently implements `IUpdated, IActivatable, ILockable, IGuidIdentified`.

## Agentic AI tooling

The same DTO/service metadata that drives the UI also exposes every service operation as a tool an LLM agent can call,
with no behavioural change to the operation itself:

```csharp
[Description("Returns one Model by its numeric identifier, scoped to the calling user's tenant. Returns " +
             "null when the model does not exist or is not visible to the caller.")]
[ServiceOperation("EntityAnalysisModelGet", OperationKind.Read, Idempotent = true)]
public async Task<EntityAnalysisModelDto?> GetByIdAsync(
    [Description("Numeric identifier of the model.")]
    int id,
    CancellationToken token = default)
{
    using var op = OperationScope.Start("EntityAnalysisModel", "Get", userName, tenantRegistryId, auditLog, log,
        serviceChangeBus);
    // ... EnsurePermitted, repository call, OperationScope.Entity(id), map to DTO ...
}
```

- `[ServiceOperation(name, kind)]` — `name` is a globally-unique, `PascalCase` tool name (`<Area><Operation>`, no
  underscores); `kind` is `OperationKind.Read`/`Write`/`Delete`; `Idempotent` and `Destructive` are safety hints an
  agent host uses to decide whether a call needs confirmation.
- `[Description]` on the method and each parameter is the same attribute used for form-field help text — reused verbatim
  as the tool and argument descriptions.
- Every operation is registered once in `Jube.Service/Agent/ServiceToolCatalogue/ServiceToolCatalogue.<Area>.cs`:

```csharp
new ServiceToolDescriptor(
    "EntityAnalysisModelGet", OperationKind.Read, Idempotent: true, Destructive: false,
    "Returns one Model by id, scoped to the caller's tenant."),
```

Identity is never a tool argument. `userName`/`tenantRegistryId` are bound at the composition root from the
authenticated principal (HTTP claims, or an agent session's own authenticated service account) — an LLM can influence
which rows a DTO's business fields describe, never which tenant or user it acts as.

Tracing, metrics and the change-event stream that back this layer are documented in
[Environment Variables](../EnvironmentVariables/index.html) (`EnableOpenTelemetry`, `EnableServiceChangeStream`) — both
are opt-in and no-op when disabled; the structured audit record written by `OperationScope` on every call (reads
included) is unconditional.

## Status

This pattern is applied per controller as each one is migrated (`EntityAnalysisModelController` first); the generic
Blazor `DynamicCrud<TDto>` control intended to consume the Forms attributes at runtime, and the agent host that consumes
`ServiceToolCatalogue`, are being built out alongside it. Until an area is migrated, its controller, Razor page and
hand-written `CRUD.js` remain the source of truth for that entity's UI, per
[Validation Patterns](../SoftwareValidations/index.html).

For the exhaustive, always-current member list of any attribute or interface described above, the source in
`Jube.Dto/Forms/` and `Jube.Dto/Interfaces/` is authoritative — each is a small, single-purpose, low-churn type (one per
file), quicker to read directly than to keep a second description of in sync here.
