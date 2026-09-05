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

namespace Jube.Dto.EntityAnalysisModelHttpAdaptation
{
    [FormEndpoint("EntityAnalysisModelAdaptation")]
    [FormKeys(Id = nameof(Id), Parent = nameof(EntityAnalysisModelId), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Endpoint", Order = 20)]
    [FormGroup("Output", Order = 30)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class EntityAnalysisModelHttpAdaptationDto : IUpdated, IActivatable, ILockable, ITreeChild
    {
        [Description("Identifier of the Model this HTTP Adaptation is registered against. Set from the parent " +
                     "Model context; not user-editable.")]
        public int EntityAnalysisModelId { get; set; }

        [Description("Display name of this HTTP Adaptation. Unique within the Model (case-insensitive).")]
        [FormField(Group = "Identity", Order = 10)]
        [ListColumn(Order = 10, Title = "Name")]
        public string? Name { get; set; }

        [Description("The remainder of the URL to POST the transaction payload to, appended to the " +
                     "HttpAdaptationUrl environment variable's value (which carries no trailing slash) -- so " +
                     "this value must begin with '/', e.g. '/api/invoke/ExampleFraudScoreLocalEndpoint'.")]
        [FormField(Group = "Endpoint", Order = 10)]
        [ListColumn(Order = 20, Title = "HTTP Endpoint")]
        public string? HttpEndpoint { get; set; }

        [Description("Ascending execution order among the HTTP Adaptations on a Model, to support " +
                     "boosting/model chaining -- a lower Priority Adaptation is recalled first, and its result " +
                     "is available to a later one via the request body. Defaults to 0.")]
        [FormField(Group = "Endpoint", Order = 20, Widget = "number")]
        [NewDefault(0)]
        public double Priority { get; set; }

        [Description("When true, every recall of this HTTP Adaptation is persisted to the reporting table for " +
                     "later analysis.")]
        [FormField(Group = "Output", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        public bool ReportTable { get; set; }

        [Description("Whether the response payload is returned for a recall of this HTTP Adaptation.")]
        [FormField(Group = "Output", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        public bool ResponsePayload { get; set; }

        [Description("Legacy inherited-rule linkage, retained for backward compatibility. Not surfaced on the " +
                     "current page and not populated by it -- see the migration report.")]
        public int InheritedId { get; set; }

        [Description("When true, this HTTP Adaptation participates in evaluation on transaction invocation.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        public bool Active { get; set; }

        [Description("When true, this HTTP Adaptation is locked and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [NewDefault(false)]
        public bool Locked { get; set; }

        [Description("Server-assigned row identifier. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Id { get; set; }

        [Description("User who created this HTTP Adaptation. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        public string? CreatedUser { get; set; }

        [Description("Timestamp (UTC) this HTTP Adaptation was created. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("User who last updated this HTTP Adaptation. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        public string? UpdatedUser { get; set; }

        [Description("Timestamp (UTC) this HTTP Adaptation was last updated. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned optimistic-concurrency version number, incremented on every update. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 60, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Version { get; set; }

        [Description("User who deleted this HTTP Adaptation, if soft-deleted. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 70, ReadOnly = true)]
        public string? DeletedUser { get; set; }

        [Description("Timestamp (UTC) this HTTP Adaptation was soft-deleted, if applicable. Server-assigned. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 80, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}