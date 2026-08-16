---
layout: default
title: Validation Patterns
nav_order: 10
parent: Concepts
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from Jube's developer — real sovereignty, zero vendor lock-in.

## Native .NET MVC Pipeline

Jube is substantially a .NET ASP.NET application and uses the native MVC pipeline. It follows that certain injection
risks are natively handled, and XSS risks are substantially reduced. Any malicious code is escaped far upstream of the
software architectures.

For example:

![TryingToInject](TryingToInject.png)

![EscapedProperlyNoAlert](EscapedProperlyNoAlert.png)

Unless developer mode is enabled explicitly, all errors rendered from the ASP.NET pipeline will suppress the actual
error, and only general errors are communicated (Div by Zero thrown internally for the purpose of example):

![ExceptionBeingThrown](ExceptionBeingThrown.png)

![ProductionModeNoException](ProductionModeNoException.png)

In developer mode, which is set via an environment variable, the picture is different:

![InDeveloperModeException](InDeveloperModeException.png)

## Authenticate Attribute Decoration

The .NET authentication pipeline is implemented, which takes care of identity. All controllers and pages are decorated
with the `[Authorize]` attribute, with the exception of the authentication controller, which by its nature is intended
to establish authentication. For example:

![AuthorizeAttribute](AuthorizeAttribute.png)

When authenticated, attempts to access this page without authentication will be redirected to login:

![RedirectedToLogin](RedirectedToLogin.png)

In the case of an API recall without authentication, this will be served a 401:

![APIWithoutAuthorize](APIWithoutAuthorize.png)

## Data Transformation Object (DTO)

A DTO is a model (class) that exists for the sole purpose of serialising and deserialising data, to and from, a user's
request. For example, suppose the UI or independent service invokes an API endpoint with a JSON payload — the JSON
payload must deserialise to the contract specified in the DTO. Taking the DTO `AuthenticationRequestDto`:

![ADtoClass](ADtoClass.png)

This DTO forms the contract for validation in the endpoint:

![MVCHandlingOfDto](MVCHandlingOfDto.png)

It follows that the first stage of validation is that the JSON passed to the endpoint serialises as per contract. In the
event that it does not serialise, the endpoint will be passed null. Assuming a vague attempt to serialise to the DTO,
the next step in each endpoint is validation via strongly typed validators. Fluent Validation is used as the very first
step of controller logic:

![CallingFluentValidator](CallingFluentValidator.png)

The validation will apply a series of validation rules which are more akin to rules:

![FluentValidatorRules](FluentValidatorRules.png)

Assuming validation, there is other practical validation that takes place in the mapping of the DTO to the corresponding
strongly typed data objects, which is handled directly as logic in the controller, or via AutoMapper where the data
layer is being invoked.

It is worth special mention that .NET is a strongly typed programming language and there is no dynamic invocation of
code.

# Dynamic SQL Validation

The vast majority of the database interactions take place via repository patterns, where each repository performs
database interactions via strongly typed C# LINQ2DB, an Object Relation Mapper (ORM) which is transposed to SQL without
intervention from the developer.
It follows that parameterisation is almost universally assured and there is no means to inject SQL where ORMs are used.

![LINQ](LINQ.png)

There are certain cases where the repository pattern is not appropriate, and in such cases LINQ remains the first choice
to constitute a query, albeit with a query pattern:

![BigLINQ](BigLINQ.png)

In certain cases, and it is minimal, amounting to around ten database interactions across the whole platform, there is
rendered and truly dynamic SQL constructed. In such cases parameterisation is fully implemented, and is something that
our own software scanners monitor for (in the form of direct user input mapping to SQL inputs):

![DynamicSQL](DynamicSQL.png)

In the case of all dynamically generated SQL, they are executed only via read replicas and database service accounts
that only have access to read. Yet notwithstanding, there exists a further level of validation for all dynamically
generated SQL, in the form of the Assert Select Only Parser, which uses the Postgres SQL libraries to perform a parse of
the SQL and allow only on it being of the form `SELECT`:

![ScreenshotOfCallToParser](ScreenshotOfCallToParser.png)

![TheSQLSoftParserCode](TheSQLSoftParserCode.png)

In the case of malicious SQL, the query will simply fail and log out the exception.

The Assert Select Only Parser itself can be bypassed via the `ParserAssertSelectOnly` Environment Variable, which
exists purely to make certain test scenarios easier to construct where standing up a full Postgres-backed
integration test is impractical. **This must be set to True for every production workload** - see
[Environment Variables](../EnvironmentVariables/index.html) - since setting it to False removes this validation
layer from every dynamic SQL execution path in the platform, not just the one being tested.

There exists one administrator-only page which allows for the embedding of SQL:

![DangerZone](DangerZone.png)

Noting the Assert Select Only Parser.

# Validation of Dynamic .NET via Rule Token Parser

The rules engine operates by the compilation of .NET code dynamically, on model synchronisation. It stands to reason
that this would be a significant attack vector. The validation is more comprehensively explained in the Rule Parser
section of
the [Rule Compilation Algorithm](/aml-fraud-transaction-monitoring/Configuration/Models/RuleCompilationAlgorithm/), but
it suffices to say that .NET code is filtered on a token basis for allowed tokens only.

# Generic Validations and Display of Error Messages

The following describes the scenario where there is database interuption which will bring about errors in the Jube
software.
Such errors are not displayed directly in the software and are instead bubbled up as a generic message. For example, a
fairly
standard CRUD process as follows, where the database has been terminated:

In the case above, the following error is bubbled up to the user interface:

![ErrorInUiForCRUD.png](ErrorInUiForCRUD.png)

Further proof as follows that the exception does not come across the wire in the background either:

Meanwhile the error is available in the logs:

![ErrorInLogs.png](ErrorInLogs.png)

There are certain administrative pages in the application that do bubble up more detailed errors,
given that their purpose is the to created reports on the basis of SQl, it does provide more reliable feedback as to the
error.

## Direct Object Reference and Role Based Access (RBAC) Validation

The vertical slices that exist in Jube follow the pattern of Controller > Repository > Data Context (Object Relation
Mapper) > Database. The create bulk of the system does not make direct SQL calls to the database and instead pushes SQL
down via LINQ. The approach makes for a very strongly typed approach where models are mapped through the layers of the
application
whereby there is no direct object reference. In the following case it can be seen that the input from the user is mapped
indirectly to the object required of the Repository layer:

![PassingObjectsAround.png](PassingObjectsAround.png)

Meanwhile,  the repository layer maps once more via LINQ to the underlying SQL:

![StronglyTypedDatabaseAccess.png](StronglyTypedDatabaseAccess.png)

In terms of RBAC, much of the horizontal data isolation is achived by passing the users identity as part of paramaterised 
query, where the identity is taken from the .NET authentication pipeline only:

![CheckingPermissionsAtController.png](CheckingPermissionsAtController.png)

In addition to validations set out above, every call to an API will first validate RBAC for that functionality, and the functionality
is only adressible in the case a Permission is added:

![ControllerPermissions.png](ControllerPermissions.png)

Or in the case of horizontal data isolation:

![HorizontalPagePemissions.png](HorizontalPagePemissions.png)