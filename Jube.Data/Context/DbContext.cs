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

namespace Jube.Data.Context
{
    using LinqToDB;
    using LinqToDB.Configuration;
    using LinqToDB.Data;
    using Poco;

    public class DbContext(LinqToDbConnectionOptions<DbContext> options) : DataConnection(options)
    {
        public ITable<ActivationWatcher> ActivationWatcher
        {
            get
            {
                return GetTable<ActivationWatcher>();
            }
        }
        public ITable<EntityAnalysisModelTag> EntityAnalysisModelTag
        {
            get
            {
                return GetTable<EntityAnalysisModelTag>();
            }
        }
        public ITable<CaseWorkflowFormEntryValue> CaseWorkflowFormEntryValue
        {
            get
            {
                return GetTable<CaseWorkflowFormEntryValue>();
            }
        }
        public ITable<CaseWorkflowFormEntry> CaseWorkflowFormEntry
        {
            get
            {
                return GetTable<CaseWorkflowFormEntry>();
            }
        }
        public ITable<CaseFile> CaseFile
        {
            get
            {
                return GetTable<CaseFile>();
            }
        }
        public ITable<UserLogin> UserLogin
        {
            get
            {
                return GetTable<UserLogin>();
            }
        }
        public ITable<CaseNote> CaseNote
        {
            get
            {
                return GetTable<CaseNote>();
            }
        }
        public ITable<SessionCaseJournal> SessionCaseJournal
        {
            get
            {
                return GetTable<SessionCaseJournal>();
            }
        }

        public ITable<SessionCaseSearchCompiledSql> SessionCaseSearchCompiledSql
        {
            get
            {
                return GetTable<SessionCaseSearchCompiledSql>();
            }
        }

        public ITable<ArchiveEntityAnalysisModelAbstractionEntry> ArchiveEntityAnalysisModelAbstractionEntry
        {
            get
            {
                return GetTable<ArchiveEntityAnalysisModelAbstractionEntry>();
            }
        }

        public ITable<EntityAnalysisModelSearchKeyDistinctValueCalculationInstance>
            EntityAnalysisModelSearchKeyDistinctValueCalculationInstance
        {
            get
            {
                return GetTable<EntityAnalysisModelSearchKeyDistinctValueCalculationInstance>();
            }
        }

        public ITable<EntityAnalysisModelSearchKeyCalculationInstance>
            EntityAnalysisModelSearchKeyCalculationInstance
        {
            get
            {
                return GetTable<EntityAnalysisModelSearchKeyCalculationInstance>();
            }
        }

        public ITable<EntityAnalysisInstance> EntityAnalysisInstance
        {
            get
            {
                return GetTable<EntityAnalysisInstance>();
            }
        }

        public ITable<EntityAnalysisModelInstance> EntityAnalysisModelInstance
        {
            get
            {
                return GetTable<EntityAnalysisModelInstance>();
            }
        }

        public ITable<EntityAnalysisModelSynchronisationNodeStatusEntry>
            EntityAnalysisModelSynchronisationNodeStatusEntry
        {
            get
            {
                return GetTable<EntityAnalysisModelSynchronisationNodeStatusEntry>();
            }
        }

        public ITable<EntityAnalysisModelSynchronisationSchedule> EntityAnalysisModelSynchronisationSchedule
        {
            get
            {
                return GetTable<EntityAnalysisModelSynchronisationSchedule>();
            }
        }

        public ITable<ArchiveKey> ArchiveKey
        {
            get
            {
                return GetTable<ArchiveKey>();
            }
        }
        public ITable<ExhaustiveSearchInstance> ExhaustiveSearchInstance
        {
            get
            {
                return GetTable<ExhaustiveSearchInstance>();
            }
        }
        public ITable<UserRegistry> UserRegistry
        {
            get
            {
                return GetTable<UserRegistry>();
            }
        }
        public ITable<Currency> Currency
        {
            get
            {
                return GetTable<Currency>();
            }
        }

        public ITable<ExhaustiveSearchInstanceVariable> ExhaustiveSearchInstanceVariable
        {
            get
            {
                return GetTable<ExhaustiveSearchInstanceVariable>();
            }
        }

        public ITable<ExhaustiveSearchInstanceVariableClassification> ExhaustiveSearchInstanceVariableClassification
        {
            get
            {
                return GetTable<ExhaustiveSearchInstanceVariableClassification>();
            }
        }

        public ITable<ExhaustiveSearchInstanceTrialInstanceVariable> ExhaustiveSearchInstanceTrialInstanceVariable
        {
            get
            {
                return GetTable<ExhaustiveSearchInstanceTrialInstanceVariable>();
            }
        }

        public ITable<ExhaustiveSearchInstanceTrialInstance> ExhaustiveSearchInstanceTrialInstance
        {
            get
            {
                return GetTable<ExhaustiveSearchInstanceTrialInstance>();
            }
        }

        public ITable<ExhaustiveSearchInstancePromotedTrialInstanceVariable>
            ExhaustiveSearchInstanceTrialInstanceVariablePrescription
        {
            get
            {
                return GetTable<ExhaustiveSearchInstancePromotedTrialInstanceVariable>();
            }
        }

        public ITable<ExhaustiveSearchInstanceTrialInstanceVariablePrescriptionHistogram>
            ExhaustiveSearchInstanceTrialInstanceVariablePrescriptionHistogram
        {
            get
            {
                return GetTable<ExhaustiveSearchInstanceTrialInstanceVariablePrescriptionHistogram>();
            }
        }

        public ITable<ExhaustiveSearchInstancePromotedTrialInstanceSensitivity>
            ExhaustiveSearchInstancePromotedTrialInstanceSensitivity
        {
            get
            {
                return GetTable<ExhaustiveSearchInstancePromotedTrialInstanceSensitivity>();
            }
        }

        public ITable<ExhaustiveSearchInstancePromotedTrialInstanceRoc>
            ExhaustiveSearchInstancePromotedTrialInstanceRoc
        {
            get
            {
                return GetTable<ExhaustiveSearchInstancePromotedTrialInstanceRoc>();
            }
        }

        public ITable<ExhaustiveSearchInstanceVariableHistogram> ExhaustiveSearchInstanceVariableHistogram
        {
            get
            {
                return GetTable<ExhaustiveSearchInstanceVariableHistogram>();
            }
        }

        public ITable<ExhaustiveSearchInstancePromotedTrialInstance> ExhaustiveSearchInstancePromotedTrialInstance
        {
            get
            {
                return GetTable<ExhaustiveSearchInstancePromotedTrialInstance>();
            }
        }

        public ITable<ExhaustiveSearchInstancePromotedTrialInstancePredictedActual>
            ExhaustiveSearchInstancePromotedTrialInstancePredictedActual
        {
            get
            {
                return GetTable<ExhaustiveSearchInstancePromotedTrialInstancePredictedActual>();
            }
        }

        public ITable<ExhaustiveSearchInstanceTrialInstanceTopologyTrial>
            ExhaustiveSearchInstanceTrialInstanceTopologyTrial
        {
            get
            {
                return GetTable<ExhaustiveSearchInstanceTrialInstanceTopologyTrial>();
            }
        }

        public ITable<ExhaustiveSearchInstanceTrialInstanceSensitivity>
            ExhaustiveSearchInstanceTrialInstanceSensitivity
        {
            get
            {
                return GetTable<ExhaustiveSearchInstanceTrialInstanceSensitivity>();
            }
        }

        public ITable<ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial>
            ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial
        {
            get
            {
                return GetTable<ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial>();
            }
        }

        public ITable<ExhaustiveSearchInstanceVariableMultiCollinearity>
            ExhaustiveSearchInstanceVariableMultiCollinearity
        {
            get
            {
                return GetTable<ExhaustiveSearchInstanceVariableMultiCollinearity>();
            }
        }

        public ITable<HttpProcessingCounter> HttpProcessingCounter
        {
            get
            {
                return GetTable<HttpProcessingCounter>();
            }
        }
        public ITable<Archive> Archive
        {
            get
            {
                return GetTable<Archive>();
            }
        }
        public ITable<MockArchive> MockArchive
        {
            get
            {
                return GetTable<MockArchive>();
            }
        }

        public ITable<EntityAnalysisModelProcessingCounter> EntityAnalysisModelProcessingCounter
        {
            get
            {
                return GetTable<EntityAnalysisModelProcessingCounter>();
            }
        }

        public ITable<SanctionEntry> SanctionEntry
        {
            get
            {
                return GetTable<SanctionEntry>();
            }
        }
        public ITable<SanctionEntrySource> SanctionEntrySource
        {
            get
            {
                return GetTable<SanctionEntrySource>();
            }
        }
        public ITable<CaseWorkflow> CaseWorkflow
        {
            get
            {
                return GetTable<CaseWorkflow>();
            }
        }
        public ITable<CaseEvent> CaseEvent
        {
            get
            {
                return GetTable<CaseEvent>();
            }
        }
        public ITable<Case> Case
        {
            get
            {
                return GetTable<Case>();
            }
        }
        public ITable<CaseWorkflowStatus> CaseWorkflowStatus
        {
            get
            {
                return GetTable<CaseWorkflowStatus>();
            }
        }
        public ITable<CaseWorkflowXPath> CaseWorkflowXPath
        {
            get
            {
                return GetTable<CaseWorkflowXPath>();
            }
        }
        public ITable<CaseWorkflowForm> CaseWorkflowForm
        {
            get
            {
                return GetTable<CaseWorkflowForm>();
            }
        }
        public ITable<CaseWorkflowAction> CaseWorkflowAction
        {
            get
            {
                return GetTable<CaseWorkflowAction>();
            }
        }
        public ITable<CaseWorkflowDisplay> CaseWorkflowDisplay
        {
            get
            {
                return GetTable<CaseWorkflowDisplay>();
            }
        }
        public ITable<CaseWorkflowFilter> CaseWorkflowFilter
        {
            get
            {
                return GetTable<CaseWorkflowFilter>();
            }
        }
        public ITable<CaseWorkflowMacro> CaseWorkflowMacro
        {
            get
            {
                return GetTable<CaseWorkflowMacro>();
            }
        }
        public ITable<PermissionSpecification> PermissionSpecification
        {
            get
            {
                return GetTable<PermissionSpecification>();
            }
        }
        public ITable<RoleRegistry> RoleRegistry
        {
            get
            {
                return GetTable<RoleRegistry>();
            }
        }
        public ITable<RoleRegistryPermission> RoleRegistryPermission
        {
            get
            {
                return GetTable<RoleRegistryPermission>();
            }
        }
        public ITable<UserInTenant> UserInTenant
        {
            get
            {
                return GetTable<UserInTenant>();
            }
        }
        public ITable<UserInTenantSwitchLog> UserInTenantSwitchLog
        {
            get
            {
                return GetTable<UserInTenantSwitchLog>();
            }
        }
        public ITable<EntityAnalysisModel> EntityAnalysisModel
        {
            get
            {
                return GetTable<EntityAnalysisModel>();
            }
        }

        public ITable<EntityAnalysisModelGatewayRule> EntityAnalysisModelGatewayRule
        {
            get
            {
                return GetTable<EntityAnalysisModelGatewayRule>();
            }
        }

        public ITable<EntityAnalysisModelActivationRule> EntityAnalysisModelActivationRule
        {
            get
            {
                return GetTable<EntityAnalysisModelActivationRule>();
            }
        }

        public ITable<EntityAnalysisModelSanction> EntityAnalysisModelSanction
        {
            get
            {
                return GetTable<EntityAnalysisModelSanction>();
            }
        }

        public ITable<EntityAnalysisInlineScript> EntityAnalysisInlineScript
        {
            get
            {
                return GetTable<EntityAnalysisInlineScript>();
            }
        }

        public ITable<EntityAnalysisModelListCsvFileUpload> EntityAnalysisModelListCsvFileUpload
        {
            get
            {
                return GetTable<EntityAnalysisModelListCsvFileUpload>();
            }
        }

        public ITable<EntityAnalysisModelDictionaryCsvFileUpload> EntityAnalysisModelDictionaryCsvFileUpload
        {
            get
            {
                return GetTable<EntityAnalysisModelDictionaryCsvFileUpload>();
            }
        }

        public ITable<EntityAnalysisModelInlineFunction> EntityAnalysisModelInlineFunction
        {
            get
            {
                return GetTable<EntityAnalysisModelInlineFunction>();
            }
        }

        public ITable<EntityAnalysisModelRequestXpath> EntityAnalysisModelRequestXpath
        {
            get
            {
                return GetTable<EntityAnalysisModelRequestXpath>();
            }
        }

        public ITable<EntityAnalysisModelTtlCounter> EntityAnalysisModelTtlCounter
        {
            get
            {
                return GetTable<EntityAnalysisModelTtlCounter>();
            }
        }

        public ITable<EntityAnalysisModelAbstractionCalculation> EntityAnalysisModelAbstractionCalculation
        {
            get
            {
                return GetTable<EntityAnalysisModelAbstractionCalculation>();
            }
        }

        public ITable<EntityAnalysisModelHttpAdaptation> EntityAnalysisModelHttpAdaptation
        {
            get
            {
                return GetTable<EntityAnalysisModelHttpAdaptation>();
            }
        }

        public ITable<TenantRegistry> TenantRegistry
        {
            get
            {
                return GetTable<TenantRegistry>();
            }
        }
        public ITable<VisualisationRegistry> VisualisationRegistry
        {
            get
            {
                return GetTable<VisualisationRegistry>();
            }
        }

        public ITable<VisualisationRegistryParameter> VisualisationRegistryParameter
        {
            get
            {
                return GetTable<VisualisationRegistryParameter>();
            }
        }

        public ITable<VisualisationRegistryDatasource> VisualisationRegistryDatasource
        {
            get
            {
                return GetTable<VisualisationRegistryDatasource>();
            }
        }

        public ITable<EntityAnalysisModelDictionary> EntityAnalysisModelDictionary
        {
            get
            {
                return GetTable<EntityAnalysisModelDictionary>();
            }
        }

        public ITable<EntityAnalysisModelReprocessingRule> EntityAnalysisModelReprocessingRule
        {
            get
            {
                return GetTable<EntityAnalysisModelReprocessingRule>();
            }
        }

        public ITable<EntityAnalysisModelReprocessingRuleInstance> EntityAnalysisModelReprocessingRuleInstance
        {
            get
            {
                return GetTable<EntityAnalysisModelReprocessingRuleInstance>();
            }
        }

        public ITable<EntityAnalysisModelList> EntityAnalysisModelList
        {
            get
            {
                return GetTable<EntityAnalysisModelList>();
            }
        }

        public ITable<EntityAnalysisAsynchronousQueueBalance> EntityAnalysisAsynchronousQueueBalance
        {
            get
            {
                return GetTable<EntityAnalysisAsynchronousQueueBalance>();
            }
        }

        public ITable<EntityAnalysisModelListValue> EntityAnalysisModelListValue
        {
            get
            {
                return GetTable<EntityAnalysisModelListValue>();
            }
        }

        public ITable<EntityAnalysisModelSuppression> EntityAnalysisModelSuppression
        {
            get
            {
                return GetTable<EntityAnalysisModelSuppression>();
            }
        }

        public ITable<EntityAnalysisModelActivationRuleSuppression> EntityAnalysisModelActivationRuleSuppression
        {
            get
            {
                return GetTable<EntityAnalysisModelActivationRuleSuppression>();
            }
        }

        public ITable<EntityAnalysisModelDictionaryKvp> EntityAnalysisModelDictionaryKvp
        {
            get
            {
                return GetTable<EntityAnalysisModelDictionaryKvp>();
            }
        }

        public ITable<RuleScriptToken> RuleScriptToken
        {
            get
            {
                return GetTable<RuleScriptToken>();
            }
        }

        public ITable<EntityAnalysisModelAbstractionRule> EntityAnalysisModelAbstractionRule
        {
            get
            {
                return GetTable<EntityAnalysisModelAbstractionRule>();
            }
        }

        public ITable<EntityAnalysisModelInlineScript> EntityAnalysisModelInlineScript
        {
            get
            {
                return GetTable<EntityAnalysisModelInlineScript>();
            }
        }

        public ITable<VisualisationRegistryDatasourceSeries> VisualisationRegistryDatasourceSeries
        {
            get
            {
                return GetTable<VisualisationRegistryDatasourceSeries>();
            }
        }

        public ITable<EntityAnalysisModelAsynchronousQueueBalance> EntityAnalysisModelAsynchronousQueueBalance
        {
            get
            {
                return GetTable<EntityAnalysisModelAsynchronousQueueBalance>();
            }
        }

        public ITable<EntityAnalysisModelRequestXpathVersion> EntityAnalysisModelRequestXpathVersion
        {
            get
            {
                return GetTable<EntityAnalysisModelRequestXpathVersion>();
            }
        }

        public ITable<EntityAnalysisModelInlineFunctionVersion> EntityAnalysisModelInlineFunctionVersion
        {
            get
            {
                return GetTable<EntityAnalysisModelInlineFunctionVersion>();
            }
        }

        public ITable<EntityAnalysisModelInlineScriptVersion> EntityAnalysisModelInlineScriptVersion
        {
            get
            {
                return GetTable<EntityAnalysisModelInlineScriptVersion>();
            }
        }

        public ITable<EntityAnalysisModelGatewayRuleVersion> EntityAnalysisModelGatewayRuleVersion
        {
            get
            {
                return GetTable<EntityAnalysisModelGatewayRuleVersion>();
            }
        }

        public ITable<EntityAnalysisModelTagVersion> EntityAnalysisModelTagVersion
        {
            get
            {
                return GetTable<EntityAnalysisModelTagVersion>();
            }
        }

        public ITable<EntityAnalysisModelSanctionVersion> EntityAnalysisModelSanctionVersion
        {
            get
            {
                return GetTable<EntityAnalysisModelSanctionVersion>();
            }
        }

        public ITable<EntityAnalysisModelAbstractionRuleVersion> EntityAnalysisModelAbstractionRuleVersion
        {
            get
            {
                return GetTable<EntityAnalysisModelAbstractionRuleVersion>();
            }
        }

        public ITable<EntityAnalysisModelAbstractionCalculationVersion>
            EntityAnalysisModelAbstractionCalculationVersion
        {
            get
            {
                return GetTable<EntityAnalysisModelAbstractionCalculationVersion>();
            }
        }

        public ITable<EntityAnalysisModelHttpAdaptationVersion> EntityAnalysisModelHttpAdaptationVersion
        {
            get
            {
                return GetTable<EntityAnalysisModelHttpAdaptationVersion>();
            }
        }

        public ITable<EntityAnalysisModelActivationRuleVersion> EntityAnalysisModelActivationRuleVersion
        {
            get
            {
                return GetTable<EntityAnalysisModelActivationRuleVersion>();
            }
        }

        public ITable<EntityAnalysisModelListVersion> EntityAnalysisModelListVersion
        {
            get
            {
                return GetTable<EntityAnalysisModelListVersion>();
            }
        }

        public ITable<EntityAnalysisModelDictionaryVersion> EntityAnalysisModelDictionaryVersion
        {
            get
            {
                return GetTable<EntityAnalysisModelDictionaryVersion>();
            }
        }

        public ITable<CaseWorkflowXPathVersion> CaseWorkflowXPathVersion
        {
            get
            {
                return GetTable<CaseWorkflowXPathVersion>();
            }
        }

        public ITable<CaseWorkflowFormVersion> CaseWorkflowFormVersion
        {
            get
            {
                return GetTable<CaseWorkflowFormVersion>();
            }
        }

        public ITable<CaseWorkflowActionVersion> CaseWorkflowActionVersion
        {
            get
            {
                return GetTable<CaseWorkflowActionVersion>();
            }
        }

        public ITable<CaseWorkflowDisplayVersion> CaseWorkflowDisplayVersion
        {
            get
            {
                return GetTable<CaseWorkflowDisplayVersion>();
            }
        }

        public ITable<CaseWorkflowMacroVersion> CaseWorkflowMacroVersion
        {
            get
            {
                return GetTable<CaseWorkflowMacroVersion>();
            }
        }

        public ITable<CaseWorkflowFilterVersion> CaseWorkflowFilterVersion
        {
            get
            {
                return GetTable<CaseWorkflowFilterVersion>();
            }
        }

        public ITable<EntityAnalysisModelListValueVersion> EntityAnalysisModelListValueVersion
        {
            get
            {
                return GetTable<EntityAnalysisModelListValueVersion>();
            }
        }

        public ITable<EntityAnalysisModelDictionaryKvpVersion> EntityAnalysisModelDictionaryKvpVersion
        {
            get
            {
                return GetTable<EntityAnalysisModelDictionaryKvpVersion>();
            }
        }

        public ITable<VisualisationRegistryDatasourceVersion> VisualisationRegistryDatasourceVersion
        {
            get
            {
                return GetTable<VisualisationRegistryDatasourceVersion>();
            }
        }

        public ITable<VisualisationRegistryParameterVersion> VisualisationRegistryParameterVersion
        {
            get
            {
                return GetTable<VisualisationRegistryParameterVersion>();
            }
        }

        public ITable<ExhaustiveSearchInstanceVariableAnomaly> ExhaustiveSearchInstanceVariableAnomaly
        {
            get
            {
                return GetTable<ExhaustiveSearchInstanceVariableAnomaly>();
            }
        }

        public ITable<ExhaustiveSearchInstanceVariableHistogramClassification>
            ExhaustiveSearchInstanceVariableHistogramClassification
        {
            get
            {
                return GetTable<ExhaustiveSearchInstanceVariableHistogramClassification>();
            }
        }

        public ITable<ExhaustiveSearchInstanceVariableHistogramAnomaly> ExhaustiveSearchInstanceVariableHistogramAnomaly
        {
            get
            {
                return GetTable<ExhaustiveSearchInstanceVariableHistogramAnomaly>();
            }
        }

        public ITable<ExhaustiveSearchInstancePromotedTrialInstanceVariable>
            ExhaustiveSearchInstancePromotedTrialInstanceVariable
        {
            get
            {
                return GetTable<ExhaustiveSearchInstancePromotedTrialInstanceVariable>();
            }
        }

        public ITable<ExhaustiveSearchInstanceData> ExhaustiveSearchInstanceData
        {
            get
            {
                return GetTable<ExhaustiveSearchInstanceData>();
            }
        }

        public ITable<Import> Import
        {
            get
            {
                return GetTable<Import>();
            }
        }

        public ITable<Export> Export
        {
            get
            {
                return GetTable<Export>();
            }
        }

        public ITable<ExportPeek> ExportPeek
        {
            get
            {
                return GetTable<ExportPeek>();
            }
        }

        public ITable<LocalCacheInstance> LocalCacheInstance
        {
            get
            {
                return GetTable<LocalCacheInstance>();
            }
        }

        public ITable<LocalCacheInstanceKey> LocalCacheInstanceKey
        {
            get
            {
                return GetTable<LocalCacheInstanceKey>();
            }
        }

        public ITable<LocalCacheInstanceLru> LocalCacheInstanceLru
        {
            get
            {
                return GetTable<LocalCacheInstanceLru>();
            }
        }
    }
}
