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

namespace Jube.Dto.EntityAnalysisModelSanction
{
    [FormEndpoint("EntityAnalysisModelSanction")]
    [FormKeys(Id = nameof(Id), Parent = nameof(EntityAnalysisModelId), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Matching", Order = 20)]
    [FormGroup("Caching", Order = 30, Collapsed = true)]
    [FormGroup("Output", Order = 40, Collapsed = true)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class EntityAnalysisModelSanctionDto : IUpdated, IActivatable, ILockable, ITreeChild
    {
        [Description("Identifier of the Model this Sanction check is registered against. Set from the parent " +
                     "Model context; not user-editable.")]
        public int EntityAnalysisModelId { get; set; }

        [Description("Display name of this Sanction check registration. Unique within the Model " +
                     "(case-insensitive).")]
        [FormField(Group = "Identity", Order = 10)]
        [ListColumn(Order = 10, Title = "Name")]
        public string? Name { get; set; }

        [Description("Name of the data element on the parent Model supplying the multi-part string (a full name " +
                     "separated by spaces only) to check against the sanctions lists.")]
        [FormField(Group = "Matching", Order = 10, Widget = "select")]
        [Lookup("/api/GetEntityAnalysisPotentialMultiPartStringNames", TextField = "value", ValueField = "value",
            ParentField = nameof(EntityAnalysisModelId))]
        [ListColumn(Order = 20, Title = "Multipart String")]
        public string? MultipartStringDataName { get; set; }

        [Description("Maximum Levenshtein Distance for the fuzzy-matching step; a candidate whose edit distance " +
                     "exceeds this is not considered a match.")]
        [FormField(Group = "Matching", Order = 20, Widget = "slider")]
        [ListColumn(Order = 30, Title = "Distance")]
        public int Distance { get; set; }

        [Description("How the distances of every matched sanction entry are combined into the single value made " +
                     "available for rule evaluation: 1 = Sum, 2 = Average (default), 3 = Count, 4 = Max, 5 = Min, " +
                     "6 = First, 7 = Last, 8 = Confidence. Leave unset to use the server default (Average).")]
        [FormField(Group = "Matching", Order = 30, Widget = "select")]
        public byte? AggregationTypeId { get; set; }

        [Description("Overrides the server-wide maximum distance ratio for this Sanction check only, scaling " +
                     "down the allowed Levenshtein distance in proportion to the shorter token length being " +
                     "compared. Leave unset to use the server-wide default.")]
        [FormField(Group = "Matching", Order = 40, Widget = "slider")]
        public double? MaxDistanceRatio { get; set; }

        [Description("Overrides the server-wide maximum coverage ratio for this Sanction check only, rejecting a " +
                     "candidate match whose token count differs too greatly from the input's token count. Leave " +
                     "unset to use the server-wide default.")]
        [FormField(Group = "Matching", Order = 50, Widget = "slider")]
        public double? MaxCoverageRatio { get; set; }

        [Description("Interval unit for how long a matched multipart string's distance is cached: 's' (seconds), " +
                     "'n' (minutes), 'h' (hours) or 'd' (days).")]
        [FormField(Group = "Caching", Order = 10, Widget = "radio")]
        public char CacheInterval { get; set; }

        [Description("Length of time, in CacheInterval units, that a cached distance for a multipart string " +
                     "remains valid.")]
        [FormField(Group = "Caching", Order = 20)]
        public int CacheValue { get; set; }

        [Description("When true, a matching row is written to the sanctions report table for this transaction.")]
        [FormField(Group = "Output", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        public bool ReportTable { get; set; }

        [Description("When true, the response payload is returned for a match on this Sanction check.")]
        [FormField(Group = "Output", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        public bool ResponsePayload { get; set; }

        [Description("When true, this Sanction check participates in evaluation on transaction invocation.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        public bool Active { get; set; }

        [Description("When true, this Sanction check registration is locked and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [NewDefault(false)]
        public bool Locked { get; set; }

        [Description("Server-assigned row identifier. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Id { get; set; }

        [Description("User who created this Sanction check registration. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        public string? CreatedUser { get; set; }

        [Description("Timestamp (UTC) this Sanction check registration was created. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("User who last updated this Sanction check registration. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        public string? UpdatedUser { get; set; }

        [Description("Timestamp (UTC) this Sanction check registration was last updated. Server-assigned. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned optimistic-concurrency version number, incremented on every update. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 60, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Version { get; set; }

        [Description("User who deleted this Sanction check registration, if soft-deleted. Server-assigned. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 70, ReadOnly = true)]
        public string? DeletedUser { get; set; }

        [Description("Timestamp (UTC) this Sanction check registration was soft-deleted, if applicable. " +
                     "Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 80, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}