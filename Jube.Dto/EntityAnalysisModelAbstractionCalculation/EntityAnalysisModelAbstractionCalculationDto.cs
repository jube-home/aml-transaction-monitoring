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

namespace Jube.Dto.EntityAnalysisModelAbstractionCalculation
{
    [FormEndpoint("EntityAnalysisModelAbstractionCalculation")]
    [FormKeys(Id = nameof(Id), Parent = nameof(EntityAnalysisModelId), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Calculation", Order = 20)]
    [FormGroup("Output", Order = 30)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class EntityAnalysisModelAbstractionCalculationDto : IUpdated, IActivatable, ILockable, ITreeChild
    {
        [Description("Identifier of the Model this Abstraction Calculation is registered against. Set from the " +
                     "parent Model context; not user-editable.")]
        public int EntityAnalysisModelId { get; set; }

        [Description("Display name of this Abstraction Calculation. Unique within the Model " +
                     "(case-insensitive).")]
        [FormField(Group = "Identity", Order = 10)]
        [ListColumn(Order = 10, Title = "Name")]
        public string? Name { get; set; }

        [Description("Which surface computes the result: 1 Add, 2 Subtract, 3 Divide, 4 Multiply (all four take " +
                     "the Left and Right Abstraction Rule values), or 5 Coder (a hand-written VB.Net function " +
                     "fragment supplied in FunctionScript). The arithmetic surfaces and the Coder surface are " +
                     "mutually exclusive.")]
        [FormField(Group = "Calculation", Order = 10, Widget = "radio")]
        [NewDefault(3)]
        public int AbstractionCalculationTypeId { get; set; }

        [Description("The Abstraction Rule occupying the left-hand side of the arithmetic. Required unless " +
                     "AbstractionCalculationTypeId is 5 (Coder).")]
        [FormField(Group = "Calculation", Order = 20, Widget = "select")]
        [VisibleWhen(nameof(AbstractionCalculationTypeId), 5, Op = ConditionOp.NotEquals)]
        [RequiredWhen(nameof(AbstractionCalculationTypeId), 5, Op = ConditionOp.NotEquals)]
        [Lookup("/api/TreeChildren/AbstractionRule", TextField = "name", ValueField = "name",
            ParentField = nameof(EntityAnalysisModelId))]
        public string? EntityAnalysisModelAbstractionNameLeft { get; set; }

        [Description("The Abstraction Rule occupying the right-hand side of the arithmetic. Required unless " +
                     "AbstractionCalculationTypeId is 5 (Coder).")]
        [FormField(Group = "Calculation", Order = 30, Widget = "select")]
        [VisibleWhen(nameof(AbstractionCalculationTypeId), 5, Op = ConditionOp.NotEquals)]
        [RequiredWhen(nameof(AbstractionCalculationTypeId), 5, Op = ConditionOp.NotEquals)]
        [Lookup("/api/TreeChildren/AbstractionRule", TextField = "name", ValueField = "name",
            ParentField = nameof(EntityAnalysisModelId))]
        public string? EntityAnalysisModelAbstractionNameRight { get; set; }

        [Description("The hand-written VB.Net function fragment computing the result. Required when " +
                     "AbstractionCalculationTypeId is 5 (Coder); not required for the arithmetic surfaces.")]
        [FormField(Group = "Calculation", Order = 40)]
        [VisibleWhen(nameof(AbstractionCalculationTypeId), 5)]
        [RequiredWhen(nameof(AbstractionCalculationTypeId), 5)]
        [Forms.Editor("RuleBuilder")]
        public string? FunctionScript { get; set; }

        [Description("When true, every evaluation of this Abstraction Calculation is persisted to the reporting " +
                     "table for later analysis.")]
        [FormField(Group = "Output", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        public bool ReportTable { get; set; }

        [Description("Whether the response payload is returned for this Abstraction Calculation.")]
        [FormField(Group = "Output", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        public bool ResponsePayload { get; set; }

        [Description("When true, this Abstraction Calculation participates in evaluation on transaction " +
                     "invocation.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        public bool Active { get; set; }

        [Description("When true, this Abstraction Calculation is locked and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [NewDefault(false)]
        public bool Locked { get; set; }

        [Description("Server-assigned row identifier. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Id { get; set; }

        [Description("User who created this Abstraction Calculation. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        public string? CreatedUser { get; set; }

        [Description("Timestamp (UTC) this Abstraction Calculation was created. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("User who last updated this Abstraction Calculation. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        public string? UpdatedUser { get; set; }

        [Description("Timestamp (UTC) this Abstraction Calculation was last updated. Server-assigned. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned optimistic-concurrency version number, incremented on every update. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 60, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Version { get; set; }

        [Description("User who deleted this Abstraction Calculation, if soft-deleted. Server-assigned. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 70, ReadOnly = true)]
        public string? DeletedUser { get; set; }

        [Description("Timestamp (UTC) this Abstraction Calculation was soft-deleted, if applicable. " +
                     "Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 80, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}