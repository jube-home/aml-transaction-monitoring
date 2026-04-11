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

namespace Jube.Preservation
{
    using Cryptography;
    using Data.Context;
    using Data.Poco;
    using Data.Repository;
    using MessagePack;
    using Models;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.NamingConventions;

    public class Preservation(DbContext dbContext, string userName, string? salt = null, bool legacyFallbackEnabled = false)
    {
        private readonly string salt = salt ?? "";

        public async Task ImportAsync(byte[] bytes, ImportExportOptions options, CancellationToken token = default)
        {
            var importRepository = new ImportRepository(dbContext, userName);
            var import = new Import
            {
                Bytes = bytes,
                CreatedDate = DateTime.Now,
                Guid = Guid.NewGuid()
            };

            import = await importRepository.InsertAsync(import, token);

            try
            {
                var aesEncryption = new AesEncryption(options.Password ?? "", salt, legacyFallbackEnabled);
                var decryptedBytes = aesEncryption.Decrypt(bytes);

                var lz4Options =
                    MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);
                var wrapper = MessagePackSerializer.Deserialize<Wrapper>(decryptedBytes, lz4Options);

                await dbContext.BeginTransactionAsync(token).ConfigureAwait(false);

                var entityAnalysisModelRepository = new EntityAnalysisModelRepository(dbContext, import.TenantRegistryId);
                await entityAnalysisModelRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var entityAnalysisModelRequestXPathRepository =
                    new EntityAnalysisModelRequestXPathRepository(dbContext, import.TenantRegistryId);
                await entityAnalysisModelRequestXPathRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var entityAnalysisModelInlineFunctionRepository =
                    new EntityAnalysisModelInlineFunctionRepository(dbContext, import.TenantRegistryId);
                await entityAnalysisModelInlineFunctionRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var entityAnalysisModelInlineScriptRepository =
                    new EntityAnalysisModelInlineScriptRepository(dbContext, import.TenantRegistryId);
                await entityAnalysisModelInlineScriptRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var entityAnalysisModelGatewayRuleRepository =
                    new EntityAnalysisModelGatewayRuleRepository(dbContext, import.TenantRegistryId);
                await entityAnalysisModelGatewayRuleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var entityAnalysisModelSanctionRepository =
                    new EntityAnalysisModelSanctionRepository(dbContext, import.TenantRegistryId);
                await entityAnalysisModelSanctionRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var entityAnalysisModelTagRepository =
                    new EntityAnalysisModelTagRepository(dbContext, import.TenantRegistryId);
                await entityAnalysisModelTagRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var entityAnalysisModelTtlCounterRepository =
                    new EntityAnalysisModelTtlCounterRepository(dbContext, import.TenantRegistryId);
                await entityAnalysisModelTtlCounterRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var entityAnalysisModelAbstractionRuleRepository =
                    new EntityAnalysisModelAbstractionRuleRepository(dbContext, import.TenantRegistryId);
                await entityAnalysisModelAbstractionRuleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var entityAnalysisModelAbstractionCalculationRepository =
                    new EntityAnalysisModelAbstractionCalculationRepository(dbContext, import.TenantRegistryId);
                await entityAnalysisModelAbstractionCalculationRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId,
                    import.Id, token);

                var entityAnalysisModelHttpAdaptationRepository =
                    new EntityAnalysisModelHttpAdaptationRepository(dbContext, import.TenantRegistryId);
                await entityAnalysisModelHttpAdaptationRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var entityAnalysisModelActivationRuleRepository =
                    new EntityAnalysisModelActivationRuleRepository(dbContext, import.TenantRegistryId);
                await entityAnalysisModelActivationRuleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var caseWorkflowRepository = new CaseWorkflowRepository(dbContext, import.TenantRegistryId);
                await caseWorkflowRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var caseWorkflowXPathRepository = new CaseWorkflowXPathRepository(dbContext, import.TenantRegistryId);
                await caseWorkflowXPathRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var caseWorkflowStatusRepository = new CaseWorkflowStatusRepository(dbContext, import.TenantRegistryId);
                await caseWorkflowStatusRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var caseWorkflowFormRepository = new CaseWorkflowFormRepository(dbContext, import.TenantRegistryId);
                await caseWorkflowFormRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var caseWorkflowActionRepository = new CaseWorkflowActionRepository(dbContext, import.TenantRegistryId);
                await caseWorkflowActionRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var caseWorkflowDisplayRepository = new CaseWorkflowDisplayRepository(dbContext, import.TenantRegistryId);
                await caseWorkflowDisplayRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var caseWorkflowMacro = new CaseWorkflowMacroRepository(dbContext, import.TenantRegistryId);
                await caseWorkflowMacro.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var caseWorkflowFilterRepository = new CaseWorkflowFilterRepository(dbContext, import.TenantRegistryId);
                await caseWorkflowFilterRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var entityAnalysisModelSuppressionRepository =
                    new EntityAnalysisModelSuppressionRepository(dbContext, import.TenantRegistryId);
                await entityAnalysisModelSuppressionRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId,
                    import.Id, token);

                var entityAnalysisModelActivationRuleSuppressionRepository =
                    new EntityAnalysisModelActivationRuleSuppressionRepository(dbContext,
                        import.TenantRegistryId);
                await entityAnalysisModelActivationRuleSuppressionRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId,
                    import.Id, token);

                var exhaustiveSearchInstanceRepository =
                    new ExhaustiveSearchInstanceRepository(dbContext, import.TenantRegistryId);
                await exhaustiveSearchInstanceRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstanceDataRepository =
                    new ExhaustiveSearchInstanceDataRepository(dbContext);
                await exhaustiveSearchInstanceDataRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId,
                    import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstanceTrialInstanceRepository =
                    new ExhaustiveSearchInstanceTrialInstanceRepository(dbContext);
                await exhaustiveSearchInstanceTrialInstanceRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId,
                    import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstanceVariableRepository =
                    new ExhaustiveSearchInstanceVariableRepository(dbContext);
                await exhaustiveSearchInstanceVariableRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId,
                    import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstanceTrialInstanceVariableRepository =
                    new ExhaustiveSearchInstanceTrialInstanceVariableRepository(dbContext);
                await exhaustiveSearchInstanceTrialInstanceVariableRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId,
                    import.Id, token);

                var exhaustiveSearchInstancePromotedTrialInstanceRepository =
                    new ExhaustiveSearchInstancePromotedTrialInstanceRepository(dbContext,
                        import.TenantRegistryId);
                await exhaustiveSearchInstancePromotedTrialInstanceRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId,
                    import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository =
                    new ExhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository(dbContext);
                await exhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstancePromotedTrialInstanceRocRepository =
                    new ExhaustiveSearchInstancePromotedTrialInstanceRocRepository(dbContext);
                await exhaustiveSearchInstancePromotedTrialInstanceRocRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId,
                    import.Id, token);

                var exhaustiveSearchInstanceTrialInstanceTopologyTrialRepository =
                    new ExhaustiveSearchInstanceTrialInstanceTopologyTrialRepository(dbContext);
                await exhaustiveSearchInstanceTrialInstanceTopologyTrialRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId,
                    import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstanceTrialInstanceSensitivityRepository =
                    new ExhaustiveSearchInstanceTrialInstanceSensitivityRepository(dbContext);
                await exhaustiveSearchInstanceTrialInstanceSensitivityRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId,
                    import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstanceTrialInstanceActivationFunctionTrialRepository =
                    new ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrialRepository(dbContext);
                await exhaustiveSearchInstanceTrialInstanceActivationFunctionTrialRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository =
                    new ExhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository(dbContext);
                await exhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstancePromotedTrialInstanceVariableRepository =
                    new ExhaustiveSearchInstancePromotedTrialInstanceVariableRepository(dbContext);
                await exhaustiveSearchInstancePromotedTrialInstanceVariableRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstanceVariableAnomalyRepository =
                    new ExhaustiveSearchInstanceVariableAnomalyRepository(dbContext);
                await exhaustiveSearchInstanceVariableAnomalyRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId,
                    import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstanceVariableClassificationRepository =
                    new ExhaustiveSearchInstanceVariableClassificationRepository(dbContext);
                await exhaustiveSearchInstanceVariableClassificationRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId,
                    import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstanceVariableHistogramRepository =
                    new ExhaustiveSearchInstanceVariableHistogramRepository(dbContext);
                await exhaustiveSearchInstanceVariableHistogramRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId,
                    import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstanceVariableHistogramClassificationRepository =
                    new ExhaustiveSearchInstanceVariableHistogramClassificationRepository(dbContext);
                await exhaustiveSearchInstanceVariableHistogramClassificationRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId, import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstanceVariableHistogramAnomalyRepository =
                    new ExhaustiveSearchInstanceVariableHistogramAnomalyRepository(dbContext);
                await exhaustiveSearchInstanceVariableHistogramAnomalyRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId,
                    import.Id, token).ConfigureAwait(false);

                var exhaustiveSearchInstanceVariableMulticollinearityRepository =
                    new ExhaustiveSearchInstanceVariableMultiColiniarityRepository(dbContext);
                await exhaustiveSearchInstanceVariableMulticollinearityRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(
                    import.TenantRegistryId,
                    import.Id, token).ConfigureAwait(false);

                var entityAnalysisModelListRepository =
                    new EntityAnalysisModelListRepository(dbContext, import.TenantRegistryId);
                await entityAnalysisModelListRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var entityAnalysisModelListValueRepository =
                    new EntityAnalysisModelListValueRepository(dbContext, import.TenantRegistryId);
                await entityAnalysisModelListValueRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId,
                    import.Id, token);

                var entityAnalysisModelDictionaryRepository =
                    new EntityAnalysisModelDictionaryRepository(dbContext, import.TenantRegistryId);
                await entityAnalysisModelDictionaryRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId,
                    import.Id, token);

                var entityAnalysisModelDictionaryKvpRepository =
                    new EntityAnalysisModelDictionaryKvpRepository(dbContext, import.TenantRegistryId);
                await entityAnalysisModelDictionaryKvpRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId,
                    import.Id, token);

                var visualisationRegistryRepository =
                    new VisualisationRegistryRepository(dbContext, import.TenantRegistryId);
                await visualisationRegistryRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var visualisationRegistryDatasourceRepository =
                    new VisualisationRegistryDatasourceRepository(dbContext, import.TenantRegistryId);
                await visualisationRegistryDatasourceRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var visualisationRegistryParameterRepository =
                    new VisualisationRegistryParameterRepository(dbContext, import.TenantRegistryId);
                await visualisationRegistryParameterRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var caseWorkflowRoleRepository = new CaseWorkflowRoleRepository(dbContext, import.TenantRegistryId);
                await caseWorkflowRoleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var caseWorkflowActionRoleRepository = new CaseWorkflowActionRoleRepository(dbContext, import.TenantRegistryId);
                await caseWorkflowActionRoleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var caseWorkflowDisplayRoleRepository = new CaseWorkflowDisplayRoleRepository(dbContext, import.TenantRegistryId);
                await caseWorkflowDisplayRoleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var caseWorkflowFilterRoleRepository = new CaseWorkflowFilterRoleRepository(dbContext, import.TenantRegistryId);
                await caseWorkflowFilterRoleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var caseWorkflowFormRoleRepository = new CaseWorkflowFormRoleRepository(dbContext, import.TenantRegistryId);
                await caseWorkflowFormRoleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var caseWorkflowMacroRoleRepository = new CaseWorkflowMacroRoleRepository(dbContext, import.TenantRegistryId);
                await caseWorkflowMacroRoleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var caseWorkflowStatusRoleRepository = new CaseWorkflowStatusRoleRepository(dbContext, import.TenantRegistryId);
                await caseWorkflowStatusRoleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var caseWorkflowXPathRoleRepository = new CaseWorkflowXPathRoleRepository(dbContext, import.TenantRegistryId);
                await caseWorkflowXPathRoleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var visualisationRegistryRoleRepository = new VisualisationRegistryRoleRepository(dbContext, import.TenantRegistryId);
                await visualisationRegistryRoleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);
                
                var entityAnalysisModelRoleRepository = new EntityAnalysisModelRoleRepository(dbContext, import.TenantRegistryId);
                await entityAnalysisModelRoleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);
                
                var visualisationRegistryDatasourceRoleRepository = new VisualisationRegistryDatasourceRoleRepository(dbContext, import.TenantRegistryId);
                await visualisationRegistryDatasourceRoleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                var visualisationRegistryParameterRoleRepository = new VisualisationRegistryParameterRoleRepository(dbContext, import.TenantRegistryId);
                await visualisationRegistryParameterRoleRepository.DeleteByTenantRegistryIdOutsideOfInstanceAsync(import.TenantRegistryId, import.Id, token);

                if (wrapper.Payload?.EntityAnalysisModel != null)
                {
                    foreach (var oldEntityAnalysisModel in wrapper.Payload.EntityAnalysisModel)
                    {
                        var newEntityAnalysisModel = await entityAnalysisModelRepository.InsertAsync(oldEntityAnalysisModel, token);

                        foreach (var entityAnalysisModelRequestXpath in oldEntityAnalysisModel
                                     .EntityAnalysisModelRequestXpath)
                        {
                            entityAnalysisModelRequestXpath.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            await entityAnalysisModelRequestXPathRepository.InsertAsync(entityAnalysisModelRequestXpath, token);
                        }

                        if (options.Suppressions)
                        {
                            if (oldEntityAnalysisModel.EntityAnalysisModelSuppression != null)
                            {
                                foreach (var entityAnalysisModelSuppression in oldEntityAnalysisModel.EntityAnalysisModelSuppression)
                                {
                                    await entityAnalysisModelSuppressionRepository.InsertAsync(entityAnalysisModelSuppression, token);
                                }

                                foreach (var entityAnalysisModelActivationRuleSuppression in oldEntityAnalysisModel
                                             .EntityAnalysisModelActivationRuleSuppression)
                                {
                                    await entityAnalysisModelActivationRuleSuppressionRepository.InsertAsync(
                                        entityAnalysisModelActivationRuleSuppression, token);
                                }
                            }
                        }

                        foreach (var entityAnalysisModelInlineFunction in oldEntityAnalysisModel
                                     .EntityAnalysisModelInlineFunction)
                        {
                            entityAnalysisModelInlineFunction.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            await entityAnalysisModelInlineFunctionRepository.InsertAsync(entityAnalysisModelInlineFunction, token);
                        }

                        foreach (var entityAnalysisModelInlineScript in oldEntityAnalysisModel
                                     .EntityAnalysisModelInlineScript)
                        {
                            entityAnalysisModelInlineScript.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            await entityAnalysisModelInlineScriptRepository.InsertAsync(entityAnalysisModelInlineScript, token);
                        }

                        foreach (var entityAnalysisModelGatewayRule in oldEntityAnalysisModel
                                     .EntityAnalysisModelGatewayRule)
                        {
                            entityAnalysisModelGatewayRule.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            await entityAnalysisModelGatewayRuleRepository.InsertAsync(entityAnalysisModelGatewayRule, token);
                        }

                        foreach (var entityAnalysisModelSanction in oldEntityAnalysisModel
                                     .EntityAnalysisModelSanction)
                        {
                            entityAnalysisModelSanction.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            await entityAnalysisModelSanctionRepository.InsertAsync(entityAnalysisModelSanction, token);
                        }

                        foreach (var entityAnalysisModelTag in oldEntityAnalysisModel
                                     .EntityAnalysisModelTag)
                        {
                            entityAnalysisModelTag.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            await entityAnalysisModelTagRepository.InsertAsync(entityAnalysisModelTag, token);
                        }

                        foreach (var entityAnalysisModelTtlCounter in oldEntityAnalysisModel
                                     .EntityAnalysisModelTtlCounter)
                        {
                            entityAnalysisModelTtlCounter.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            await entityAnalysisModelTtlCounterRepository.InsertAsync(entityAnalysisModelTtlCounter, token);
                        }

                        foreach (var entityAnalysisModelAbstractionRule in oldEntityAnalysisModel
                                     .EntityAnalysisModelAbstractionRule)
                        {
                            entityAnalysisModelAbstractionRule.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            await entityAnalysisModelAbstractionRuleRepository.InsertAsync(entityAnalysisModelAbstractionRule, token);
                        }

                        foreach (var entityAnalysisModelAbstractionCalculations in oldEntityAnalysisModel
                                     .EntityAnalysisModelAbstractionCalculation)
                        {
                            entityAnalysisModelAbstractionCalculations.EntityAnalysisModelId =
                                newEntityAnalysisModel.Id;
                            await entityAnalysisModelAbstractionCalculationRepository.InsertAsync(
                                entityAnalysisModelAbstractionCalculations, token);
                        }

                        foreach (var entityAnalysisModelHttpAdaptation in oldEntityAnalysisModel
                                     .EntityAnalysisModelHttpAdaptation)
                        {
                            entityAnalysisModelHttpAdaptation.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            await entityAnalysisModelHttpAdaptationRepository.InsertAsync(entityAnalysisModelHttpAdaptation, token);
                        }

                        if (options.Exhaustive)
                        {
                            if (oldEntityAnalysisModel.ExhaustiveSearchInstance != null)
                            {
                                foreach (var entityAnalysisModelExhaustiveSearchInstance in oldEntityAnalysisModel.ExhaustiveSearchInstance)
                                {
                                    entityAnalysisModelExhaustiveSearchInstance.EntityAnalysisModelId =
                                        newEntityAnalysisModel.Id;

                                    var entityAnalysisModelExhaustiveSearchInstanceId = (await exhaustiveSearchInstanceRepository
                                        .InsertAsync(entityAnalysisModelExhaustiveSearchInstance, token).ConfigureAwait(false)).Id;

                                    foreach (var exhaustiveSearchInstanceData in entityAnalysisModelExhaustiveSearchInstance
                                                 .ExhaustiveSearchInstanceData)
                                    {
                                        exhaustiveSearchInstanceData.ExhaustiveSearchInstanceId =
                                            entityAnalysisModelExhaustiveSearchInstanceId;

                                        await exhaustiveSearchInstanceDataRepository.InsertAsync(
                                            exhaustiveSearchInstanceData, token).ConfigureAwait(false);
                                    }

                                    foreach (var exhaustiveSearchInstanceTrialInstance in
                                             entityAnalysisModelExhaustiveSearchInstance
                                                 .ExhaustiveSearchInstanceTrialInstance)
                                    {
                                        exhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstanceId =
                                            entityAnalysisModelExhaustiveSearchInstanceId;

                                        var exhaustiveSearchInstanceTrialInstanceId =
                                            (await exhaustiveSearchInstanceTrialInstanceRepository.InsertAsync(
                                                exhaustiveSearchInstanceTrialInstance, token).ConfigureAwait(false)).Id;

                                        foreach (var exhaustiveSearchInstanceTrialInstanceVariable in
                                                 exhaustiveSearchInstanceTrialInstance
                                                     .ExhaustiveSearchInstanceTrialInstanceVariable)
                                        {
                                            exhaustiveSearchInstanceTrialInstanceVariable
                                                    .ExhaustiveSearchInstanceTrialInstanceId =
                                                exhaustiveSearchInstanceTrialInstanceId;

                                            var exhaustiveSearchInstanceTrialInstanceVariableId =
                                                (await exhaustiveSearchInstanceTrialInstanceVariableRepository.InsertAsync(
                                                    exhaustiveSearchInstanceTrialInstanceVariable, token).ConfigureAwait(false)).Id;

                                            foreach (var exhaustiveSearchInstancePromotedTrialInstanceSensitivity in
                                                     exhaustiveSearchInstanceTrialInstanceVariable
                                                         .ExhaustiveSearchInstancePromotedTrialInstanceSensitivity)
                                            {
                                                exhaustiveSearchInstancePromotedTrialInstanceSensitivity
                                                        .ExhaustiveSearchInstanceTrialInstanceVariableId =
                                                    exhaustiveSearchInstanceTrialInstanceVariableId;

                                                await exhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository.InsertAsync(
                                                    exhaustiveSearchInstancePromotedTrialInstanceSensitivity, token).ConfigureAwait(false);
                                            }

                                            foreach (var exhaustiveSearchInstancePromotedTrialInstanceVariable in
                                                     exhaustiveSearchInstanceTrialInstanceVariable
                                                         .ExhaustiveSearchInstancePromotedTrialInstanceVariable)
                                            {
                                                exhaustiveSearchInstancePromotedTrialInstanceVariable
                                                        .ExhaustiveSearchInstanceTrialInstanceVariableId =
                                                    exhaustiveSearchInstanceTrialInstanceVariableId;

                                                await exhaustiveSearchInstancePromotedTrialInstanceVariableRepository.InsertAsync(
                                                    exhaustiveSearchInstancePromotedTrialInstanceVariable, token).ConfigureAwait(false);
                                            }
                                        }

                                        foreach (var exhaustiveSearchInstancePromotedTrialInstance in
                                                 exhaustiveSearchInstanceTrialInstance
                                                     .ExhaustiveSearchInstancePromotedTrialInstance)
                                        {
                                            exhaustiveSearchInstancePromotedTrialInstance
                                                    .ExhaustiveSearchInstanceTrialInstanceId =
                                                exhaustiveSearchInstanceTrialInstanceId;

                                            await exhaustiveSearchInstancePromotedTrialInstanceRepository.InsertAsync(
                                                exhaustiveSearchInstancePromotedTrialInstance, token).ConfigureAwait(false);
                                        }

                                        foreach (var exhaustiveSearchInstancePromotedTrialInstancePredictedActual in
                                                 exhaustiveSearchInstanceTrialInstance
                                                     .ExhaustiveSearchInstancePromotedTrialInstancePredictedActual)
                                        {
                                            exhaustiveSearchInstancePromotedTrialInstancePredictedActual
                                                    .ExhaustiveSearchInstanceTrialInstanceId =
                                                exhaustiveSearchInstanceTrialInstanceId;

                                            await exhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository.InsertAsync(
                                                exhaustiveSearchInstancePromotedTrialInstancePredictedActual, token).ConfigureAwait(false);
                                        }

                                        foreach (var exhaustiveSearchInstancePromotedTrialInstanceRoc in
                                                 exhaustiveSearchInstanceTrialInstance
                                                     .ExhaustiveSearchInstancePromotedTrialInstanceRoc)
                                        {
                                            exhaustiveSearchInstancePromotedTrialInstanceRoc
                                                    .ExhaustiveSearchInstanceTrialInstanceId =
                                                exhaustiveSearchInstanceTrialInstanceId;

                                            await exhaustiveSearchInstancePromotedTrialInstanceRocRepository.InsertAsync(
                                                exhaustiveSearchInstancePromotedTrialInstanceRoc, token).ConfigureAwait(false);
                                        }

                                        foreach (var exhaustiveSearchInstanceTrialInstanceTopologyTrial in
                                                 exhaustiveSearchInstanceTrialInstance
                                                     .ExhaustiveSearchInstanceTrialInstanceTopologyTrial)
                                        {
                                            exhaustiveSearchInstanceTrialInstanceTopologyTrial
                                                    .ExhaustiveSearchInstanceTrialInstanceId =
                                                exhaustiveSearchInstanceTrialInstanceId;

                                            await exhaustiveSearchInstanceTrialInstanceTopologyTrialRepository.InsertAsync(
                                                exhaustiveSearchInstanceTrialInstanceTopologyTrial, token).ConfigureAwait(false);
                                        }

                                        foreach (var exhaustiveSearchInstanceTrialInstanceSensitivity in
                                                 exhaustiveSearchInstanceTrialInstance
                                                     .ExhaustiveSearchInstanceTrialInstanceSensitivity)
                                        {
                                            exhaustiveSearchInstanceTrialInstanceSensitivity
                                                    .ExhaustiveSearchInstanceTrialInstanceId =
                                                exhaustiveSearchInstanceTrialInstanceId;

                                            await exhaustiveSearchInstanceTrialInstanceSensitivityRepository.InsertAsync(
                                                exhaustiveSearchInstanceTrialInstanceSensitivity, token).ConfigureAwait(false);
                                        }

                                        foreach (var exhaustiveSearchInstanceTrialInstanceActivationFunctionTrial in
                                                 exhaustiveSearchInstanceTrialInstance
                                                     .ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial)
                                        {
                                            exhaustiveSearchInstanceTrialInstanceActivationFunctionTrial
                                                    .ExhaustiveSearchInstanceTrialInstanceId =
                                                exhaustiveSearchInstanceTrialInstanceId;

                                            await exhaustiveSearchInstanceTrialInstanceActivationFunctionTrialRepository.InsertAsync(
                                                exhaustiveSearchInstanceTrialInstanceActivationFunctionTrial, token).ConfigureAwait(false);
                                        }
                                    }

                                    foreach (var exhaustiveSearchInstanceVariable in
                                             entityAnalysisModelExhaustiveSearchInstance
                                                 .ExhaustiveSearchInstanceVariable)
                                    {
                                        exhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceId =
                                            entityAnalysisModelExhaustiveSearchInstanceId;

                                        var exhaustiveSearchInstanceVariableId =
                                            await exhaustiveSearchInstanceVariableRepository.InsertAsync(
                                                exhaustiveSearchInstanceVariable, token).ConfigureAwait(false);

                                        foreach (var exhaustiveSearchInstanceVariableHistogram in
                                                 exhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceVariableHistogram)
                                        {
                                            exhaustiveSearchInstanceVariableHistogram.ExhaustiveSearchInstanceVariableId =
                                                exhaustiveSearchInstanceVariableId;

                                            await exhaustiveSearchInstanceVariableHistogramRepository.InsertAsync(
                                                exhaustiveSearchInstanceVariableHistogram, token).ConfigureAwait(false);
                                        }

                                        foreach (var exhaustiveSearchInstanceVariableAnomaly in
                                                 exhaustiveSearchInstanceVariable
                                                     .ExhaustiveSearchInstanceVariableAnomaly)
                                        {
                                            exhaustiveSearchInstanceVariableAnomaly.ExhaustiveSearchInstanceVariableId =
                                                exhaustiveSearchInstanceVariableId;

                                            var exhaustiveSearchInstanceVariableAnomalyId =
                                                await exhaustiveSearchInstanceVariableAnomalyRepository.InsertAsync(
                                                    exhaustiveSearchInstanceVariableAnomaly, token).ConfigureAwait(false);

                                            foreach (var exhaustiveSearchInstanceVariableHistogramAnomaly in
                                                     exhaustiveSearchInstanceVariable
                                                         .ExhaustiveSearchInstanceVariableHistogramAnomaly)
                                            {
                                                exhaustiveSearchInstanceVariableHistogramAnomaly
                                                        .ExhaustiveSearchInstanceVariableAnomalyId =
                                                    exhaustiveSearchInstanceVariableAnomalyId;

                                                await exhaustiveSearchInstanceVariableHistogramAnomalyRepository.InsertAsync(
                                                    exhaustiveSearchInstanceVariableHistogramAnomaly, token).ConfigureAwait(false);
                                            }
                                        }

                                        foreach (var exhaustiveSearchInstanceVariableClassification in
                                                 exhaustiveSearchInstanceVariable
                                                     .ExhaustiveSearchInstanceVariableClassification)
                                        {
                                            exhaustiveSearchInstanceVariableClassification
                                                    .ExhaustiveSearchInstanceVariableId =
                                                exhaustiveSearchInstanceVariableId;

                                            var exhaustiveSearchInstanceVariableClassificationId =
                                                await exhaustiveSearchInstanceVariableClassificationRepository.InsertAsync(
                                                    exhaustiveSearchInstanceVariableClassification, token).ConfigureAwait(false);

                                            foreach (var exhaustiveSearchInstanceVariableHistogramClassification in
                                                     exhaustiveSearchInstanceVariable
                                                         .ExhaustiveSearchInstanceVariableHistogramClassification)
                                            {
                                                exhaustiveSearchInstanceVariableHistogramClassification
                                                        .ExhaustiveSearchInstanceVariableClassificationId =
                                                    exhaustiveSearchInstanceVariableClassificationId;

                                                await exhaustiveSearchInstanceVariableHistogramClassificationRepository.InsertAsync(
                                                    exhaustiveSearchInstanceVariableHistogramClassification, token).ConfigureAwait(false);
                                            }
                                        }

                                        foreach (var exhaustiveSearchInstanceVariableMultiCollinearity in
                                                 exhaustiveSearchInstanceVariable
                                                     .ExhaustiveSearchInstanceVariableMultiCollinearity)
                                        {
                                            exhaustiveSearchInstanceVariableMultiCollinearity
                                                    .ExhaustiveSearchInstanceVariableId =
                                                exhaustiveSearchInstanceVariableId;

                                            await exhaustiveSearchInstanceVariableMulticollinearityRepository.InsertAsync(
                                                exhaustiveSearchInstanceVariableMultiCollinearity, token).ConfigureAwait(false);
                                        }
                                    }
                                }
                            }
                        }

                        foreach (var entityAnalysisModelActivationRule in oldEntityAnalysisModel
                                     .EntityAnalysisModelActivationRule)
                        {
                            entityAnalysisModelActivationRule.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            await entityAnalysisModelActivationRuleRepository.InsertAsync(entityAnalysisModelActivationRule, token);
                        }

                        foreach (var oldEntityAnalysisModelCaseWorkflow in oldEntityAnalysisModel
                                     .CaseWorkflow)
                        {
                            oldEntityAnalysisModelCaseWorkflow.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            var entityAnalysisModelCaseWorkflowId =
                                (await caseWorkflowRepository.InsertAsync(oldEntityAnalysisModelCaseWorkflow, token)).Id;

                            foreach (var oldCaseWorkflowsXPath in oldEntityAnalysisModelCaseWorkflow
                                         .CaseWorkflowXPath)
                            {
                                oldCaseWorkflowsXPath.CaseWorkflowId = entityAnalysisModelCaseWorkflowId;
                                await caseWorkflowXPathRepository.InsertAsync(oldCaseWorkflowsXPath, token);
                            }

                            foreach (var oldCaseWorkflowsAction in oldEntityAnalysisModelCaseWorkflow
                                         .CaseWorkflowAction)
                            {
                                oldCaseWorkflowsAction.CaseWorkflowId = entityAnalysisModelCaseWorkflowId;
                                await caseWorkflowActionRepository.InsertAsync(oldCaseWorkflowsAction, token);
                            }

                            foreach (var oldCaseWorkflowsDisplay in oldEntityAnalysisModelCaseWorkflow
                                         .CaseWorkflowDisplay)
                            {
                                oldCaseWorkflowsDisplay.CaseWorkflowId = entityAnalysisModelCaseWorkflowId;
                                await caseWorkflowDisplayRepository.InsertAsync(oldCaseWorkflowsDisplay, token).ConfigureAwait(false);
                            }

                            foreach (var oldCaseWorkflowsForm in oldEntityAnalysisModelCaseWorkflow
                                         .CaseWorkflowForm)
                            {
                                oldCaseWorkflowsForm.CaseWorkflowId = entityAnalysisModelCaseWorkflowId;
                                await caseWorkflowFormRepository.InsertAsync(oldCaseWorkflowsForm, token);
                            }

                            foreach (var oldCaseWorkflowsFilter in oldEntityAnalysisModelCaseWorkflow
                                         .CaseWorkflowFilter)
                            {
                                oldCaseWorkflowsFilter.CaseWorkflowId = entityAnalysisModelCaseWorkflowId;
                                await caseWorkflowFilterRepository.InsertAsync(oldCaseWorkflowsFilter, token);
                            }

                            foreach (var oldCaseWorkflowsMacro in oldEntityAnalysisModelCaseWorkflow
                                         .CaseWorkflowMacro)
                            {
                                oldCaseWorkflowsMacro.CaseWorkflowId = entityAnalysisModelCaseWorkflowId;
                                await caseWorkflowMacro.InsertAsync(oldCaseWorkflowsMacro, token);
                            }

                            foreach (var caseWorkflowsStatus in oldEntityAnalysisModelCaseWorkflow
                                         .CaseWorkflowStatus)
                            {
                                caseWorkflowsStatus.CaseWorkflowId = entityAnalysisModelCaseWorkflowId;
                                await caseWorkflowStatusRepository.InsertAsync(caseWorkflowsStatus, token);
                            }
                        }

                        if (options.Lists)
                        {
                            if (oldEntityAnalysisModel.EntityAnalysisModelList != null)
                            {
                                foreach (var oldEntityAnalysisModelList in oldEntityAnalysisModel.EntityAnalysisModelList)
                                {
                                    oldEntityAnalysisModelList.EntityAnalysisModelGuid = newEntityAnalysisModel.Guid;
                                    var newEntityAnalysisModelListId =
                                        (await entityAnalysisModelListRepository.InsertAsync(oldEntityAnalysisModelList, token)).Id;

                                    foreach (var entityAnalysisModelListValue in oldEntityAnalysisModelList
                                                 .EntityAnalysisModelListValue)
                                    {
                                        entityAnalysisModelListValue.EntityAnalysisModelListId =
                                            newEntityAnalysisModelListId;
                                        await entityAnalysisModelListValueRepository.InsertAsync(entityAnalysisModelListValue, token);
                                    }
                                }
                            }
                        }

                        if (options.Dictionaries)
                        {
                            if (oldEntityAnalysisModel.EntityAnalysisModelDictionary != null)
                            {
                                foreach (var oldEntityAnalysisModelDictionary in oldEntityAnalysisModel.EntityAnalysisModelDictionary)
                                {
                                    oldEntityAnalysisModelDictionary.Guid = newEntityAnalysisModel.Guid;
                                    var newEntityAnalysisModelDictionaryId = (await entityAnalysisModelDictionaryRepository
                                        .InsertAsync(oldEntityAnalysisModelDictionary, token)).Id;

                                    foreach (var oldEntityAnalysisModelDictionaryKvp in oldEntityAnalysisModelDictionary
                                                 .EntityAnalysisModelDictionaryKvp)
                                    {
                                        oldEntityAnalysisModelDictionaryKvp.EntityAnalysisModelDictionaryId =
                                            newEntityAnalysisModelDictionaryId;
                                        await entityAnalysisModelDictionaryKvpRepository.InsertAsync(
                                            oldEntityAnalysisModelDictionaryKvp, token);
                                    }
                                }
                            }
                        }
                    }
                }

                if (wrapper.Payload is { EntityPermission: not null })
                {
                    if (wrapper.Payload.EntityPermission.EntityAnalysisModelRole != null)
                    {
                        foreach (var entityAnalysisModelRole in wrapper.Payload.EntityPermission.EntityAnalysisModelRole)
                        {
                            await entityAnalysisModelRoleRepository.InsertAsync(entityAnalysisModelRole, token);
                        }
                    }
                    
                    if (wrapper.Payload.EntityPermission.CaseWorkflowRole != null)
                    {
                        foreach (var caseWorkflowRole in wrapper.Payload.EntityPermission.CaseWorkflowRole)
                        {
                            await caseWorkflowRoleRepository.InsertAsync(caseWorkflowRole, token);
                        }
                    }

                    if (wrapper.Payload.EntityPermission.CaseWorkflowActionRole != null)
                    {
                        foreach (var caseWorkflowActionRole in wrapper.Payload.EntityPermission.CaseWorkflowActionRole)
                        {
                            await caseWorkflowActionRoleRepository.InsertAsync(caseWorkflowActionRole, token);
                        }
                    }

                    if (wrapper.Payload.EntityPermission.CaseWorkflowDisplayRole != null)
                    {
                        foreach (var caseWorkflowDisplayRole in wrapper.Payload.EntityPermission.CaseWorkflowDisplayRole)
                        {
                            await caseWorkflowDisplayRoleRepository.InsertAsync(caseWorkflowDisplayRole, token);
                        }
                    }

                    if (wrapper.Payload.EntityPermission.CaseWorkflowFilterRole != null)
                    {
                        foreach (var caseWorkflowFilterRole in wrapper.Payload.EntityPermission.CaseWorkflowFilterRole)
                        {
                            await caseWorkflowFilterRoleRepository.InsertAsync(caseWorkflowFilterRole, token);
                        }
                    }

                    if (wrapper.Payload.EntityPermission.CaseWorkflowFormRole != null)
                    {
                        foreach (var caseWorkflowFormRole in wrapper.Payload.EntityPermission.CaseWorkflowFormRole)
                        {
                            await caseWorkflowFormRoleRepository.InsertAsync(caseWorkflowFormRole, token);
                        }
                    }

                    if (wrapper.Payload.EntityPermission.CaseWorkflowMacroRole != null)
                    {
                        foreach (var caseWorkflowMacroRole in wrapper.Payload.EntityPermission.CaseWorkflowMacroRole)
                        {
                            await caseWorkflowMacroRoleRepository.InsertAsync(caseWorkflowMacroRole, token);
                        }
                    }

                    if (wrapper.Payload.EntityPermission.CaseWorkflowStatusRole != null)
                    {
                        foreach (var caseWorkflowStatusRole in wrapper.Payload.EntityPermission.CaseWorkflowStatusRole)
                        {
                            await caseWorkflowStatusRoleRepository.InsertAsync(caseWorkflowStatusRole, token);
                        }
                    }

                    if (wrapper.Payload.EntityPermission.CaseWorkflowXPathRole != null)
                    {
                        foreach (var caseWorkflowXPathRole in wrapper.Payload.EntityPermission.CaseWorkflowXPathRole)
                        {
                            await caseWorkflowXPathRoleRepository.InsertAsync(caseWorkflowXPathRole, token);
                        }
                    }
                }

                if (options.Visualisations)
                {
                    if (wrapper.Payload?.VisualisationRegistry != null)
                    {
                        foreach (var visualisationRegistry in wrapper.Payload.VisualisationRegistry)
                        {
                            var visualisationRegistryId = (await visualisationRegistryRepository.InsertAsync(visualisationRegistry, token)).Id;

                            foreach (var visualisationRegistryDatasource in visualisationRegistry
                                         .VisualisationRegistryDatasource)
                            {
                                visualisationRegistryDatasource.VisualisationRegistryId = visualisationRegistryId;
                                await visualisationRegistryDatasourceRepository.InsertAsync(visualisationRegistryDatasource, token);
                            }

                            foreach (var visualisationRegistryParameter in visualisationRegistry
                                         .VisualisationRegistryParameter)
                            {
                                visualisationRegistryParameter.VisualisationRegistryId = visualisationRegistryId;
                                await visualisationRegistryParameterRepository.InsertAsync(visualisationRegistryParameter, token);
                            }
                        }
                    }

                    if (wrapper.Payload is { EntityPermission: not null })
                    {
                        if (wrapper.Payload.EntityPermission.VisualisationRegistryRole != null)
                        {
                            foreach (var visualisationRegistryRole in wrapper.Payload.EntityPermission.VisualisationRegistryRole)
                            {
                                await visualisationRegistryRoleRepository.InsertAsync(visualisationRegistryRole, token);
                            }
                        }

                        if (wrapper.Payload.EntityPermission.VisualisationRegistryParameterRole != null)
                        {
                            foreach (var visualisationRegistryParameterRole in wrapper.Payload.EntityPermission.VisualisationRegistryParameterRole)
                            {
                                await visualisationRegistryParameterRoleRepository.InsertAsync(visualisationRegistryParameterRole, token);
                            }
                        }

                        if (wrapper.Payload.EntityPermission.VisualisationRegistryDatasourceRole != null)
                        {
                            foreach (var visualisationRegistryDatasourceRole in wrapper.Payload.EntityPermission.VisualisationRegistryDatasourceRole)
                            {
                                await visualisationRegistryDatasourceRoleRepository.InsertAsync(visualisationRegistryDatasourceRole, token);
                            }
                        }
                    }
                }

                await dbContext.CommitTransactionAsync(token).ConfigureAwait(false);
                import.CompletedDate = DateTime.Now;
                import.ExportGuid = wrapper.Guid;
                import.ExportVersion = wrapper.Version;

                await importRepository.UpdateAsync(import, token);
            }
            catch (Exception ex)
            {
                await dbContext.RollbackTransactionAsync(token).ConfigureAwait(false);

                import.InError = 1;
                import.ErrorStack = ex.ToString();

                await importRepository.UpdateAsync(import, token);
                throw;
            }
        }

        public async Task<Export> ExportAsync(ImportExportOptions options, CancellationToken token = default)
        {
            var exportRepository = new ExportRepository(dbContext, userName);
            var export = new Export
            {
                CreatedDate = DateTime.Now,
                Guid = Guid.NewGuid()
            };

            export = await exportRepository.InsertAsync(export, token);

            try
            {
                var wrapper = new Wrapper
                {
                    Guid = export.Guid,
                    Version = 1,
                    Payload = await ExportPayloadAsync(options, export.TenantRegistryId, token).ConfigureAwait(false)
                };

                var lz4Options =
                    MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

                var bytes = MessagePackSerializer.Serialize(wrapper, lz4Options);

                var aesEncryption = new AesEncryption(options.Password ?? "", salt, legacyFallbackEnabled);
                var encryptedBytes = aesEncryption.Encrypt(bytes);

                export.Bytes = bytes;
                export.EncryptedBytes = encryptedBytes;
                export.CompletedDate = DateTime.Now;
                export.Version = wrapper.Version;

                await exportRepository.UpdateAsync(export, token);

                return export;
            }
            catch (Exception ex)
            {
                export.InError = 1;
                export.ErrorStack = ex.ToString();
                await exportRepository.UpdateAsync(export, token);
                throw;
            }
        }

        public async Task<ExportPeek> ExportPeekAsync(ImportExportOptions options, CancellationToken token = default)
        {
            var exportPeekRepository = new ExportPeekRepository(dbContext, userName);

            var exportPeek = new ExportPeek
            {
                Guid = Guid.NewGuid(),
                CreatedUser = userName
            };

            exportPeek = await exportPeekRepository.InsertAsync(exportPeek, token).ConfigureAwait(false);

            try
            {
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                var payload = await ExportPayloadAsync(options, exportPeek.TenantRegistryId, token).ConfigureAwait(false);

                exportPeek.Yaml = serializer.Serialize(payload);
                exportPeek.CompletedDate = DateTime.Now;

                await exportPeekRepository.UpdateAsync(exportPeek, token);

                return exportPeek;
            }
            catch (Exception ex)
            {
                exportPeek.InError = 1;
                exportPeek.ErrorStack = ex.ToString();
                await exportPeekRepository.UpdateAsync(exportPeek, token);
                throw;
            }
        }

        private async Task<Payload> ExportPayloadAsync(ImportExportOptions options, int tenantRegistryId, CancellationToken token = default)
        {
            var payload = new Payload
            {
                EntityPermission = new EntityPermission()
            };

            var entityAnalysisModelRepository = new EntityAnalysisModelRepository(dbContext, tenantRegistryId);
            payload.EntityAnalysisModel = await entityAnalysisModelRepository.GetAsync(token).ConfigureAwait(false);

            foreach (var entityAnalysisModel in payload.EntityAnalysisModel)
            {
                if (options.Suppressions)
                {
                    var entityAnalysisModelSuppressionRepository =
                        new EntityAnalysisModelSuppressionRepository(dbContext, tenantRegistryId);
                    entityAnalysisModel.EntityAnalysisModelSuppression
                        = await entityAnalysisModelSuppressionRepository
                            .GetByEntityAnalysisModelGuidOrderByIdAsync(entityAnalysisModel.Guid, token);

                    var entityAnalysisModelActivationRuleSuppressionRepository =
                        new EntityAnalysisModelActivationRuleSuppressionRepository(dbContext, tenantRegistryId);
                    entityAnalysisModel.EntityAnalysisModelActivationRuleSuppression
                        = await entityAnalysisModelActivationRuleSuppressionRepository
                            .GetByEntityAnalysisModelGuidOrderByIdAsync(entityAnalysisModel.Guid, token).ConfigureAwait(false);
                }

                var entityAnalysisModelRequestXPathRepository =
                    new EntityAnalysisModelRequestXPathRepository(dbContext, tenantRegistryId);
                entityAnalysisModel.EntityAnalysisModelRequestXpath
                    = await entityAnalysisModelRequestXPathRepository.GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModel.Id, token).ConfigureAwait(false);

                var entityAnalysisModelInlineFunctionRepository =
                    new EntityAnalysisModelInlineFunctionRepository(dbContext, tenantRegistryId);
                entityAnalysisModel.EntityAnalysisModelInlineFunction
                    = await entityAnalysisModelInlineFunctionRepository
                        .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModel.Id, token).ConfigureAwait(false);

                var entityAnalysisModelInlineScriptRepository =
                    new EntityAnalysisModelInlineScriptRepository(dbContext, tenantRegistryId);
                entityAnalysisModel.EntityAnalysisModelInlineScript
                    = await entityAnalysisModelInlineScriptRepository.GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModel.Id, token).ConfigureAwait(false);

                var entityAnalysisModelGatewayRuleRepository =
                    new EntityAnalysisModelGatewayRuleRepository(dbContext, tenantRegistryId);
                entityAnalysisModel.EntityAnalysisModelGatewayRule
                    = await entityAnalysisModelGatewayRuleRepository.GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModel.Id, token).ConfigureAwait(false);

                var entityAnalysisModelSanctionRepository =
                    new EntityAnalysisModelSanctionRepository(dbContext, tenantRegistryId);
                entityAnalysisModel.EntityAnalysisModelSanction
                    = await entityAnalysisModelSanctionRepository.GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModel.Id, token).ConfigureAwait(false);

                var entityAnalysisModelTagRepository =
                    new EntityAnalysisModelTagRepository(dbContext, tenantRegistryId);
                entityAnalysisModel.EntityAnalysisModelTag
                    = await entityAnalysisModelTagRepository.GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModel.Id, token).ConfigureAwait(false);

                var entityAnalysisModelTtlCounterRepository =
                    new EntityAnalysisModelTtlCounterRepository(dbContext, tenantRegistryId);
                entityAnalysisModel.EntityAnalysisModelTtlCounter
                    = await entityAnalysisModelTtlCounterRepository.GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModel.Id, token).ConfigureAwait(false);

                var entityAnalysisModelAbstractionRuleRepository =
                    new EntityAnalysisModelAbstractionRuleRepository(dbContext, tenantRegistryId);
                entityAnalysisModel.EntityAnalysisModelAbstractionRule
                    = await entityAnalysisModelAbstractionRuleRepository
                        .GetByEntityAnalysisModelIdOrderByIdDescAsync(entityAnalysisModel.Id, token).ConfigureAwait(false);

                var entityAnalysisModelAbstractionCalculationRepository =
                    new EntityAnalysisModelAbstractionCalculationRepository(dbContext, tenantRegistryId);
                entityAnalysisModel.EntityAnalysisModelAbstractionCalculation
                    = await entityAnalysisModelAbstractionCalculationRepository
                        .GetByEntityAnalysisModelIdOrderByIdDescAsync(entityAnalysisModel.Id, token).ConfigureAwait(false);

                var entityAnalysisModelHttpAdaptationRepository =
                    new EntityAnalysisModelHttpAdaptationRepository(dbContext, tenantRegistryId);
                entityAnalysisModel.EntityAnalysisModelHttpAdaptation
                    = await entityAnalysisModelHttpAdaptationRepository
                        .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModel.Id, token).ConfigureAwait(false);

                if (options.Exhaustive)
                {
                    var exhaustiveSearchInstanceRepository =
                        new ExhaustiveSearchInstanceRepository(dbContext, tenantRegistryId);
                    entityAnalysisModel.ExhaustiveSearchInstance
                        = await exhaustiveSearchInstanceRepository.GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModel.Id, token).ConfigureAwait(false);

                    foreach (var exhaustiveSearchInstance in entityAnalysisModel.ExhaustiveSearchInstance)
                    {
                        var exhaustiveSearchInstanceDataRepository =
                            new ExhaustiveSearchInstanceDataRepository(dbContext);
                        exhaustiveSearchInstance.ExhaustiveSearchInstanceData =
                            await exhaustiveSearchInstanceDataRepository.GetByExhaustiveSearchInstanceIdOrderByIdAsync(
                                exhaustiveSearchInstance.Id, token).ConfigureAwait(false);

                        var exhaustiveSearchInstanceTrialInstanceRepository =
                            new ExhaustiveSearchInstanceTrialInstanceRepository(dbContext);
                        exhaustiveSearchInstance.ExhaustiveSearchInstanceTrialInstance =
                            await exhaustiveSearchInstanceTrialInstanceRepository.GetByExhaustiveSearchInstanceIdOrderByIdAsync(
                                exhaustiveSearchInstance.Id, token).ConfigureAwait(false);

                        var exhaustiveSearchInstanceVariableRepository =
                            new ExhaustiveSearchInstanceVariableRepository(dbContext);
                        exhaustiveSearchInstance.ExhaustiveSearchInstanceVariable =
                            await exhaustiveSearchInstanceVariableRepository.GetByExhaustiveSearchInstanceIdOrderByIdAsync(
                                exhaustiveSearchInstance.Id, token).ConfigureAwait(false);

                        foreach (var exhaustiveSearchInstanceTrialInstance in exhaustiveSearchInstance
                                     .ExhaustiveSearchInstanceTrialInstance)
                        {
                            var exhaustiveSearchInstanceTrialInstanceVariableRepository =
                                new ExhaustiveSearchInstanceTrialInstanceVariableRepository(dbContext);
                            exhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstanceTrialInstanceVariable =
                                await exhaustiveSearchInstanceTrialInstanceVariableRepository
                                    .GetByExhaustiveSearchInstanceTrialInstanceIdOrderByIdAsync(
                                        exhaustiveSearchInstanceTrialInstance
                                            .Id, token);

                            var exhaustiveSearchInstancePromotedTrialInstanceRepository =
                                new ExhaustiveSearchInstancePromotedTrialInstanceRepository(dbContext);
                            exhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstancePromotedTrialInstance =
                                await exhaustiveSearchInstancePromotedTrialInstanceRepository
                                    .GetByExhaustiveSearchInstanceTrialInstanceIdOrderByIdAsync(
                                        exhaustiveSearchInstanceTrialInstance
                                            .Id, token).ConfigureAwait(false);

                            var exhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository =
                                new ExhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository(dbContext);
                            exhaustiveSearchInstanceTrialInstance
                                    .ExhaustiveSearchInstancePromotedTrialInstancePredictedActual =
                                await exhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository
                                    .GetByExhaustiveSearchInstanceTrialInstanceIdOrderByIdAsync(
                                        exhaustiveSearchInstanceTrialInstance
                                            .Id, token).ConfigureAwait(false);

                            var exhaustiveSearchInstancePromotedTrialInstanceRocRepository =
                                new ExhaustiveSearchInstancePromotedTrialInstanceRocRepository(dbContext);
                            exhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstancePromotedTrialInstanceRoc =
                                await exhaustiveSearchInstancePromotedTrialInstanceRocRepository
                                    .GetByExhaustiveSearchInstanceTrialInstanceIdOrderByIdAsync(exhaustiveSearchInstance
                                        .Id, token);

                            var exhaustiveSearchInstanceTrialInstanceTopologyTrialRepository =
                                new ExhaustiveSearchInstanceTrialInstanceTopologyTrialRepository(dbContext);
                            exhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstanceTrialInstanceTopologyTrial =
                                await exhaustiveSearchInstanceTrialInstanceTopologyTrialRepository
                                    .GetByExhaustiveSearchInstanceTrialInstanceIdOrderByIdAsync(
                                        exhaustiveSearchInstanceTrialInstance
                                            .Id, token).ConfigureAwait(false);

                            var exhaustiveSearchInstanceTrialInstanceSensitivityRepository =
                                new ExhaustiveSearchInstanceTrialInstanceSensitivityRepository(dbContext);
                            exhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstanceTrialInstanceSensitivity =
                                await exhaustiveSearchInstanceTrialInstanceSensitivityRepository
                                    .GetByExhaustiveSearchInstanceTrialInstanceIdOrderByIdAsync(
                                        exhaustiveSearchInstanceTrialInstance
                                            .Id, token).ConfigureAwait(false);

                            var exhaustiveSearchInstanceTrialInstanceActivationFunctionTrialRepository =
                                new ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrialRepository(dbContext);
                            exhaustiveSearchInstanceTrialInstance
                                    .ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial =
                                await exhaustiveSearchInstanceTrialInstanceActivationFunctionTrialRepository
                                    .GetByExhaustiveSearchInstanceTrialInstanceIdOrderByIdAsync(
                                        exhaustiveSearchInstanceTrialInstance
                                            .Id, token).ConfigureAwait(false);

                            foreach (var exhaustiveSearchInstanceTrialInstanceVariable in
                                     exhaustiveSearchInstanceTrialInstance
                                         .ExhaustiveSearchInstanceTrialInstanceVariable)
                            {
                                var exhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository =
                                    new ExhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository(dbContext);
                                exhaustiveSearchInstanceTrialInstanceVariable
                                        .ExhaustiveSearchInstancePromotedTrialInstanceSensitivity =
                                    await exhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository
                                        .GetByExhaustiveSearchInstanceTrialInstanceVariableIdOrderByIdAsync(
                                            exhaustiveSearchInstanceTrialInstanceVariable.Id, token).ConfigureAwait(false);

                                var exhaustiveSearchInstancePromotedTrialInstanceVariableRepository =
                                    new ExhaustiveSearchInstancePromotedTrialInstanceVariableRepository(dbContext);
                                exhaustiveSearchInstanceTrialInstanceVariable
                                        .ExhaustiveSearchInstancePromotedTrialInstanceVariable =
                                    await exhaustiveSearchInstancePromotedTrialInstanceVariableRepository
                                        .GetByExhaustiveSearchInstanceTrialInstanceVariableIdOrderByIdAsync(
                                            exhaustiveSearchInstanceTrialInstanceVariable.Id, token).ConfigureAwait(false);
                            }
                        }

                        foreach (var exhaustiveSearchInstanceVariable in exhaustiveSearchInstance
                                     .ExhaustiveSearchInstanceVariable)
                        {
                            var exhaustiveSearchInstanceVariableAnomalyRepository =
                                new ExhaustiveSearchInstanceVariableAnomalyRepository(dbContext);
                            exhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceVariableAnomaly =
                                await exhaustiveSearchInstanceVariableAnomalyRepository
                                    .GetByExhaustiveSearchInstanceVariableIdOrderByIdAsync(
                                        exhaustiveSearchInstanceVariable
                                            .Id, token).ConfigureAwait(false);

                            var exhaustiveSearchInstanceVariableClassificationRepository =
                                new ExhaustiveSearchInstanceVariableClassificationRepository(dbContext);
                            exhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceVariableClassification =
                                await exhaustiveSearchInstanceVariableClassificationRepository
                                    .GetByExhaustiveSearchInstanceVariableIdOrderByIdAsync(exhaustiveSearchInstanceVariable
                                        .Id, token).ConfigureAwait(false);

                            var exhaustiveSearchInstanceVariableHistogramRepository =
                                new ExhaustiveSearchInstanceVariableHistogramRepository(dbContext);
                            exhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceVariableHistogram =
                                await exhaustiveSearchInstanceVariableHistogramRepository
                                    .GetByExhaustiveSearchInstanceVariableIdOrderByIdAsync(
                                        exhaustiveSearchInstanceVariable
                                            .Id, token).ConfigureAwait(false);

                            var exhaustiveSearchInstanceVariableHistogramClassificationRepository =
                                new ExhaustiveSearchInstanceVariableHistogramClassificationRepository(dbContext);
                            exhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceVariableHistogramClassification =
                                await exhaustiveSearchInstanceVariableHistogramClassificationRepository
                                    .GetByExhaustiveSearchInstanceVariableIdAsync(exhaustiveSearchInstanceVariable
                                        .Id, token).ConfigureAwait(false);

                            var exhaustiveSearchInstanceVariableHistogramAnomalyRepository =
                                new ExhaustiveSearchInstanceVariableHistogramAnomalyRepository(dbContext);
                            exhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceVariableHistogramAnomaly =
                                await exhaustiveSearchInstanceVariableHistogramAnomalyRepository
                                    .GetByExhaustiveSearchInstanceVariableIdOrderByIdAsync(exhaustiveSearchInstanceVariable
                                        .Id, token).ConfigureAwait(false);

                            var exhaustiveSearchInstanceVariableMulticollinearityRepository =
                                new ExhaustiveSearchInstanceVariableMultiColiniarityRepository(dbContext);
                            exhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceVariableMultiCollinearity =
                                await exhaustiveSearchInstanceVariableMulticollinearityRepository
                                    .GetByExhaustiveSearchInstanceVariableIdOrderByIdAsync(exhaustiveSearchInstanceVariable
                                        .Id, token).ConfigureAwait(false);
                        }
                    }
                }

                var entityAnalysisModelActivationRuleRepository =
                    new EntityAnalysisModelActivationRuleRepository(dbContext, tenantRegistryId);
                entityAnalysisModel.EntityAnalysisModelActivationRule
                    = await entityAnalysisModelActivationRuleRepository
                        .GetByEntityAnalysisModelIdOrderByIdDescAsync(entityAnalysisModel.Id, token);

                var caseWorkflowRepository = new CaseWorkflowRepository(dbContext, tenantRegistryId);
                entityAnalysisModel.CaseWorkflow
                    = await caseWorkflowRepository.GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModel.Id, token);

                foreach (var entityAnalysisModelCaseWorkflowEntityAnalysisModel in entityAnalysisModel
                             .CaseWorkflow)
                {
                    var caseWorkflowXPathRepository =
                        new CaseWorkflowXPathRepository(dbContext, tenantRegistryId);
                    entityAnalysisModelCaseWorkflowEntityAnalysisModel.CaseWorkflowXPath
                        = await caseWorkflowXPathRepository.GetByCasesWorkflowIdOrderByIdDescAsync(
                            entityAnalysisModelCaseWorkflowEntityAnalysisModel.Id, token);

                    var caseWorkflowStatusRepository =
                        new CaseWorkflowStatusRepository(dbContext, tenantRegistryId);
                    entityAnalysisModelCaseWorkflowEntityAnalysisModel.CaseWorkflowStatus
                        = await caseWorkflowStatusRepository.GetByCasesWorkflowIdOrderByIdAsync(
                            entityAnalysisModelCaseWorkflowEntityAnalysisModel.Id, token);

                    var caseWorkflowFormRepository = new CaseWorkflowFormRepository(dbContext, tenantRegistryId);
                    entityAnalysisModelCaseWorkflowEntityAnalysisModel.CaseWorkflowForm
                        = await caseWorkflowFormRepository.GetByCasesWorkflowIdOrderByIdAsync(
                            entityAnalysisModelCaseWorkflowEntityAnalysisModel.Id, token);

                    var caseWorkflowActionRepository =
                        new CaseWorkflowActionRepository(dbContext, tenantRegistryId);
                    entityAnalysisModelCaseWorkflowEntityAnalysisModel.CaseWorkflowAction
                        = await caseWorkflowActionRepository.GetByCasesWorkflowIdOrderByIdAsync(
                            entityAnalysisModelCaseWorkflowEntityAnalysisModel.Id, token);

                    var caseWorkflowDisplayRepository =
                        new CaseWorkflowDisplayRepository(dbContext, tenantRegistryId);
                    entityAnalysisModelCaseWorkflowEntityAnalysisModel.CaseWorkflowDisplay
                        = await caseWorkflowDisplayRepository.GetByCasesWorkflowIdOrderByIdAsync(
                            entityAnalysisModelCaseWorkflowEntityAnalysisModel.Id, token);

                    var caseWorkflowMacro = new CaseWorkflowMacroRepository(dbContext, tenantRegistryId);
                    entityAnalysisModelCaseWorkflowEntityAnalysisModel.CaseWorkflowMacro
                        = await caseWorkflowMacro.GetByCasesWorkflowIdOrderByIdAsync(
                            entityAnalysisModelCaseWorkflowEntityAnalysisModel.Id, token);

                    var caseWorkflowFilterRepository =
                        new CaseWorkflowFilterRepository(dbContext, tenantRegistryId);
                    entityAnalysisModelCaseWorkflowEntityAnalysisModel.CaseWorkflowFilter
                        = await caseWorkflowFilterRepository.GetByCasesWorkflowIdOrderByIdAsync(
                            entityAnalysisModelCaseWorkflowEntityAnalysisModel.Id, token);
                }

                if (options.Lists)
                {
                    var entityAnalysisModelListRepository =
                        new EntityAnalysisModelListRepository(dbContext, tenantRegistryId);
                    entityAnalysisModel.EntityAnalysisModelList
                        = await entityAnalysisModelListRepository.GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModel.Id, token).ConfigureAwait(false);

                    foreach (var entityAnalysisModelListsEntityAnalysisModel in entityAnalysisModel
                                 .EntityAnalysisModelList)
                    {
                        var entityAnalysisModelListValueRepository =
                            new EntityAnalysisModelListValueRepository(dbContext, tenantRegistryId);
                        entityAnalysisModelListsEntityAnalysisModel.EntityAnalysisModelListValue
                            = await entityAnalysisModelListValueRepository
                                .GetByEntityAnalysisModelListIdOrderByIdAsync(entityAnalysisModelListsEntityAnalysisModel.Id, token);
                    }
                }

                if (options.Dictionaries)
                {
                    var entityAnalysisModelDictionaryRepository =
                        new EntityAnalysisModelDictionaryRepository(dbContext, tenantRegistryId);
                    entityAnalysisModel.EntityAnalysisModelDictionary
                        = await entityAnalysisModelDictionaryRepository
                            .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModel.Id, token).ConfigureAwait(false);

                    foreach (var entityAnalysisModelDictionaryEntityAnalysisModel in entityAnalysisModel
                                 .EntityAnalysisModelDictionary)
                    {
                        var entityAnalysisModelDictionaryKvpRepository =
                            new EntityAnalysisModelDictionaryKvpRepository(dbContext, tenantRegistryId);
                        entityAnalysisModelDictionaryEntityAnalysisModel
                                .EntityAnalysisModelDictionaryKvp
                            = await entityAnalysisModelDictionaryKvpRepository
                                .GetByEntityAnalysisModelDictionaryIdOrderByIdAsync(
                                    entityAnalysisModelDictionaryEntityAnalysisModel
                                        .Id, token).ConfigureAwait(false);
                    }
                }

                if (options.Visualisations)
                {
                    var visualisationRegistryRepository =
                        new VisualisationRegistryRepository(dbContext, tenantRegistryId);
                    payload.VisualisationRegistry = await visualisationRegistryRepository.GetOrderByIdAsync(token);

                    foreach (var visualisationRegistry in payload.VisualisationRegistry)
                    {
                        var visualisationRegistryDatasourceRepository =
                            new VisualisationRegistryDatasourceRepository(dbContext, tenantRegistryId);
                        visualisationRegistry.VisualisationRegistryDatasource
                            = await visualisationRegistryDatasourceRepository
                                .GetByVisualisationRegistryIdOrderByIdAsync(visualisationRegistry.Id, token);

                        var visualisationRegistryParameterRepository =
                            new VisualisationRegistryParameterRepository(dbContext, tenantRegistryId);
                        visualisationRegistry.VisualisationRegistryParameter
                            = await visualisationRegistryParameterRepository
                                .GetByVisualisationRegistryIdOrderByIdAsync(visualisationRegistry.Id, token);
                    }
                }
            }
            
            var entityAnalysisModelRole = new EntityAnalysisModelRoleRepository(dbContext, tenantRegistryId);
            payload.EntityPermission.EntityAnalysisModelRole = await entityAnalysisModelRole.GetAllDescAsync(token);

            var caseWorkflowRoleRepository = new CaseWorkflowRoleRepository(dbContext, tenantRegistryId);
            payload.EntityPermission.CaseWorkflowRole = await caseWorkflowRoleRepository.GetAllDescAsync(token);

            var caseWorkflowActionRoleRepository = new CaseWorkflowActionRoleRepository(dbContext, tenantRegistryId);
            payload.EntityPermission.CaseWorkflowActionRole = await caseWorkflowActionRoleRepository.GetAllDescAsync(token);

            var caseWorkflowDisplayRoleRepository = new CaseWorkflowDisplayRoleRepository(dbContext, tenantRegistryId);
            payload.EntityPermission.CaseWorkflowDisplayRole = await caseWorkflowDisplayRoleRepository.GetAllDescAsync(token);

            var caseWorkflowFilterRoleRepository = new CaseWorkflowFilterRoleRepository(dbContext, tenantRegistryId);
            payload.EntityPermission.CaseWorkflowFilterRole = await caseWorkflowFilterRoleRepository.GetAllDescAsync(token);

            var caseWorkflowFormRoleRepository = new CaseWorkflowFormRoleRepository(dbContext, tenantRegistryId);
            payload.EntityPermission.CaseWorkflowFormRole = await caseWorkflowFormRoleRepository.GetAllDescAsync(token);

            var caseWorkflowMacroRoleRepository = new CaseWorkflowMacroRoleRepository(dbContext, tenantRegistryId);
            payload.EntityPermission.CaseWorkflowMacroRole = await caseWorkflowMacroRoleRepository.GetAllDescAsync(token);

            var caseWorkflowStatusRoleRepository = new CaseWorkflowStatusRoleRepository(dbContext, tenantRegistryId);
            payload.EntityPermission.CaseWorkflowStatusRole = await caseWorkflowStatusRoleRepository.GetAllDescAsync(token);

            var caseWorkflowXPathRoleRepository = new CaseWorkflowXPathRoleRepository(dbContext, tenantRegistryId);
            payload.EntityPermission.CaseWorkflowXPathRole = await caseWorkflowXPathRoleRepository.GetAllDescAsync(token);

            if (options.Visualisations)
            {
                var visualisationRegistryRole = new VisualisationRegistryRoleRepository(dbContext, tenantRegistryId);
                payload.EntityPermission.VisualisationRegistryRole = await visualisationRegistryRole.GetAllDescAsync(token);

                var visualisationRegistryParameterRole = new VisualisationRegistryParameterRoleRepository(dbContext, tenantRegistryId);
                payload.EntityPermission.VisualisationRegistryParameterRole = await visualisationRegistryParameterRole.GetAllDescAsync(token);

                var visualisationRegistryDatasourceRole = new VisualisationRegistryDatasourceRoleRepository(dbContext, tenantRegistryId);
                payload.EntityPermission.VisualisationRegistryDatasourceRole = await visualisationRegistryDatasourceRole.GetAllDescAsync(token);
            }

            return payload;
        }
    }
}
