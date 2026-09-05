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

namespace Jube.Dto.EntityAnalysisModelTtlCounter
{
    [FormEndpoint("EntityAnalysisModelTtlCounter")]
    [FormKeys(Id = nameof(Id), Parent = nameof(EntityAnalysisModelId), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Counter", Order = 20)]
    [FormGroup("Response & Reporting", Order = 30)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class EntityAnalysisModelTtlCounterDto : IUpdated, IActivatable, ILockable, ITreeChild, IGuidIdentified
    {
        [Description("Identifier of the Model this TTL Counter is registered against. Set from the parent Model " +
                     "context; not user-editable.")]
        public int EntityAnalysisModelId { get; set; }

        [Description("Display name of this TTL Counter. Unique within the Model (case-insensitive). Selected by " +
                     "name in the Activation Rule that increments it, and referenced by name in the Archive/" +
                     "response payload.")]
        [FormField(Group = "Identity", Order = 10)]
        [ListColumn(Order = 10, Title = "Name")]
        public string? Name { get; set; }

        [Description("When true, TTL Counter entries are aggregated in real time on every event -- more expensive " +
                     "per-event, but avoids maintaining a separate cached summary counter.")]
        [FormField(Group = "Counter", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        public bool OnlineAggregation { get; set; }

        [Description("When true, this TTL Counter never decrements: the counter is updated but no counter entry " +
                     "is created to wind it back, so the interval, resolution, data-name and sum configuration " +
                     "below do not apply.")]
        [FormField(Group = "Counter", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        public bool EnableLiveForever { get; set; }

        [Description("Key extracted from the invocation payload used to group this TTL Counter (e.g. Account " +
                     "Id), sourced from the parent Model's string-typed Request XPaths / Inline Scripts. Not " +
                     "applicable when EnableLiveForever is true.")]
        [FormField(Group = "Counter", Order = 30, Widget = "dropdown")]
        [VisibleWhen(nameof(EnableLiveForever), false)]
        [Lookup("GetEntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeQuery",
            TextField = "name", ValueField = "name", ParentField = nameof(EntityAnalysisModelId))]
        public string? TtlCounterDataName { get; set; }

        [Description("When true, an Integer or Float value extracted from the invocation payload is added to " +
                     "the TTL Counter rather than incrementing it by one. Not applicable when EnableLiveForever " +
                     "is true.")]
        [FormField(Group = "Counter", Order = 40, Widget = "switch")]
        [VisibleWhen(nameof(EnableLiveForever), false)]
        [NewDefault(false)]
        public bool EnableSum { get; set; }

        [Description("Integer- or Float-typed field from the invocation payload whose value increments the TTL " +
                     "Counter, sourced from the parent Model's numeric Request XPaths / Inline Scripts. Required " +
                     "when EnableSum is true.")]
        [FormField(Group = "Counter", Order = 50, Widget = "dropdown")]
        [VisibleWhen(nameof(EnableLiveForever), false)]
        [VisibleWhen(nameof(EnableSum), true)]
        [RequiredWhen(nameof(EnableSum), true)]
        [Lookup("GetEntityAnalysisRequestXPathInlineScriptNamesByStringIntegerFloatDataTypeQuery",
            TextField = "name", ValueField = "name", ParentField = nameof(EntityAnalysisModelId))]
        public string? TtlCounterDataValue { get; set; }

        [Description("Unit of time before this TTL Counter is decremented after an entry increments it: 's' " +
                     "seconds, 'n' minutes, 'h' hours, 'd' days, 'm' months, 'y' years. Taken together with " +
                     "TtlCounterValue. Not applicable when EnableLiveForever is true.")]
        [FormField(Group = "Counter", Order = 60, Widget = "radio")]
        [VisibleWhen(nameof(EnableLiveForever), false)]
        public string? TtlCounterInterval { get; set; }

        [Description("Amount of time, in TtlCounterInterval units, before this TTL Counter is decremented after " +
                     "an entry increments it. Not applicable when EnableLiveForever is true.")]
        [FormField(Group = "Counter", Order = 70, Widget = "number")]
        [VisibleWhen(nameof(EnableLiveForever), false)]
        public int TtlCounterValue { get; set; }

        [Description("Truncation of the transaction's reference date used to bucket TTL Counter Entries for " +
                     "background deprecation: 'n' minutes, 'h' hours, 'd' days. Not applicable when " +
                     "EnableLiveForever is true.")]
        [FormField(Group = "Counter", Order = 80, Widget = "radio")]
        [VisibleWhen(nameof(EnableLiveForever), false)]
        public string? ResolutionInterval { get; set; }

        [Description("When true, this TTL Counter's current value is merged into the Archive section of the " +
                     "response payload returned to the caller.")]
        [FormField(Group = "Response & Reporting", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        public bool ResponsePayload { get; set; }

        [Description("When true, this TTL Counter's current value is written to the reporting table.")]
        [FormField(Group = "Response & Reporting", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        public bool ReportTable { get; set; }

        [Description("When true, this TTL Counter is eligible to be incremented and recalled.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        [ListColumn(Order = 20, Title = "Active")]
        public bool Active { get; set; }

        [Description("Server-assigned globally-unique identifier. Referenced by Activation Rules that increment " +
                     "this TTL Counter, and used to look counters up across Models. Read-only.")]
        [FormField(Group = "Identity", Order = 15, ReadOnly = true, Widget = "guid")]
        public Guid Guid { get; set; }

        [Description("When true, this TTL Counter registration is locked and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [NewDefault(false)]
        public bool Locked { get; set; }

        [Description("Server-assigned row identifier. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Id { get; set; }

        [Description("User who created this TTL Counter. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        public string? CreatedUser { get; set; }

        [Description("Timestamp (UTC) this TTL Counter was created. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("User who last updated this TTL Counter. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        public string? UpdatedUser { get; set; }

        [Description("Timestamp (UTC) this TTL Counter was last updated. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned optimistic-concurrency version number, incremented on every update. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 60, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Version { get; set; }

        [Description("User who deleted this TTL Counter, if soft-deleted. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 70, ReadOnly = true)]
        public string? DeletedUser { get; set; }

        [Description("Timestamp (UTC) this TTL Counter was soft-deleted, if applicable. Server-assigned. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 80, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}