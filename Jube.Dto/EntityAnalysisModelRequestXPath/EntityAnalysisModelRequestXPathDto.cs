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

namespace Jube.Dto.EntityAnalysisModelRequestXPath
{
    [FormEndpoint("EntityAnalysisModelRequestXPath")]
    [FormKeys(Id = nameof(Id), Parent = nameof(EntityAnalysisModelId), NaturalKey = nameof(Name))]
    [LockField(nameof(Locked))]
    [FormGroup("Identity", Order = 10)]
    [FormGroup("Extraction", Order = 20)]
    [FormGroup("Search Key", Order = 30, Collapsed = true)]
    [FormGroup("Search Key Cache", Order = 40, Collapsed = true)]
    [FormGroup("Behaviour", Order = 50)]
    [FormGroup("Audit", Order = 90, Collapsed = true)]
    public class EntityAnalysisModelRequestXPathDto : IUpdated, IActivatable, ILockable, ITreeChild
    {
        [Description("Identifier of the Model this Request XPath belongs to. Set from the parent Model context; " +
                     "not user-editable.")]
        public int EntityAnalysisModelId { get; set; }

        [Description("Display name of this field. Unique within the Model (case-insensitive).")]
        [FormField(Group = "Identity", Order = 10)]
        [ListColumn(Order = 10, Title = "Name")]
        public string? Name { get; set; }

        [Description("The datatype of the value extracted from the HTTP POST body: String, Integer, Float, Date, " +
                     "Boolean, Latitude or Longitude.")]
        [FormField(Group = "Extraction", Order = 10, Widget = "select")]
        [NewDefault(1)]
        public int DataTypeId { get; set; }

        [Description("JSONPath specifying the location of this field's value in the HTTP POST body.")]
        [FormField(Group = "Extraction", Order = 20)]
        public string? XPath { get; set; }

        [Description("The default value used when the XPath returns a null token. For a Date field this is an " +
                     "integer number of days offset from now; for a Boolean field this is \"1\" or \"0\".")]
        [FormField(Group = "Extraction", Order = 30)]
        public string? DefaultValue { get; set; }

        [Description("Encrypts the extracted value before it is stored and archived. Only meaningful for String " +
                     "fields (Data Type = String).")]
        [FormField(Group = "Extraction", Order = 40, Widget = "radio")]
        [VisibleWhen(nameof(DataTypeId), 1)]
        [NewDefault(0)]
        public int EncryptionId { get; set; }

        [Description("When true, this field's extracted value is available in the Suppression page, allowing the " +
                     "consequences of Rule Activations to be ignored based on a key/value match.")]
        [FormField(Group = "Behaviour", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        public bool EnableSuppression { get; set; }

        [Description("When true, this field's extracted value is serialised to the cache to support Abstraction " +
                     "Rules.")]
        [FormField(Group = "Behaviour", Order = 20, Widget = "switch")]
        [NewDefault(false)]
        public bool Cache { get; set; }

        [Description("When true, this field's extracted value is written to the report table.")]
        [FormField(Group = "Behaviour", Order = 30, Widget = "switch")]
        [NewDefault(false)]
        public bool ReportTable { get; set; }

        [Description("When true, this field's extracted value is included in the response payload.")]
        [FormField(Group = "Behaviour", Order = 40, Widget = "switch")]
        [NewDefault(false)]
        public bool ResponsePayload { get; set; }

        [Description("When true, the value extracted from this field is used to query the cache during " +
                     "Abstraction Rule processing (for example, a count of transactions on the same IP address).")]
        [FormField(Group = "Search Key", Order = 10, Widget = "switch")]
        [NewDefault(false)]
        public bool SearchKey { get; set; }

        [Description("Unit of Search Key TTL Interval Value: Seconds, Minutes, Hours or Days.")]
        [FormField(Group = "Search Key", Order = 20, Widget = "radio")]
        [VisibleWhen(nameof(SearchKey), true)]
        [RequiredWhen(nameof(SearchKey), true)]
        [NewDefault("h")]
        public string? SearchKeyTtlInterval { get; set; }

        [Description("Offset, in Search Key TTL Interval units, before the current reference date at which the " +
                     "search key index entry is eligible for deletion.")]
        [FormField(Group = "Search Key", Order = 30, Widget = "number")]
        [VisibleWhen(nameof(SearchKey), true)]
        [RequiredWhen(nameof(SearchKey), true)]
        [NewDefault(1)]
        public int SearchKeyTtlIntervalValue { get; set; }

        [Description("Maximum number of records returned from the cache for this search key per lookup.")]
        [FormField(Group = "Search Key", Order = 40, Widget = "number")]
        [VisibleWhen(nameof(SearchKey), true)]
        [RequiredWhen(nameof(SearchKey), true)]
        [NewDefault(100)]
        public int SearchKeyFetchLimit { get; set; }

        [Description("When true, calculation of this search key is deferred to a background engine and the " +
                     "result cached, rather than recomputed on every transaction.")]
        [FormField(Group = "Search Key", Order = 50, Widget = "switch")]
        [VisibleWhen(nameof(SearchKey), true)]
        [NewDefault(false)]
        public bool SearchKeyCache { get; set; }

        [Description("Unit of Search Key Cache Interval Value: how frequently the cached search key value is " +
                     "recalculated.")]
        [FormField(Group = "Search Key Cache", Order = 10, Widget = "radio")]
        [VisibleWhen(nameof(SearchKey), true)]
        [VisibleWhen(nameof(SearchKeyCache), true)]
        [RequiredWhen(nameof(SearchKey), true)]
        [RequiredWhen(nameof(SearchKeyCache), true)]
        [NewDefault("h")]
        public string? SearchKeyCacheInterval { get; set; }

        [Description("Length, in Search Key Cache Interval units, between recalculations of the cached search " +
                     "key value.")]
        [FormField(Group = "Search Key Cache", Order = 20, Widget = "number")]
        [VisibleWhen(nameof(SearchKey), true)]
        [VisibleWhen(nameof(SearchKeyCache), true)]
        [RequiredWhen(nameof(SearchKey), true)]
        [RequiredWhen(nameof(SearchKeyCache), true)]
        [NewDefault(1)]
        public int SearchKeyCacheValue { get; set; }

        [Description("When true, only a representative sample of the data for each distinct search key value is " +
                     "used, reducing calculation load.")]
        [FormField(Group = "Search Key Cache", Order = 30, Widget = "switch")]
        [VisibleWhen(nameof(SearchKey), true)]
        [VisibleWhen(nameof(SearchKeyCache), true)]
        [NewDefault(false)]
        public bool SearchKeyCacheSample { get; set; }

        [Description("Maximum number of transactions returned from the cache for each distinct search key value " +
                     "when calculating the cached result.")]
        [FormField(Group = "Search Key Cache", Order = 40, Widget = "number")]
        [VisibleWhen(nameof(SearchKey), true)]
        [VisibleWhen(nameof(SearchKeyCache), true)]
        [NewDefault(100000)]
        public int SearchKeyCacheFetchLimit { get; set; }

        [Description("Unit of Search Key Cache TTL Interval Value: how long a calculated search key value lives " +
                     "before being purged from the cache.")]
        [FormField(Group = "Search Key Cache", Order = 50, Widget = "radio")]
        [VisibleWhen(nameof(SearchKey), true)]
        [VisibleWhen(nameof(SearchKeyCache), true)]
        [RequiredWhen(nameof(SearchKey), true)]
        [RequiredWhen(nameof(SearchKeyCache), true)]
        [NewDefault("h")]
        public string? SearchKeyCacheTtlInterval { get; set; }

        [Description("Length, in Search Key Cache TTL Interval units, that a calculated search key value lives " +
                     "before being purged from the cache.")]
        [FormField(Group = "Search Key Cache", Order = 60, Widget = "number")]
        [VisibleWhen(nameof(SearchKey), true)]
        [VisibleWhen(nameof(SearchKeyCache), true)]
        [NewDefault(1)]
        public int SearchKeyCacheTtlValue { get; set; }

        [Description("Not currently editable in the user interface and not persisted -- carried on the DTO for " +
                     "parity with the legacy payload shape only. Always false on read; any value sent is " +
                     "discarded.")]
        [FormField(Group = "Extraction", Order = 45, Widget = "switch", ReadOnly = true)]
        public bool EncryptOffDeploymentRegion { get; set; }

        [Description("Not currently editable in the user interface and not persisted -- carried on the DTO for " +
                     "parity with the legacy payload shape only. Always false on read; any value sent is " +
                     "discarded.")]
        [FormField(Group = "Extraction", Order = 25, Widget = "switch", ReadOnly = true)]
        public bool XPathExpression { get; set; }

        [Description("Not currently editable in the user interface and not persisted -- carried on the DTO for " +
                     "parity with the legacy payload shape only. Always false on read; any value sent is " +
                     "discarded.")]
        [FormField(Group = "Behaviour", Order = 50, Widget = "switch", ReadOnly = true)]
        public bool HashEntityKeyComposite { get; set; }

        [Description("Not currently editable in the user interface and not persisted -- carried on the DTO for " +
                     "parity with the legacy payload shape only. Always false on read; any value sent is " +
                     "discarded.")]
        [FormField(Group = "Behaviour", Order = 60, Widget = "switch", ReadOnly = true)]
        public bool HashEntryKeyComposite { get; set; }

        [Description("When true, this field participates in extraction on transaction invocation.")]
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

        [Description("User who created this Request XPath. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 20, ReadOnly = true)]
        public string? CreatedUser { get; set; }

        [Description("Timestamp (UTC) this Request XPath was created. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 30, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? CreatedDate { get; set; }

        [Description("User who last updated this Request XPath. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 40, ReadOnly = true)]
        public string? UpdatedUser { get; set; }

        [Description("Timestamp (UTC) this Request XPath was last updated. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 50, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? UpdatedDate { get; set; }

        [Description("Server-assigned optimistic-concurrency version number, incremented on every update. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 60, ReadOnly = true)]
        [ListColumn(Hidden = true)]
        public int Version { get; set; }

        [Description("User who deleted this Request XPath, if soft-deleted. Server-assigned. Read-only.")]
        [FormField(Group = "Audit", Order = 70, ReadOnly = true)]
        public string? DeletedUser { get; set; }

        [Description("Timestamp (UTC) this Request XPath was soft-deleted, if applicable. Server-assigned. " +
                     "Read-only.")]
        [FormField(Group = "Audit", Order = 80, ReadOnly = true, Widget = "date")]
        public DateTimeOffset? DeletedDate { get; set; }
    }
}