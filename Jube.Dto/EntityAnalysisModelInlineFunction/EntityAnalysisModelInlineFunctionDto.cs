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
using EditorAttribute = Jube.Dto.Forms.EditorAttribute;

namespace Jube.Dto.EntityAnalysisModelInlineFunction
{
    [FormEndpoint("EntityAnalysisModelInlineFunction")]
    [FormKeys(Id = nameof(Id), Parent = nameof(EntityAnalysisModelId), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Function", Order = 20)]
    [FormGroup("Behaviour", Order = 30)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class EntityAnalysisModelInlineFunctionDto : IUpdated, IActivatable, ILockable, ITreeChild
    {
        [Description("Identifier of the Model this Inline Function belongs to. Set from the parent Model context; " +
                     "not user-editable.")]
        public int EntityAnalysisModelId { get; set; }

        [Description("Display name of this field. Unique within the Model (case-insensitive).")]
        [FormField(Group = "Identity", Order = 10)]
        [ListColumn(Order = 10, Title = "Name")]
        public string? Name { get; set; }

        [Description("The datatype of the value returned by the Function Script, so that Abstraction and " +
                     "Activation Rules use the right functions against it: String, Integer, Float, Date or " +
                     "Boolean.")]
        [FormField(Group = "Function", Order = 10, Widget = "select")]
        [NewDefault(1)]
        public int ReturnDataTypeId { get; set; }

        [Description("A VB.net code fragment evaluated against the fields extracted so far by Request XPath and " +
                     "Inline Scripts, returning its result in the Matched variable.")]
        [FormField(Group = "Function", Order = 20, Widget = "code")]
        [Editor("BuilderCoder")]
        public string? FunctionScript { get; set; }

        [Description("Encrypts the returned value before it is stored and archived. Only shown, and only " +
                     "meaningful, when Return Data Type is String.")]
        [FormField(Group = "Function", Order = 30, Widget = "radio")]
        [VisibleWhen(nameof(ReturnDataTypeId), 1)]
        [NewDefault(0)]
        public int EncryptionId { get; set; }

        [Description("When true, this field's returned value is written to the report table.")]
        [FormField(Group = "Behaviour", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        public bool ReportTable { get; set; }

        [Description("When true, this field's returned value is included in the response payload.")]
        [FormField(Group = "Behaviour", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        public bool ResponsePayload { get; set; }

        [Description("When true, this field participates in evaluation on transaction invocation.")]
        [FormField(Group = "Identity", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        public bool Active { get; set; }

        [Description("When true, this field is locked and cannot be edited or deleted.")]
        [FormField(Group = "Identity", Order = 30, Widget = "switch")]
        [NewDefault(false)]
        public bool Locked { get; set; }

        [Description("Server-assigned row identifier. Read-only.")]
        [FormField(Group = "Audit", Order = 10, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Id { get; set; }

        [Description("User who created this Inline Function. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        public string? CreatedUser { get; set; }

        [Description("Timestamp (UTC) this Inline Function was created. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("User who last updated this Inline Function. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        public string? UpdatedUser { get; set; }

        [Description("Timestamp (UTC) this Inline Function was last updated. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned optimistic-concurrency version number, incremented on every update. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 60, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Version { get; set; }

        [Description("User who deleted this Inline Function, if soft-deleted. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 70, ReadOnly = true)]
        public string? DeletedUser { get; set; }

        [Description("Timestamp (UTC) this Inline Function was soft-deleted, if applicable. Server-assigned. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 80, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}