---
layout: default
title: HTTP Adaptation Protocol
nav_order: 20
parent: Models
grand_parent: Configuration
---

🚀 Get to pre-production in weeks, not months, with private [training](https://www.jube.io/jube-training) direct from Jube's developer — real sovereignty, zero vendor lock-in.

# HTTP Adaptation Protocol

This page is the wire specification for HTTP Adaptation endpoints - R Plumber, Python Flask, or
anything else that can receive a POST and return JSON. It documents what Jube actually sends, what
Jube actually accepts back, and how the response is used once it is inside Jube. It supersedes the
payload description on the [HTTP Adaptation](../HTTPAdaptation/index.html) configuration page, which
covers how to create an adaptation in the UI rather than the shape of the request and response.

Version on the wire: `1.1`. There is no breaking change from the original bare-double contract: a
bare JSON number remains a legal whole-body response, forever.

## 1. Non-negotiables

1. A bare JSON number is a legal whole-body response, forever.
2. `Value` is the only required member of an `Adaptation` object.
3. Vocabularies (`Family`, `Method`, `Space`, and so on) are open strings on the wire, documented as
   constants in `ProtocolConstants`, never enums. An unrecognised value is archived, not thrown on.
4. Unknown members are ignored on deserialisation.
5. `Journey: null` is the correct answer for families with no journey, not a degradation.
6. An error must never masquerade as a score: if `Error` is non-null, `Value` must be null.

## 2. Request: what Jube sends

Jube POSTs the entire `EntityAnalysisModelInstanceEntryPayload` for the transaction - Payload,
Abstraction, TTL Counters, Abstraction Calculations, Sanctions, any HTTP Adaptations already
recalled earlier in the same invocation, and so on - as the JSON body, using the endpoint's
configured URL. This is not a hand-picked subset: an adaptation can read anything already resolved
about the transaction, including the `Value` of an adaptation with a lower Priority that has
already run (see [§6.3](#63-execution-order-priority-and-boosting)).

```json
{
  "Payload": {
    "SettlementAmount": 100000,
    "ResponseCode": "0"
  },
  "Abstraction": {
    "ResponseCodeEqual0Volume": 1
  },
  "TtlCounter": {
    "...": 0
  },
  "HttpAdaptation": {
    "AnEarlierModel": {
      "Value": 0.12,
      "...": "..."
    }
  },
  "...": "every other resolved field on the transaction"
}
```

## 3. Response: the `Adaptation` object

A response body is either a bare JSON number (§3.1) or a JSON object matching the shape below. Every
member other than `Value` is optional; omit what a family has nothing to say about rather than
sending nulls for it.

### 3.1 The bare-number floor

```json
0.91
```

Also accepted, for compatibility with jsonlite's default vector serialisation: a single number
wrapped in an array, `[0.91]`. Both forms are equivalent to `{"Value": 0.91}`.

### 3.2 `Adaptation`

| Member            | Type                                                 | Notes                                                                                                 |
|-------------------|------------------------------------------------------|-------------------------------------------------------------------------------------------------------|
| `Value`           | number \| null                                       | The only required member. Null when suppressed (§4).                                                  |
| `Error`           | string                                               | Non-null forces `Value` to null. A plain-language failure statement.                                  |
| `Narrative`       | string                                               | Free text. Display-only - Jube never parses it.                                                       |
| `HumanLabel`      | string                                               | Free text. Display-only.                                                                              |
| `ProtocolVersion` | string                                               | Emitted as `"1.1"` by a well-behaved endpoint. Absent is read as unversioned; nothing branches on it. |
| `Model`           | [`ModelDescriptor`](#33-modeldescriptor)             |                                                                                                       |
| `Result`          | [`ResultDescriptor`](#34-resultdescriptor)           |                                                                                                       |
| `Calibration`     | [`CalibrationDescriptor`](#35-calibrationdescriptor) |                                                                                                       |
| `Contribution`    | [`ContributionSet`](#36-contributionset)             |                                                                                                       |
| `Journey`         | [`JourneyDescriptor`](#37-journeydescriptor)         | Null for families with no fired path (e.g. a Bayesian network).                                       |

### 3.3 `ModelDescriptor`

| Member                                            | Type                                               | Notes                                                                                                                                                                                               |
|---------------------------------------------------|----------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Name`                                            | string                                             |                                                                                                                                                                                                     |
| `Family`                                          | string                                             | Open vocabulary. See [`ProtocolConstants.Family`](#38-protocolconstants-open-vocabularies).                                                                                                         |
| `Version`                                         | string                                             |                                                                                                                                                                                                     |
| `ArtifactHash`                                    | string                                             |                                                                                                                                                                                                     |
| `TrainedDate`                                     | date-time                                          |                                                                                                                                                                                                     |
| `FeatureCount`                                    | integer                                            |                                                                                                                                                                                                     |
| `Validation`                                      | [`ValidationDescriptor`](#39-validationdescriptor) |                                                                                                                                                                                                     |
| `BootstrapReplicates`                             | integer                                            | Bayesian networks: `R` as passed to `bnlearn::boot.strength`.                                                                                                                                       |
| `LabelsVersion`, `LabelsHash`                     | string                                             | Provenance of the curated `Narrative`/`HumanLabel` template dictionary. No wire member is produced by a generative language model; a phrasing change is traceable the same way a weights change is. |
| `TopologyVersion`, `TopologyHash`, `TopologyDate` | string, string, date-time                          | Structure, versioned separately from weights, so a re-fit on an unchanged topology doesn't duplicate `ArtifactHash`.                                                                                |
| `WeightsVersion`, `WeightsHash`, `WeightsDate`    | string, string, date-time                          |                                                                                                                                                                                                     |
| `StructureLearning`                               | string                                             | Open vocabulary: `HillClimbing`, `MMHC`, `TabuSearch`, `Expert`, `Constrained`, `None`.                                                                                                             |
| `WhitelistedArcs`, `BlacklistedArcs`              | integer                                            | Bayesian networks: counts of expert-constrained arcs.                                                                                                                                               |
| `HiddenLayers`, `ProcessingElements`              | integer                                            | Populated for `NeuralNetwork`, omitted otherwise.                                                                                                                                                   |

Omit the topology/weights fields entirely, rather than duplicating `ArtifactHash` into them, where a
family has no meaningful topology/weights split.

### 3.4 `ResultDescriptor`

| Member                 | Type    | Notes                                                                                                                                                                                                                                |
|------------------------|---------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Threshold`            | number  |                                                                                                                                                                                                                                      |
| `Activated`            | boolean |                                                                                                                                                                                                                                      |
| `ExpectedPositiveRate` | number  | The thermostat's operational meaning: the expected outcome rate at or above `Threshold`, taken from the validation sample. Only legitimate - and Jube expects endpoints to only populate it - when `Calibration.Calibrated` is true. |

### 3.5 `CalibrationDescriptor`

Whether `Value` is a score that merely ranks, or a score that has been validated to mean something.
Probabilistic narration of `Value` - "one in five", operational capacity planning - is only
legitimate when `Calibrated` is true.

| Member               | Type                                           | Notes                                                                                                                                                                                                                                                                                |
|----------------------|------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `Space`              | string                                         | What `Value` actually is. See [`ProtocolConstants.ValueSpace`](#38-protocolconstants-open-vocabularies). Deliberately distinct from `Contribution.Space`, which is the space of the weights - they frequently differ (e.g. XGBoost: `Value` in `Probability`, weights in `LogOdds`). |
| `Calibrated`         | boolean                                        | True if and only if `Space` is `Probability` and a calibration/validation artifact was loaded and is in date. Never asserted on faith.                                                                                                                                               |
| `Method`             | string                                         | Open vocabulary: `None`, `Native`, `Platt`, `Isotonic`, `Beta`. `Native` means the fitting library's own probability output with no post-hoc transform.                                                                                                                              |
| `ValidatedDate`      | date-time                                      |                                                                                                                                                                                                                                                                                      |
| `Sample`             | integer                                        |                                                                                                                                                                                                                                                                                      |
| `Brier`              | number                                         | Brier score on the validation sample.                                                                                                                                                                                                                                                |
| `Intercept`, `Slope` | number                                         | From the regression of observed outcome on predicted log-odds. Perfect calibration is intercept 0, slope 1.                                                                                                                                                                          |
| `Band`               | array of [`CalibrationBand`](#calibrationband) | The thermostat table.                                                                                                                                                                                                                                                                |

#### `CalibrationBand`

| Member           | Type                                            |
|------------------|-------------------------------------------------|
| `Lower`, `Upper` | number                                          |
| `Expected`       | number - mean predicted probability in the band |
| `Observed`       | number - realised rate on the validation sample |
| `Count`          | integer                                         |

### 3.6 `ContributionSet`

| Member      | Type                        | Notes                                                                                                                   |
|-------------|-----------------------------|-------------------------------------------------------------------------------------------------------------------------|
| `Space`     | string                      | The space of the `Items` weights. Open vocabulary, e.g. `Relative`.                                                     |
| `Method`    | string                      | Open vocabulary: `Coefficient`, `BootstrapStrength`, `ArcStrength`, `ConnectionWeight`, and others as families require. |
| `Exact`     | boolean                     | True only when `BaseValue + Σ Weight` reconstructs `Value` exactly - a verified property, not an asserted one.          |
| `BaseValue` | number                      |                                                                                                                         |
| `Items`     | array of `ContributionItem` |                                                                                                                         |

**`ContributionItem`**

| Member         | Type   | Notes                                                                                                          |
|----------------|--------|----------------------------------------------------------------------------------------------------------------|
| `Name`         | string |                                                                                                                |
| `Weight`       | number |                                                                                                                |
| `Direction`    | number | Bayesian bootstrap structure strength: proportion of resamples in which the arc ran in the emitted direction.  |
| `Significance` | number | GLM per-term p-value.                                                                                          |
| `Source`       | string | What happened versus what it meant. See [`ProtocolConstants.Source`](#38-protocolconstants-open-vocabularies). |
| `HumanLabel`   | string |                                                                                                                |

### 3.7 `JourneyDescriptor`

The single fired path through a model that has one - a decision tree, a rule - structured
sufficiently to transcribe into a Jube coder rule without ever touching HTTP serialisation again.
`null` for families with no such path (a Bayesian network, an ensemble).

| Member | Type                   |
|--------|------------------------|
| `Path` | array of `JourneyNode` |

**`JourneyNode`**

| Member              | Type                                                                               |
|---------------------|------------------------------------------------------------------------------------|
| `Feature`           | string                                                                             |
| `Operator`          | string                                                                             |
| `Threshold`         | number                                                                             |
| `ThresholdCategory` | string                                                                             |
| `Source`            | string - see [`ProtocolConstants.Source`](#38-protocolconstants-open-vocabularies) |
| `HumanLabel`        | string                                                                             |

### 3.8 `ProtocolConstants` (open vocabularies)

These are documented values, not a closed set. An endpoint may emit a value not listed here; Jube
archives it rather than rejecting the response.

| Vocabulary           | Values                                                                                          |
|----------------------|-------------------------------------------------------------------------------------------------|
| `Family`             | `GLM`, `RandomForest`, `C5`, `XGBoost`, `SVM`, `BayesianNetwork`, `NeuralNetwork`, `ExpertRule` |
| `ContributionSpace`  | `Relative`                                                                                      |
| `ContributionMethod` | `Coefficient`, `BootstrapStrength`, `ArcStrength`, `ConnectionWeight`                           |
| `ValueSpace`         | `Probability`, `LogOdds`, `DecisionFunction`, `VoteFraction`, `Score`                           |
| `CalibrationMethod`  | `None`, `Native`, `Platt`, `Isotonic`, `Beta`                                                   |
| `StructureLearning`  | `HillClimbing`, `MMHC`, `TabuSearch`, `Expert`, `Constrained`, `None`                           |
| `Source`             | `Payload`, `Abstraction`, `AbstractionCalculation`, `TtlCounter`, `Dictionary`, `Sanction`      |

## 4. Suppression

`Error` non-null forces `Value` to null, whatever the wire body literally says. The legacy shape
`{"Value": 0.0, "Error": "..."}` is still parseable, but is now treated the same as
`{"Value": null, "Error": "..."}`: suppressed. An endpoint author should send `Value` as `null` (or
omit it) alongside a non-null `Error`; sending `0.0` alongside an `Error` is accepted for backward
compatibility, not recommended.

A response body that is neither a bare number nor a parseable `Adaptation` object - malformed JSON,
an empty body, a non-JSON error page - is treated identically to an explicit `Error`: `Value` is
null and the failure reason is recorded.

## 5. Consumption inside Jube

### 5.1 Payload storage

`EntityAnalysisModelInstanceEntryPayload.HttpAdaptation` is a dictionary of adaptation name to the
full `Adaptation` object - not just the score. This is the single stored copy: Model, Result,
Calibration, Contribution and Journey travel with it, so the Case UI can build a model journey
visualisation, and it is what gets archived (`ArchiveJson`) for the case record. There is no
separate flattened copy kept alongside it.

Where `ReportTable` is enabled on the adaptation, `Value` is additionally written to a flat
`ArchiveKey` row for SQL-level reporting, the same mechanism used for Abstraction, TTL Counters and
every other reportable metric - a deliberate denormalisation for querying, not a second source of
truth for `Value` itself.

### 5.2 Rule script access

A rule authored (Builder or Coder) as `HttpAdaptation.Example` is compiled through to
`HTTPAdaptation("Example").Value` - the Parser rewrites the dot-notation path automatically, so
existing rule text does not need to be rewritten by hand. The path stays recognisable; only the
compiled output changed.

A suppressed adaptation's `Value` is `null`. A VB.NET expression that compares or does arithmetic on
a null `Nullable(Of Double)` and assigns the (nullable) result to the rule's `Matched` boolean throws
`InvalidOperationException` on that narrowing conversion; the exception is caught by the rule's own
wrapper (every rule is compiled inside a `Try`/`Catch`), logged at Info level, and `Matched` defaults
to `False`. In other words: a suppressed adaptation safely does not match, at the cost of a log line
rather than a silent short-circuit.

`Value` is registered as an always-allowed token in the Parser (alongside `HttpAdaptation` itself),
so it does not need to be added to the `RuleScriptToken` table for existing or new rules to compile.
See [Rule Compilation Tokens and Extensions](../RuleCompilationAlgorithm/index.html).

### 5.3 Execution order: Priority and boosting

Adaptations on a model run in ascending `Priority` order (a `double`, default `0`, editable on the
HTTP Adaptation page). Because the POST body is the whole transaction payload (§2), a later
adaptation can read an earlier one's result directly, e.g. as an input feature or as a
`HttpAdaptation.EarlierModel.Value` term in the endpoint's own scoring logic - this is what makes
boosting / model chaining possible. Ties, and the historic default of `0` for every adaptation, are
broken by whatever order the database returns them in for that priority, which is not itself
guaranteed - give adaptations distinct priorities if run order matters.

### 5.4 Reference and mock endpoints

`Jube.App/Controllers/Mocks/MockHttpAdaptation.cs` exposes a series of unauthenticated endpoints
under `/api/MockHttpAdaptation/*` - one per family and edge case (bare number, suppressed, the
legacy error shape, malformed/empty bodies, a stale-calibration example, and so on) - so an
`EntityAnalysisModelHttpAdaptation.HttpEndpoint` can be pointed at a worked example of the protocol
without standing up an R/Python sandbox. `GET /api/MockHttpAdaptation` lists every scenario.

## 6. Source of record

The types in this document are a description of, not a substitute for, the C# records under
`Jube.Engine/EntityAnalysisModelInvoke/Context/Extensions/HttpAdaptations/Protocol/`. Where this page
and the code disagree, the code is correct and this page is stale.
