/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.ComponentModel;
using Jube.Dto.Forms;
using Jube.Dto.Interfaces;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Jube.Dto.EntityAnalysisModelActivationRule
{
    [FormEndpoint("EntityAnalysisModelActivationRule")]
    [FormKeys(Id = nameof(Id), Parent = nameof(EntityAnalysisModelId), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Review", Order = 20)]
    [FormGroup("Rule", Order = 30)]
    [FormGroup("Suppression", Order = 40)]
    [FormGroup("Case Workflow", Order = 50)]
    [FormGroup("Response Elevation", Order = 60)]
    [FormGroup("Notification", Order = 70, Collapsed = true)]
    [FormGroup("TTL Counter", Order = 80, Collapsed = true)]
    [FormGroup("Output and Sampling", Order = 90)]
    [FormGroup("Activity", Order = 95, Collapsed = true)]
    [FormGroup("Audit", Order = 100, Collapsed = true)]
    public class EntityAnalysisModelActivationRuleDto : IUpdated, IActivatable, ILockable, ITreeChild,
        IRuleBuilderJson
    {
        [Description("Identifier of the Model this Activation Rule is registered against. Set from the parent " +
                     "Model context; not user-editable.")]
        public int EntityAnalysisModelId { get; set; }

        [Description("Display name of this Activation Rule. Unique within the Model (case-insensitive).")]
        [FormField(Group = "Identity", Order = 10)]
        [ListColumn(Order = 10, Title = "Name")]
        public string? Name { get; set; }

        [Description("The order of evaluation for this Activation Rule relative to the other Activation Rules on " +
                     "the Model, for the purpose of Activation Rule Chaining -- the outcome of a lower-Priority " +
                     "Activation Rule is available to a higher-Priority one via the invocation context.")]
        [FormField(Group = "Identity", Order = 40, Widget = "number")]
        [NewDefault(0)]
        public double Priority { get; set; }

        [Description("Maker-checker review status: 0 New, 1 New Pending Review, 2 Updated Pending Review, " +
                     "3 Rejected by Review, 4 Approved by Review. Only a rule with status 4 (Approved by Review) " +
                     "is eligible for activation on transaction invocation. Setting this to 4 requires the caller " +
                     "to additionally hold the Allow Approved By Review permission.")]
        [FormField(Group = "Review", Order = 10, Widget = "select")]
        [NewDefault(0)]
        public int ReviewStatusId { get; set; }

        [Description("Which rule authoring surface is authoritative for this rule: 1 = visual Builder (requires " +
                     "BuilderRuleScript and Json), 2 = hand-written Coder (requires CoderRuleScript). The two " +
                     "surfaces are mutually exclusive -- only the selected surface's script is required.")]
        [FormField(Group = "Rule", Order = 10)]
        [Forms.Editor("RuleBuilder")]
        public int RuleScriptTypeId { get; set; }

        [Description("The hand-written Coder-surface source for this rule. Required when RuleScriptTypeId is 2 " +
                     "(Coder); not required when RuleScriptTypeId is 1 (Builder).")]
        [FormField(Group = "Rule", Order = 40)]
        [Forms.Editor("RuleBuilder")]
        public string? CoderRuleScript { get; set; }

        [Description("When true, this Activation Rule's matches are eligible for suppression (see the separate " +
                     "Activation Rule Suppression configuration).")]
        [FormField(Group = "Suppression", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        public bool EnableSuppression { get; set; }

        [Description("When true, a Case is created as a consequence of this Activation Rule matching.")]
        [FormField(Group = "Case Workflow", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        public bool EnableCaseWorkflow { get; set; }

        [Description("The Case Workflow the new Case will be assigned to. Required when EnableCaseWorkflow is " +
                     "true.")]
        [FormField(Group = "Case Workflow", Order = 20, Widget = "select")]
        [VisibleWhen(nameof(EnableCaseWorkflow), true)]
        [RequiredWhen(nameof(EnableCaseWorkflow), true)]
        [Lookup("/api/CaseWorkflow/ByEntityAnalysisModelId", TextField = "name", ValueField = "guid",
            ParentField = nameof(EntityAnalysisModelId))]
        public Guid CaseWorkflowGuid { get; set; }

        [Description("The Case Workflow Status, rolling up to Case Workflow, that the new Case will be assigned " +
                     "to. Required when EnableCaseWorkflow is true.")]
        [FormField(Group = "Case Workflow", Order = 30, Widget = "select")]
        [VisibleWhen(nameof(EnableCaseWorkflow), true)]
        [RequiredWhen(nameof(EnableCaseWorkflow), true)]
        [Lookup("/api/CaseWorkflowStatus/ByCaseWorkflowGuid", TextField = "name", ValueField = "guid",
            ParentField = nameof(CaseWorkflowGuid))]
        public Guid CaseWorkflowStatusGuid { get; set; }

        [Description("The Request XPath, Inline Script or Inline Function data element used as the Case Key " +
                     "(e.g. Account Id) when creating the Case. Required when EnableCaseWorkflow is true.")]
        [FormField(Group = "Case Workflow", Order = 40, Widget = "select")]
        [VisibleWhen(nameof(EnableCaseWorkflow), true)]
        [RequiredWhen(nameof(EnableCaseWorkflow), true)]
        [Lookup("/api/GetEntityAnalysisPotentialMultiPartStringNames", TextField = "value", ValueField = "value",
            ParentField = nameof(EntityAnalysisModelId))]
        public string? CaseKey { get; set; }

        [Description("When true, the percentage of Case creations given by BypassSuspendSample are set to a " +
                     "Bypass Suspend status rather than progressing normally.")]
        [FormField(Group = "Case Workflow", Order = 50, Widget = "switch")]
        [VisibleWhen(nameof(EnableCaseWorkflow), true)]
        [NewDefault(false)]
        public bool EnableBypass { get; set; }

        [Description("The percentage (0-1) of Case creations to set to a Bypass Suspend status.")]
        [FormField(Group = "Case Workflow", Order = 60, Widget = "slider")]
        [VisibleWhen(nameof(EnableCaseWorkflow), true)]
        [VisibleWhen(nameof(EnableBypass), true)]
        [NewDefault(0)]
        public double BypassSuspendSample { get; set; }

        [Description("Unit of Bypass Suspend Value, for the purpose of setting the date a Bypass-Suspended Case " +
                     "moves to Closed: Minutes (n), Hours (h), Days (d), Months (m).")]
        [FormField(Group = "Case Workflow", Order = 70, Widget = "radio")]
        [VisibleWhen(nameof(EnableCaseWorkflow), true)]
        [VisibleWhen(nameof(EnableBypass), true)]
        [NewDefault('h')]
        public char BypassSuspendInterval { get; set; } = 'h';

        [Description("The interval value, taken together with BypassSuspendInterval, for the purpose of setting " +
                     "the date a Bypass-Suspended Case moves to Closed.")]
        [FormField(Group = "Case Workflow", Order = 80, Widget = "number")]
        [VisibleWhen(nameof(EnableCaseWorkflow), true)]
        [VisibleWhen(nameof(EnableBypass), true)]
        [NewDefault(0)]
        public int BypassSuspendValue { get; set; }

        [Description("When true, the Response Elevation value is raised alongside other outbound communication " +
                     "messages on this Activation Rule matching.")]
        [FormField(Group = "Response Elevation", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        public bool EnableResponseElevation { get; set; }

        [Description("The upwardly-moving numeric value reported in the response payload such that a " +
                     "consolidated response (e.g. a transaction decline) can be inferred. Enforced when " +
                     "EnableResponseElevation is true.")]
        [FormField(Group = "Response Elevation", Order = 20, Widget = "number")]
        [VisibleWhen(nameof(EnableResponseElevation), true)]
        [NewDefault(0)]
        public double ResponseElevation { get; set; }

        [Description("Free text detailing the Activation Rule match -- for example, the message to be served to " +
                     "the customer in an online fraud prevention implementation.")]
        [FormField(Group = "Response Elevation", Order = 30, Widget = "textarea")]
        [VisibleWhen(nameof(EnableResponseElevation), true)]
        public string? ResponseElevationContent { get; set; }

        [Description("A URL to redirect the user or advertising exchange to on this Activation Rule matching.")]
        [FormField(Group = "Response Elevation", Order = 40)]
        [VisibleWhen(nameof(EnableResponseElevation), true)]
        public string? ResponseElevationRedirect { get; set; }

        [Description("When true, this Activation Rule match ticks out a message to the Activation Watcher. Used " +
                     "in conjunction with a Model-level sample so as not to flood the Activation Watcher in " +
                     "high-throughput implementations.")]
        [FormField(Group = "Response Elevation", Order = 50, Widget = "switch")]
        [VisibleWhen(nameof(EnableResponseElevation), true)]
        [NewDefault(false)]
        public bool SendToActivationWatcher { get; set; }

        [Description("The identifier value (e.g. Account Id) accompanying the message sent to the Activation " +
                     "Watcher. Required when EnableResponseElevation and SendToActivationWatcher are both true.")]
        [FormField(Group = "Response Elevation", Order = 60, Widget = "select")]
        [VisibleWhen(nameof(EnableResponseElevation), true)]
        [VisibleWhen(nameof(SendToActivationWatcher), true)]
        [RequiredWhen(nameof(EnableResponseElevation), true)]
        [RequiredWhen(nameof(SendToActivationWatcher), true)]
        [Lookup("/api/GetEntityAnalysisPotentialMultiPartStringNames", TextField = "value", ValueField = "value",
            ParentField = nameof(EntityAnalysisModelId))]
        public string? ResponseElevationKey { get; set; }

        [Description("The fore colour (hex) sent to the Activation Watcher and response payload. Required when " +
                     "EnableResponseElevation and SendToActivationWatcher are both true.")]
        [FormField(Group = "Response Elevation", Order = 70, Widget = "color")]
        [VisibleWhen(nameof(EnableResponseElevation), true)]
        [VisibleWhen(nameof(SendToActivationWatcher), true)]
        [RequiredWhen(nameof(EnableResponseElevation), true)]
        [RequiredWhen(nameof(SendToActivationWatcher), true)]
        public string? ResponseElevationForeColor { get; set; }

        [Description("The back colour (hex) sent to the Activation Watcher and response payload. Required when " +
                     "EnableResponseElevation and SendToActivationWatcher are both true.")]
        [FormField(Group = "Response Elevation", Order = 80, Widget = "color")]
        [VisibleWhen(nameof(EnableResponseElevation), true)]
        [VisibleWhen(nameof(SendToActivationWatcher), true)]
        [RequiredWhen(nameof(EnableResponseElevation), true)]
        [RequiredWhen(nameof(SendToActivationWatcher), true)]
        public string? ResponseElevationBackColor { get; set; }

        [Description("When true, an asynchronous Notification (Email or SMS) is dispatched on this Activation " +
                     "Rule matching.")]
        [FormField(Group = "Notification", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        public bool EnableNotification { get; set; }

        [Description("The Notification channel: 1 Email, 2 SMS. Enforced when EnableNotification is true.")]
        [FormField(Group = "Notification", Order = 20, Widget = "radio")]
        [VisibleWhen(nameof(EnableNotification), true)]
        [NewDefault(1)]
        public int NotificationTypeId { get; set; }

        [Description("The destination address (email address or phone number) for the Notification.")]
        [FormField(Group = "Notification", Order = 30)]
        [VisibleWhen(nameof(EnableNotification), true)]
        public string? NotificationDestination { get; set; }

        [Description("The tokenized subject -- tokens are replaced with values from the Payload -- for an Email " +
                     "Notification.")]
        [FormField(Group = "Notification", Order = 40)]
        [VisibleWhen(nameof(EnableNotification), true)]
        public string? NotificationSubject { get; set; }

        [Description("The tokenized body -- tokens are replaced with values from the Payload -- for the " +
                     "Notification.")]
        [FormField(Group = "Notification", Order = 50, Widget = "textarea")]
        [VisibleWhen(nameof(EnableNotification), true)]
        public string? NotificationBody { get; set; }

        [Description("When true, TTL Counter incrementation occurs on this Activation Rule matching.")]
        [FormField(Group = "TTL Counter", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        public bool EnableTtlCounter { get; set; }

        [Description("The Model containing the TTL Counter to increment. Required when EnableTtlCounter is true.")]
        [FormField(Group = "TTL Counter", Order = 20, Widget = "select")]
        [VisibleWhen(nameof(EnableTtlCounter), true)]
        [RequiredWhen(nameof(EnableTtlCounter), true)]
        [Lookup("/api/EntityAnalysisModel", TextField = "name", ValueField = "guid")]
        public Guid EntityAnalysisModelGuidTtlCounter { get; set; }

        [Description("The TTL Counter, configured on the selected Model, to be incremented. Required when " +
                     "EnableTtlCounter is true.")]
        [FormField(Group = "TTL Counter", Order = 30, Widget = "select")]
        [VisibleWhen(nameof(EnableTtlCounter), true)]
        [RequiredWhen(nameof(EnableTtlCounter), true)]
        [Lookup("/api/EntityAnalysisModelTtlCounter/ByEntityAnalysisModelGuid", TextField = "name",
            ValueField = "guid", ParentField = nameof(EntityAnalysisModelGuidTtlCounter))]
        public Guid EntityAnalysisModelTtlCounterGuid { get; set; }

        [Description("A percentage (0-1) of matching transactions or events that are persisted to the database. " +
                     "The sample is only taken when the rule returns True -- useful for high-volume Models where " +
                     "reviewing every match is impractical.")]
        [FormField(Group = "Output and Sampling", Order = 10, Widget = "slider")]
        [NewDefault(1)]
        public double ActivationSample { get; set; }

        [Description("When true, an occurrence of this Activation Rule matching is displayed in the Case " +
                     "Management Case Key Journal. Typically left false for TTL Counter increments that should " +
                     "not clutter the journal.")]
        [FormField(Group = "Output and Sampling", Order = 20, Widget = "switch")]
        [NewDefault(true)]
        public bool Visible { get; set; }

        [Description("When true, this Activation Rule is subject and visible to a reprocessing job.")]
        [FormField(Group = "Output and Sampling", Order = 30, Widget = "switch")]
        [NewDefault(false)]
        public bool EnableReprocessing { get; set; }

        [Description("When true, every match of this Activation Rule is persisted to the reporting table for " +
                     "later analysis.")]
        [FormField(Group = "Output and Sampling", Order = 40, Widget = "switch")]
        [NewDefault(false)]
        public bool ReportTable { get; set; }

        [Description("Whether the response payload is returned for a match on this Activation Rule.")]
        [FormField(Group = "Output and Sampling", Order = 50, Widget = "switch")]
        [NewDefault(false)]
        public bool ResponsePayload { get; set; }

        [Description("Count of transactions or events evaluated against this Activation Rule. Server-maintained. " +
                     "Read-only; reset via the dedicated Reset operation.")]
        [FormField(Group = "Activity", Order = 10, ReadOnly = true)]
        public long EvaluationCounter { get; set; }

        [Description("Count of matches (activations) of this Activation Rule. Server-maintained. Read-only; " +
                     "reset via the dedicated Reset operation.")]
        [FormField(Group = "Activity", Order = 20, ReadOnly = true)]
        public long ActivationCounter { get; set; }

        [Description("Timestamp (UTC) of the most recent activation counted in ActivationCounter. Server-" +
                     "maintained. Read-only.")]
        [FormField(Group = "Activity", Order = 30, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? ActivationCounterDate { get; set; }

        [Description("When true, this Activation Rule participates in evaluation on transaction invocation.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        public bool Active { get; set; }

        [Description("When true, this Activation Rule is locked and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [NewDefault(false)]
        public bool Locked { get; set; }

        [Description("The compiled Builder/Coder rule script tested against the Payload and other collection " +
                     "data objects available thus far in model invocation (TTL Counters, Abstraction, Adaptation, " +
                     "Abstraction Calculation, Sanctions, Dictionary, Activation); must return True on match, " +
                     "False otherwise. Required when RuleScriptTypeId is 1 (Builder); not required when " +
                     "RuleScriptTypeId is 2 (Coder).")]
        [FormField(Group = "Rule", Order = 20)]
        [Forms.Editor("RuleBuilder")]
        public string BuilderRuleScript { get; set; } = string.Empty;

        [Description("The Query Builder JSON definition backing BuilderRuleScript. Required when RuleScriptTypeId " +
                     "is 1 (Builder); not required when RuleScriptTypeId is 2 (Coder).")]
        [FormField(Group = "Rule", Order = 30)]
        [Forms.Editor("RuleBuilder")]
        public string Json { get; set; } = string.Empty;

        [Description("Server-assigned row identifier. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Id { get; set; }

        [Description("User who created this Activation Rule. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        public string? CreatedUser { get; set; }

        [Description("Timestamp (UTC) this Activation Rule was created. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("User who last updated this Activation Rule. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        public string? UpdatedUser { get; set; }

        [Description("Timestamp (UTC) this Activation Rule was last updated. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned optimistic-concurrency version number, incremented on every update. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 60, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Version { get; set; }

        [Description("User who deleted this Activation Rule, if soft-deleted. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 70, ReadOnly = true)]
        public string? DeletedUser { get; set; }

        [Description("Timestamp (UTC) this Activation Rule was soft-deleted, if applicable. Server-assigned. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 80, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}