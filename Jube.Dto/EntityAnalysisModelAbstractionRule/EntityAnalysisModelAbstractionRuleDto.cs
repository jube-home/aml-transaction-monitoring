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

namespace Jube.Dto.EntityAnalysisModelAbstractionRule
{
    [FormEndpoint("EntityAnalysisModelAbstractionRule")]
    [FormKeys(Id = nameof(Id), Parent = nameof(EntityAnalysisModelId), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Rule", Order = 20)]
    [FormGroup("Search and Aggregation", Order = 30)]
    [FormGroup("Offset", Order = 40, Collapsed = true)]
    [FormGroup("Output", Order = 50)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class EntityAnalysisModelAbstractionRuleDto : IUpdated, IActivatable, ILockable, ITreeChild,
        IRuleBuilderJson
    {
        [Description("Identifier of the Model this Abstraction Rule is registered against. Set from the parent " +
                     "Model context; not user-editable.")]
        public int EntityAnalysisModelId { get; set; }

        [Description("Display name of this Abstraction Rule. Unique within the Model (case-insensitive).")]
        [FormField(Group = "Identity", Order = 10)]
        [ListColumn(Order = 10, Title = "Name")]
        public string? Name { get; set; }

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

        [Description("When true, this Abstraction Rule is tested against every record returned from the cache " +
                     "for the given Search Key, rather than only the transaction currently being processed. " +
                     "Enables the Search Key, Search Value, Search Interval, Function and Offset settings below.")]
        [FormField(Group = "Search and Aggregation", Order = 10, Widget = "switch")]
        [NewDefault(true)]
        public bool Search { get; set; }

        [Description("The Request XPath, Inline Script or Inline Function data element (flagged as a Search Key " +
                     "on the parent Model) used to retrieve matching cache records for this Model. Required when " +
                     "Search is enabled.")]
        [FormField(Group = "Search and Aggregation", Order = 20, Widget = "select")]
        [VisibleWhen(nameof(Search), true)]
        [RequiredWhen(nameof(Search), true)]
        [Lookup("/api/GetEntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeQuery",
            TextField = "name", ValueField = "name", ParentField = nameof(EntityAnalysisModelId))]
        public string? SearchKey { get; set; }

        [Description("Length of the Search Interval, taken together with Search Interval, that a cache record's " +
                     "timestamp must fall within (reaching back from the transaction's reference date) to be " +
                     "eligible for matching. Only enforced when Search is enabled.")]
        [FormField(Group = "Search and Aggregation", Order = 30, Widget = "number")]
        [VisibleWhen(nameof(Search), true)]
        [NewDefault(1)]
        public int SearchValue { get; set; }

        [Description("Unit of Search Value: Seconds (s), Minutes (n), Hours (h) or Days (d). Only enforced when " +
                     "Search is enabled.")]
        [FormField(Group = "Search and Aggregation", Order = 40, Widget = "radio")]
        [VisibleWhen(nameof(Search), true)]
        [NewDefault("h")]
        public string? SearchInterval { get; set; }

        [Description("The aggregation function applied to the matched, offset-reduced cache records: 1 Count, " +
                     "2 Distinct Count, 3 Sum, 4 Average, 5 Median, 6 Kurtosis, 7 Skew, 8 Standard Deviation, " +
                     "11 Mode, 12 Same Count, 13 Actual Value, 14 Max, 15 Min, 16 Since.")]
        [FormField(Group = "Search and Aggregation", Order = 50, Widget = "select")]
        [VisibleWhen(nameof(Search), true)]
        [NewDefault(1)]
        public int SearchFunctionTypeId { get; set; }

        [Description("The data element that the Function Type aggregates over (e.g. the field to Sum or Average). " +
                     "Not applicable when Function Type is Count (1) or Distinct Count (2)/Same Count (12), which " +
                     "instead use a string-typed key.")]
        [FormField(Group = "Search and Aggregation", Order = 60, Widget = "select")]
        [VisibleWhen(nameof(Search), true)]
        [VisibleWhen(nameof(SearchFunctionTypeId), 1, Op = ConditionOp.NotEquals)]
        [Lookup("/api/GetEntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeQuery",
            TextField = "name", ValueField = "name", ParentField = nameof(EntityAnalysisModelId))]
        public string? SearchFunctionKey { get; set; }

        [Description("When true, the matched cache records are reduced to a sub-range (First/Last/Skip " +
                     "First/Take Last, by Offset Value) before the Function Type aggregation is applied. Only " +
                     "meaningful when Search is enabled.")]
        [FormField(Group = "Offset", Order = 10, Widget = "switch")]
        [VisibleWhen(nameof(Search), true)]
        [NewDefault(false)]
        public bool Offset { get; set; }

        [Description("How the matched cache records are reduced when Offset is enabled: 1 First, 2 Last, " +
                     "3 Skip First, 4 Take Last.")]
        [FormField(Group = "Offset", Order = 20, Widget = "radio")]
        [VisibleWhen(nameof(Search), true)]
        [VisibleWhen(nameof(Offset), true)]
        [RequiredWhen(nameof(Offset), true)]
        [NewDefault(1)]
        public int OffsetTypeId { get; set; }

        [Description("The number of records the Offset Type reduction applies to (e.g. skip the first N, or take " +
                     "the last N).")]
        [FormField(Group = "Offset", Order = 30, Widget = "number")]
        [VisibleWhen(nameof(Search), true)]
        [VisibleWhen(nameof(Offset), true)]
        [NewDefault(0)]
        public int OffsetValue { get; set; }

        [Description("When true, every match of this Abstraction Rule is persisted to the reporting table for " +
                     "later analysis.")]
        [FormField(Group = "Output", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        public bool ReportTable { get; set; }

        [Description("Whether the response payload is returned for a match on this Abstraction Rule.")]
        [FormField(Group = "Output", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        public bool ResponsePayload { get; set; }

        [Description("Legacy inherited-rule linkage, retained for backward compatibility. Not surfaced on the " +
                     "current page and not populated by it -- see the migration report.")]
        public int InheritedId { get; set; }

        [Description("When true, this Abstraction Rule participates in evaluation on transaction invocation.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        public bool Active { get; set; }

        [Description("When true, this Abstraction Rule is locked and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [NewDefault(false)]
        public bool Locked { get; set; }

        [Description("The compiled Builder/Coder rule script tested for a match against the Payload and, when " +
                     "Search is enabled, each cache record returned for the Search Key; must return True on " +
                     "match, False otherwise. Required when RuleScriptTypeId is 1 (Builder); not required when " +
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

        [Description("User who created this Abstraction Rule. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        public string? CreatedUser { get; set; }

        [Description("Timestamp (UTC) this Abstraction Rule was created. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("User who last updated this Abstraction Rule. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        public string? UpdatedUser { get; set; }

        [Description("Timestamp (UTC) this Abstraction Rule was last updated. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned optimistic-concurrency version number, incremented on every update. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 60, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Version { get; set; }

        [Description("User who deleted this Abstraction Rule, if soft-deleted. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 70, ReadOnly = true)]
        public string? DeletedUser { get; set; }

        [Description("Timestamp (UTC) this Abstraction Rule was soft-deleted, if applicable. Server-assigned. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 80, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}