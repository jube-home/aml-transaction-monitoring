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

    public class Preservation(DbContext dbContext, string userName, string? salt = null)
    {
        private readonly string salt = salt ?? "";

        public void Import(byte[] bytes, ImportExportOptions options)
        {
            var importRepository = new ImportRepository(dbContext, userName);
            var import = new Import
            {
                Bytes = bytes,
                CreatedDate = DateTime.Now,
                Guid = Guid.NewGuid()
            };

            import = importRepository.Insert(import);

            try
            {
                var aesEncryption = new AesEncryption(options.Password ?? "", salt);
                var decryptedBytes = aesEncryption.Decrypt(bytes);

                var lz4Options =
                    MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);
                var wrapper = MessagePackSerializer.Deserialize<Wrapper>(decryptedBytes, lz4Options);

                dbContext.BeginTransaction();

                var entityAnalysisModelRepository = new EntityAnalysisModelRepository(dbContext, import.TenantRegistryId);
                entityAnalysisModelRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId, import.Id);

                var entityAnalysisModelRequestXPathRepository =
                    new EntityAnalysisModelRequestXPathRepository(dbContext, import.TenantRegistryId);
                entityAnalysisModelRequestXPathRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId, import.Id);

                var entityAnalysisModelInlineFunctionRepository =
                    new EntityAnalysisModelInlineFunctionRepository(dbContext, import.TenantRegistryId);
                entityAnalysisModelInlineFunctionRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId, import.Id);

                var entityAnalysisModelInlineScriptRepository =
                    new EntityAnalysisModelInlineScriptRepository(dbContext, import.TenantRegistryId);
                entityAnalysisModelInlineScriptRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId, import.Id);

                var entityAnalysisModelGatewayRuleRepository =
                    new EntityAnalysisModelGatewayRuleRepository(dbContext, import.TenantRegistryId);
                entityAnalysisModelGatewayRuleRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId, import.Id);

                var entityAnalysisModelSanctionRepository =
                    new EntityAnalysisModelSanctionRepository(dbContext, import.TenantRegistryId);
                entityAnalysisModelSanctionRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId, import.Id);

                var entityAnalysisModelTagRepository =
                    new EntityAnalysisModelTagRepository(dbContext, import.TenantRegistryId);
                entityAnalysisModelTagRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId, import.Id);

                var entityAnalysisModelTtlCounterRepository =
                    new EntityAnalysisModelTtlCounterRepository(dbContext, import.TenantRegistryId);
                entityAnalysisModelTtlCounterRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId, import.Id);

                var entityAnalysisModelAbstractionRuleRepository =
                    new EntityAnalysisModelAbstractionRuleRepository(dbContext, import.TenantRegistryId);
                entityAnalysisModelAbstractionRuleRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId, import.Id);

                var entityAnalysisModelAbstractionCalculationRepository =
                    new EntityAnalysisModelAbstractionCalculationRepository(dbContext, import.TenantRegistryId);
                entityAnalysisModelAbstractionCalculationRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId,
                    import.Id);

                var entityAnalysisModelHttpAdaptationRepository =
                    new EntityAnalysisModelHttpAdaptationRepository(dbContext, import.TenantRegistryId);
                entityAnalysisModelHttpAdaptationRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId, import.Id);

                var entityAnalysisModelActivationRuleRepository =
                    new EntityAnalysisModelActivationRuleRepository(dbContext, import.TenantRegistryId);
                entityAnalysisModelActivationRuleRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId, import.Id);

                var caseWorkflowRepository = new CaseWorkflowRepository(dbContext, import.TenantRegistryId);
                caseWorkflowRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId, import.Id);

                var caseWorkflowXPathRepository = new CaseWorkflowXPathRepository(dbContext, import.TenantRegistryId);
                caseWorkflowXPathRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId, import.Id);

                var caseWorkflowStatusRepository = new CaseWorkflowStatusRepository(dbContext, import.TenantRegistryId);
                caseWorkflowStatusRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId, import.Id);

                var caseWorkflowFormRepository = new CaseWorkflowFormRepository(dbContext, import.TenantRegistryId);
                caseWorkflowFormRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId, import.Id);

                var caseWorkflowActionRepository = new CaseWorkflowActionRepository(dbContext, import.TenantRegistryId);
                caseWorkflowActionRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId, import.Id);

                var caseWorkflowDisplayRepository = new CaseWorkflowDisplayRepository(dbContext, import.TenantRegistryId);
                caseWorkflowDisplayRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId, import.Id);

                var caseWorkflowMacro = new CaseWorkflowMacroRepository(dbContext, import.TenantRegistryId);
                caseWorkflowMacro.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId, import.Id);

                var caseWorkflowFilterRepository = new CaseWorkflowFilterRepository(dbContext, import.TenantRegistryId);
                caseWorkflowFilterRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId, import.Id);

                var entityAnalysisModelSuppressionRepository =
                    new EntityAnalysisModelSuppressionRepository(dbContext, import.TenantRegistryId);
                entityAnalysisModelSuppressionRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId,
                    import.Id);

                var entityAnalysisModelActivationRuleSuppressionRepository =
                    new EntityAnalysisModelActivationRuleSuppressionRepository(dbContext,
                        import.TenantRegistryId);
                entityAnalysisModelActivationRuleSuppressionRepository.DeleteByTenantRegistryIdOutsideOfInstance(
                    import.TenantRegistryId,
                    import.Id);

                var exhaustiveSearchInstanceRepository =
                    new ExhaustiveSearchInstanceRepository(dbContext, import.TenantRegistryId);
                exhaustiveSearchInstanceRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId, import.Id);

                var exhaustiveSearchInstanceDataRepository =
                    new ExhaustiveSearchInstanceDataRepository(dbContext);
                exhaustiveSearchInstanceDataRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId,
                    import.Id);

                var exhaustiveSearchInstanceTrialInstanceRepository =
                    new ExhaustiveSearchInstanceTrialInstanceRepository(dbContext);
                exhaustiveSearchInstanceTrialInstanceRepository.DeleteByTenantRegistryIdOutsideOfInstance(
                    import.TenantRegistryId,
                    import.Id);

                var exhaustiveSearchInstanceVariableRepository =
                    new ExhaustiveSearchInstanceVariableRepository(dbContext);
                exhaustiveSearchInstanceVariableRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId,
                    import.Id);

                var exhaustiveSearchInstanceTrialInstanceVariableRepository =
                    new ExhaustiveSearchInstanceTrialInstanceVariableRepository(dbContext);
                exhaustiveSearchInstanceTrialInstanceVariableRepository.DeleteByTenantRegistryIdOutsideOfInstance(
                    import.TenantRegistryId,
                    import.Id);

                var exhaustiveSearchInstancePromotedTrialInstanceRepository =
                    new ExhaustiveSearchInstancePromotedTrialInstanceRepository(dbContext,
                        import.TenantRegistryId);
                exhaustiveSearchInstancePromotedTrialInstanceRepository.DeleteByTenantRegistryIdOutsideOfInstance(
                    import.TenantRegistryId,
                    import.Id);

                var exhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository =
                    new ExhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository(dbContext);
                exhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository.DeleteByTenantRegistryIdOutsideOfInstance(
                    import.TenantRegistryId, import.Id);

                var exhaustiveSearchInstancePromotedTrialInstanceRocRepository =
                    new ExhaustiveSearchInstancePromotedTrialInstanceRocRepository(dbContext);
                exhaustiveSearchInstancePromotedTrialInstanceRocRepository.DeleteByTenantRegistryIdOutsideOfInstance(
                    import.TenantRegistryId,
                    import.Id);

                var exhaustiveSearchInstanceTrialInstanceTopologyTrialRepository =
                    new ExhaustiveSearchInstanceTrialInstanceTopologyTrialRepository(dbContext);
                exhaustiveSearchInstanceTrialInstanceTopologyTrialRepository.DeleteByTenantRegistryIdOutsideOfInstance(
                    import.TenantRegistryId,
                    import.Id);

                var exhaustiveSearchInstanceTrialInstanceSensitivityRepository =
                    new ExhaustiveSearchInstanceTrialInstanceSensitivityRepository(dbContext);
                exhaustiveSearchInstanceTrialInstanceSensitivityRepository.DeleteByTenantRegistryIdOutsideOfInstance(
                    import.TenantRegistryId,
                    import.Id);

                var exhaustiveSearchInstanceTrialInstanceActivationFunctionTrialRepository =
                    new ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrialRepository(dbContext);
                exhaustiveSearchInstanceTrialInstanceActivationFunctionTrialRepository.DeleteByTenantRegistryIdOutsideOfInstance(
                    import.TenantRegistryId, import.Id);

                var exhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository =
                    new ExhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository(dbContext);
                exhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository.DeleteByTenantRegistryIdOutsideOfInstance(
                    import.TenantRegistryId, import.Id);

                var exhaustiveSearchInstancePromotedTrialInstanceVariableRepository =
                    new ExhaustiveSearchInstancePromotedTrialInstanceVariableRepository(dbContext);
                exhaustiveSearchInstancePromotedTrialInstanceVariableRepository.DeleteByTenantRegistryIdOutsideOfInstance(
                    import.TenantRegistryId, import.Id);

                var exhaustiveSearchInstanceVariableAnomalyRepository =
                    new ExhaustiveSearchInstanceVariableAnomalyRepository(dbContext);
                exhaustiveSearchInstanceVariableAnomalyRepository.DeleteByTenantRegistryIdOutsideOfInstance(
                    import.TenantRegistryId,
                    import.Id);

                var exhaustiveSearchInstanceVariableClassificationRepository =
                    new ExhaustiveSearchInstanceVariableClassificationRepository(dbContext);
                exhaustiveSearchInstanceVariableClassificationRepository.DeleteByTenantRegistryIdOutsideOfInstance(
                    import.TenantRegistryId,
                    import.Id);

                var exhaustiveSearchInstanceVariableHistogramRepository =
                    new ExhaustiveSearchInstanceVariableHistogramRepository(dbContext);
                exhaustiveSearchInstanceVariableHistogramRepository.DeleteByTenantRegistryIdOutsideOfInstance(
                    import.TenantRegistryId,
                    import.Id);

                var exhaustiveSearchInstanceVariableHistogramClassificationRepository =
                    new ExhaustiveSearchInstanceVariableHistogramClassificationRepository(dbContext);
                exhaustiveSearchInstanceVariableHistogramClassificationRepository.DeleteByTenantRegistryIdOutsideOfInstance(
                    import.TenantRegistryId, import.Id);

                var exhaustiveSearchInstanceVariableHistogramAnomalyRepository =
                    new ExhaustiveSearchInstanceVariableHistogramAnomalyRepository(dbContext);
                exhaustiveSearchInstanceVariableHistogramAnomalyRepository.DeleteByTenantRegistryIdOutsideOfInstance(
                    import.TenantRegistryId,
                    import.Id);

                var exhaustiveSearchInstanceVariableMulticollinearityRepository =
                    new ExhaustiveSearchInstanceVariableMultiColiniarityRepository(dbContext);
                exhaustiveSearchInstanceVariableMulticollinearityRepository.DeleteByTenantRegistryIdOutsideOfInstance(
                    import.TenantRegistryId,
                    import.Id);

                var entityAnalysisModelListRepository =
                    new EntityAnalysisModelListRepository(dbContext, import.TenantRegistryId);
                entityAnalysisModelListRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId, import.Id);

                var entityAnalysisModelListValueRepository =
                    new EntityAnalysisModelListValueRepository(dbContext, import.TenantRegistryId);
                entityAnalysisModelListValueRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId,
                    import.Id);

                var entityAnalysisModelDictionaryRepository =
                    new EntityAnalysisModelDictionaryRepository(dbContext, import.TenantRegistryId);
                entityAnalysisModelDictionaryRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId,
                    import.Id);

                var entityAnalysisModelDictionaryKvpRepository =
                    new EntityAnalysisModelDictionaryKvpRepository(dbContext, import.TenantRegistryId);
                entityAnalysisModelDictionaryKvpRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId,
                    import.Id);

                var visualisationRegistryRepository =
                    new VisualisationRegistryRepository(dbContext, import.TenantRegistryId);
                visualisationRegistryRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId, import.Id);

                var visualisationRegistryDatasourceRepository =
                    new VisualisationRegistryDatasourceRepository(dbContext, import.TenantRegistryId);
                visualisationRegistryDatasourceRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId, import.Id);

                var visualisationRegistryParameterRepository =
                    new VisualisationRegistryParameterRepository(dbContext, import.TenantRegistryId);
                visualisationRegistryParameterRepository.DeleteByTenantRegistryIdOutsideOfInstance(import.TenantRegistryId, import.Id);

                if (wrapper.Payload?.EntityAnalysisModel != null)
                {
                    foreach (var oldEntityAnalysisModel in wrapper.Payload.EntityAnalysisModel)
                    {
                        var newEntityAnalysisModel = entityAnalysisModelRepository.Insert(oldEntityAnalysisModel);

                        foreach (var entityAnalysisModelRequestXpath in oldEntityAnalysisModel
                                     .EntityAnalysisModelRequestXpath)
                        {
                            entityAnalysisModelRequestXpath.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            entityAnalysisModelRequestXPathRepository.Insert(entityAnalysisModelRequestXpath);
                        }

                        if (options.Suppressions)
                        {
                            foreach (var entityAnalysisModelSuppression in oldEntityAnalysisModel
                                         .EntityAnalysisModelSuppression)
                            {
                                entityAnalysisModelSuppressionRepository.Insert(entityAnalysisModelSuppression);
                            }

                            foreach (var entityAnalysisModelActivationRuleSuppression in oldEntityAnalysisModel
                                         .EntityAnalysisModelActivationRuleSuppression)
                            {
                                entityAnalysisModelActivationRuleSuppressionRepository.Insert(
                                    entityAnalysisModelActivationRuleSuppression);
                            }
                        }

                        foreach (var entityAnalysisModelInlineFunction in oldEntityAnalysisModel
                                     .EntityAnalysisModelInlineFunction)
                        {
                            entityAnalysisModelInlineFunction.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            entityAnalysisModelInlineFunctionRepository.Insert(entityAnalysisModelInlineFunction);
                        }

                        foreach (var entityAnalysisModelInlineScript in oldEntityAnalysisModel
                                     .EntityAnalysisModelInlineScript)
                        {
                            entityAnalysisModelInlineScript.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            entityAnalysisModelInlineScriptRepository.Insert(entityAnalysisModelInlineScript);
                        }

                        foreach (var entityAnalysisModelGatewayRule in oldEntityAnalysisModel
                                     .EntityAnalysisModelGatewayRule)
                        {
                            entityAnalysisModelGatewayRule.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            entityAnalysisModelGatewayRuleRepository.Insert(entityAnalysisModelGatewayRule);
                        }

                        foreach (var entityAnalysisModelSanction in oldEntityAnalysisModel
                                     .EntityAnalysisModelSanction)
                        {
                            entityAnalysisModelSanction.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            entityAnalysisModelSanctionRepository.Insert(entityAnalysisModelSanction);
                        }

                        foreach (var entityAnalysisModelTag in oldEntityAnalysisModel
                                     .EntityAnalysisModelTag)
                        {
                            entityAnalysisModelTag.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            entityAnalysisModelTagRepository.Insert(entityAnalysisModelTag);
                        }

                        foreach (var entityAnalysisModelTtlCounter in oldEntityAnalysisModel
                                     .EntityAnalysisModelTtlCounter)
                        {
                            entityAnalysisModelTtlCounter.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            entityAnalysisModelTtlCounterRepository.Insert(entityAnalysisModelTtlCounter);
                        }

                        foreach (var entityAnalysisModelAbstractionRule in oldEntityAnalysisModel
                                     .EntityAnalysisModelAbstractionRule)
                        {
                            entityAnalysisModelAbstractionRule.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            entityAnalysisModelAbstractionRuleRepository.Insert(entityAnalysisModelAbstractionRule);
                        }

                        foreach (var entityAnalysisModelAbstractionCalculations in oldEntityAnalysisModel
                                     .EntityAnalysisModelAbstractionCalculation)
                        {
                            entityAnalysisModelAbstractionCalculations.EntityAnalysisModelId =
                                newEntityAnalysisModel.Id;
                            entityAnalysisModelAbstractionCalculationRepository.Insert(
                                entityAnalysisModelAbstractionCalculations);
                        }

                        foreach (var entityAnalysisModelHttpAdaptation in oldEntityAnalysisModel
                                     .EntityAnalysisModelHttpAdaptation)
                        {
                            entityAnalysisModelHttpAdaptation.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            entityAnalysisModelHttpAdaptationRepository.Insert(entityAnalysisModelHttpAdaptation);
                        }

                        if (options.Exhaustive)
                        {
                            foreach (var entityAnalysisModelExhaustiveSearchInstance in oldEntityAnalysisModel
                                         .ExhaustiveSearchInstance)
                            {
                                entityAnalysisModelExhaustiveSearchInstance.EntityAnalysisModelId =
                                    newEntityAnalysisModel.Id;

                                var entityAnalysisModelExhaustiveSearchInstanceId = exhaustiveSearchInstanceRepository
                                    .Insert(entityAnalysisModelExhaustiveSearchInstance).Id;

                                foreach (var exhaustiveSearchInstanceData in entityAnalysisModelExhaustiveSearchInstance
                                             .ExhaustiveSearchInstanceData)
                                {
                                    exhaustiveSearchInstanceData.ExhaustiveSearchInstanceId =
                                        entityAnalysisModelExhaustiveSearchInstanceId;

                                    exhaustiveSearchInstanceDataRepository.Insert(
                                        exhaustiveSearchInstanceData);
                                }

                                foreach (var exhaustiveSearchInstanceTrialInstance in
                                         entityAnalysisModelExhaustiveSearchInstance
                                             .ExhaustiveSearchInstanceTrialInstance)
                                {
                                    exhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstanceId =
                                        entityAnalysisModelExhaustiveSearchInstanceId;

                                    var exhaustiveSearchInstanceTrialInstanceId =
                                        exhaustiveSearchInstanceTrialInstanceRepository.Insert(
                                            exhaustiveSearchInstanceTrialInstance).Id;

                                    foreach (var exhaustiveSearchInstanceTrialInstanceVariable in
                                             exhaustiveSearchInstanceTrialInstance
                                                 .ExhaustiveSearchInstanceTrialInstanceVariable)
                                    {
                                        exhaustiveSearchInstanceTrialInstanceVariable
                                                .ExhaustiveSearchInstanceTrialInstanceId =
                                            exhaustiveSearchInstanceTrialInstanceId;

                                        var exhaustiveSearchInstanceTrialInstanceVariableId =
                                            exhaustiveSearchInstanceTrialInstanceVariableRepository.Insert(
                                                exhaustiveSearchInstanceTrialInstanceVariable).Id;

                                        foreach (var exhaustiveSearchInstancePromotedTrialInstanceSensitivity in
                                                 exhaustiveSearchInstanceTrialInstanceVariable
                                                     .ExhaustiveSearchInstancePromotedTrialInstanceSensitivity)
                                        {
                                            exhaustiveSearchInstancePromotedTrialInstanceSensitivity
                                                    .ExhaustiveSearchInstanceTrialInstanceVariableId =
                                                exhaustiveSearchInstanceTrialInstanceVariableId;

                                            exhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository.Insert(
                                                exhaustiveSearchInstancePromotedTrialInstanceSensitivity);
                                        }

                                        foreach (var exhaustiveSearchInstancePromotedTrialInstanceVariable in
                                                 exhaustiveSearchInstanceTrialInstanceVariable
                                                     .ExhaustiveSearchInstancePromotedTrialInstanceVariable)
                                        {
                                            exhaustiveSearchInstancePromotedTrialInstanceVariable
                                                    .ExhaustiveSearchInstanceTrialInstanceVariableId =
                                                exhaustiveSearchInstanceTrialInstanceVariableId;

                                            exhaustiveSearchInstancePromotedTrialInstanceVariableRepository.Insert(
                                                exhaustiveSearchInstancePromotedTrialInstanceVariable);
                                        }
                                    }

                                    foreach (var exhaustiveSearchInstancePromotedTrialInstance in
                                             exhaustiveSearchInstanceTrialInstance
                                                 .ExhaustiveSearchInstancePromotedTrialInstance)
                                    {
                                        exhaustiveSearchInstancePromotedTrialInstance
                                                .ExhaustiveSearchInstanceTrialInstanceId =
                                            exhaustiveSearchInstanceTrialInstanceId;

                                        exhaustiveSearchInstancePromotedTrialInstanceRepository.Insert(
                                            exhaustiveSearchInstancePromotedTrialInstance);
                                    }

                                    foreach (var exhaustiveSearchInstancePromotedTrialInstancePredictedActual in
                                             exhaustiveSearchInstanceTrialInstance
                                                 .ExhaustiveSearchInstancePromotedTrialInstancePredictedActual)
                                    {
                                        exhaustiveSearchInstancePromotedTrialInstancePredictedActual
                                                .ExhaustiveSearchInstanceTrialInstanceId =
                                            exhaustiveSearchInstanceTrialInstanceId;

                                        exhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository.Insert(
                                            exhaustiveSearchInstancePromotedTrialInstancePredictedActual);
                                    }

                                    foreach (var exhaustiveSearchInstancePromotedTrialInstanceRoc in
                                             exhaustiveSearchInstanceTrialInstance
                                                 .ExhaustiveSearchInstancePromotedTrialInstanceRoc)
                                    {
                                        exhaustiveSearchInstancePromotedTrialInstanceRoc
                                                .ExhaustiveSearchInstanceTrialInstanceId =
                                            exhaustiveSearchInstanceTrialInstanceId;

                                        exhaustiveSearchInstancePromotedTrialInstanceRocRepository.Insert(
                                            exhaustiveSearchInstancePromotedTrialInstanceRoc);
                                    }

                                    foreach (var exhaustiveSearchInstanceTrialInstanceTopologyTrial in
                                             exhaustiveSearchInstanceTrialInstance
                                                 .ExhaustiveSearchInstanceTrialInstanceTopologyTrial)
                                    {
                                        exhaustiveSearchInstanceTrialInstanceTopologyTrial
                                                .ExhaustiveSearchInstanceTrialInstanceId =
                                            exhaustiveSearchInstanceTrialInstanceId;

                                        exhaustiveSearchInstanceTrialInstanceTopologyTrialRepository.Insert(
                                            exhaustiveSearchInstanceTrialInstanceTopologyTrial);
                                    }

                                    foreach (var exhaustiveSearchInstanceTrialInstanceSensitivity in
                                             exhaustiveSearchInstanceTrialInstance
                                                 .ExhaustiveSearchInstanceTrialInstanceSensitivity)
                                    {
                                        exhaustiveSearchInstanceTrialInstanceSensitivity
                                                .ExhaustiveSearchInstanceTrialInstanceId =
                                            exhaustiveSearchInstanceTrialInstanceId;

                                        exhaustiveSearchInstanceTrialInstanceSensitivityRepository.Insert(
                                            exhaustiveSearchInstanceTrialInstanceSensitivity);
                                    }

                                    foreach (var exhaustiveSearchInstanceTrialInstanceActivationFunctionTrial in
                                             exhaustiveSearchInstanceTrialInstance
                                                 .ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial)
                                    {
                                        exhaustiveSearchInstanceTrialInstanceActivationFunctionTrial
                                                .ExhaustiveSearchInstanceTrialInstanceId =
                                            exhaustiveSearchInstanceTrialInstanceId;

                                        exhaustiveSearchInstanceTrialInstanceActivationFunctionTrialRepository.Insert(
                                            exhaustiveSearchInstanceTrialInstanceActivationFunctionTrial);
                                    }
                                }

                                foreach (var exhaustiveSearchInstanceVariable in
                                         entityAnalysisModelExhaustiveSearchInstance
                                             .ExhaustiveSearchInstanceVariable)
                                {
                                    exhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceId =
                                        entityAnalysisModelExhaustiveSearchInstanceId;

                                    var exhaustiveSearchInstanceVariableId =
                                        exhaustiveSearchInstanceVariableRepository.Insert(
                                            exhaustiveSearchInstanceVariable);

                                    foreach (var exhaustiveSearchInstanceVariableHistogram in
                                             exhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceVariableHistogram)
                                    {
                                        exhaustiveSearchInstanceVariableHistogram.ExhaustiveSearchInstanceVariableId =
                                            exhaustiveSearchInstanceVariableId;

                                        exhaustiveSearchInstanceVariableHistogramRepository.Insert(
                                            exhaustiveSearchInstanceVariableHistogram);
                                    }

                                    foreach (var exhaustiveSearchInstanceVariableAnomaly in
                                             exhaustiveSearchInstanceVariable
                                                 .ExhaustiveSearchInstanceVariableAnomaly)
                                    {
                                        exhaustiveSearchInstanceVariableAnomaly.ExhaustiveSearchInstanceVariableId =
                                            exhaustiveSearchInstanceVariableId;

                                        var exhaustiveSearchInstanceVariableAnomalyId =
                                            exhaustiveSearchInstanceVariableAnomalyRepository.Insert(
                                                exhaustiveSearchInstanceVariableAnomaly);

                                        foreach (var exhaustiveSearchInstanceVariableHistogramAnomaly in
                                                 exhaustiveSearchInstanceVariable
                                                     .ExhaustiveSearchInstanceVariableHistogramAnomaly)
                                        {
                                            exhaustiveSearchInstanceVariableHistogramAnomaly
                                                    .ExhaustiveSearchInstanceVariableAnomalyId =
                                                exhaustiveSearchInstanceVariableAnomalyId;

                                            exhaustiveSearchInstanceVariableHistogramAnomalyRepository.Insert(
                                                exhaustiveSearchInstanceVariableHistogramAnomaly);
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
                                            exhaustiveSearchInstanceVariableClassificationRepository.Insert(
                                                exhaustiveSearchInstanceVariableClassification);

                                        foreach (var exhaustiveSearchInstanceVariableHistogramClassification in
                                                 exhaustiveSearchInstanceVariable
                                                     .ExhaustiveSearchInstanceVariableHistogramClassification)
                                        {
                                            exhaustiveSearchInstanceVariableHistogramClassification
                                                    .ExhaustiveSearchInstanceVariableClassificationId =
                                                exhaustiveSearchInstanceVariableClassificationId;

                                            exhaustiveSearchInstanceVariableHistogramClassificationRepository.Insert(
                                                exhaustiveSearchInstanceVariableHistogramClassification);
                                        }
                                    }

                                    foreach (var exhaustiveSearchInstanceVariableMultiCollinearity in
                                             exhaustiveSearchInstanceVariable
                                                 .ExhaustiveSearchInstanceVariableMultiCollinearity)
                                    {
                                        exhaustiveSearchInstanceVariableMultiCollinearity
                                                .ExhaustiveSearchInstanceVariableId =
                                            exhaustiveSearchInstanceVariableId;

                                        exhaustiveSearchInstanceVariableMulticollinearityRepository.Insert(
                                            exhaustiveSearchInstanceVariableMultiCollinearity);
                                    }
                                }
                            }
                        }

                        foreach (var entityAnalysisModelActivationRule in oldEntityAnalysisModel
                                     .EntityAnalysisModelActivationRule)
                        {
                            entityAnalysisModelActivationRule.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            entityAnalysisModelActivationRuleRepository.Insert(entityAnalysisModelActivationRule);
                        }

                        foreach (var oldEntityAnalysisModelCaseWorkflow in oldEntityAnalysisModel
                                     .CaseWorkflow)
                        {
                            oldEntityAnalysisModelCaseWorkflow.EntityAnalysisModelId = newEntityAnalysisModel.Id;
                            var entityAnalysisModelCaseWorkflowId =
                                caseWorkflowRepository.Insert(oldEntityAnalysisModelCaseWorkflow).Id;

                            foreach (var oldCaseWorkflowsXPath in oldEntityAnalysisModelCaseWorkflow
                                         .CaseWorkflowXPath)
                            {
                                oldCaseWorkflowsXPath.CaseWorkflowId = entityAnalysisModelCaseWorkflowId;
                                caseWorkflowXPathRepository.Insert(oldCaseWorkflowsXPath);
                            }

                            foreach (var oldCaseWorkflowsAction in oldEntityAnalysisModelCaseWorkflow
                                         .CaseWorkflowAction)
                            {
                                oldCaseWorkflowsAction.CaseWorkflowId = entityAnalysisModelCaseWorkflowId;
                                caseWorkflowActionRepository.Insert(oldCaseWorkflowsAction);
                            }

                            foreach (var oldCaseWorkflowsDisplay in oldEntityAnalysisModelCaseWorkflow
                                         .CaseWorkflowDisplay)
                            {
                                oldCaseWorkflowsDisplay.CaseWorkflowId = entityAnalysisModelCaseWorkflowId;
                                caseWorkflowDisplayRepository.Insert(oldCaseWorkflowsDisplay);
                            }

                            foreach (var oldCaseWorkflowsForm in oldEntityAnalysisModelCaseWorkflow
                                         .CaseWorkflowForm)
                            {
                                oldCaseWorkflowsForm.CaseWorkflowId = entityAnalysisModelCaseWorkflowId;
                                caseWorkflowFormRepository.Insert(oldCaseWorkflowsForm);
                            }

                            foreach (var oldCaseWorkflowsFilter in oldEntityAnalysisModelCaseWorkflow
                                         .CaseWorkflowFilter)
                            {
                                oldCaseWorkflowsFilter.CaseWorkflowId = entityAnalysisModelCaseWorkflowId;
                                caseWorkflowFilterRepository.Insert(oldCaseWorkflowsFilter);
                            }

                            foreach (var oldCaseWorkflowsMacro in oldEntityAnalysisModelCaseWorkflow
                                         .CaseWorkflowMacro)
                            {
                                oldCaseWorkflowsMacro.CaseWorkflowId = entityAnalysisModelCaseWorkflowId;
                                caseWorkflowMacro.Insert(oldCaseWorkflowsMacro);
                            }

                            foreach (var caseWorkflowsStatus in oldEntityAnalysisModelCaseWorkflow
                                         .CaseWorkflowStatus)
                            {
                                caseWorkflowsStatus.CaseWorkflowId = entityAnalysisModelCaseWorkflowId;
                                caseWorkflowStatusRepository.Insert(caseWorkflowsStatus);
                            }
                        }

                        if (options.Lists)
                        {
                            foreach (var oldEntityAnalysisModelList in oldEntityAnalysisModel
                                         .EntityAnalysisModelList)
                            {
                                oldEntityAnalysisModelList.EntityAnalysisModelGuid = newEntityAnalysisModel.Guid;
                                var newEntityAnalysisModelListId =
                                    entityAnalysisModelListRepository.Insert(oldEntityAnalysisModelList).Id;

                                foreach (var entityAnalysisModelListValue in oldEntityAnalysisModelList
                                             .EntityAnalysisModelListValue)
                                {
                                    entityAnalysisModelListValue.EntityAnalysisModelListId =
                                        newEntityAnalysisModelListId;
                                    entityAnalysisModelListValueRepository.Insert(entityAnalysisModelListValue);
                                }
                            }
                        }

                        // ReSharper disable once InvertIf
                        if (options.Dictionaries)
                        {
                            foreach (var oldEntityAnalysisModelDictionary in oldEntityAnalysisModel
                                         .EntityAnalysisModelDictionary)
                            {
                                oldEntityAnalysisModelDictionary.Guid = newEntityAnalysisModel.Guid;
                                var newEntityAnalysisModelDictionaryId = entityAnalysisModelDictionaryRepository
                                    .Insert(oldEntityAnalysisModelDictionary).Id;

                                foreach (var oldEntityAnalysisModelDictionaryKvp in oldEntityAnalysisModelDictionary
                                             .EntityAnalysisModelDictionaryKvp)
                                {
                                    oldEntityAnalysisModelDictionaryKvp.EntityAnalysisModelDictionaryId =
                                        newEntityAnalysisModelDictionaryId;
                                    entityAnalysisModelDictionaryKvpRepository.Insert(
                                        oldEntityAnalysisModelDictionaryKvp);
                                }
                            }
                        }
                    }
                }

                if (options.Visualisations)
                {
                    if (wrapper.Payload?.VisualisationRegistry != null)
                    {
                        foreach (var visualisationRegistry in wrapper.Payload.VisualisationRegistry)
                        {
                            var visualisationRegistryId = visualisationRegistryRepository.Insert(visualisationRegistry).Id;

                            foreach (var visualisationRegistryDatasource in visualisationRegistry
                                         .VisualisationRegistryDatasource)
                            {
                                visualisationRegistryDatasource.VisualisationRegistryId = visualisationRegistryId;
                                visualisationRegistryDatasourceRepository.Insert(visualisationRegistryDatasource);
                            }

                            foreach (var visualisationRegistryParameter in visualisationRegistry
                                         .VisualisationRegistryParameter)
                            {
                                visualisationRegistryParameter.VisualisationRegistryId = visualisationRegistryId;
                                visualisationRegistryParameterRepository.Insert(visualisationRegistryParameter);
                            }
                        }
                    }
                }

                dbContext.CommitTransaction();
                import.CompletedDate = DateTime.Now;
                import.ExportGuid = wrapper.Guid;
                import.ExportVersion = wrapper.Version;

                importRepository.Update(import);
            }
            catch (Exception ex)
            {
                dbContext.RollbackTransaction();
                import.InError = 1;
                import.ErrorStack = ex.ToString();
                importRepository.Update(import);
                throw;
            }
        }

        public Export Export(ImportExportOptions options)
        {
            var exportRepository = new ExportRepository(dbContext, userName);
            var export = new Export
            {
                CreatedDate = DateTime.Now,
                Guid = Guid.NewGuid()
            };

            export = exportRepository.Insert(export);

            try
            {
                var wrapper = new Wrapper
                {
                    Guid = export.Guid,
                    Version = 1,
                    Payload = ExportPayload(options, export.TenantRegistryId)
                };

                var lz4Options =
                    MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

                var bytes = MessagePackSerializer.Serialize(wrapper, lz4Options);

                var aesEncryption = new AesEncryption(options.Password ?? "", salt);
                var encryptedBytes = aesEncryption.Encrypt(bytes);

                export.Bytes = bytes;
                export.EncryptedBytes = encryptedBytes;
                export.CompletedDate = DateTime.Now;
                export.Version = wrapper.Version;

                exportRepository.Update(export);

                return export;
            }
            catch (Exception ex)
            {
                export.InError = 1;
                export.ErrorStack = ex.ToString();
                exportRepository.Update(export);
                throw;
            }
        }

        public ExportPeek ExportPeek(ImportExportOptions options)
        {
            var exportPeekRepository = new ExportPeekRepository(dbContext, userName);

            var exportPeek = new ExportPeek
            {
                Guid = Guid.NewGuid(),
                CreatedUser = userName
            };

            exportPeek = exportPeekRepository.Insert(exportPeek);

            try
            {
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();

                var payload = ExportPayload(options, exportPeek.TenantRegistryId);

                exportPeek.Yaml = serializer.Serialize(payload);
                exportPeek.CompletedDate = DateTime.Now;

                exportPeekRepository.Update(exportPeek);

                return exportPeek;
            }
            catch (Exception ex)
            {
                exportPeek.InError = 1;
                exportPeek.ErrorStack = ex.ToString();
                exportPeekRepository.Update(exportPeek);
                throw;
            }
        }

        private Payload ExportPayload(ImportExportOptions options, int tenantRegistryId)
        {
            var payload = new Payload();

            var entityAnalysisModelRepository = new EntityAnalysisModelRepository(dbContext, tenantRegistryId);
            payload.EntityAnalysisModel = entityAnalysisModelRepository.Get().ToList();

            foreach (var entityAnalysisModel in payload.EntityAnalysisModel)
            {
                if (options.Suppressions)
                {
                    var entityAnalysisModelSuppressionRepository =
                        new EntityAnalysisModelSuppressionRepository(dbContext, tenantRegistryId);
                    entityAnalysisModel.EntityAnalysisModelSuppression
                        = entityAnalysisModelSuppressionRepository
                            .GetByEntityAnalysisModelGuidOrderById(entityAnalysisModel.Guid)
                            .ToList();

                    var entityAnalysisModelActivationRuleSuppressionRepository =
                        new EntityAnalysisModelActivationRuleSuppressionRepository(dbContext, tenantRegistryId);
                    entityAnalysisModel.EntityAnalysisModelActivationRuleSuppression
                        = entityAnalysisModelActivationRuleSuppressionRepository
                            .GetByEntityAnalysisModelGuidOrderById(entityAnalysisModel.Guid).ToList();
                }

                var entityAnalysisModelRequestXPathRepository =
                    new EntityAnalysisModelRequestXPathRepository(dbContext, tenantRegistryId);
                entityAnalysisModel.EntityAnalysisModelRequestXpath
                    = entityAnalysisModelRequestXPathRepository.GetByEntityAnalysisModelIdOrderById(entityAnalysisModel.Id)
                        .ToList();

                var entityAnalysisModelInlineFunctionRepository =
                    new EntityAnalysisModelInlineFunctionRepository(dbContext, tenantRegistryId);
                entityAnalysisModel.EntityAnalysisModelInlineFunction
                    = entityAnalysisModelInlineFunctionRepository
                        .GetByEntityAnalysisModelIdOrderById(entityAnalysisModel.Id)
                        .ToList();

                var entityAnalysisModelInlineScriptRepository =
                    new EntityAnalysisModelInlineScriptRepository(dbContext, tenantRegistryId);
                entityAnalysisModel.EntityAnalysisModelInlineScript
                    = entityAnalysisModelInlineScriptRepository.GetByEntityAnalysisModelIdOrderById(entityAnalysisModel.Id)
                        .ToList();

                var entityAnalysisModelGatewayRuleRepository =
                    new EntityAnalysisModelGatewayRuleRepository(dbContext, tenantRegistryId);
                entityAnalysisModel.EntityAnalysisModelGatewayRule
                    = entityAnalysisModelGatewayRuleRepository.GetByEntityAnalysisModelIdOrderById(entityAnalysisModel.Id)
                        .ToList();

                var entityAnalysisModelSanctionRepository =
                    new EntityAnalysisModelSanctionRepository(dbContext, tenantRegistryId);
                entityAnalysisModel.EntityAnalysisModelSanction
                    = entityAnalysisModelSanctionRepository.GetByEntityAnalysisModelIdOrderById(entityAnalysisModel.Id)
                        .ToList();

                var entityAnalysisModelTagRepository =
                    new EntityAnalysisModelTagRepository(dbContext, tenantRegistryId);
                entityAnalysisModel.EntityAnalysisModelTag
                    = entityAnalysisModelTagRepository.GetByEntityAnalysisModelIdOrderById(entityAnalysisModel.Id).ToList();

                var entityAnalysisModelTtlCounterRepository =
                    new EntityAnalysisModelTtlCounterRepository(dbContext, tenantRegistryId);
                entityAnalysisModel.EntityAnalysisModelTtlCounter
                    = entityAnalysisModelTtlCounterRepository.GetByEntityAnalysisModelIdOrderById(entityAnalysisModel.Id)
                        .ToList();

                var entityAnalysisModelAbstractionRuleRepository =
                    new EntityAnalysisModelAbstractionRuleRepository(dbContext, tenantRegistryId);
                entityAnalysisModel.EntityAnalysisModelAbstractionRule
                    = entityAnalysisModelAbstractionRuleRepository
                        .GetByEntityAnalysisModelIdOrderByIdDesc(entityAnalysisModel.Id)
                        .ToList();

                var entityAnalysisModelAbstractionCalculationRepository =
                    new EntityAnalysisModelAbstractionCalculationRepository(dbContext, tenantRegistryId);
                entityAnalysisModel.EntityAnalysisModelAbstractionCalculation
                    = entityAnalysisModelAbstractionCalculationRepository
                        .GetByEntityAnalysisModelIdOrderByIdDesc(entityAnalysisModel.Id)
                        .ToList();

                var entityAnalysisModelHttpAdaptationRepository =
                    new EntityAnalysisModelHttpAdaptationRepository(dbContext, tenantRegistryId);
                entityAnalysisModel.EntityAnalysisModelHttpAdaptation
                    = entityAnalysisModelHttpAdaptationRepository
                        .GetByEntityAnalysisModelIdOrderById(entityAnalysisModel.Id)
                        .ToList();

                if (options.Exhaustive)
                {
                    var exhaustiveSearchInstanceRepository =
                        new ExhaustiveSearchInstanceRepository(dbContext, tenantRegistryId);
                    entityAnalysisModel.ExhaustiveSearchInstance
                        = exhaustiveSearchInstanceRepository.GetByEntityAnalysisModelIdOrderById(entityAnalysisModel.Id)
                            .ToList();

                    foreach (var exhaustiveSearchInstance in entityAnalysisModel.ExhaustiveSearchInstance)
                    {
                        var exhaustiveSearchInstanceDataRepository =
                            new ExhaustiveSearchInstanceDataRepository(dbContext);
                        exhaustiveSearchInstance.ExhaustiveSearchInstanceData =
                            exhaustiveSearchInstanceDataRepository.GetByExhaustiveSearchInstanceIdOrderById(
                                exhaustiveSearchInstance.Id).ToList();

                        var exhaustiveSearchInstanceTrialInstanceRepository =
                            new ExhaustiveSearchInstanceTrialInstanceRepository(dbContext);
                        exhaustiveSearchInstance.ExhaustiveSearchInstanceTrialInstance =
                            exhaustiveSearchInstanceTrialInstanceRepository.GetByExhaustiveSearchInstanceIdOrderById(
                                exhaustiveSearchInstance.Id).ToList();

                        var exhaustiveSearchInstanceVariableRepository =
                            new ExhaustiveSearchInstanceVariableRepository(dbContext);
                        exhaustiveSearchInstance.ExhaustiveSearchInstanceVariable =
                            exhaustiveSearchInstanceVariableRepository.GetByExhaustiveSearchInstanceIdOrderById(
                                exhaustiveSearchInstance
                                    .Id).ToList();

                        foreach (var exhaustiveSearchInstanceTrialInstance in exhaustiveSearchInstance
                                     .ExhaustiveSearchInstanceTrialInstance)
                        {
                            var exhaustiveSearchInstanceTrialInstanceVariableRepository =
                                new ExhaustiveSearchInstanceTrialInstanceVariableRepository(dbContext);
                            exhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstanceTrialInstanceVariable =
                                exhaustiveSearchInstanceTrialInstanceVariableRepository
                                    .GetByExhaustiveSearchInstanceTrialInstanceIdOrderById(
                                        exhaustiveSearchInstanceTrialInstance
                                            .Id).ToList();

                            var exhaustiveSearchInstancePromotedTrialInstanceRepository =
                                new ExhaustiveSearchInstancePromotedTrialInstanceRepository(dbContext);
                            exhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstancePromotedTrialInstance =
                                exhaustiveSearchInstancePromotedTrialInstanceRepository
                                    .GetByExhaustiveSearchInstanceTrialInstanceIdOrderById(
                                        exhaustiveSearchInstanceTrialInstance
                                            .Id).ToList();

                            var exhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository =
                                new ExhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository(dbContext);
                            exhaustiveSearchInstanceTrialInstance
                                    .ExhaustiveSearchInstancePromotedTrialInstancePredictedActual =
                                exhaustiveSearchInstancePromotedTrialInstancePredictedActualRepository
                                    .GetByExhaustiveSearchInstanceTrialInstanceIdOrderById(
                                        exhaustiveSearchInstanceTrialInstance
                                            .Id).ToList();

                            var exhaustiveSearchInstancePromotedTrialInstanceRocRepository =
                                new ExhaustiveSearchInstancePromotedTrialInstanceRocRepository(dbContext);
                            exhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstancePromotedTrialInstanceRoc =
                                exhaustiveSearchInstancePromotedTrialInstanceRocRepository
                                    .GetByExhaustiveSearchInstanceTrialInstanceIdOrderById(exhaustiveSearchInstance
                                        .Id).ToList();

                            var exhaustiveSearchInstanceTrialInstanceTopologyTrialRepository =
                                new ExhaustiveSearchInstanceTrialInstanceTopologyTrialRepository(dbContext);
                            exhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstanceTrialInstanceTopologyTrial =
                                exhaustiveSearchInstanceTrialInstanceTopologyTrialRepository
                                    .GetByExhaustiveSearchInstanceTrialInstanceIdOrderById(
                                        exhaustiveSearchInstanceTrialInstance
                                            .Id).ToList();

                            var exhaustiveSearchInstanceTrialInstanceSensitivityRepository =
                                new ExhaustiveSearchInstanceTrialInstanceSensitivityRepository(dbContext);
                            exhaustiveSearchInstanceTrialInstance.ExhaustiveSearchInstanceTrialInstanceSensitivity =
                                exhaustiveSearchInstanceTrialInstanceSensitivityRepository
                                    .GetByExhaustiveSearchInstanceTrialInstanceIdOrderById(
                                        exhaustiveSearchInstanceTrialInstance
                                            .Id).ToList();

                            var exhaustiveSearchInstanceTrialInstanceActivationFunctionTrialRepository =
                                new ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrialRepository(dbContext);
                            exhaustiveSearchInstanceTrialInstance
                                    .ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial =
                                exhaustiveSearchInstanceTrialInstanceActivationFunctionTrialRepository
                                    .GetByExhaustiveSearchInstanceTrialInstanceIdOrderById(
                                        exhaustiveSearchInstanceTrialInstance
                                            .Id).ToList();

                            foreach (var exhaustiveSearchInstanceTrialInstanceVariable in
                                     exhaustiveSearchInstanceTrialInstance
                                         .ExhaustiveSearchInstanceTrialInstanceVariable)
                            {
                                var exhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository =
                                    new ExhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository(dbContext);
                                exhaustiveSearchInstanceTrialInstanceVariable
                                        .ExhaustiveSearchInstancePromotedTrialInstanceSensitivity =
                                    exhaustiveSearchInstancePromotedTrialInstanceSensitivityRepository
                                        .GetByExhaustiveSearchInstanceTrialInstanceVariableIdOrderById(
                                            exhaustiveSearchInstanceTrialInstanceVariable.Id).ToList();

                                var exhaustiveSearchInstancePromotedTrialInstanceVariableRepository =
                                    new ExhaustiveSearchInstancePromotedTrialInstanceVariableRepository(dbContext);
                                exhaustiveSearchInstanceTrialInstanceVariable
                                        .ExhaustiveSearchInstancePromotedTrialInstanceVariable =
                                    exhaustiveSearchInstancePromotedTrialInstanceVariableRepository
                                        .GetByExhaustiveSearchInstanceTrialInstanceVariableIdOrderById(
                                            exhaustiveSearchInstanceTrialInstanceVariable.Id).ToList();
                            }
                        }

                        foreach (var exhaustiveSearchInstanceVariable in exhaustiveSearchInstance
                                     .ExhaustiveSearchInstanceVariable)
                        {
                            var exhaustiveSearchInstanceVariableAnomalyRepository =
                                new ExhaustiveSearchInstanceVariableAnomalyRepository(dbContext);
                            exhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceVariableAnomaly =
                                exhaustiveSearchInstanceVariableAnomalyRepository
                                    .GetByExhaustiveSearchInstanceVariableIdOrderById(
                                        exhaustiveSearchInstanceVariable
                                            .Id).ToList();

                            var exhaustiveSearchInstanceVariableClassificationRepository =
                                new ExhaustiveSearchInstanceVariableClassificationRepository(dbContext);
                            exhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceVariableClassification =
                                exhaustiveSearchInstanceVariableClassificationRepository
                                    .GetByExhaustiveSearchInstanceVariableIdOrderById(exhaustiveSearchInstanceVariable
                                        .Id).ToList();

                            var exhaustiveSearchInstanceVariableHistogramRepository =
                                new ExhaustiveSearchInstanceVariableHistogramRepository(dbContext);
                            exhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceVariableHistogram =
                                exhaustiveSearchInstanceVariableHistogramRepository
                                    .GetByExhaustiveSearchInstanceVariableIdOrderById(
                                        exhaustiveSearchInstanceVariable
                                            .Id).ToList();

                            var exhaustiveSearchInstanceVariableHistogramClassificationRepository =
                                new ExhaustiveSearchInstanceVariableHistogramClassificationRepository(dbContext);
                            exhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceVariableHistogramClassification =
                                exhaustiveSearchInstanceVariableHistogramClassificationRepository
                                    .GetByExhaustiveSearchInstanceVariableId(exhaustiveSearchInstanceVariable
                                        .Id).ToList();

                            var exhaustiveSearchInstanceVariableHistogramAnomalyRepository =
                                new ExhaustiveSearchInstanceVariableHistogramAnomalyRepository(dbContext);
                            exhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceVariableHistogramAnomaly =
                                exhaustiveSearchInstanceVariableHistogramAnomalyRepository
                                    .GetByExhaustiveSearchInstanceVariableIdOrderById(exhaustiveSearchInstanceVariable
                                        .Id).ToList();

                            var exhaustiveSearchInstanceVariableMulticollinearityRepository =
                                new ExhaustiveSearchInstanceVariableMultiColiniarityRepository(dbContext);
                            exhaustiveSearchInstanceVariable.ExhaustiveSearchInstanceVariableMultiCollinearity =
                                exhaustiveSearchInstanceVariableMulticollinearityRepository
                                    .GetByExhaustiveSearchInstanceVariableIdOrderById(exhaustiveSearchInstanceVariable
                                        .Id).ToList();
                        }
                    }
                }

                var entityAnalysisModelActivationRuleRepository =
                    new EntityAnalysisModelActivationRuleRepository(dbContext, tenantRegistryId);
                entityAnalysisModel.EntityAnalysisModelActivationRule
                    = entityAnalysisModelActivationRuleRepository
                        .GetByEntityAnalysisModelIdOrderByIdDesc(entityAnalysisModel.Id)
                        .ToList();

                var caseWorkflowRepository = new CaseWorkflowRepository(dbContext, tenantRegistryId);
                entityAnalysisModel.CaseWorkflow
                    = caseWorkflowRepository.GetByEntityAnalysisModelIdOrderById(entityAnalysisModel.Id).ToList();

                foreach (var entityAnalysisModelCaseWorkflowEntityAnalysisModel in entityAnalysisModel
                             .CaseWorkflow)
                {
                    var caseWorkflowXPathRepository =
                        new CaseWorkflowXPathRepository(dbContext, tenantRegistryId);
                    entityAnalysisModelCaseWorkflowEntityAnalysisModel.CaseWorkflowXPath
                        = caseWorkflowXPathRepository.GetByCasesWorkflowIdOrderByIdDesc(
                            entityAnalysisModelCaseWorkflowEntityAnalysisModel.Id);

                    var caseWorkflowStatusRepository =
                        new CaseWorkflowStatusRepository(dbContext, tenantRegistryId);
                    entityAnalysisModelCaseWorkflowEntityAnalysisModel.CaseWorkflowStatus
                        = caseWorkflowStatusRepository.GetByCasesWorkflowIdOrderById(
                            entityAnalysisModelCaseWorkflowEntityAnalysisModel.Id);

                    var caseWorkflowFormRepository = new CaseWorkflowFormRepository(dbContext, tenantRegistryId);
                    entityAnalysisModelCaseWorkflowEntityAnalysisModel.CaseWorkflowForm
                        = caseWorkflowFormRepository.GetByCasesWorkflowIdOrderById(
                            entityAnalysisModelCaseWorkflowEntityAnalysisModel.Id);

                    var caseWorkflowActionRepository =
                        new CaseWorkflowActionRepository(dbContext, tenantRegistryId);
                    entityAnalysisModelCaseWorkflowEntityAnalysisModel.CaseWorkflowAction
                        = caseWorkflowActionRepository.GetByCasesWorkflowIdOrderById(
                            entityAnalysisModelCaseWorkflowEntityAnalysisModel.Id);

                    var caseWorkflowDisplayRepository =
                        new CaseWorkflowDisplayRepository(dbContext, tenantRegistryId);
                    entityAnalysisModelCaseWorkflowEntityAnalysisModel.CaseWorkflowDisplay
                        = caseWorkflowDisplayRepository.GetByCasesWorkflowIdOrderById(
                            entityAnalysisModelCaseWorkflowEntityAnalysisModel.Id);

                    var caseWorkflowMacro = new CaseWorkflowMacroRepository(dbContext, tenantRegistryId);
                    entityAnalysisModelCaseWorkflowEntityAnalysisModel.CaseWorkflowMacro
                        = caseWorkflowMacro.GetByCasesWorkflowIdOrderById(
                            entityAnalysisModelCaseWorkflowEntityAnalysisModel.Id);

                    var caseWorkflowFilterRepository =
                        new CaseWorkflowFilterRepository(dbContext, tenantRegistryId);
                    entityAnalysisModelCaseWorkflowEntityAnalysisModel.CaseWorkflowFilter
                        = caseWorkflowFilterRepository.GetByCasesWorkflowIdOrderById(
                            entityAnalysisModelCaseWorkflowEntityAnalysisModel.Id);
                }

                if (options.Lists)
                {
                    var entityAnalysisModelListRepository =
                        new EntityAnalysisModelListRepository(dbContext, tenantRegistryId);
                    entityAnalysisModel.EntityAnalysisModelList
                        = entityAnalysisModelListRepository.GetByEntityAnalysisModelIdOrderById(entityAnalysisModel.Id)
                            .ToList();

                    foreach (var entityAnalysisModelListsEntityAnalysisModel in entityAnalysisModel
                                 .EntityAnalysisModelList)
                    {
                        var entityAnalysisModelListValueRepository =
                            new EntityAnalysisModelListValueRepository(dbContext, tenantRegistryId);
                        entityAnalysisModelListsEntityAnalysisModel.EntityAnalysisModelListValue
                            = entityAnalysisModelListValueRepository
                                .GetByEntityAnalysisModelListIdOrderById(entityAnalysisModelListsEntityAnalysisModel.Id)
                                .ToList();
                    }
                }

                if (options.Dictionaries)
                {
                    var entityAnalysisModelDictionaryRepository =
                        new EntityAnalysisModelDictionaryRepository(dbContext, tenantRegistryId);
                    entityAnalysisModel.EntityAnalysisModelDictionary
                        = entityAnalysisModelDictionaryRepository
                            .GetByEntityAnalysisModelIdOrderById(entityAnalysisModel.Id)
                            .ToList();

                    foreach (var entityAnalysisModelDictionaryEntityAnalysisModel in entityAnalysisModel
                                 .EntityAnalysisModelDictionary)
                    {
                        var entityAnalysisModelDictionaryKvpRepository =
                            new EntityAnalysisModelDictionaryKvpRepository(dbContext, tenantRegistryId);
                        entityAnalysisModelDictionaryEntityAnalysisModel
                                .EntityAnalysisModelDictionaryKvp
                            = entityAnalysisModelDictionaryKvpRepository
                                .GetByEntityAnalysisModelDictionaryIdOrderById(
                                    entityAnalysisModelDictionaryEntityAnalysisModel
                                        .Id)
                                .ToList();
                    }
                }

                // ReSharper disable once InvertIf
                if (options.Visualisations)
                {
                    var visualisationRegistryRepository =
                        new VisualisationRegistryRepository(dbContext, tenantRegistryId);
                    payload.VisualisationRegistry = visualisationRegistryRepository.GetOrderById().ToList();

                    foreach (var visualisationRegistry in payload.VisualisationRegistry)
                    {
                        var visualisationRegistryDatasourceRepository =
                            new VisualisationRegistryDatasourceRepository(dbContext, tenantRegistryId);
                        visualisationRegistry.VisualisationRegistryDatasource
                            = visualisationRegistryDatasourceRepository
                                .GetByVisualisationRegistryIdOrderById(visualisationRegistry.Id)
                                .ToList();

                        var visualisationRegistryParameterRepository =
                            new VisualisationRegistryParameterRepository(dbContext, tenantRegistryId);
                        visualisationRegistry.VisualisationRegistryParameter
                            = visualisationRegistryParameterRepository
                                .GetByVisualisationRegistryIdOrderById(visualisationRegistry.Id)
                                .ToList();
                    }
                }
            }

            return payload;
        }
    }
}
