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

namespace Jube.Data.Poco
{
    using System;
    using System.Collections.Generic;
    using LinqToDB;
    using LinqToDB.Mapping;
    using MessagePack;

    [Table]
    [MessagePackObject]
    public class ActivationWatcher
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Key(1)] public int? TenantRegistryId { get; set; }
        [Column] [Nullable] [Key(2)] public string Key { get; set; }
        [Column] [Nullable] [Key(3)] public string KeyValue { get; set; }
        [Column] [Nullable] [Key(4)] public double? Longitude { get; set; }
        [Column] [Nullable] [Key(5)] public double? Latitude { get; set; }
        [Column] [Nullable] [Key(6)] public string ActivationRuleSummary { get; set; }
        [Column] [Nullable] [Key(7)] public string ResponseElevationContent { get; set; }
        [Column] [Nullable] [Key(8)] public double? ResponseElevation { get; set; }
        [Column] [Nullable] [Key(9)] public string BackColor { get; set; }
        [Column] [Nullable] [Key(10)] public string ForeColor { get; set; }
        [Column] [Nullable] [Key(11)] public DateTime? CreatedDate { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class ArchiveEntityAnalysisModelAbstractionEntry
    {
        [Column] [Identity] [Key(0)] public long Id { get; set; }
        [Column] [Nullable] [Key(1)] public string SearchKey { get; set; }
        [Column] [Nullable] [Key(2)] public string SearchValue { get; set; }
        [Column] [Nullable] [Key(3)] public double? Value { get; set; }

        [Column]
        [Nullable]
        [Key(4)]
        public long? EntityAnalysisModelSearchKeyDistinctValueCalculationInstanceId { get; set; }

        [Column] [Nullable] [Key(5)] public int? EntityAnalysisModelAbstractionRuleId { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? CreatedDate { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class LocalCacheInstance
    {
        [Column] [Identity] [Key(0)] public long Id { get; set; }
        [Column] [Nullable] [Key(1)] public string Instance { get; set; }
        [Column] [Nullable] [Key(2)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(3)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Fill { get; set; }
        [Column] [Nullable] [Key(5)] public DateTime? FillStartedDate { get; set; }
        [Column] [Nullable] [Key(6)] public long? FillCount { get; set; }
        [Column] [Nullable] [Key(7)] public long? FillBytes { get; set; }
        [Column] [Nullable] [Key(8)] public byte? Filled { get; set; }
        [Column] [Nullable] [Key(9)] public DateTime? FillEndedDate { get; set; }
        [Column] [Nullable] [Key(10)] public long? Bytes { get; set; }
        [Column] [Nullable] [Key(11)] public long? Count { get; set; }
        [Column] [Nullable] [Key(12)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(13)] public long? HeapSizeBytes { get; set; }
        [Column] [Nullable] [Key(14)] public long? TotalCommittedBytes { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class LocalCacheInstanceKey
    {
        [Column] [Identity] [Key(0)] public long Id { get; set; }
        [Column] [Nullable] [Key(1)] public long? LocalCacheInstanceId { get; set; }
        [Column] [Nullable] [Key(2)] public string Key { get; set; }
        [Column] [Nullable] [Key(3)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(4)] public int? Requests { get; set; }
        [Column] [Nullable] [Key(5)] public int? Misses { get; set; }
        [Column] [Nullable] [Key(6)] public long? MissRemoteResponseTime { get; set; }
        [Column] [Nullable] [Key(7)] public long? UnpackResponseTime { get; set; }
        [Column] [Nullable] [Key(8)] public int? HashSetSubscriptions { get; set; }
        [Column] [Nullable] [Key(9)] public int? HashRemove { get; set; }
        [Column] [Nullable] [Key(10)] public int? HashRemoveMiss { get; set; }
        [Column] [Nullable] [Key(11)] public int? HashRemoveSubscription { get; set; }
        [Column] [Nullable] [Key(12)] public int? HashRemoveSubscriptionMiss { get; set; }
        [Column] [Nullable] [Key(13)] public int? DualMiss { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class LocalCacheInstanceLru
    {
        [Column] [Identity] [Key(0)] public long Id { get; set; }
        [Column] [Nullable] [Key(1)] public long? LocalCacheInstanceId { get; set; }
        [Column] [Nullable] [Key(2)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(3)] public long? Bytes { get; set; }
        [Column] [Nullable] [Key(4)] public long? Count { get; set; }
        [Column] [Nullable] [Key(5)] public long? RequestBytes { get; set; }
        [Column] [Nullable] [Key(6)] public long? RequestCount { get; set; }
        [Column] [Nullable] [Key(7)] public long? AddBytes { get; set; }
        [Column] [Nullable] [Key(8)] public long? AddCount { get; set; }
        [Column] [Nullable] [Key(9)] public long? RemoveBytes { get; set; }
        [Column] [Nullable] [Key(10)] public long? RemoveCount { get; set; }
        [Column] [Nullable] [Key(11)] public long? EvictionBytes { get; set; }
        [Column] [Nullable] [Key(12)] public long? EvictionCount { get; set; }
        [Column] [Nullable] [Key(13)] public long? UpdateBytes { get; set; }
        [Column] [Nullable] [Key(14)] public long? UpdateCount { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class ArchiveKey
    {
        [Column] [Identity] [Key(0)] public long Id { get; set; }
        [Column] [Nullable] [Key(1)] public byte? ProcessingTypeId { get; set; }
        [Column] [Nullable] [Key(2)] public string Key { get; set; }
        [Column] [Nullable] [Key(3)] public string KeyValueString { get; set; }
        [Column] [Nullable] [Key(4)] public int? KeyValueInteger { get; set; }
        [Column] [Nullable] [Key(5)] public double? KeyValueFloat { get; set; }
        [Column] [Nullable] [Key(6)] public byte? KeyValueBoolean { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? KeyValueDate { get; set; }
        [Column] [Nullable] [Key(8)] public Guid EntityAnalysisModelInstanceEntryGuid { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class Case
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public Guid EntityAnalysisModelInstanceEntryGuid { get; set; }
        [Column] [Nullable] [Key(2)] public DateTime? DiaryDate { get; set; }
        [Column] [Nullable] [Key(3)] public Guid CaseWorkflowGuid { get; set; }
        [Column] [Nullable] [Key(4)] public Guid CaseWorkflowStatusGuid { get; set; }
        [Column] [Nullable] [Key(5)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(6)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(7)] public string LockedUser { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? LockedDate { get; set; }
        [Column] [Nullable] [Key(9)] public byte? ClosedStatusId { get; set; }
        [Column] [Nullable] [Key(10)] public DateTime? ClosedDate { get; set; }
        [Column] [Nullable] [Key(11)] public string ClosedUser { get; set; }
        [Column] [Nullable] [Key(12)] public string CaseKey { get; set; }
        [Column] [Nullable] [Key(13)] public byte? Diary { get; set; }
        [Column] [Nullable] [Key(14)] public string DiaryUser { get; set; }
        [Column] [Nullable] [Key(15)] public byte? Rating { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(16)]
        public string Json { get; set; }

        [Column] [Nullable] [Key(17)] public string CaseKeyValue { get; set; }
        [Column] [Nullable] [Key(18)] public byte? LastClosedStatus { get; set; }
        [Column] [Nullable] [Key(19)] public DateTime? ClosedStatusMigrationDate { get; set; }

        [Association(ThisKey = "CaseWorkflowGuid", OtherKey = "Guid", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(20)]
        public CaseWorkflow CaseWorkflow { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class SessionCaseJournal
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(1)]
        public string Json { get; set; }

        [Column] [Nullable] [Key(2)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(3)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(4)] public int? CaseWorkflowId { get; set; }

        [Association(ThisKey = "CaseWorkflowId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(5)]
        public CaseWorkflow CaseWorkflow { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class CaseEvent
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? CaseEventTypeId { get; set; }
        [Column] [Nullable] [Key(2)] public string Before { get; set; }
        [Column] [Nullable] [Key(3)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(4)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(5)] public string CaseKey { get; set; }
        [Column] [Nullable] [Key(6)] public int? CaseId { get; set; }
        [Column] [Nullable] [Key(7)] public string After { get; set; }
        [Column] [Nullable] [Key(8)] public string CaseKeyValue { get; set; }

        [Association(ThisKey = "CaseId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(9)]
        public Case Case { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class CaseFile
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public byte[] Object { get; set; }
        [Column] [Nullable] [Key(2)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(3)] public string Name { get; set; }
        [Column] [Nullable] [Key(4)] public string ContentType { get; set; }
        [Column] [Nullable] [Key(5)] public string Extension { get; set; }
        [Column] [Nullable] [Key(6)] public long Size { get; set; }
        [Column] [Nullable] [Key(7)] public string CaseKey { get; set; }
        [Column] [Nullable] [Key(8)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(9)] public string CaseKeyValue { get; set; }
        [Column] [Nullable] [Key(10)] public int? CaseId { get; set; }
        [Column] [Nullable] [Key(11)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(12)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(13)] public byte? Deleted { get; set; }

        [Association(ThisKey = "CaseId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(14)]
        public Case Case { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class CaseNote
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public string Note { get; set; }
        [Column] [Nullable] [Key(2)] public int? ActionId { get; set; }
        [Column] [Nullable] [Key(3)] public int? PriorityId { get; set; }
        [Column] [Nullable] [Key(4)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(5)] public string CaseKey { get; set; }
        [Column] [Nullable] [Key(6)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(7)] public string CaseKeyValue { get; set; }
        [Column] [Nullable] [Key(8)] public int? CaseId { get; set; }

        [Association(ThisKey = "CaseId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(9)]
        public Case Case { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class SessionCaseSearchCompiledSql
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public Guid Guid { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(2)]
        public string FilterJson { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(3)]
        public string FilterTokens { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(4)]
        public string SelectJson { get; set; }

        [Column] [Nullable] [Key(5)] public string FilterSql { get; set; }
        [Column] [Nullable] [Key(6)] public string SelectSqlSearch { get; set; }
        [Column] [Nullable] [Key(7)] public string SelectSqlDisplay { get; set; }
        [Column] [Nullable] [Key(8)] public string WhereSql { get; set; }
        [Column] [Nullable] [Key(9)] public string OrderSql { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Prepared { get; set; }
        [Column] [Nullable] [Key(11)] public string Error { get; set; }
        [Column] [Nullable] [Key(12)] public Guid CaseWorkflowGuid { get; set; }
        [Column] [Nullable] [Key(13)] public Guid CaseWorkflowFilterGuid { get; set; }
        [Column] [Nullable] [Key(14)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(15)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(16)] public byte? Rebuild { get; set; }
        [Column] [Nullable] [Key(17)] public DateTime? RebuildDate { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class SessionCaseSearchCompiledSqlExecution
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int SessionCaseSearchCompiledSqlId { get; set; }
        [Column] [Nullable] [Key(2)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(3)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(4)] public int ResponseTime { get; set; }
        [Column] [Nullable] [Key(5)] public int Records { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class CaseWorkflow
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public string Name { get; set; }
        [Column] [Nullable] [Key(2)] public int? TenantRegistryId { get; set; }
        [Column] [Nullable] [Key(3)] public int? CaseStatusId { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(7)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(9)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(11)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(12)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(13)] public Guid VisualisationRegistryGuid { get; set; }
        [Column] [Nullable] [Key(14)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(15)] public byte? EnableVisualisation { get; set; }
        [Column] [Nullable] [Key(16)] public int? Version { get; set; }

        [Association(ThisKey = "Id", OtherKey = "CaseWorkflowId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(17)]
        public IEnumerable<CaseWorkflowAction> CaseWorkflowAction { get; set; }

        [Association(ThisKey = "Id", OtherKey = "CaseWorkflowId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(18)]
        public IEnumerable<CaseWorkflowDisplay> CaseWorkflowDisplay { get; set; }

        [Association(ThisKey = "Id", OtherKey = "CaseWorkflowId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(19)]
        public IEnumerable<CaseWorkflowFilter> CaseWorkflowFilter { get; set; }

        [Association(ThisKey = "Id", OtherKey = "CaseWorkflowId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(20)]
        public IEnumerable<CaseWorkflowForm> CaseWorkflowForm { get; set; }

        [Association(ThisKey = "Id", OtherKey = "CaseWorkflowId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(21)]
        public IEnumerable<CaseWorkflowMacro> CaseWorkflowMacro { get; set; }

        [Association(ThisKey = "Id", OtherKey = "CaseWorkflowId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(22)]
        public IEnumerable<CaseWorkflowStatus> CaseWorkflowStatus { get; set; }

        [Association(ThisKey = "Id", OtherKey = "CaseWorkflowId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(23)]
        public IEnumerable<CaseWorkflowXPath> CaseWorkflowXPath { get; set; }

        [Association(ThisKey = "TenantRegistryId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(24)]
        public TenantRegistry TenantRegistry { get; set; }

        [Association(ThisKey = "EntityAnalysisModelId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(25)]
        public EntityAnalysisModel EntityAnalysisModel { get; set; }

        [Column] [Nullable] [Key(26)] public Guid Guid { get; set; }

        [Column] [Nullable] [Key(27)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class CaseWorkflowVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public string Name { get; set; }
        [Column] [Nullable] [Key(2)] public int? CaseWorkflowId { get; set; }
        [Column] [Nullable] [Key(3)] public int? TenantRegistryId { get; set; }
        [Column] [Nullable] [Key(4)] public int? CaseStatusId { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(6)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(8)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(9)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(10)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(11)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(12)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(13)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(14)] public int? VisualisationRegistryId { get; set; }
        [Column] [Nullable] [Key(15)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(16)] public byte? EnableVisualisation { get; set; }
        [Column] [Nullable] [Key(17)] public int? Version { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class CaseWorkflowActionVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? CaseWorkflowActionId { get; set; }
        [Column] [Nullable] [Key(2)] public int? CaseWorkflowId { get; set; }
        [Column] [Nullable] [Key(3)] public string Name { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(7)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(9)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(11)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(12)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(13)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(14)] public int? Version { get; set; }
        [Column] [Nullable] [Key(15)] public byte? EnableHttpEndpoint { get; set; }
        [Column] [Nullable] [Key(16)] public string HttpEndpoint { get; set; }
        [Column] [Nullable] [Key(17)] public byte? HttpEndpointTypeId { get; set; }
        [Column] [Nullable] [Key(18)] public byte? EnableNotification { get; set; }
        [Column] [Nullable] [Key(19)] public byte? NotificationTypeId { get; set; }
        [Column] [Nullable] [Key(20)] public string NotificationDestination { get; set; }
        [Column] [Nullable] [Key(21)] public string NotificationSubject { get; set; }
        [Column] [Nullable] [Key(22)] public string NotificationBody { get; set; }
        [Column] [Nullable] [Key(23)] public Guid Guid { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class CaseWorkflowAction
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column("CaseWorkflowId")]
        [Nullable]
        [Key(1)]
        public int? CaseWorkflowId { get; set; }

        [Column] [Nullable] [Key(2)] public string Name { get; set; }
        [Column] [Nullable] [Key(3)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(5)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(6)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(8)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(9)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(10)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(11)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(12)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(13)] public int? Version { get; set; }
        [Column] [Nullable] [Key(14)] public byte? EnableHttpEndpoint { get; set; }
        [Column] [Nullable] [Key(15)] public string HttpEndpoint { get; set; }
        [Column] [Nullable] [Key(16)] public byte? HttpEndpointTypeId { get; set; }
        [Column] [Nullable] [Key(17)] public byte? EnableNotification { get; set; }
        [Column] [Nullable] [Key(18)] public byte? NotificationTypeId { get; set; }
        [Column] [Nullable] [Key(19)] public string NotificationDestination { get; set; }
        [Column] [Nullable] [Key(20)] public string NotificationSubject { get; set; }
        [Column] [Nullable] [Key(21)] public string NotificationBody { get; set; }

        [Association(ThisKey = "CaseWorkflowId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(22)]
        public CaseWorkflow CaseWorkflow { get; set; }

        [Column] [Nullable] [Key(23)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(24)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class CaseWorkflowDisplayVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? CaseWorkflowDisplayId { get; set; }
        [Column] [Nullable] [Key(2)] public int? CaseWorkflowId { get; set; }
        [Column] [Nullable] [Key(3)] public string Name { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(7)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(9)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(11)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(12)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(13)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(14)] public int? Version { get; set; }
        [Column] [Nullable] [Key(15)] public string Html { get; set; }
        [Column] [Nullable] [Key(16)] public Guid Guid { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class CaseWorkflowDisplay
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? CaseWorkflowId { get; set; }
        [Column] [Nullable] [Key(2)] public string Name { get; set; }
        [Column] [Nullable] [Key(3)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(5)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(6)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(8)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(9)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(10)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(11)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(12)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(13)] public int? Version { get; set; }
        [Column] [Nullable] [Key(14)] public string Html { get; set; }

        [Association(ThisKey = "CaseWorkflowId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(15)]
        public CaseWorkflow CaseWorkflow { get; set; }

        [Column] [Nullable] [Key(16)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(17)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class CaseWorkflowFilterVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? CaseWorkflowFilterId { get; set; }
        [Column] [Nullable] [Key(2)] public int? CaseWorkflowId { get; set; }
        [Column] [Nullable] [Key(3)] public string Name { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(4)]
        public string FilterJson { get; set; }

        [Column] [Nullable] [Key(5)] public string FilterSql { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(6)]
        public string SelectJson { get; set; }

        [Column] [Nullable] [Key(7)] public string FilterTokens { get; set; }
        [Column] [Nullable] [Key(8)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(9)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(10)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(11)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(12)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(13)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(14)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(15)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(16)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(17)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(18)] public int? Version { get; set; }
        [Column] [Nullable] [Key(19)] public int? VisualisationRegistryId { get; set; }
        [Column] [Nullable] [Key(20)] public Guid Guid { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class CaseWorkflowFilter
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? CaseWorkflowId { get; set; }
        [Column] [Nullable] [Key(2)] public string Name { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(3)]
        public string FilterJson { get; set; }

        [Column] [Nullable] [Key(4)] public string FilterSql { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(5)]
        public string SelectJson { get; set; }

        [Column] [Nullable] [Key(6)] public string FilterTokens { get; set; }
        [Column] [Nullable] [Key(7)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(8)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(9)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(10)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(11)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(12)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(13)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(14)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(15)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(16)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(17)] public int? Version { get; set; }
        [Column] [Nullable] [Key(18)] public int? VisualisationRegistryId { get; set; }

        [Association(ThisKey = "CaseWorkflowId", OtherKey = "Id", CanBeNull = true)]
        [Key(19)]
        public CaseWorkflow CaseWorkflow { get; set; }

        [Column] [Nullable] [Key(20)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(21)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class CaseWorkflowFormVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? CaseWorkflowFormId { get; set; }
        [Column] [Nullable] [Key(2)] public int? CaseWorkflowId { get; set; }
        [Column] [Nullable] [Key(3)] public string Name { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(7)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(9)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(11)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(12)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(13)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(14)] public int? Version { get; set; }
        [Column] [Nullable] [Key(15)] public string Html { get; set; }
        [Column] [Nullable] [Key(16)] public byte? EnableHttpEndpoint { get; set; }
        [Column] [Nullable] [Key(17)] public string HttpEndpoint { get; set; }
        [Column] [Nullable] [Key(18)] public byte? HttpEndpointTypeId { get; set; }
        [Column] [Nullable] [Key(19)] public byte? EnableNotification { get; set; }
        [Column] [Nullable] [Key(20)] public byte? NotificationTypeId { get; set; }
        [Column] [Nullable] [Key(21)] public string NotificationDestination { get; set; }
        [Column] [Nullable] [Key(22)] public string NotificationSubject { get; set; }
        [Column] [Nullable] [Key(23)] public string NotificationBody { get; set; }
        [Column] [Nullable] [Key(24)] public Guid Guid { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class CaseWorkflowForm
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? CaseWorkflowId { get; set; }
        [Column] [Nullable] [Key(2)] public string Name { get; set; }
        [Column] [Nullable] [Key(3)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(5)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(6)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(8)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(9)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(10)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(11)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(12)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(13)] public int? Version { get; set; }
        [Column] [Nullable] [Key(14)] public string Html { get; set; }
        [Column] [Nullable] [Key(15)] public byte? EnableHttpEndpoint { get; set; }
        [Column] [Nullable] [Key(16)] public string HttpEndpoint { get; set; }
        [Column] [Nullable] [Key(17)] public byte? HttpEndpointTypeId { get; set; }
        [Column] [Nullable] [Key(18)] public byte? EnableNotification { get; set; }
        [Column] [Nullable] [Key(19)] public byte? NotificationTypeId { get; set; }
        [Column] [Nullable] [Key(20)] public string NotificationDestination { get; set; }
        [Column] [Nullable] [Key(21)] public string NotificationSubject { get; set; }
        [Column] [Nullable] [Key(22)] public string NotificationBody { get; set; }

        [Association(ThisKey = "CaseWorkflowId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(23)]
        public CaseWorkflow CaseWorkflow { get; set; }

        [Column] [Nullable] [Key(24)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(25)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class CaseWorkflowFormEntryValue
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public string Name { get; set; }
        [Column] [Nullable] [Key(2)] public string Value { get; set; }
        [Column] [Nullable] [Key(3)] public int? CaseWorkflowFormEntryId { get; set; }

        [Association(ThisKey = "CaseWorkflowFormEntryId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(4)]
        public CaseWorkflowFormEntry CaseWorkflowsFormsEntry { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class CaseWorkflowFormEntry
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public string Payload { get; set; }
        [Column] [Nullable] [Key(2)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(3)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(4)] public string CaseKey { get; set; }
        [Column] [Nullable] [Key(5)] public int? CaseId { get; set; }
        [Column] [Nullable] [Key(6)] public int? CaseWorkflowFormId { get; set; }
        [Column] [Nullable] [Key(7)] public string CaseKeyValue { get; set; }

        [Association(ThisKey = "CaseId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(8)]
        public Case Case { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class CaseWorkflowMacroVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? CaseWorkflowMacroId { get; set; }
        [Column] [Nullable] [Key(2)] public int? CaseWorkflowId { get; set; }
        [Column] [Nullable] [Key(3)] public string Name { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(7)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(9)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(11)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(12)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(13)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(14)] public int? Version { get; set; }
        [Column] [Nullable] [Key(15)] public string Javascript { get; set; }
        [Column] [Nullable] [Key(16)] public string ImageLocation { get; set; }
        [Column] [Nullable] [Key(17)] public byte? EnableHttpEndpoint { get; set; }
        [Column] [Nullable] [Key(18)] public string HttpEndpoint { get; set; }
        [Column] [Nullable] [Key(19)] public byte? HttpEndpointTypeId { get; set; }
        [Column] [Nullable] [Key(20)] public byte? EnableNotification { get; set; }
        [Column] [Nullable] [Key(21)] public byte? NotificationTypeId { get; set; }
        [Column] [Nullable] [Key(22)] public string NotificationDestination { get; set; }
        [Column] [Nullable] [Key(23)] public string NotificationSubject { get; set; }
        [Column] [Nullable] [Key(24)] public string NotificationBody { get; set; }
        [Column] [Nullable] [Key(25)] public Guid Guid { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class CaseWorkflowMacro
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? CaseWorkflowId { get; set; }
        [Column] [Nullable] [Key(2)] public string Name { get; set; }
        [Column] [Nullable] [Key(3)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(5)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(6)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(8)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(9)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(10)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(11)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(12)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(13)] public int? Version { get; set; }
        [Column] [Nullable] [Key(14)] public string Javascript { get; set; }
        [Column] [Nullable] [Key(15)] public string ImageLocation { get; set; }
        [Column] [Nullable] [Key(16)] public byte? EnableHttpEndpoint { get; set; }
        [Column] [Nullable] [Key(17)] public string HttpEndpoint { get; set; }
        [Column] [Nullable] [Key(18)] public byte? HttpEndpointTypeId { get; set; }
        [Column] [Nullable] [Key(19)] public byte? EnableNotification { get; set; }
        [Column] [Nullable] [Key(20)] public byte? NotificationTypeId { get; set; }
        [Column] [Nullable] [Key(21)] public string NotificationDestination { get; set; }
        [Column] [Nullable] [Key(22)] public string NotificationSubject { get; set; }
        [Column] [Nullable] [Key(23)] public string NotificationBody { get; set; }

        [Association(ThisKey = "CaseWorkflowId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(24)]
        public CaseWorkflow CaseWorkflow { get; set; }

        [Column] [Nullable] [Key(25)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(26)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class CaseWorkflowStatus
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? CaseWorkflowId { get; set; }
        [Column] [Nullable] [Key(2)] public string Name { get; set; }
        [Column] [Nullable] [Key(3)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(5)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(6)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(8)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(9)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(10)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(11)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(12)] public int? Version { get; set; }
        [Column] [Nullable] [Key(13)] public string ForeColor { get; set; }
        [Column] [Nullable] [Key(14)] public string BackColor { get; set; }
        [Column] [Nullable] [Key(15)] public byte? EnableHttpEndpoint { get; set; }
        [Column] [Nullable] [Key(16)] public string HttpEndpoint { get; set; }
        [Column] [Nullable] [Key(17)] public byte? HttpEndpointTypeId { get; set; }
        [Column] [Nullable] [Key(18)] public byte? EnableNotification { get; set; }
        [Column] [Nullable] [Key(19)] public byte? NotificationTypeId { get; set; }
        [Column] [Nullable] [Key(20)] public string NotificationDestination { get; set; }
        [Column] [Nullable] [Key(21)] public string NotificationSubject { get; set; }
        [Column] [Nullable] [Key(22)] public string NotificationBody { get; set; }
        [Column] [Nullable] [Key(23)] public byte? Priority { get; set; }

        [Association(ThisKey = "CaseWorkflowId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(24)]
        public CaseWorkflow CaseWorkflow { get; set; }

        [Column] [Nullable] [Key(25)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(26)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class CaseWorkflowStatusVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? CaseWorkflowStatusId { get; set; }
        [Column] [Nullable] [Key(2)] public int? CaseWorkflowId { get; set; }
        [Column] [Nullable] [Key(3)] public string Name { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(7)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(9)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(11)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(12)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(13)] public int? Version { get; set; }
        [Column] [Nullable] [Key(14)] public string ForeColor { get; set; }
        [Column] [Nullable] [Key(15)] public string BackColor { get; set; }
        [Column] [Nullable] [Key(16)] public byte? EnableHttpEndpoint { get; set; }
        [Column] [Nullable] [Key(17)] public string HttpEndpoint { get; set; }
        [Column] [Nullable] [Key(18)] public byte? HttpEndpointTypeId { get; set; }
        [Column] [Nullable] [Key(19)] public byte? EnableNotification { get; set; }
        [Column] [Nullable] [Key(20)] public byte? NotificationTypeId { get; set; }
        [Column] [Nullable] [Key(21)] public string NotificationDestination { get; set; }
        [Column] [Nullable] [Key(22)] public string NotificationSubject { get; set; }
        [Column] [Nullable] [Key(23)] public string NotificationBody { get; set; }
        [Column] [Nullable] [Key(24)] public byte? Priority { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class CaseWorkflowXPathVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? CaseWorkflowXPathId { get; set; }
        [Column] [Nullable] [Key(2)] public int? CaseWorkflowId { get; set; }
        [Column] [Nullable] [Key(3)] public string Name { get; set; }
        [Column] [Nullable] [Key(4)] public string XPath { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(6)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(8)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(9)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(10)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(11)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(12)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(13)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(14)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(15)] public int? Version { get; set; }
        [Column] [Nullable] [Key(16)] public byte? ConditionalRegularExpressionFormatting { get; set; }
        [Column] [Nullable] [Key(17)] public string ConditionalFormatForeColor { get; set; }
        [Column] [Nullable] [Key(18)] public string ConditionalFormatBackColor { get; set; }
        [Column] [Nullable] [Key(19)] public string RegularExpression { get; set; }
        [Column] [Nullable] [Key(20)] public byte? ForeRowColorScope { get; set; }
        [Column] [Nullable] [Key(21)] public byte? BackRowColorScope { get; set; }
        [Column] [Nullable] [Key(22)] public byte? Drill { get; set; }
        [Column] [Nullable] [Key(23)] public string BoldLineFormatForeColor { get; set; }
        [Column] [Nullable] [Key(24)] public string BoldLineFormatBackColor { get; set; }
        [Column] [Nullable] [Key(25)] public byte? BoldLineMatched { get; set; }
        [Column] [Nullable] [Key(26)] public Guid Guid { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class CaseWorkflowXPath
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? CaseWorkflowId { get; set; }
        [Column] [Nullable] [Key(2)] public string Name { get; set; }
        [Column] [Nullable] [Key(3)] public string XPath { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(7)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(9)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(11)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(12)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(13)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(14)] public int? Version { get; set; }
        [Column] [Nullable] [Key(15)] public byte? ConditionalRegularExpressionFormatting { get; set; }
        [Column] [Nullable] [Key(16)] public string ConditionalFormatForeColor { get; set; }
        [Column] [Nullable] [Key(17)] public string ConditionalFormatBackColor { get; set; }
        [Column] [Nullable] [Key(18)] public string RegularExpression { get; set; }
        [Column] [Nullable] [Key(19)] public byte? ForeRowColorScope { get; set; }
        [Column] [Nullable] [Key(20)] public byte? BackRowColorScope { get; set; }
        [Column] [Nullable] [Key(21)] public byte? Drill { get; set; }
        [Column] [Nullable] [Key(22)] public string BoldLineFormatForeColor { get; set; }
        [Column] [Nullable] [Key(23)] public string BoldLineFormatBackColor { get; set; }
        [Column] [Nullable] [Key(24)] public byte? BoldLineMatched { get; set; }

        [Association(ThisKey = "CaseWorkflowId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(25)]
        public CaseWorkflow CaseWorkflow { get; set; }

        [Column] [Nullable] [Key(26)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(27)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class Country
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public string Name { get; set; }
        [Column] [Nullable] [Key(2)] public string Alpha2 { get; set; }
        [Column] [Nullable] [Key(3)] public string Alpha3 { get; set; }
        [Column] [Nullable] [Key(4)] public int? Numeric { get; set; }
        [Column] [Nullable] [Key(5)] public double? Latitude { get; set; }
        [Column] [Nullable] [Key(6)] public double? Longitude { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelListCsvFileUpload
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelListId { get; set; }
        [Column] [Nullable] [Key(2)] public string FileName { get; set; }
        [Column] [Nullable] [Key(3)] public DateTime? CreatedDate { get; set; }

        [Column("CreatedUser")]
        [Nullable]
        [Key(4)]
        public string CreatedUser { get; set; }

        [Column] [Nullable] [Key(5)] public int Records { get; set; }
        [Column] [Nullable] [Key(6)] public int Errors { get; set; }
        [Column] [Nullable] [Key(7)] public long Length { get; set; }
        [Column] [Nullable] [Key(8)] public int Version { get; set; }
        [Column] [Nullable] [Key(9)] public int InheritedId { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(11)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(12)] public DateTime? DeletedDate { get; set; }

        [Association(ThisKey = "EntityAnalysisModelListId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(13)]
        public EntityAnalysisModelList EntityAnalysisModelList { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelDictionaryCsvFileUpload
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelDictionaryId { get; set; }
        [Column] [Nullable] [Key(2)] public string FileName { get; set; }
        [Column] [Nullable] [Key(3)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(4)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(5)] public int Records { get; set; }
        [Column] [Nullable] [Key(6)] public int Errors { get; set; }
        [Column] [Nullable] [Key(7)] public long Length { get; set; }
        [Column] [Nullable] [Key(8)] public int Version { get; set; }
        [Column] [Nullable] [Key(9)] public int InheritedId { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(11)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(12)] public DateTime? DeletedDate { get; set; }

        [Association(ThisKey = "EntityAnalysisModelDictionaryId", OtherKey = "Id",
            CanBeNull = true, Relationship = Relationship.ManyToOne)]
        [Key(13)]
        public EntityAnalysisModelDictionary EntityAnalysisModelDictionary { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class Currency
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public string Name { get; set; }
        [Column] [Nullable] [Key(2)] public string Symbol { get; set; }
        [Column] [Nullable] [Key(3)] public double? ExchangeRateToBaseCurrency { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisInlineScript
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public string Code { get; set; }
        [Column] [Nullable] [Key(2)] public string Dependency { get; set; }
        [Column] [Nullable] [Key(3)] public string ClassName { get; set; }
        [Column] [Nullable] [Key(4)] public string MethodName { get; set; }
        [Column] [Nullable] [Key(5)] public string Name { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? CreatedDate { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisInstance
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(2)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(3)] public string Instance { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(2)] public string Name { get; set; }
        [Column] [Nullable] [Key(3)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(4)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(5)] public string EntryXPath { get; set; }
        [Column] [Nullable] [Key(6)] public string ReferenceDateXPath { get; set; }
        [Column] [Nullable] [Key(7)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(9)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(10)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(11)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(12)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(13)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(14)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(15)] public string EntryName { get; set; }
        [Column] [Nullable] [Key(16)] public string ReferenceDateName { get; set; }
        [Column] [Nullable] [Key(17)] public int? TenantRegistryId { get; set; }
        [Column] [Nullable] [Key(18)] public int? CacheFetchLimit { get; set; }
        [Column] [Nullable] [Key(19)] public byte? ReferenceDatePayloadLocationTypeId { get; set; }
        [Column] [Nullable] [Key(20)] public double? MaxResponseElevation { get; set; }
        [Column] [Nullable] [Key(21)] public char? MaxResponseElevationInterval { get; set; }
        [Column] [Nullable] [Key(22)] public int? MaxResponseElevationValue { get; set; }
        [Column] [Nullable] [Key(23)] public int? MaxResponseElevationThreshold { get; set; }
        [Column] [Nullable] [Key(24)] public char? MaxActivationWatcherInterval { get; set; }
        [Column] [Nullable] [Key(25)] public int? MaxActivationWatcherValue { get; set; }
        [Column] [Nullable] [Key(26)] public int? MaxActivationWatcherThreshold { get; set; }
        [Column] [Nullable] [Key(27)] public double? ActivationWatcherSample { get; set; }
        [Column] [Nullable] [Key(28)] public byte? EnableCache { get; set; }
        [Column] [Nullable] [Key(29)] public byte? EnableTtlCounter { get; set; }
        [Column] [Nullable] [Key(30)] public byte? EnableSanctionCache { get; set; }
        [Column] [Nullable] [Key(31)] public byte? EnableRdbmsArchive { get; set; }
        [Column] [Nullable] [Key(32)] public int? Version { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModel
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public string Name { get; set; }
        [Column] [Nullable] [Key(2)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(3)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(4)] public string EntryXPath { get; set; }
        [Column] [Nullable] [Key(5)] public string ReferenceDateXPath { get; set; }
        [Column] [Nullable] [Key(6)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(8)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(9)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(10)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(11)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(12)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(13)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(14)] public string EntryName { get; set; }
        [Column] [Nullable] [Key(15)] public string ReferenceDateName { get; set; }
        [Column] [Nullable] [Key(16)] public int? TenantRegistryId { get; set; }
        [Column] [Nullable] [Key(17)] public int? CacheFetchLimit { get; set; }
        [Column] [Nullable] [Key(18)] public char? CacheTtlInterval { get; set; }
        [Column] [Nullable] [Key(19)] public int? CacheTtlIntervalValue { get; set; }
        [Column] [Nullable] [Key(20)] public byte? ReferenceDatePayloadLocationTypeId { get; set; }
        [Column] [Nullable] [Key(21)] public double? MaxResponseElevation { get; set; }
        [Column] [Nullable] [Key(22)] public char? MaxResponseElevationInterval { get; set; }
        [Column] [Nullable] [Key(23)] public int? MaxResponseElevationValue { get; set; }
        [Column] [Nullable] [Key(24)] public int? MaxResponseElevationThreshold { get; set; }
        [Column] [Nullable] [Key(25)] public char? MaxActivationWatcherInterval { get; set; }
        [Column] [Nullable] [Key(26)] public int? MaxActivationWatcherValue { get; set; }
        [Column] [Nullable] [Key(27)] public int? MaxActivationWatcherThreshold { get; set; }
        [Column] [Nullable] [Key(28)] public double? ActivationWatcherSample { get; set; }
        [Column] [Nullable] [Key(29)] public byte? EnableActivationArchive { get; set; }
        [Column] [Nullable] [Key(30)] public byte? EnableCache { get; set; }
        [Column] [Nullable] [Key(31)] public byte? EnableTtlCounter { get; set; }
        [Column] [Nullable] [Key(32)] public byte? EnableSanctionCache { get; set; }
        [Column] [Nullable] [Key(33)] public byte? EnableRdbmsArchive { get; set; }
        [Column] [Nullable] [Key(34)] public byte? EnableActivationWatcher { get; set; }
        [Column] [Nullable] [Key(35)] public byte? EnableResponseElevationLimit { get; set; }
        [Column] [Nullable] [Key(36)] public int? Version { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(37)]
        public IEnumerable<EntityAnalysisModelVersion> EntityAnalysisModelVersion { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(38)]
        public IEnumerable<EntityAnalysisModelAbstractionCalculation>
            EntityAnalysisModelAbstractionCalculation { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(39)]
        public IEnumerable<EntityAnalysisModelAbstractionRule> EntityAnalysisModelAbstractionRule { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(40)]
        public IEnumerable<EntityAnalysisModelActivationRule> EntityAnalysisModelActivationRule { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(41)]
        public IEnumerable<EntityAnalysisModelHttpAdaptation> EntityAnalysisModelHttpAdaptation { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(42)]
        public IEnumerable<EntityAnalysisModelDictionary> EntityAnalysisModelDictionary { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(43)]
        public IEnumerable<EntityAnalysisModelGatewayRule> EntityAnalysisModelGatewayRule { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(44)]
        public IEnumerable<EntityAnalysisModelInlineFunction> EntityAnalysisModelInlineFunction { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(45)]
        public IEnumerable<EntityAnalysisModelInlineScript> EntityAnalysisModelInlineScript { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(46)]
        public IEnumerable<EntityAnalysisModelList> EntityAnalysisModelList { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(47)]
        public IEnumerable<EntityAnalysisModelRequestXpath> EntityAnalysisModelRequestXpath { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(48)]
        public IEnumerable<EntityAnalysisModelSanction> EntityAnalysisModelSanction { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(49)]
        public IEnumerable<EntityAnalysisModelTtlCounter> EntityAnalysisModelTtlCounter { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(50)]
        public IEnumerable<EntityAnalysisModelTag> EntityAnalysisModelTag { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(51)]
        public IEnumerable<ExhaustiveSearchInstance> ExhaustiveSearchInstance { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(52)]
        public IEnumerable<CaseWorkflow> CaseWorkflow { get; set; }

        [Association(ThisKey = "TenantRegistryId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(53)]
        public TenantRegistry TenantRegistry { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(54)]
        public IEnumerable<EntityAnalysisModelSuppression> EntityAnalysisModelSuppression { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(55)]
        public IEnumerable<EntityAnalysisModelActivationRuleSuppression> EntityAnalysisModelActivationRuleSuppression
        {
            get;
            set;
        }

        [Column] [Nullable] [Key(56)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelActivationRuleSuppression
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public Guid EntityAnalysisModelGuid { get; set; }
        [Column] [Nullable] [Key(2)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(3)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(4)] public int? Version { get; set; }
        [Column] [Nullable] [Key(5)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(6)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(7)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(8)] public string SuppressionKey { get; set; }
        [Column] [Nullable] [Key(9)] public string SuppressionKeyValue { get; set; }
        [Column] [Nullable] [Key(10)] public string EntityAnalysisModelActivationRuleName { get; set; }
        [Column] [Nullable] [Key(11)] public int InheritedId { get; set; }

        [Association(ThisKey = "EntityAnalysisModelGuid", OtherKey = "Guid", CanBeNull = false,
            Relationship = Relationship.ManyToOne)]
        [Key(12)]
        public EntityAnalysisModel EntityAnalysisModel { get; set; }

        [Column] [Nullable] [Key(13)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelInstance
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(2)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(3)] public Guid EntityAnalysisInstanceGuid { get; set; }
        [Column] [Nullable] [Key(4)] public Guid EntityAnalysisModelInstanceGuid { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelAbstractionCalculationVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelAbstractionCalculationId { get; set; }
        [Column] [Nullable] [Key(2)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(3)] public string Name { get; set; }
        [Column] [Nullable] [Key(4)] public string EntityAnalysisModelAbstractionNameLeft { get; set; }
        [Column] [Nullable] [Key(5)] public string EntityAnalysisModelAbstractionNameRight { get; set; }
        [Column] [Nullable] [Key(6)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(7)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(9)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(11)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(12)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(13)] public int? Version { get; set; }
        [Column] [Nullable] [Key(14)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(15)] public byte? ResponsePayload { get; set; }
        [Column] [Nullable] [Key(16)] public int? AbstractionCalculationTypeId { get; set; }
        [Column] [Nullable] [Key(17)] public byte? ReportTable { get; set; }
        [Column] [Nullable] [Key(18)] public string FunctionScript { get; set; }
        [Column] [Nullable] [Key(19)] public Guid Guid { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelAbstractionCalculation
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(2)] public string Name { get; set; }
        [Column] [Nullable] [Key(3)] public string EntityAnalysisModelAbstractionNameLeft { get; set; }
        [Column] [Nullable] [Key(4)] public string EntityAnalysisModelAbstractionNameRight { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(6)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(8)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(9)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(10)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(11)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(12)] public int? Version { get; set; }
        [Column] [Nullable] [Key(13)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(14)] public byte? ResponsePayload { get; set; }
        [Column] [Nullable] [Key(15)] public int? AbstractionCalculationTypeId { get; set; }
        [Column] [Nullable] [Key(16)] public byte? ReportTable { get; set; }
        [Column] [Nullable] [Key(17)] public string FunctionScript { get; set; }

        [Association(ThisKey = "EntityAnalysisModelId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(18)]
        public EntityAnalysisModel EntityAnalysisModel { get; set; }

        [Column] [Nullable] [Key(19)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(20)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(21)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(22)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelAbstractionRuleVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelAbstractionRuleId { get; set; }
        [Column] [Nullable] [Key(2)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(3)] public string BuilderRuleScript { get; set; }
        [Column] [Nullable] [Key(4)] public string Name { get; set; }
        [Column] [Nullable] [Key(5)] public string SearchKey { get; set; }
        [Column] [Nullable] [Key(6)] public int? SearchFunctionTypeId { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(8)] public string SearchInterval { get; set; }
        [Column] [Nullable] [Key(9)] public string SearchFunctionKey { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(10)]
        public string Json { get; set; }

        [Column] [Nullable] [Key(11)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(12)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(13)] public int SearchValue { get; set; }
        [Column] [Nullable] [Key(14)] public byte? Search { get; set; }
        [Column] [Nullable] [Key(15)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(16)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(17)] public int? Version { get; set; }
        [Column] [Nullable] [Key(18)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(19)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(20)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(21)] public byte? ResponsePayload { get; set; }
        [Column] [Nullable] [Key(22)] public byte? ReportTable { get; set; }
        [Column] [Nullable] [Key(23)] public byte? Offset { get; set; }
        [Column] [Nullable] [Key(24)] public byte? OffsetTypeId { get; set; }
        [Column] [Nullable] [Key(25)] public int? OffsetValue { get; set; }
        [Column] [Nullable] [Key(26)] public string CoderRuleScript { get; set; }
        [Column] [Nullable] [Key(27)] public byte? RuleScriptTypeId { get; set; }
        [Column] [Nullable] [Key(28)] public Guid Guid { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelAbstractionRule
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(2)] public string BuilderRuleScript { get; set; }
        [Column] [Nullable] [Key(3)] public string Name { get; set; }
        [Column] [Nullable] [Key(4)] public string SearchKey { get; set; }
        [Column] [Nullable] [Key(5)] public int? SearchFunctionTypeId { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(7)] public string SearchInterval { get; set; }
        [Column] [Nullable] [Key(8)] public string SearchFunctionKey { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(9)]
        public string Json { get; set; }

        [Column] [Nullable] [Key(10)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(11)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(12)] public int SearchValue { get; set; }
        [Column] [Nullable] [Key(13)] public byte? Search { get; set; }
        [Column] [Nullable] [Key(14)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(15)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(16)] public int? Version { get; set; }
        [Column] [Nullable] [Key(17)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(18)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(19)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(20)] public byte? ResponsePayload { get; set; }
        [Column] [Nullable] [Key(21)] public byte? ReportTable { get; set; }
        [Column] [Nullable] [Key(22)] public byte? Offset { get; set; }
        [Column] [Nullable] [Key(23)] public byte? OffsetTypeId { get; set; }
        [Column] [Nullable] [Key(24)] public int? OffsetValue { get; set; }
        [Column] [Nullable] [Key(25)] public string CoderRuleScript { get; set; }
        [Column] [Nullable] [Key(26)] public byte? RuleScriptTypeId { get; set; }

        [Association(ThisKey = "EntityAnalysisModelId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(27)]
        public EntityAnalysisModel EntityAnalysisModel { get; set; }

        [Column] [Nullable] [Key(28)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(29)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(30)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(31)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelActivationRuleVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelActivationRuleId { get; set; }
        [Column] [Nullable] [Key(2)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(3)] public string BuilderRuleScript { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(4)]
        public string Json { get; set; }

        [Column] [Nullable] [Key(5)] public string Name { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(7)] public double? ResponseElevation { get; set; }
        [Column] [Nullable] [Key(8)] public Guid CaseWorkflowGuid { get; set; }
        [Column] [Nullable] [Key(9)] public int? EnableCaseWorkflow { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(11)] public int? Version { get; set; }
        [Column] [Nullable] [Key(12)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(13)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(14)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(15)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(16)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(17)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(18)] public Guid EntityAnalysisModelTtlCounterGuid { get; set; }
        [Column] [Nullable] [Key(19)] public Guid EntityAnalysisModelGuidTtlCounter { get; set; }
        [Column] [Nullable] [Key(20)] public byte? ResponsePayload { get; set; }
        [Column] [Nullable] [Key(21)] public byte? EnableTtlCounter { get; set; }
        [Column] [Nullable] [Key(22)] public string ResponseElevationContent { get; set; }
        [Column] [Nullable] [Key(23)] public byte? SendToActivationWatcher { get; set; }
        [Column] [Nullable] [Key(24)] public string ResponseElevationForeColor { get; set; }
        [Column] [Nullable] [Key(25)] public string ResponseElevationBackColor { get; set; }
        [Column] [Nullable] [Key(26)] public Guid CaseWorkflowStatusGuid { get; set; }
        [Column] [Nullable] [Key(27)] public double? ActivationSample { get; set; }
        [Column] [Nullable] [Key(28)] public long? ActivationCounter { get; set; }
        [Column] [Nullable] [Key(29)] public DateTime? ActivationCounterDate { get; set; }
        [Column] [Nullable] [Key(30)] public string ResponseElevationRedirect { get; set; }
        [Column] [Nullable] [Key(31)] public byte? ReviewStatusId { get; set; }
        [Column] [Nullable] [Key(32)] public byte? ReportTable { get; set; }
        [Column] [Nullable] [Key(33)] public byte? EnableNotification { get; set; }
        [Column] [Nullable] [Key(34)] public byte? NotificationTypeId { get; set; }
        [Column] [Nullable] [Key(35)] public string NotificationDestination { get; set; }
        [Column] [Nullable] [Key(36)] public string NotificationSubject { get; set; }
        [Column] [Nullable] [Key(37)] public string NotificationBody { get; set; }
        [Column] [Nullable] [Key(38)] public string CoderRuleScript { get; set; }
        [Column] [Nullable] [Key(39)] public byte? RuleScriptTypeId { get; set; }
        [Column] [Nullable] [Key(40)] public byte? EnableResponseElevation { get; set; }
        [Column] [Nullable] [Key(41)] public string CaseKey { get; set; }
        [Column] [Nullable] [Key(42)] public string ResponseElevationKey { get; set; }
        [Column] [Nullable] [Key(43)] public byte? EnableBypass { get; set; }
        [Column] [Nullable] [Key(44)] public char? BypassSuspendInterval { get; set; }
        [Column] [Nullable] [Key(45)] public int? BypassSuspendValue { get; set; }
        [Column] [Nullable] [Key(46)] public double? BypassSuspendSample { get; set; }
        [Column] [Nullable] [Key(47)] public byte? Visible { get; set; }
        [Column] [Nullable] [Key(48)] public byte? EnableReprocessing { get; set; }
        [Column] [Nullable] [Key(49)] public byte? EnableSuppression { get; set; }
        [Column] [Nullable] [Key(50)] public Guid Guid { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelActivationRule
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(2)] public string BuilderRuleScript { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(3)]
        public string Json { get; set; }

        [Column] [Nullable] [Key(4)] public string Name { get; set; }
        [Column] [Nullable] [Key(5)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(6)] public double? ResponseElevation { get; set; }
        [Column] [Nullable] [Key(7)] public Guid CaseWorkflowGuid { get; set; }
        [Column] [Nullable] [Key(8)] public int? EnableCaseWorkflow { get; set; }
        [Column] [Nullable] [Key(9)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(10)] public int? Version { get; set; }
        [Column] [Nullable] [Key(11)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(12)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(13)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(14)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(15)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(16)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(17)] public Guid EntityAnalysisModelTtlCounterGuid { get; set; }
        [Column] [Nullable] [Key(18)] public Guid EntityAnalysisModelGuidTtlCounter { get; set; }
        [Column] [Nullable] [Key(19)] public byte? ResponsePayload { get; set; }
        [Column] [Nullable] [Key(20)] public byte? EnableTtlCounter { get; set; }
        [Column] [Nullable] [Key(21)] public string ResponseElevationContent { get; set; }
        [Column] [Nullable] [Key(22)] public byte? SendToActivationWatcher { get; set; }
        [Column] [Nullable] [Key(23)] public string ResponseElevationForeColor { get; set; }
        [Column] [Nullable] [Key(24)] public string ResponseElevationBackColor { get; set; }
        [Column] [Nullable] [Key(25)] public Guid CaseWorkflowStatusGuid { get; set; }
        [Column] [Nullable] [Key(26)] public double? ActivationSample { get; set; }
        [Column] [Nullable] [Key(27)] public long? ActivationCounter { get; set; }
        [Column] [Nullable] [Key(28)] public DateTime? ActivationCounterDate { get; set; }
        [Column] [Nullable] [Key(29)] public string ResponseElevationRedirect { get; set; }
        [Column] [Nullable] [Key(30)] public byte? ReviewStatusId { get; set; }
        [Column] [Nullable] [Key(31)] public byte? ReportTable { get; set; }
        [Column] [Nullable] [Key(32)] public byte? EnableNotification { get; set; }
        [Column] [Nullable] [Key(33)] public byte? NotificationTypeId { get; set; }
        [Column] [Nullable] [Key(34)] public string NotificationDestination { get; set; }
        [Column] [Nullable] [Key(35)] public string NotificationSubject { get; set; }
        [Column] [Nullable] [Key(36)] public string NotificationBody { get; set; }
        [Column] [Nullable] [Key(37)] public string CoderRuleScript { get; set; }
        [Column] [Nullable] [Key(38)] public byte? RuleScriptTypeId { get; set; }
        [Column] [Nullable] [Key(39)] public byte? EnableResponseElevation { get; set; }
        [Column] [Nullable] [Key(40)] public string CaseKey { get; set; }
        [Column] [Nullable] [Key(41)] public string ResponseElevationKey { get; set; }
        [Column] [Nullable] [Key(42)] public byte? EnableBypass { get; set; }
        [Column] [Nullable] [Key(43)] public char? BypassSuspendInterval { get; set; }
        [Column] [Nullable] [Key(44)] public int? BypassSuspendValue { get; set; }
        [Column] [Nullable] [Key(45)] public double? BypassSuspendSample { get; set; }
        [Column] [Nullable] [Key(46)] public byte? Visible { get; set; }
        [Column] [Nullable] [Key(47)] public byte? EnableReprocessing { get; set; }
        [Column] [Nullable] [Key(48)] public byte? EnableSuppression { get; set; }

        [Association(ThisKey = "EntityAnalysisModelId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(49)]
        public EntityAnalysisModel EntityAnalysisModel { get; set; }

        [Association(ThisKey = "CaseWorkflowStatusGuid", OtherKey = "Guid", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(50)]
        public CaseWorkflowStatus CaseWorkflowStatus { get; set; }

        [Column] [Nullable] [Key(51)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(52)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(53)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(54)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelHttpAdaptationVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelHttpAdaptationId { get; set; }
        [Column] [Nullable] [Key(2)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(3)] public string Name { get; set; }
        [Column] [Nullable] [Key(4)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(5)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(6)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(8)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(9)] public int? Version { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(11)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(12)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(13)] public byte? ResponsePayload { get; set; }
        [Column] [Nullable] [Key(14)] public byte? ReportTable { get; set; }
        [Column] [Nullable] [Key(15)] public string HttpEndpoint { get; set; }
        [Column] [Nullable] [Key(16)] public Guid Guid { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelHttpAdaptation
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(2)] public string Name { get; set; }
        [Column] [Nullable] [Key(3)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(4)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(7)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(8)] public int? Version { get; set; }
        [Column] [Nullable] [Key(9)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(11)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(12)] public byte? ResponsePayload { get; set; }
        [Column] [Nullable] [Key(13)] public byte? ReportTable { get; set; }
        [Column] [Nullable] [Key(14)] public string HttpEndpoint { get; set; }

        [Association(ThisKey = "EntityAnalysisModelId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(15)]
        public EntityAnalysisModel EntityAnalysisModel { get; set; }

        [Column] [Nullable] [Key(16)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(17)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(18)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(19)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class UserRegistry
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(2)] public int RoleRegistryId { get; set; }
        [Column] [Nullable] [Key(3)] public string Email { get; set; }
        [Column] [Nullable] [Key(4)] public string Name { get; set; }
        [Column] [Nullable] [Key(5)] public string Password { get; set; }
        [Column] [Nullable] [Key(6)] public int FailedPasswordCount { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? PasswordCreatedDate { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? PasswordLockedDate { get; set; }
        [Column] [Nullable] [Key(9)] public DateTime? PasswordExpiryDate { get; set; }
        [Column] [Nullable] [Key(10)] public DateTime? LastLoginDate { get; set; }
        [Column] [Nullable] [Key(11)] public byte? PasswordLocked { get; set; }
        [Column] [Nullable] [Key(12)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(13)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(14)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(15)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(16)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(17)] public int? Version { get; set; }
        [Column] [Nullable] [Key(18)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(19)] public int? InheritedId { get; set; }

        [Association(ThisKey = "RoleRegistryId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(20)]
        public RoleRegistry RoleRegistry { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class UserRegistryVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int UserRegistryId { get; set; }
        [Column] [Nullable] [Key(2)] public int RoleRegistryId { get; set; }
        [Column] [Nullable] [Key(3)] public string Email { get; set; }
        [Column] [Nullable] [Key(4)] public string Name { get; set; }
        [Column] [Nullable] [Key(5)] public string Password { get; set; }
        [Column] [Nullable] [Key(6)] public int FailedPasswordCount { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? PasswordCreatedDate { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? PasswordLockedDate { get; set; }
        [Column] [Nullable] [Key(9)] public DateTime? PasswordExpiryDate { get; set; }
        [Column] [Nullable] [Key(10)] public DateTime? LastLoginDate { get; set; }
        [Column] [Nullable] [Key(11)] public byte? PasswordLocked { get; set; }
        [Column] [Nullable] [Key(12)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(13)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(14)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(15)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(16)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(17)] public int? Version { get; set; }
        [Column] [Nullable] [Key(18)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(19)] public int? InheritedId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisAsynchronousQueueBalance
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(2)] public int? AsynchronousInvoke { get; set; }
        [Column] [Nullable] [Key(3)] public int? AsynchronousCallback { get; set; }
        [Column] [Nullable] [Key(4)] public int? AsynchronousCallbackTimeout { get; set; }
        [Column] [Nullable] [Key(5)] public string Instance { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelAsynchronousQueueBalance
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public Guid EntityAnalysisModelGuid { get; set; }
        [Column] [Nullable] [Key(2)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(3)] public int? Archive { get; set; }
        [Column] [Nullable] [Key(4)] public int? ActivationWatcher { get; set; }
        [Column] [Nullable] [Key(5)] public string Instance { get; set; }

        [Association(ThisKey = "EntityAnalysisModelGuid", OtherKey = "Guid", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(6)]
        public EntityAnalysisModel EntityAnalysisModel { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelDictionaryVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int EntityAnalysisModelDictionaryId { get; set; }
        [Column] [Nullable] [Key(2)] public Guid EntityAnalysisModelGuid { get; set; }
        [Column] [Nullable] [Key(3)] public string Name { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(7)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(8)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(9)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(10)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(11)] public int? Version { get; set; }
        [Column] [Nullable] [Key(12)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(13)] public string DataName { get; set; }
        [Column] [Nullable] [Key(14)] public byte? ResponsePayload { get; set; }
        [Column] [Nullable] [Key(15)] public Guid Guid { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelDictionary
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public Guid EntityAnalysisModelGuid { get; set; }
        [Column] [Nullable] [Key(2)] public string Name { get; set; }
        [Column] [Nullable] [Key(3)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(5)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(6)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(7)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(9)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(10)] public int? Version { get; set; }
        [Column] [Nullable] [Key(11)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(12)] public string DataName { get; set; }
        [Column] [Nullable] [Key(13)] public byte? ResponsePayload { get; set; }

        [Association(ThisKey = "EntityAnalysisModelGuid", OtherKey = "Guid", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(14)]
        public EntityAnalysisModel EntityAnalysisModel { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelDictionaryId",
            CanBeNull = true, Relationship = Relationship.OneToMany)]
        [Key(15)]
        public IEnumerable<EntityAnalysisModelDictionaryKvp>
            EntityAnalysisModelDictionaryKvp { get; set; }

        [Column] [Nullable] [Key(16)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(17)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(18)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(19)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelDictionaryKvpVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelDictionaryKvpId { get; set; }
        [Column] [Nullable] [Key(2)] public int? EntityAnalysisModelDictionaryId { get; set; }
        [Column] [Nullable] [Key(3)] public string KvpKey { get; set; }
        [Column] [Nullable] [Key(4)] public double? KvpValue { get; set; }
        [Column] [Nullable] [Key(5)] public DateTime? CreatedDate { get; set; }

        [Column("CreatedUser")]
        [Nullable]
        [Key(6)]
        public string CreatedUser { get; set; }

        [Column] [Nullable] [Key(7)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(9)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(10)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(11)] public int? Version { get; set; }
        [Column] [Nullable] [Key(12)] public Guid Guid { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelDictionaryKvp
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelDictionaryId { get; set; }
        [Column] [Nullable] [Key(2)] public string KvpKey { get; set; }
        [Column] [Nullable] [Key(3)] public double? KvpValue { get; set; }
        [Column] [Nullable] [Key(4)] public DateTime? CreatedDate { get; set; }

        [Column("CreatedUser")]
        [Nullable]
        [Key(5)]
        public string CreatedUser { get; set; }

        [Column] [Nullable] [Key(6)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(8)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(9)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(10)] public int? Version { get; set; }

        [Association(ThisKey = "EntityAnalysisModelDictionaryId", OtherKey = "Id",
            CanBeNull = true, Relationship = Relationship.ManyToOne)]
        [Key(11)]
        public EntityAnalysisModelDictionary EntityAnalysisModelDictionary { get; set; }

        [Column] [Nullable] [Key(12)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(13)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(14)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(15)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelGatewayRuleVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelGatewayRuleId { get; set; }
        [Column] [Nullable] [Key(2)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(3)] public int? Priority { get; set; }
        [Column] [Nullable] [Key(4)] public string BuilderRuleScript { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(5)]
        public string Json { get; set; }

        [Column] [Nullable] [Key(6)] public string Name { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(8)] public double? MaxResponseElevation { get; set; }
        [Column] [Nullable] [Key(9)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(10)] public int? Version { get; set; }
        [Column] [Nullable] [Key(11)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(12)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(13)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(14)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(15)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(16)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(17)] public int? ActivationCounter { get; set; }
        [Column] [Nullable] [Key(18)] public DateTime? ActivationCounterDate { get; set; }
        [Column] [Nullable] [Key(19)] public string CoderRuleScript { get; set; }
        [Column] [Nullable] [Key(20)] public byte? RuleScriptTypeId { get; set; }
        [Column] [Nullable] [Key(21)] public double? GatewaySample { get; set; }
        [Column] [Nullable] [Key(22)] public Guid Guid { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelGatewayRule
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(2)] public int? Priority { get; set; }
        [Column] [Nullable] [Key(3)] public string BuilderRuleScript { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(4)]
        public string Json { get; set; }

        [Column] [Nullable] [Key(5)] public string Name { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(7)] public double? MaxResponseElevation { get; set; }
        [Column] [Nullable] [Key(8)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(9)] public int? Version { get; set; }
        [Column] [Nullable] [Key(10)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(11)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(12)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(13)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(14)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(15)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(16)] public int? ActivationCounter { get; set; }
        [Column] [Nullable] [Key(17)] public DateTime? ActivationCounterDate { get; set; }
        [Column] [Nullable] [Key(18)] public string CoderRuleScript { get; set; }
        [Column] [Nullable] [Key(19)] public byte? RuleScriptTypeId { get; set; }
        [Column] [Nullable] [Key(20)] public double? GatewaySample { get; set; }

        [Association(ThisKey = "EntityAnalysisModelId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(21)]
        public EntityAnalysisModel EntityAnalysisModel { get; set; }

        [Column] [Nullable] [Key(22)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(23)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(24)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(25)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelInlineFunctionVersion
    {
        [Column("Id")]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelInlineFunctionId { get; set; }
        [Column] [Nullable] [Key(2)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(3)] public string Name { get; set; }
        [Column] [Nullable] [Key(4)] public string FunctionScript { get; set; }
        [Column] [Nullable] [Key(5)] public int? ReturnDataTypeId { get; set; }
        [Column] [Nullable] [Key(6)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(7)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(9)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(11)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(12)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(13)] public int? Version { get; set; }
        [Column] [Nullable] [Key(14)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(15)] public byte? ResponsePayload { get; set; }
        [Column] [Nullable] [Key(16)] public byte? ReportTable { get; set; }
        [Column] [Nullable] [Key(17)] public Guid Guid { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelInlineFunction
    {
        [Column("Id")]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(2)] public string Name { get; set; }
        [Column] [Nullable] [Key(3)] public string FunctionScript { get; set; }
        [Column] [Nullable] [Key(4)] public int? ReturnDataTypeId { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(6)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(8)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(9)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(10)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(11)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(12)] public int? Version { get; set; }
        [Column] [Nullable] [Key(13)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(14)] public byte? ResponsePayload { get; set; }
        [Column] [Nullable] [Key(15)] public byte? ReportTable { get; set; }

        [Association(ThisKey = "EntityAnalysisModelId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(16)]
        public EntityAnalysisModel EntityAnalysisModel { get; set; }

        [Column] [Nullable] [Key(17)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(18)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(19)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(20)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelInlineScriptVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelInlineScriptId { get; set; }
        [Column] [Nullable] [Key(2)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(3)] public int? EntityAnalysisInlineScriptId { get; set; }
        [Column] [Nullable] [Key(4)] public string Name { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(6)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(8)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(9)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(10)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(11)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(12)] public int? Version { get; set; }
        [Column] [Nullable] [Key(13)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(14)] public Guid Guid { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelInlineScript
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(2)] public int? EntityAnalysisInlineScriptId { get; set; }
        [Column] [Nullable] [Key(3)] public string Name { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(7)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(8)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(9)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(10)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(11)] public int? Version { get; set; }
        [Column] [Nullable] [Key(12)] public int? InheritedId { get; set; }

        [Association(ThisKey = "EntityAnalysisInlineScriptId", OtherKey = "Id",
            CanBeNull = true, Relationship = Relationship.ManyToOne)]
        [Key(13)]
        public EntityAnalysisInlineScript EntityAnalysisInlineScript { get; set; }

        [Association(ThisKey = "EntityAnalysisModelId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(14)]
        public EntityAnalysisModel EntityAnalysisModel { get; set; }

        [Column] [Nullable] [Key(15)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(16)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(17)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(18)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelTagVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelTagId { get; set; }
        [Column] [Nullable] [Key(2)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(3)] public string Name { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(6)] public byte? ResponsePayload { get; set; }
        [Column] [Nullable] [Key(7)] public byte? ReportTable { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(9)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(11)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(12)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(13)] public int? Version { get; set; }
        [Column] [Nullable] [Key(14)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(15)] public Guid Guid { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelTag
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(2)] public string Name { get; set; }
        [Column] [Nullable] [Key(3)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(5)] public byte? ResponsePayload { get; set; }
        [Column] [Nullable] [Key(6)] public byte? ReportTable { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(8)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(9)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(10)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(11)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(12)] public int? Version { get; set; }
        [Column] [Nullable] [Key(13)] public int? InheritedId { get; set; }

        [Association(ThisKey = "EntityAnalysisModelId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(14)]
        public EntityAnalysisModel EntityAnalysisModel { get; set; }

        [Column] [Nullable] [Key(15)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(16)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(17)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(18)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelListVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int EntityAnalysisModelListId { get; set; }
        [Column] [Nullable] [Key(2)] public Guid EntityAnalysisModelGuid { get; set; }
        [Column] [Nullable] [Key(3)] public string Name { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(7)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(8)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(9)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(10)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(11)] public int? Version { get; set; }
        [Column] [Nullable] [Key(12)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(13)] public Guid Guid { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelList
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public Guid EntityAnalysisModelGuid { get; set; }
        [Column] [Nullable] [Key(2)] public string Name { get; set; }
        [Column] [Nullable] [Key(3)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(5)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(6)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(7)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(9)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(10)] public int? Version { get; set; }
        [Column] [Nullable] [Key(11)] public int? InheritedId { get; set; }

        [Association(ThisKey = "EntityAnalysisModelGuid", OtherKey = "Guid", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(12)]
        public EntityAnalysisModel EntityAnalysisModel { get; set; }

        [Association(ThisKey = "Id", OtherKey = "EntityAnalysisModelListId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(13)]
        public IEnumerable<EntityAnalysisModelListValue> EntityAnalysisModelListValue { get; set; }

        [Column] [Nullable] [Key(14)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(15)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(16)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(17)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelListValueVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelListValueId { get; set; }
        [Column] [Nullable] [Key(2)] public int? EntityAnalysisModelListId { get; set; }
        [Column] [Nullable] [Key(3)] public string ListValue { get; set; }
        [Column] [Nullable] [Key(4)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(5)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(6)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(8)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(9)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(10)] public int? Version { get; set; }
        [Column] [Nullable] [Key(11)] public Guid Guid { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelListValue
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelListId { get; set; }
        [Column] [Nullable] [Key(2)] public string ListValue { get; set; }
        [Column] [Nullable] [Key(3)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(4)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(7)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(8)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(9)] public int? Version { get; set; }

        [Association(ThisKey = "EntityAnalysisModelListId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(10)]
        public EntityAnalysisModelList EntityAnalysisModelList { get; set; }

        [Column] [Nullable] [Key(11)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(12)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(13)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(14)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelProcessingCounter
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(2)] public string Instance { get; set; }
        [Column] [Nullable] [Key(3)] public int? ModelInvoke { get; set; }
        [Column] [Nullable] [Key(4)] public int? GatewayMatch { get; set; }
        [Column] [Nullable] [Key(5)] public Guid EntityAnalysisModelGuid { get; set; }
        [Column] [Nullable] [Key(6)] public int? ResponseElevation { get; set; }
        [Column] [Nullable] [Key(7)] public double? ResponseElevationSum { get; set; }
        [Column] [Nullable] [Key(8)] public double? ActivationWatcher { get; set; }
        [Column] [Nullable] [Key(9)] public int? ResponseElevationValueLimit { get; set; }
        [Column] [Nullable] [Key(10)] public int? ResponseElevationLimit { get; set; }
        [Column] [Nullable] [Key(11)] public int? ResponseElevationValueGatewayLimit { get; set; }

        [Association(ThisKey = "EntityAnalysisModelGuid", OtherKey = "Guid", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(12)]
        public EntityAnalysisModel EntityAnalysisModel { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelReprocessingRule
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(2)] public int? Priority { get; set; }
        [Column] [Nullable] [Key(3)] public string BuilderRuleScript { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(4)]
        public string Json { get; set; }

        [Column] [Nullable] [Key(5)] public string Name { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(7)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(8)] public int? Version { get; set; }
        [Column] [Nullable] [Key(9)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(11)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(12)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(13)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(14)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(15)] public double? ReprocessingSample { get; set; }
        [Column] [Nullable] [Key(16)] public string CoderRuleScript { get; set; }
        [Column] [Nullable] [Key(17)] public byte? RuleScriptTypeId { get; set; }
        [Column] [Nullable] [Key(18)] public int? ReprocessingValue { get; set; }
        [Column] [Nullable] [Key(19)] public string ReprocessingInterval { get; set; }

        [Association(ThisKey = "EntityAnalysisModelId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(20)]
        public EntityAnalysisModel EntityAnalysisModel { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelReprocessingRuleInstance
    {
        [Column("Id")]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelReprocessingRuleId { get; set; }
        [Column] [Nullable] [Key(2)] public byte? StatusId { get; set; }
        [Column] [Nullable] [Key(3)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(4)] public DateTime? StartedDate { get; set; }
        [Column] [Nullable] [Key(5)] public long? AvailableCount { get; set; }
        [Column] [Nullable] [Key(6)] public long? SampledCount { get; set; }
        [Column] [Nullable] [Key(7)] public long? MatchedCount { get; set; }
        [Column] [Nullable] [Key(8)] public long? ProcessedCount { get; set; }
        [Column] [Nullable] [Key(9)] public DateTime? CompletedDate { get; set; }
        [Column] [Nullable] [Key(10)] public long? ErrorCount { get; set; }
        [Column] [Nullable] [Key(11)] public DateTime? ReferenceDate { get; set; }
        [Column] [Nullable] [Key(12)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(13)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(14)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(15)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(16)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(17)] public int Version { get; set; }
        [Column] [Nullable] [Key(18)] public int InheritedId { get; set; }

        [Association(ThisKey = "EntityAnalysisModelReprocessingRuleId",
            OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(19)]
        public EntityAnalysisModelReprocessingRule EntityAnalysisModelReprocessingRule { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelRequestXpathVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelRequestXpathId { get; set; }
        [Column] [Nullable] [Key(2)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(3)] public string Name { get; set; }
        [Column] [Nullable] [Key(4)] public int? DataTypeId { get; set; }
        [Column] [Nullable] [Key(5)] public string XPath { get; set; }
        [Column] [Nullable] [Key(6)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(7)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(8)] public byte? SearchKey { get; set; }
        [Column] [Nullable] [Key(9)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(10)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(11)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(12)] public int? Version { get; set; }
        [Column] [Nullable] [Key(13)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(14)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(15)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(16)] public byte? SearchKeyCache { get; set; }
        [Column] [Nullable] [Key(17)] public string SearchKeyCacheInterval { get; set; }
        [Column] [Nullable] [Key(18)] public int? SearchKeyCacheValue { get; set; }
        [Column] [Nullable] [Key(19)] public byte? ResponsePayload { get; set; }
        [Column] [Nullable] [Key(20)] public string SearchKeyTtlInterval { get; set; }
        [Column] [Nullable] [Key(21)] public int? SearchKeyTtlIntervalValue { get; set; }
        [Column] [Nullable] [Key(22)] public string SearchKeyCacheTtlInterval { get; set; }
        [Column] [Nullable] [Key(23)] public int? SearchKeyCacheTtlValue { get; set; }
        [Column] [Nullable] [Key(24)] public int? SearchKeyCacheFetchLimit { get; set; }
        [Column] [Nullable] [Key(25)] public byte? ReportTable { get; set; }
        [Column] [Nullable] [Key(26)] public byte? SearchKeyCacheSample { get; set; }
        [Column] [Nullable] [Key(27)] public byte? EnableSuppression { get; set; }
        [Column] [Nullable] [Key(28)] public string DefaultValue { get; set; }
        [Column] [Nullable] [Key(29)] public int? SearchKeyFetchLimit { get; set; }
        [Column] [Nullable] [Key(30)] public Guid Guid { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelRequestXpath
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(2)] public string Name { get; set; }
        [Column] [Nullable] [Key(3)] public int? DataTypeId { get; set; }
        [Column] [Nullable] [Key(4)] public string XPath { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(6)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(7)] public byte? SearchKey { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(9)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(10)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(11)] public int? Version { get; set; }
        [Column] [Nullable] [Key(12)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(13)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(14)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(15)] public byte? SearchKeyCache { get; set; }
        [Column] [Nullable] [Key(16)] public string SearchKeyCacheInterval { get; set; }
        [Column] [Nullable] [Key(17)] public int? SearchKeyCacheValue { get; set; }
        [Column] [Nullable] [Key(18)] public byte? ResponsePayload { get; set; }
        [Column] [Nullable] [Key(19)] public string SearchKeyTtlInterval { get; set; }
        [Column] [Nullable] [Key(20)] public int? SearchKeyTtlIntervalValue { get; set; }
        [Column] [Nullable] [Key(21)] public string SearchKeyCacheTtlInterval { get; set; }
        [Column] [Nullable] [Key(22)] public int? SearchKeyCacheTtlValue { get; set; }
        [Column] [Nullable] [Key(23)] public int? SearchKeyCacheFetchLimit { get; set; }
        [Column] [Nullable] [Key(24)] public byte? ReportTable { get; set; }
        [Column] [Nullable] [Key(25)] public byte? SearchKeyCacheSample { get; set; }
        [Column] [Nullable] [Key(26)] public byte? EnableSuppression { get; set; }
        [Column] [Nullable] [Key(27)] public string DefaultValue { get; set; }
        [Column] [Nullable] [Key(28)] public int? SearchKeyFetchLimit { get; set; }

        [Association(ThisKey = "EntityAnalysisModelId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(29)]
        public EntityAnalysisModel EntityAnalysisModel { get; set; }

        [Column] [Nullable] [Key(30)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(31)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(32)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(33)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelSanctionVersion
    {
        [Column("Id")]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelSanctionId { get; set; }
        [Column] [Nullable] [Key(2)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(3)] public string MultipartStringDataName { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Distance { get; set; }
        [Column] [Nullable] [Key(5)] public string Name { get; set; }
        [Column] [Nullable] [Key(6)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(7)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(9)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(11)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(12)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(13)] public int? Version { get; set; }
        [Column] [Nullable] [Key(14)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(15)] public byte? ResponsePayload { get; set; }
        [Column] [Nullable] [Key(16)] public byte? ReportTable { get; set; }
        [Column] [Nullable] [Key(17)] public int? CacheValue { get; set; }
        [Column] [Nullable] [Key(18)] public char? CacheInterval { get; set; }
        [Column] [Nullable] [Key(19)] public Guid Guid { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class Export
    {
        [Column("Id")]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(2)] public int TenantRegistryId { get; set; }
        [Column] [Nullable] [Key(3)] public byte[] Bytes { get; set; }
        [Column] [Nullable] [Key(4)] public byte[] EncryptedBytes { get; set; }
        [Column] [Nullable] [Key(5)] public byte? InError { get; set; }
        [Column] [Nullable] [Key(6)] public string ErrorStack { get; set; }
        [Column] [Nullable] [Key(7)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(9)] public DateTime? CompletedDate { get; set; }
        [Column] [Nullable] [Key(10)] public int? Version { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class ExportPeek
    {
        [Column("Id")]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(2)] public int TenantRegistryId { get; set; }
        [Column] [Nullable] [Key(3)] public string Yaml { get; set; }
        [Column] [Nullable] [Key(5)] public byte? InError { get; set; }
        [Column] [Nullable] [Key(6)] public string ErrorStack { get; set; }
        [Column] [Nullable] [Key(7)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(9)] public DateTime? CompletedDate { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class Import
    {
        [Column("Id")]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(2)] public int TenantRegistryId { get; set; }
        [Column] [Nullable] [Key(3)] public byte[] Bytes { get; set; }
        [Column] [Nullable] [Key(4)] public byte? InError { get; set; }
        [Column] [Nullable] [Key(5)] public string ErrorStack { get; set; }
        [Column] [Nullable] [Key(6)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? CompletedDate { get; set; }
        [Column] [Nullable] [Key(9)] public int? ExportVersion { get; set; }
        [Column] [Nullable] [Key(10)] public Guid ExportGuid { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelSanction
    {
        [Column("Id")]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(2)] public string MultipartStringDataName { get; set; }
        [Column] [Nullable] [Key(3)] public byte? Distance { get; set; }
        [Column] [Nullable] [Key(4)] public string Name { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(6)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(8)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(9)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(10)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(11)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(12)] public int? Version { get; set; }
        [Column] [Nullable] [Key(13)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(14)] public byte? ResponsePayload { get; set; }
        [Column] [Nullable] [Key(15)] public byte? ReportTable { get; set; }
        [Column] [Nullable] [Key(16)] public int? CacheValue { get; set; }
        [Column] [Nullable] [Key(17)] public char? CacheInterval { get; set; }

        [Association(ThisKey = "EntityAnalysisModelId", OtherKey = "Id", CanBeNull = true)]
        [Key(18)]
        public EntityAnalysisModel EntityAnalysisModel { get; set; }

        [Column] [Nullable] [Key(19)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(20)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(21)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(22)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelSearchKeyCalculationInstance
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public string SearchKey { get; set; }
        [Column] [Nullable] [Key(2)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(3)] public byte? Completed { get; set; }
        [Column] [Nullable] [Key(4)] public DateTime? CompletedDate { get; set; }
        [Column] [Nullable] [Key(5)] public Guid EntityAnalysisModelGuid { get; set; }
        [Column] [Nullable] [Key(6)] public int? DistinctValuesCount { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? DistinctValuesUpdatedDate { get; set; }
        [Column] [Nullable] [Key(8)] public int? DistinctValuesProcessedValuesCount { get; set; }
        [Column] [Nullable] [Key(9)] public DateTime? DistinctValuesProcessedValuesUpdatedDate { get; set; }
        [Column] [Nullable] [Key(10)] public DateTime? DistinctFetchToDate { get; set; }
        [Column] [Nullable] [Key(11)] public DateTime? ExpiredSearchKeyCacheDate { get; set; }
        [Column] [Nullable] [Key(12)] public int? ExpiredSearchKeyCacheCount { get; set; }

        [Association(ThisKey = "EntityAnalysisModelGuid", OtherKey = "Guid", CanBeNull = false,
            Relationship = Relationship.ManyToOne)]
        [Key(13)]
        public EntityAnalysisModel EntityAnalysisModel { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelSearchKeyDistinctValueCalculationInstance
    {
        [Column("Id")]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelSearchKeyCalculationInstanceId { get; set; }
        [Column] [Nullable] [Key(2)] public string SearchKeyValue { get; set; }
        [Column] [Nullable] [Key(3)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(4)] public int? EntryCount { get; set; }
        [Column] [Nullable] [Key(5)] public DateTime? EntryCountUpdatedDate { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? AbstractionRulesMatchesUpdatedDate { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? CompletedDate { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelSynchronisationNodeStatusEntry
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public string Instance { get; set; }
        [Column] [Nullable] [Key(2)] public DateTime? HeartbeatDate { get; set; }
        [Column] [Nullable] [Key(3)] public DateTime? SynchronisedDate { get; set; }
        [Column] [Nullable] [Key(4)] public int? TenantRegistryId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelSynchronisationSchedule
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(2)] public DateTime? ScheduleDate { get; set; }
        [Column] [Nullable] [Key(3)] public int? TenantRegistryId { get; set; }
        [Column] [Nullable] [Key(4)] public string CreatedUser { get; set; }

        [Association(ThisKey = "TenantRegistryId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(5)]
        public TenantRegistry TenantRegistry { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelTtlCounter
    {
        [Column("Id")]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public string Name { get; set; }
        [Column] [Nullable] [Key(2)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(3)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Locked { get; set; }

        [Column("CreatedDate")]
        [Nullable]
        [Key(5)]
        public DateTime? CreatedDate { get; set; }

        [Column] [Nullable] [Key(6)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(7)] public int? Version { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(9)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(11)] public string TtlCounterInterval { get; set; }
        [Column] [Nullable] [Key(12)] public int? TtlCounterValue { get; set; }
        [Column] [Nullable] [Key(13)] public byte? ResponsePayload { get; set; }
        [Column] [Nullable] [Key(14)] public byte? ReportTable { get; set; }
        [Column] [Nullable] [Key(15)] public string TtlCounterDataName { get; set; }
        [Column] [Nullable] [Key(16)] public byte? OnlineAggregation { get; set; }
        [Column] [Nullable] [Key(17)] public byte? EnableLiveForever { get; set; }

        [Association(ThisKey = "EntityAnalysisModelId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(18)]
        public EntityAnalysisModel EntityAnalysisModel { get; set; }

        [Column] [Nullable] [Key(19)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(20)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelTtlCounterVersion
    {
        [Column("Id")]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? EntityAnalysisModelTtlCounterId { get; set; }
        [Column] [Nullable] [Key(2)] public string Name { get; set; }
        [Column] [Nullable] [Key(3)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Locked { get; set; }

        [Column("CreatedDate")]
        [Nullable]
        [Key(6)]
        public DateTime? CreatedDate { get; set; }

        [Column] [Nullable] [Key(7)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(8)] public int? Version { get; set; }
        [Column] [Nullable] [Key(9)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(10)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(11)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(12)] public string TtlCounterInterval { get; set; }
        [Column] [Nullable] [Key(13)] public int? TtlCounterValue { get; set; }
        [Column] [Nullable] [Key(14)] public byte? ResponsePayload { get; set; }
        [Column] [Nullable] [Key(15)] public byte? ReportTable { get; set; }
        [Column] [Nullable] [Key(16)] public string TtlCounterDataName { get; set; }
        [Column] [Nullable] [Key(17)] public byte? OnlineAggregation { get; set; }
        [Column] [Nullable] [Key(18)] public byte? EnableLiveForever { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class EntityAnalysisModelSuppression
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public Guid EntityAnalysisModelGuid { get; set; }
        [Column] [Nullable] [Key(2)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(3)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(4)] public int? Version { get; set; }
        [Column] [Nullable] [Key(5)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(6)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(7)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(8)] public string SuppressionKeyValue { get; set; }
        [Column] [Nullable] [Key(9)] public string SuppressionKey { get; set; }
        [Column] [Nullable] [Key(10)] public int InheritedId { get; set; }

        [Association(ThisKey = "EntityAnalysisModelGuid", OtherKey = "Guid", CanBeNull = false,
            Relationship = Relationship.ManyToOne)]
        [Key(11)]
        public EntityAnalysisModel EntityAnalysisModel { get; set; }

        [Column] [Nullable] [Key(12)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class ExhaustiveSearchInstance
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(2)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(3)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(5)] public byte? ReportTable { get; set; }
        [Column] [Nullable] [Key(6)] public byte? ResponsePayload { get; set; }
        [Column] [Nullable] [Key(7)] public int? ModelsSinceBest { get; set; }
        [Column] [Nullable] [Key(8)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(9)] public string Name { get; set; }
        [Column] [Nullable] [Key(10)] public byte? StatusId { get; set; }
        [Column] [Nullable] [Key(11)] public int? Models { get; set; }
        [Column] [Nullable] [Key(12)] public double? Score { get; set; }
        [Column] [Nullable] [Key(13)] public int? TopologyComplexity { get; set; }
        [Column] [Nullable] [Key(14)] public DateTime? CompletedDate { get; set; }
        [Column] [Nullable] [Key(15)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(16)] public byte[] Object { get; set; }
        [Column] [Nullable] [Key(17)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(18)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(19)] public DateTime DeletedDate { get; set; }
        [Column] [Nullable] [Key(20)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(21)] public int? Version { get; set; }
        [Column] [Nullable] [Key(22)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(23)] public byte? Anomaly { get; set; }
        [Column] [Nullable] [Key(24)] public double AnomalyProbability { get; set; }
        [Column] [Nullable] [Key(25)] public byte? Filter { get; set; }
        [Column] [Nullable] [Key(26)] public string FilterSql { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(27)]
        public string FilterJson { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(28)]
        public string FilterTokens { get; set; }

        [Association(ThisKey = "EntityAnalysisModelId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(29)]
        public EntityAnalysisModel EntityAnalysisModel { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(30)]
        public IEnumerable<ExhaustiveSearchInstanceTrialInstance> ExhaustiveSearchInstanceTrialInstance { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(31)]
        public IEnumerable<ExhaustiveSearchInstanceVariable> ExhaustiveSearchInstanceVariable { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(32)]
        public IEnumerable<ExhaustiveSearchInstanceData> ExhaustiveSearchInstanceData { get; set; }

        [Column] [Nullable] [Key(33)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class ExhaustiveSearchInstanceVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int ExhaustiveSearchInstanceId { get; set; }
        [Column] [Nullable] [Key(2)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(3)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(6)] public byte? ReportTable { get; set; }
        [Column] [Nullable] [Key(7)] public byte? ResponsePayload { get; set; }
        [Column] [Nullable] [Key(8)] public int? ModelsSinceBest { get; set; }
        [Column] [Nullable] [Key(9)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(10)] public string Name { get; set; }
        [Column] [Nullable] [Key(11)] public byte? StatusId { get; set; }
        [Column] [Nullable] [Key(12)] public int? Models { get; set; }
        [Column] [Nullable] [Key(13)] public double? Score { get; set; }
        [Column] [Nullable] [Key(14)] public int? TopologyComplexity { get; set; }
        [Column] [Nullable] [Key(15)] public DateTime? CompletedDate { get; set; }
        [Column] [Nullable] [Key(16)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(17)] public byte[] Object { get; set; }
        [Column] [Nullable] [Key(18)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(19)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(20)] public DateTime DeletedDate { get; set; }
        [Column] [Nullable] [Key(21)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(22)] public int? Version { get; set; }
        [Column] [Nullable] [Key(23)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(24)] public byte? Anomaly { get; set; }
        [Column] [Nullable] [Key(25)] public double AnomalyProbability { get; set; }
        [Column] [Nullable] [Key(26)] public byte? Filter { get; set; }
        [Column] [Nullable] [Key(27)] public string FilterSql { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(28)]
        public string FilterJson { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(29)]
        public string FilterTokens { get; set; }

        [Association(ThisKey = "EntityAnalysisModelId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(30)]
        public EntityAnalysisModel EntityAnalysisModel { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class ExhaustiveSearchInstancePromotedTrialInstancePredictedActual
    {
        [Column("Id")]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public double? Predicted { get; set; }
        [Column] [Nullable] [Key(2)] public double? Actual { get; set; }
        [Column] [Nullable] [Key(3)] public int? ExhaustiveSearchInstanceTrialInstanceId { get; set; }
        [Column] [Nullable] [Key(4)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Deleted { get; set; }

        [Association(ThisKey = "ExhaustiveSearchInstanceTrialInstanceId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(6)]
        public ExhaustiveSearchInstanceTrialInstance ExhaustiveSearchInstanceTrialInstance { get; set; }

        [Column] [Nullable] [Key(7)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class ExhaustiveSearchInstancePromotedTrialInstanceRoc
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? TruePositive { get; set; }
        [Column] [Nullable] [Key(2)] public int? TrueNegative { get; set; }
        [Column] [Nullable] [Key(3)] public int? FalsePositive { get; set; }
        [Column] [Nullable] [Key(4)] public int? FalseNegative { get; set; }
        [Column] [Nullable] [Key(5)] public double? Score { get; set; }
        [Column] [Nullable] [Key(6)] public int? ExhaustiveSearchInstanceTrialInstanceId { get; set; }
        [Column] [Nullable] [Key(7)] public double? Threshold { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(9)] public byte? Deleted { get; set; }

        [Association(ThisKey = "ExhaustiveSearchInstanceTrialInstanceId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(11)]
        public ExhaustiveSearchInstanceTrialInstance ExhaustiveSearchInstanceTrialInstance { get; set; }

        [Column] [Nullable] [Key(12)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class ExhaustiveSearchInstancePromotedTrialInstanceSensitivity
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public double? Sensitivity { get; set; }
        [Column] [Nullable] [Key(2)] public int? ExhaustiveSearchInstanceTrialInstanceVariableId { get; set; }
        [Column] [Nullable] [Key(3)] public int? SensitivityRank { get; set; }

        [Column] [Nullable] [Key(4)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Deleted { get; set; }

        [Association(ThisKey = "ExhaustiveSearchInstanceTrialInstanceVariableId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(6)]
        public ExhaustiveSearchInstanceTrialInstanceVariable ExhaustiveSearchInstanceTrialInstanceVariable { get; set; }

        [Column] [Nullable] [Key(7)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class ExhaustiveSearchInstancePromotedTrialInstance
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public double? Score { get; set; }
        [Column] [Nullable] [Key(2)] public int? TopologyComplexity { get; set; }
        [Column] [Nullable] [Key(3)] public int? TruePositive { get; set; }
        [Column] [Nullable] [Key(4)] public int? TrueNegative { get; set; }
        [Column] [Nullable] [Key(5)] public int? FalsePositive { get; set; }
        [Column] [Nullable] [Key(6)] public int? FalseNegative { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(7)]
        public string Json { get; set; }

        [Column] [Nullable] [Key(8)] public int? ExhaustiveSearchInstanceTrialInstanceId { get; set; }
        [Column] [Nullable] [Key(9)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Active { get; set; }

        [Association(ThisKey = "ExhaustiveSearchInstanceTrialInstanceId",
            OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(11)]
        public ExhaustiveSearchInstanceTrialInstance ExhaustiveSearchInstanceTrialInstance { get; set; }

        [Column] [Nullable] [Key(12)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(13)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(14)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class ExhaustiveSearchInstanceTrialInstance
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? ExhaustiveSearchInstanceId { get; set; }
        [Column] [Nullable] [Key(2)] public DateTime? CreatedDate { get; set; }

        [Association(ThisKey = "ExhaustiveSearchInstanceId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(3)]
        public ExhaustiveSearchInstance ExhaustiveSearchInstance { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceTrialInstanceId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(4)]
        public IEnumerable<ExhaustiveSearchInstancePromotedTrialInstance> ExhaustiveSearchInstancePromotedTrialInstance
        {
            get;
            set;
        }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceTrialInstanceId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(5)]
        public IEnumerable<ExhaustiveSearchInstancePromotedTrialInstancePredictedActual>
            ExhaustiveSearchInstancePromotedTrialInstancePredictedActual { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceTrialInstanceId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(6)]
        public IEnumerable<ExhaustiveSearchInstancePromotedTrialInstanceRoc>
            ExhaustiveSearchInstancePromotedTrialInstanceRoc { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceTrialInstanceId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(7)]
        public IEnumerable<ExhaustiveSearchInstanceTrialInstanceTopologyTrial>
            ExhaustiveSearchInstanceTrialInstanceTopologyTrial { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceTrialInstanceId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(8)]
        public IEnumerable<ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial>
            ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceTrialInstanceId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(9)]
        public IEnumerable<ExhaustiveSearchInstanceTrialInstanceSensitivity>
            ExhaustiveSearchInstanceTrialInstanceSensitivity { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceTrialInstanceId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(10)]
        public IEnumerable<ExhaustiveSearchInstanceTrialInstanceVariable> ExhaustiveSearchInstanceTrialInstanceVariable
        {
            get;
            set;
        }

        [Column] [Nullable] [Key(11)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(12)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(13)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? ActivationFunctionId { get; set; }
        [Column] [Nullable] [Key(2)] public int? ExhaustiveSearchInstanceTrialInstanceId { get; set; }
        [Column] [Nullable] [Key(3)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(4)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Deleted { get; set; }

        [Association(ThisKey = "ExhaustiveSearchInstanceTrialInstanceId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(6)]
        public ExhaustiveSearchInstanceTrialInstance ExhaustiveSearchInstanceTrialInstance { get; set; }

        [Column] [Nullable] [Key(7)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class ExhaustiveSearchInstanceData
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? ExhaustiveSearchInstanceId { get; set; }
        [Column] [Nullable] [Key(2)] public double? Dependent { get; set; }
        [Column] [Nullable] [Key(3)] public double? Anomaly { get; set; }
        [Column] [Nullable] [Key(4)] public double[] Independent { get; set; }
        [Column] [Nullable] [Key(5)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(7)] public byte? Deleted { get; set; }

        [Association(ThisKey = "ExhaustiveSearchInstanceId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(8)]
        public ExhaustiveSearchInstance ExhaustiveSearchInstance { get; set; }

        [Column] [Nullable] [Key(9)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class ExhaustiveSearchInstanceTrialInstanceSensitivity
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public double? Sensitivity { get; set; }
        [Column] [Nullable] [Key(2)] public int? ExhaustiveSearchInstanceVariableId { get; set; }
        [Column] [Nullable] [Key(3)] public int? ExhaustiveSearchInstanceTrialInstanceId { get; set; }
        [Column] [Nullable] [Key(4)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Deleted { get; set; }

        [Association(ThisKey = "ExhaustiveSearchInstanceTrialInstanceId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(6)]
        public ExhaustiveSearchInstanceTrialInstance ExhaustiveSearchInstanceTrialInstance { get; set; }

        [Column] [Nullable] [Key(7)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class ExhaustiveSearchInstanceTrialInstanceTopologyTrial
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? ExhaustiveSearchInstanceTrialInstanceId { get; set; }
        [Column] [Nullable] [Key(2)] public int? TrialsSinceImprovement { get; set; }
        [Column] [Nullable] [Key(3)] public int? TopologyTrials { get; set; }
        [Column] [Nullable] [Key(4)] public int? Layer { get; set; }
        [Column] [Nullable] [Key(5)] public int? Neurons { get; set; }
        [Column] [Nullable] [Key(6)] public double? Score { get; set; }
        [Column] [Nullable] [Key(7)] public byte? Finalisation { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(9)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Deleted { get; set; }

        [Association(ThisKey = "ExhaustiveSearchInstanceTrialInstanceId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(19)]
        public ExhaustiveSearchInstanceTrialInstance ExhaustiveSearchInstanceTrialInstance { get; set; }

        [Column] [Nullable] [Key(20)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class ExhaustiveSearchInstanceTrialInstanceVariable
    {
        [Column("Id")]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? ExhaustiveSearchInstanceVariableId { get; set; }
        [Column] [Nullable] [Key(2)] public int? Removed { get; set; }
        [Column] [Nullable] [Key(3)] public int? ExhaustiveSearchInstanceTrialInstanceId { get; set; }
        [Column] [Nullable] [Key(4)] public int? VariableSequence { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceTrialInstanceId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(5)]
        public IEnumerable<ExhaustiveSearchInstancePromotedTrialInstanceVariable>
            ExhaustiveSearchInstancePromotedTrialInstanceVariable { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceTrialInstanceId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(6)]
        public IEnumerable<ExhaustiveSearchInstancePromotedTrialInstanceSensitivity>
            ExhaustiveSearchInstancePromotedTrialInstanceSensitivity { get; set; }

        [Column] [Nullable] [Key(7)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(8)] public byte? Deleted { get; set; }

        [Association(ThisKey = "ExhaustiveSearchInstanceTrialInstanceId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(9)]
        public ExhaustiveSearchInstanceTrialInstance ExhaustiveSearchInstanceTrialInstance { get; set; }

        [Column] [Nullable] [Key(10)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class ExhaustiveSearchInstancePromotedTrialInstanceVariable
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? ExhaustiveSearchInstanceTrialInstanceVariableId { get; set; }
        [Column] [Nullable] [Key(2)] public double? Mean { get; set; }
        [Column] [Nullable] [Key(3)] public double? StandardDeviation { get; set; }
        [Column] [Nullable] [Key(4)] public double? Kurtosis { get; set; }
        [Column] [Nullable] [Key(5)] public double? Skewness { get; set; }
        [Column] [Nullable] [Key(6)] public double? Maximum { get; set; }
        [Column] [Nullable] [Key(7)] public double? Minimum { get; set; }
        [Column] [Nullable] [Key(8)] public double? Iqr { get; set; }
        [Column] [Nullable] [Key(9)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Deleted { get; set; }

        [Association(ThisKey = "ExhaustiveSearchInstanceTrialInstanceVariableId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(19)]
        public ExhaustiveSearchInstanceTrialInstanceVariable ExhaustiveSearchInstanceTrialInstanceVariable { get; set; }

        [Column] [Nullable] [Key(20)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class ExhaustiveSearchInstanceTrialInstanceVariablePrescriptionHistogram
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? ExhaustiveSearchInstancePromotedTrialInstanceVariableId { get; set; }
        [Column] [Nullable] [Key(2)] public int? BinIndex { get; set; }
        [Column] [Nullable] [Key(3)] public double? BinRangeStart { get; set; }
        [Column] [Nullable] [Key(4)] public double? BinRangeEnd { get; set; }
        [Column] [Nullable] [Key(5)] public int? Frequency { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class ExhaustiveSearchInstanceVariable
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? ExhaustiveSearchInstanceId { get; set; }
        [Column] [Nullable] [Key(2)] public string Name { get; set; }
        [Column] [Nullable] [Key(3)] public byte? ProcessingTypeId { get; set; }
        [Column] [Nullable] [Key(4)] public double? Mode { get; set; }
        [Column] [Nullable] [Key(5)] public double? Mean { get; set; }
        [Column] [Nullable] [Key(6)] public double? StandardDeviation { get; set; }
        [Column] [Nullable] [Key(7)] public double? Kurtosis { get; set; }
        [Column] [Nullable] [Key(8)] public double? Skewness { get; set; }
        [Column] [Nullable] [Key(9)] public double? Maximum { get; set; }
        [Column] [Nullable] [Key(10)] public double? Minimum { get; set; }
        [Column] [Nullable] [Key(11)] public double? Iqr { get; set; }
        [Column] [Nullable] [Key(12)] public byte? PrescriptionSimulation { get; set; }
        [Column] [Nullable] [Key(13)] public byte? NormalisationTypeId { get; set; }
        [Column] [Nullable] [Key(14)] public int? DistinctValues { get; set; }
        [Column] [Nullable] [Key(15)] public double? Correlation { get; set; }
        [Column] [Nullable] [Key(16)] public int? CorrelationAbsRank { get; set; }
        [Column] [Nullable] [Key(17)] public int? Bins { get; set; }
        [Column] [Nullable] [Key(18)] public int? VariableSequence { get; set; }

        [Association(ThisKey = "ExhaustiveSearchInstanceId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(19)]
        public ExhaustiveSearchInstance ExhaustiveSearchInstance { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceVariableId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(20)]
        public IEnumerable<ExhaustiveSearchInstanceVariableAnomaly> ExhaustiveSearchInstanceVariableAnomaly { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceVariableId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(21)]
        public IEnumerable<ExhaustiveSearchInstanceVariableClassification> ExhaustiveSearchInstanceVariableClassification
        {
            get;
            set;
        }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceVariableId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(22)]
        public IEnumerable<ExhaustiveSearchInstanceVariableHistogram> ExhaustiveSearchInstanceVariableHistogram
        {
            get;
            set;
        }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceVariableId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(23)]
        public IEnumerable<ExhaustiveSearchInstanceVariableHistogramClassification>
            ExhaustiveSearchInstanceVariableHistogramClassification { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceVariableId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(24)]
        public IEnumerable<ExhaustiveSearchInstanceVariableHistogramAnomaly>
            ExhaustiveSearchInstanceVariableHistogramAnomaly { get; set; }

        [Association(ThisKey = "Id", OtherKey = "ExhaustiveSearchInstanceVariableId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(25)]
        public IEnumerable<ExhaustiveSearchInstanceVariableMultiCollinearity>
            ExhaustiveSearchInstanceVariableMultiCollinearity { get; set; }

        [Column] [Nullable] [Key(26)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(27)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(28)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class ExhaustiveSearchInstanceVariableClassification
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? ExhaustiveSearchInstanceVariableId { get; set; }
        [Column] [Nullable] [Key(2)] public double? Mode { get; set; }
        [Column] [Nullable] [Key(3)] public double? Mean { get; set; }
        [Column] [Nullable] [Key(4)] public double? StandardDeviation { get; set; }
        [Column] [Nullable] [Key(5)] public double? Kurtosis { get; set; }
        [Column] [Nullable] [Key(6)] public double? Skewness { get; set; }
        [Column] [Nullable] [Key(7)] public double? Maximum { get; set; }
        [Column] [Nullable] [Key(8)] public double? Minimum { get; set; }
        [Column] [Nullable] [Key(9)] public double? Iqr { get; set; }
        [Column] [Nullable] [Key(10)] public int? DistinctValues { get; set; }
        [Column] [Nullable] [Key(11)] public int? Bins { get; set; }

        [Column] [Nullable] [Key(12)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(13)] public byte? Deleted { get; set; }

        [Association(ThisKey = "ExhaustiveSearchInstanceVariableId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(14)]
        public ExhaustiveSearchInstanceVariable ExhaustiveSearchInstanceVariable { get; set; }

        [Column] [Nullable] [Key(15)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class ExhaustiveSearchInstanceVariableAnomaly
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? ExhaustiveSearchInstanceVariableId { get; set; }
        [Column] [Nullable] [Key(2)] public double? Mode { get; set; }
        [Column] [Nullable] [Key(3)] public double? Mean { get; set; }
        [Column] [Nullable] [Key(4)] public double? StandardDeviation { get; set; }
        [Column] [Nullable] [Key(5)] public double? Kurtosis { get; set; }
        [Column] [Nullable] [Key(6)] public double? Skewness { get; set; }
        [Column] [Nullable] [Key(7)] public double? Maximum { get; set; }
        [Column] [Nullable] [Key(8)] public double? Minimum { get; set; }
        [Column] [Nullable] [Key(9)] public double? Iqr { get; set; }
        [Column] [Nullable] [Key(10)] public int? DistinctValues { get; set; }
        [Column] [Nullable] [Key(11)] public int? Bins { get; set; }

        [Column] [Nullable] [Key(12)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(13)] public byte? Deleted { get; set; }

        [Association(ThisKey = "ExhaustiveSearchInstanceVariableId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(14)]
        public ExhaustiveSearchInstanceVariable ExhaustiveSearchInstanceVariable { get; set; }

        [Column] [Nullable] [Key(15)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class ExhaustiveSearchInstanceVariableHistogram
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? ExhaustiveSearchInstanceVariableId { get; set; }
        [Column] [Nullable] [Key(2)] public int? BinSequence { get; set; }
        [Column] [Nullable] [Key(3)] public double? BinRangeStart { get; set; }
        [Column] [Nullable] [Key(4)] public double? BinRangeEnd { get; set; }
        [Column] [Nullable] [Key(5)] public int? Frequency { get; set; }

        [Column] [Nullable] [Key(6)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(7)] public byte? Deleted { get; set; }

        [Association(ThisKey = "ExhaustiveSearchInstanceVariableId", OtherKey = "Id",
            CanBeNull = true, Relationship = Relationship.ManyToOne)]
        [Key(8)]
        public ExhaustiveSearchInstanceVariable ExhaustiveSearchInstanceVariable { get; set; }

        [Column] [Nullable] [Key(9)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class ExhaustiveSearchInstanceVariableHistogramClassification
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? ExhaustiveSearchInstanceVariableClassificationId { get; set; }
        [Column] [Nullable] [Key(2)] public int? BinSequence { get; set; }
        [Column] [Nullable] [Key(3)] public double? BinRangeStart { get; set; }
        [Column] [Nullable] [Key(4)] public double? BinRangeEnd { get; set; }
        [Column] [Nullable] [Key(5)] public int? Frequency { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(7)] public byte? Deleted { get; set; }

        [Association(ThisKey = "ExhaustiveSearchInstanceVariableClassificationId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(8)]
        public ExhaustiveSearchInstanceVariableClassification ExhaustiveSearchInstanceVariableClassification { get; set; }

        [Column] [Nullable] [Key(9)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class ExhaustiveSearchInstanceVariableHistogramAnomaly
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? ExhaustiveSearchInstanceVariableAnomalyId { get; set; }
        [Column] [Nullable] [Key(2)] public int? BinSequence { get; set; }
        [Column] [Nullable] [Key(3)] public double? BinRangeStart { get; set; }
        [Column] [Nullable] [Key(4)] public double? BinRangeEnd { get; set; }
        [Column] [Nullable] [Key(5)] public int? Frequency { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(7)] public byte? Deleted { get; set; }

        [Association(ThisKey = "ExhaustiveSearchInstanceVariableAnomalyId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(8)]
        public ExhaustiveSearchInstanceVariableAnomaly ExhaustiveSearchInstanceVariableAnomaly { get; set; }

        [Column] [Nullable] [Key(9)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class ExhaustiveSearchInstanceVariableMultiCollinearity
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? ExhaustiveSearchInstanceVariableId { get; set; }
        [Column] [Nullable] [Key(2)] public int? TestExhaustiveSearchInstanceVariableId { get; set; }
        [Column] [Nullable] [Key(3)] public double? Correlation { get; set; }
        [Column] [Nullable] [Key(4)] public int? CorrelationAbsRank { get; set; }
        [Column] [Nullable] [Key(5)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(6)] public byte? Deleted { get; set; }

        [Association(ThisKey = "ExhaustiveSearchInstanceVariableId", OtherKey = "Id",
            CanBeNull = true, Relationship = Relationship.ManyToOne)]
        [Key(7)]
        public ExhaustiveSearchInstanceVariable ExhaustiveSearchInstanceVariable { get; set; }

        [Association(ThisKey = "TestExhaustiveSearchInstanceVariableId", OtherKey = "Id",
            CanBeNull = true, Relationship = Relationship.ManyToOne)]
        [Key(8)]
        public ExhaustiveSearchInstanceVariable TestExhaustiveSearchInstanceVariable { get; set; }

        [Column] [Nullable] [Key(9)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class HttpProcessingCounter
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public string Instance { get; set; }
        [Column] [Nullable] [Key(2)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(3)] public int? All { get; set; }
        [Column] [Nullable] [Key(4)] public int? Model { get; set; }
        [Column] [Nullable] [Key(5)] public int? AsynchronousModel { get; set; }
        [Column] [Nullable] [Key(6)] public int? Tag { get; set; }
        [Column] [Nullable] [Key(7)] public int? Error { get; set; }
        [Column] [Nullable] [Key(8)] public int? Exhaustive { get; set; }
        [Column] [Nullable] [Key(9)] public int? Sanction { get; set; }
        [Column] [Nullable] [Key(10)] public int? Callback { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class Archive
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(1)]
        public string Json { get; set; }

        [Column] [Nullable] [Key(2)] public Guid EntityAnalysisModelInstanceEntryGuid { get; set; }
        [Column] [Nullable] [Key(3)] public string EntryKeyValue { get; set; }
        [Column] [Nullable] [Key(4)] public double? ResponseElevation { get; set; }
        [Column] [Nullable] [Key(5)] public int? EntityAnalysisModelActivationRuleId { get; set; }
        [Column] [Nullable] [Key(6)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(7)] public int? ActivationRuleCount { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(9)] public DateTime? ReferenceDate { get; set; }
        
        [Association(ThisKey = "EntityAnalysisModelId", OtherKey = "Id", CanBeNull = false,
            Relationship = Relationship.ManyToOne)]
        [Key(10)]
        public EntityAnalysisModel EntityAnalysisModel { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class MockArchive
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column]
        [DataType(DataType.BinaryJson)]
        [Nullable]
        [Key(1)]
        public string Json { get; set; }

        [Column] [Nullable] [Key(2)] public Guid EntityAnalysisModelInstanceEntryGuid { get; set; }
        [Column] [Nullable] [Key(3)] public string EntryKeyValue { get; set; }
        [Column] [Nullable] [Key(4)] public double? ResponseElevation { get; set; }
        [Column] [Nullable] [Key(5)] public int? EntityAnalysisModelActivationRuleId { get; set; }
        [Column] [Nullable] [Key(6)] public int? EntityAnalysisModelId { get; set; }
        [Column] [Nullable] [Key(7)] public int? ActivationRuleCount { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(9)] public DateTime? ReferenceDate { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class PermissionSpecification
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public string Name { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class RoleRegistry
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(2)] public string Name { get; set; }
        [Column] [Nullable] [Key(3)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(4)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(5)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(6)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(8)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(9)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(11)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(12)] public int? TenantRegistryId { get; set; }
        [Column] [Nullable] [Key(13)] public int? Version { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class RoleRegistryVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int RoleRegistryId { get; set; }
        [Column] [Nullable] [Key(2)] public string Name { get; set; }
        [Column] [Nullable] [Key(3)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(4)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(5)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(6)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(8)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(9)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(11)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(12)] public int? TenantRegistryId { get; set; }
        [Column] [Nullable] [Key(13)] public int? Version { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class RoleRegistryPermission
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(2)] public int? PermissionSpecificationId { get; set; }
        [Column] [Nullable] [Key(3)] public int? RoleRegistryId { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(7)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(9)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(11)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(12)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(13)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(14)] public int? Version { get; set; }

        [Association(ThisKey = "PermissionSpecificationId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(15)]
        public PermissionSpecification PermissionSpecification { get; set; }

        [Association(ThisKey = "RoleRegistryId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(16)]
        public RoleRegistry RoleRegistry { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class RoleRegistryPermissionVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int RoleRegistryPermissionId { get; set; }
        [Column] [Nullable] [Key(2)] public int? PermissionSpecificationId { get; set; }
        [Column] [Nullable] [Key(3)] public int? RoleRegistryId { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(7)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(9)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(11)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(12)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(13)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(14)] public int? Version { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class RuleScriptToken
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public string Token { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class SanctionEntrySource
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public string Name { get; set; }
        [Column] [Nullable] [Key(2)] public byte? Severity { get; set; }
        [Column] [Nullable] [Key(3)] public string DirectoryLocation { get; set; }
        [Column] [Nullable] [Key(4)] public char? Delimiter { get; set; }
        [Column] [Nullable] [Key(5)] public string MultiPartStringIndex { get; set; }
        [Column] [Nullable] [Key(6)] public byte? ReferenceIndex { get; set; }
        [Column] [Nullable] [Key(7)] public byte? EnableDirectoryLocation { get; set; }
        [Column] [Nullable] [Key(8)] public byte? EnableHttpLocation { get; set; }
        [Column] [Nullable] [Key(9)] public string HttpLocation { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Skip { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class SanctionEntry
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public string SanctionEntryElementValue { get; set; }
        [Column] [Nullable] [Key(2)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(3)] public int? SanctionEntrySourceId { get; set; }
        [Column] [Nullable] [Key(4)] public string SanctionEntryReference { get; set; }
        [Column] [Nullable] [Key(5)] public string SanctionPayload { get; set; }
        [Column] [Nullable] [Key(6)] public string SanctionEntryHash { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class TenantRegistryVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int TenantRegistryId { get; set; }
        [Column] [Nullable] [Key(2)] public string Name { get; set; }
        [Column] [Nullable] [Key(3)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(4)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(5)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(6)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(8)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(9)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(11)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(12)] public byte? Landlord { get; set; }
        [Column] [Nullable] [Key(13)] public int? Version { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class TenantRegistry
    {
        [Column("Id")]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public string Name { get; set; }
        [Column] [Nullable] [Key(2)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(3)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(4)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(5)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(7)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(8)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(9)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(11)] public byte? Landlord { get; set; }
        [Column] [Nullable] [Key(12)] public int? Version { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class UserLogin
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(2)] public string RemoteIp { get; set; }
        [Column] [Nullable] [Key(3)] public string LocalIp { get; set; }
        [Column] [Nullable] [Key(4)] public string UserAgent { get; set; }
        [Column] [Nullable] [Key(5)] public int FailureTypeId { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(7)] public byte Failed { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class UserInTenant
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public string User { get; set; }
        [Column] [Nullable] [Key(2)] public int TenantRegistryId { get; set; }
        [Column] [Nullable] [Key(3)] public string SwitchedUser { get; set; }
        [Column] [Nullable] [Key(4)] public DateTime SwitchedDate { get; set; }

        [Association(ThisKey = "TenantRegistryId", OtherKey = "Id", CanBeNull = false,
            Relationship = Relationship.ManyToOne)]
        [Key(5)]
        public TenantRegistry TenantRegistry { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class UserInTenantSwitchLog
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int UserInTenantId { get; set; }
        [Column] [Nullable] [Key(2)] public int TenantRegistryId { get; set; }
        [Column] [Nullable] [Key(3)] public string SwitchedUser { get; set; }
        [Column] [Nullable] [Key(4)] public DateTime SwitchedDate { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class VisualisationRegistryVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int VisualisationRegistryId { get; set; }
        [Column] [Nullable] [Key(2)] public string Name { get; set; }
        [Column] [Nullable] [Key(3)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(7)] public byte? ShowInDirectory { get; set; }
        [Column] [Nullable] [Key(8)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(9)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(10)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(11)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(12)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(13)] public int? Columns { get; set; }
        [Column] [Nullable] [Key(14)] public int? ColumnWidth { get; set; }
        [Column] [Nullable] [Key(15)] public int? RowHeight { get; set; }
        [Column] [Nullable] [Key(16)] public int? Version { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class VisualisationRegistry
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public string Name { get; set; }
        [Column] [Nullable] [Key(2)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(3)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(5)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(6)] public byte? ShowInDirectory { get; set; }
        [Column] [Nullable] [Key(7)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(9)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(10)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(11)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(12)] public int? TenantRegistryId { get; set; }
        [Column] [Nullable] [Key(13)] public int? Columns { get; set; }
        [Column] [Nullable] [Key(14)] public int? ColumnWidth { get; set; }
        [Column] [Nullable] [Key(15)] public int? RowHeight { get; set; }
        [Column] [Nullable] [Key(16)] public int? Version { get; set; }

        [Association(ThisKey = "TenantRegistryId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(17)]
        public TenantRegistry TenantRegistry { get; set; }

        [Association(ThisKey = "Id", OtherKey = "VisualisationRegistryId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(18)]
        public IEnumerable<VisualisationRegistryDatasource> VisualisationRegistryDatasource { get; set; }

        [Association(ThisKey = "Id", OtherKey = "VisualisationRegistryId", CanBeNull = true,
            Relationship = Relationship.OneToMany)]
        [Key(19)]
        public IEnumerable<VisualisationRegistryParameter> VisualisationRegistryParameter { get; set; }

        [Column] [Nullable] [Key(20)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(21)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class VisualisationRegistryDatasourceVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? VisualisationRegistryDatasourceId { get; set; }
        [Column] [Nullable] [Key(2)] public int? VisualisationRegistryId { get; set; }
        [Column] [Nullable] [Key(3)] public string Name { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(7)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(9)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(11)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(12)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(13)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(14)] public int? Version { get; set; }
        [Column] [Nullable] [Key(15)] public int? VisualisationTypeId { get; set; }
        [Column] [Nullable] [Key(16)] public string Command { get; set; }
        [Column] [Nullable] [Key(17)] public string VisualisationText { get; set; }
        [Column] [Nullable] [Key(18)] public double? Priority { get; set; }
        [Column] [Nullable] [Key(19)] public byte? IncludeGrid { get; set; }
        [Column] [Nullable] [Key(20)] public byte? IncludeDisplay { get; set; }
        [Column] [Nullable] [Key(21)] public int? ColumnSpan { get; set; }
        [Column] [Nullable] [Key(22)] public int? RowSpan { get; set; }
        [Column] [Nullable] [Key(23)] public Guid Guid { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class VisualisationRegistryDatasource
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? VisualisationRegistryId { get; set; }
        [Column] [Nullable] [Key(2)] public string Name { get; set; }
        [Column] [Nullable] [Key(3)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(5)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(6)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(8)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(9)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(10)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(11)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(12)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(13)] public int? Version { get; set; }
        [Column] [Nullable] [Key(14)] public int? VisualisationTypeId { get; set; }
        [Column] [Nullable] [Key(15)] public string Command { get; set; }
        [Column] [Nullable] [Key(16)] public string VisualisationText { get; set; }
        [Column] [Nullable] [Key(17)] public double? Priority { get; set; }
        [Column] [Nullable] [Key(18)] public byte? IncludeGrid { get; set; }
        [Column] [Nullable] [Key(19)] public byte? IncludeDisplay { get; set; }
        [Column] [Nullable] [Key(20)] public int? ColumnSpan { get; set; }
        [Column] [Nullable] [Key(21)] public int? RowSpan { get; set; }

        [Association(ThisKey = "VisualisationRegistryId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(22)]
        public VisualisationRegistry VisualisationRegistry { get; set; }

        [Column] [Nullable] [Key(23)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(24)] public int? ImportId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class VisualisationRegistryDatasourceSeries
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? VisualisationRegistryDatasourceId { get; set; }
        [Column] [Nullable] [Key(2)] public string Name { get; set; }
        [Column] [Nullable] [Key(3)] public int DataTypeId { get; set; }

        [Association(ThisKey = "VisualisationRegistryDatasourceId", OtherKey = "Id",
            CanBeNull = true, Relationship = Relationship.ManyToOne)]
        [Key(4)]
        public VisualisationRegistryDatasource VisualisationRegistryDatasource { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class VisualisationRegistryDatasourceExecutionLog
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public string Error { get; set; }
        [Column] [Nullable] [Key(2)] public int? Records { get; set; }
        [Column] [Nullable] [Key(3)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(4)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(5)] public int? ResponseTime { get; set; }
        [Column] [Nullable] [Key(6)] public int? VisualisationRegistryDatasourceId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class VisualisationRegistryDatasourceExecutionLogParameter
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? VisualisationRegistryParameterId { get; set; }
        [Column] [Nullable] [Key(2)] public string Value { get; set; }
        [Column] [Nullable] [Key(3)] public int? VisualisationRegistryDatasourceExecutionLogId { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class VisualisationRegistryParameterVersion
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? VisualisationRegistryParameterId { get; set; }
        [Column] [Nullable] [Key(2)] public int? VisualisationRegistryId { get; set; }
        [Column] [Nullable] [Key(3)] public string Name { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(5)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(6)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(7)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(8)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(9)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(10)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(11)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(12)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(13)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(14)] public int? Version { get; set; }
        [Column] [Nullable] [Key(15)] public int? DataTypeId { get; set; }
        [Column] [Nullable] [Key(16)] public string DefaultValue { get; set; }
        [Column] [Nullable] [Key(17)] public byte? Required { get; set; }
        [Column] [Nullable] [Key(18)] public Guid Guid { get; set; }
    }

    [Table]
    [MessagePackObject]
    public class VisualisationRegistryParameter
    {
        [Column]
        [PrimaryKey]
        [Identity]
        [Key(0)]
        public int Id { get; set; }

        [Column] [Nullable] [Key(1)] public int? VisualisationRegistryId { get; set; }
        [Column] [Nullable] [Key(2)] public string Name { get; set; }
        [Column] [Nullable] [Key(3)] public byte? Active { get; set; }
        [Column] [Nullable] [Key(4)] public byte? Locked { get; set; }
        [Column] [Nullable] [Key(5)] public DateTime? CreatedDate { get; set; }
        [Column] [Nullable] [Key(6)] public string CreatedUser { get; set; }
        [Column] [Nullable] [Key(7)] public DateTime? UpdatedDate { get; set; }
        [Column] [Nullable] [Key(8)] public string UpdatedUser { get; set; }
        [Column] [Nullable] [Key(9)] public byte? Deleted { get; set; }
        [Column] [Nullable] [Key(10)] public string DeletedUser { get; set; }
        [Column] [Nullable] [Key(11)] public DateTime? DeletedDate { get; set; }
        [Column] [Nullable] [Key(12)] public int? InheritedId { get; set; }
        [Column] [Nullable] [Key(13)] public int? Version { get; set; }
        [Column] [Nullable] [Key(14)] public int? DataTypeId { get; set; }
        [Column] [Nullable] [Key(15)] public string DefaultValue { get; set; }
        [Column] [Nullable] [Key(16)] public byte? Required { get; set; }

        [Association(ThisKey = "VisualisationRegistryId", OtherKey = "Id", CanBeNull = true,
            Relationship = Relationship.ManyToOne)]
        [Key(17)]
        public VisualisationRegistry VisualisationRegistry { get; set; }

        [Column] [Nullable] [Key(18)] public Guid Guid { get; set; }
        [Column] [Nullable] [Key(19)] public int? ImportId { get; set; }
    }
}
