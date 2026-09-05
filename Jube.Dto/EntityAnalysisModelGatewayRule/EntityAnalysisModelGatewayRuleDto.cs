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

namespace Jube.Dto.EntityAnalysisModelGatewayRule
{
    [FormEndpoint("EntityAnalysisModelGatewayRule")]
    [FormKeys(Id = nameof(Id), Parent = nameof(EntityAnalysisModelId), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Rule", Order = 20)]
    [FormGroup("Gateway", Order = 30)]
    [FormGroup("Counters", Order = 40, Collapsed = true)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class EntityAnalysisModelGatewayRuleDto : IUpdated, IActivatable, ILockable, ITreeChild, IRuleBuilderJson
    {
        [Description("Identifier of the Model this Gateway Rule is registered against. Set from the parent Model " +
                     "context; not user-editable.")]
        public int EntityAnalysisModelId { get; set; }

        [Description("Display name of this Gateway Rule. Unique within the Model (case-insensitive).")]
        [FormField(Group = "Identity", Order = 10)]
        [ListColumn(Order = 10, Title = "Name")]
        public string? Name { get; set; }

        [Description("Free-text description of what this Gateway Rule filters for.")]
        [FormField(Group = "Identity", Order = 20, Widget = "textarea")]
        public string? Description { get; set; }

        [Description("Evaluation order relative to the other Gateway Rules on this Model; lower values evaluate " +
                     "first.")]
        [FormField(Group = "Identity", Order = 30)]
        [ListColumn(Order = 20, Title = "Priority")]
        public double Priority { get; set; }

        [Description("Which rule authoring surface is authoritative for this rule: 1 = visual Builder (requires " +
                     "BuilderRuleScript and Json), 2 = hand-written Coder (requires CoderRuleScript). The two " +
                     "surfaces are mutually exclusive -- only the selected surface's script is required.")]
        [FormField(Group = "Rule", Order = 30)]
        [Forms.Editor("RuleBuilder")]
        public int RuleScriptTypeId { get; set; }

        [Description("The hand-written Coder-surface source for this rule. Required when RuleScriptTypeId is 2 " +
                     "(Coder); not required when RuleScriptTypeId is 1 (Builder).")]
        [FormField(Group = "Rule", Order = 40)]
        [Forms.Editor("RuleBuilder")]
        public string? CoderRuleScript { get; set; }

        [Description("Upon a match, the largest Response Elevation that may be responded with; larger values are " +
                     "truncated to this limit. Also bounded at the Model level.")]
        [FormField(Group = "Gateway", Order = 10)]
        public int MaxResponseElevation { get; set; }

        [Description("Fraction (0-1) of matching transactions that are sampled through to subsequent processing " +
                     "steps; the remainder are held back even though the Rule matched.")]
        [FormField(Group = "Gateway", Order = 20, Widget = "slider")]
        public double GatewaySample { get; set; }

        [Description("Whether the response payload is returned for a match on this Gateway Rule. Accepted by the " +
                     "API but not currently persisted -- always False when read back (pre-existing quirk, see " +
                     "migration report).")]
        [FormField(Group = "Gateway", Order = 30, Widget = "switch")]
        [NewDefault(false)]
        public bool ResponsePayload { get; set; }

        [Description("Running count of transactions that matched this Gateway Rule. Server-maintained; reset via " +
                     "the dedicated reset operation. Read-only.")]
        [FormField(Group = "Counters", Order = 10, ReadOnly = true)]
        public int ActivationCounter { get; set; }

        [Description("Timestamp (UTC) this Gateway Rule was last matched. Server-maintained. Read-only.")]
        [FormField(Group = "Counters", Order = 20, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? ActivationCounterDate { get; set; }

        [Description("Running count of transactions this Gateway Rule was evaluated against. Server-maintained; " +
                     "reset via the dedicated reset operation. Read-only.")]
        [FormField(Group = "Counters", Order = 30, ReadOnly = true)]
        public long EvaluationCounter { get; set; }

        [Description("When true, this Gateway Rule participates in evaluation on transaction invocation.")]
        [FormField(Group = "Identity", Order = 40, Widget = "switch")]
        [NewDefault(false)]
        public bool Active { get; set; }

        [Description("When true, this Gateway Rule is locked and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 50, Widget = "switch")]
        [NewDefault(false)]
        public bool Locked { get; set; }

        [Description("The compiled Builder/Coder rule script that is evaluated against the transaction Payload " +
                     "and Dictionary values; must return True on match, False otherwise. Required when " +
                     "RuleScriptTypeId is 1 (Builder); not required when RuleScriptTypeId is 2 (Coder).")]
        [FormField(Group = "Rule", Order = 10)]
        [Forms.Editor("RuleBuilder")]
        public string BuilderRuleScript { get; set; } = string.Empty;

        [Description("The Query Builder JSON definition backing BuilderRuleScript. Required when RuleScriptTypeId " +
                     "is 1 (Builder); not required when RuleScriptTypeId is 2 (Coder).")]
        [FormField(Group = "Rule", Order = 20)]
        [Forms.Editor("RuleBuilder")]
        public string Json { get; set; } = string.Empty;

        [Description("Server-assigned row identifier. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Id { get; set; }

        [Description("User who created this Gateway Rule. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        public string? CreatedUser { get; set; }

        [Description("Timestamp (UTC) this Gateway Rule was created. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("User who last updated this Gateway Rule. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        public string? UpdatedUser { get; set; }

        [Description("Timestamp (UTC) this Gateway Rule was last updated. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned optimistic-concurrency version number, incremented on every update. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 60, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Version { get; set; }

        [Description("User who deleted this Gateway Rule, if soft-deleted. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 70, ReadOnly = true)]
        public string? DeletedUser { get; set; }

        [Description("Timestamp (UTC) this Gateway Rule was soft-deleted, if applicable. Server-assigned. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 80, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}