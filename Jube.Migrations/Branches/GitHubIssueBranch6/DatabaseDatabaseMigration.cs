using System;
using FluentMigrator;

namespace Jube.Migrations.Branches.GitHubIssueBranch6
{
    [Migration(20250405191700)]
    public class DatabaseDatabaseMigration : Migration
    {
        public override void Up()
        {
            CreateExport();
            CreateExportPeek();
            CreateImport();
            CreateEntityAnalysisModel();
            CreateExhaustiveSearchInstance();
            CreateExhaustiveSearchInstanceVersion();
            CreateEntityAnalysisModelRequestXpathVersion();
            CreateEntityAnalysisModelInlineFunction();
            CreateEntityAnalysisModelInlineScript();
            CreateEntityAnalysisModelGatewayRule();
            CreateEntityAnalysisModelTag();
            CreateEntityAnalysisModelSanction();
            CreateEntityAnalysisModelTtlCounter();
            CreateEntityAnalysisModelAbstractionRule();
            CreateEntityAnalysisModelAbstractionCalculation();
            CreateEntityAnalysisModelHttpAdaptation();
            CreateEntityAnalysisModelActivationRule();
            CreateCaseWorkflow();
            CreateEntityAnalysisModelList();
            CreateEntityAnalysisModelDictionary();
            CreateEntityAnalysisModelActivationRuleSuppression();
            CreateEntityAnalysisModelSuppression();
            CreateVisualisationRegistry();
            CreateCaseWorkflowXPath();
            CreateCaseWorkflowXPathVersion();
            CreateCaseWorkflowStatus();
            CreateCaseWorkflowForm();
            CreateCaseWorkflowAction();
            CreateCaseWorkflowDisplay();
            CreateCaseWorkflowMacro();
            CreateCaseWorkflowFilter();
            CreateEntityAnalysisModelListValue();
            CreateEntityAnalysisModelDictionaryKvp();
            CreateVisualisationRegistryDatasource();
            CreateVisualisationRegistryParameter();
            CreateSessionCaseSearchCompiledSql();
            CreateRoleRegistry();
            CreateUserRegistry();
            CreateRoleRegistryPermission();
            CreateExhaustiveSearchInstanceData();
            CreateExhaustiveSearchInstanceTrialInstance();
            CreateExhaustiveSearchInstanceVariable();
            CreateExhaustiveSearchInstanceTrialInstanceVariable();
            CreateExhaustiveSearchInstancePromotedTrialInstance();
            CreateExhaustiveSearchInstancePromotedTrialInstancePredictedActual();
            CreateExhaustiveSearchInstancePromotedTrialInstanceRoc();
            CreateExhaustiveSearchInstanceTrialInstanceTopologyTrial();
            CreateExhaustiveSearchInstanceTrialInstanceSensitivity();
            CreateExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial();
            CreateExhaustiveSearchInstancePromotedTrialInstanceSensitivity();
            CreateExhaustiveSearchInstanceVariableAnomaly();
            CreateExhaustiveSearchInstanceVariableClassification();
            CreateExhaustiveSearchInstanceVariableHistogram();
            CreateExhaustiveSearchInstanceVariableHistogramClassification();
            CreateExhaustiveSearchInstanceVariableHistogramAnomaly();
            CreateExhaustiveSearchInstanceVariableMultiCollinearity();
            CreateExhaustiveSearchInstancePromotedTrialInstanceVariable();
            CreatePreservationPermissionAndSubscribeToDefaultAdministrator();
            CreateCountersPermissionAndSubscribeToDefaultAdministrator();

            UpdateDataInCase();
            UpdateDataInEntityAnalysisModelActivationRule();
            UpdateDataInEntityAnalysisModelSuppression();
            UpdateDataInEntityAnalysisModelActivationRuleSuppression();
            UpdateDataInEntityAnalysisModelList();
            UpdateDataInEntityAnalysisModelDictionary();
            UpdateDataInVisualisationRegistry();
            UpdateDataInSessionCaseSearchCompiledSql();
            UpdateDataCaseFilterSelectJsonDataAndInstructCaseWorkflowFilterRebuild();
            UpdateEntityAnalysisModelSearchKeyCalculationInstance();
            UpdateEntityAnalysisModelProcessingCounter();
            UpdateEntityAnalysisModelAsynchronousQueueBalance();
            UpdateDefaultTenantRegistryAsLandlord();

            DropUserRoleRegistry();
            DropExhaustiveSearchInstancePromotedTrialInstanceVariableHistogram();
        }

        private void UpdateDefaultTenantRegistryAsLandlord()
        {
            Execute.Sql("""UPDATE "TenantRegistry" b SET "Landlord" = 1 WHERE b."Id" = 1;""");
        }

        private void CreateCountersPermissionAndSubscribeToDefaultAdministrator()
        {
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 39, Name = "View Performance Counters" });
            Insert.IntoTable("RoleRegistryPermission").Row(new
            {
                RoleRegistryId = 1,
                PermissionSpecificationId = 39,
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                Guid = Guid.NewGuid()
            });
        }

        private void CreatePreservationPermissionAndSubscribeToDefaultAdministrator()
        {
            Insert.IntoTable("PermissionSpecification").Row(new { Id = 38, Name = "Allow Preservation Import and Export" });
            Insert.IntoTable("RoleRegistryPermission").Row(new
            {
                RoleRegistryId = 1,
                PermissionSpecificationId = 38,
                Active = 1,
                CreatedDate = DateTime.Now,
                CreatedUser = "Administrator",
                Version = 1,
                Guid = Guid.NewGuid()
            });
        }

        private void CreateExhaustiveSearchInstancePromotedTrialInstanceVariable()
        {
            Alter.Table("ExhaustiveSearchInstancePromotedTrialInstanceVariable").AddColumn("Deleted").AsByte().Nullable();
            Alter.Table("ExhaustiveSearchInstancePromotedTrialInstanceVariable").AddColumn("DeletedDate").AsDateTime2()
                .Nullable();
            Alter.Table("ExhaustiveSearchInstancePromotedTrialInstanceVariable").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("ExhaustiveSearchInstancePromotedTrialInstanceVariable").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");
        }

        private void CreateExhaustiveSearchInstanceVariableMultiCollinearity()
        {
            Alter.Table("ExhaustiveSearchInstanceVariableMultiCollinearity").AddColumn("Deleted").AsByte().Nullable();
            Alter.Table("ExhaustiveSearchInstanceVariableMultiCollinearity").AddColumn("DeletedDate").AsDateTime2()
                .Nullable();
            Alter.Table("ExhaustiveSearchInstanceVariableMultiCollinearity").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("ExhaustiveSearchInstanceVariableMultiCollinearity").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");
        }

        private void CreateExhaustiveSearchInstanceVariableHistogramAnomaly()
        {
            Alter.Table("ExhaustiveSearchInstanceVariableHistogramAnomaly").AddColumn("Deleted").AsByte().Nullable();
            Alter.Table("ExhaustiveSearchInstanceVariableHistogramAnomaly").AddColumn("DeletedDate").AsDateTime2()
                .Nullable();
            Alter.Table("ExhaustiveSearchInstanceVariableHistogramAnomaly").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("ExhaustiveSearchInstanceVariableHistogramAnomaly").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");
        }

        private void CreateExhaustiveSearchInstanceVariableHistogramClassification()
        {
            Alter.Table("ExhaustiveSearchInstanceVariableHistogramClassification").AddColumn("Deleted").AsByte().Nullable();
            Alter.Table("ExhaustiveSearchInstanceVariableHistogramClassification").AddColumn("DeletedDate").AsDateTime2()
                .Nullable();
            Alter.Table("ExhaustiveSearchInstanceVariableHistogramClassification").AddColumn("ImportId").AsInt32()
                .Nullable();

            Create.ForeignKey().FromTable("ExhaustiveSearchInstanceVariableHistogramClassification")
                .ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");
        }

        private void CreateExhaustiveSearchInstanceVariableHistogram()
        {
            Alter.Table("ExhaustiveSearchInstanceVariableHistogram").AddColumn("Deleted").AsByte().Nullable();
            Alter.Table("ExhaustiveSearchInstanceVariableHistogram").AddColumn("DeletedDate").AsDateTime2().Nullable();
            Alter.Table("ExhaustiveSearchInstanceVariableHistogram").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("ExhaustiveSearchInstanceVariableHistogram").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");
        }

        private void CreateExhaustiveSearchInstanceVariableClassification()
        {
            Alter.Table("ExhaustiveSearchInstanceVariableClassification").AddColumn("Deleted").AsByte().Nullable();
            Alter.Table("ExhaustiveSearchInstanceVariableClassification").AddColumn("DeletedDate").AsDateTime2().Nullable();
            Alter.Table("ExhaustiveSearchInstanceVariableClassification").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("ExhaustiveSearchInstanceVariableClassification").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");
        }

        private void CreateExhaustiveSearchInstanceVariableAnomaly()
        {
            Alter.Table("ExhaustiveSearchInstanceVariableAnomaly").AddColumn("Deleted").AsByte().Nullable();
            Alter.Table("ExhaustiveSearchInstanceVariableAnomaly").AddColumn("DeletedDate").AsDateTime2().Nullable();
            Alter.Table("ExhaustiveSearchInstanceVariableAnomaly").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("ExhaustiveSearchInstanceVariableAnomaly").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");
        }

        private void CreateExhaustiveSearchInstancePromotedTrialInstanceSensitivity()
        {
            Alter.Table("ExhaustiveSearchInstancePromotedTrialInstanceSensitivity").AddColumn("Deleted").AsByte()
                .Nullable();
            Alter.Table("ExhaustiveSearchInstancePromotedTrialInstanceSensitivity").AddColumn("DeletedDate").AsDateTime2()
                .Nullable();
            Alter.Table("ExhaustiveSearchInstancePromotedTrialInstanceSensitivity").AddColumn("ImportId").AsInt32()
                .Nullable();

            Create.ForeignKey().FromTable("ExhaustiveSearchInstancePromotedTrialInstanceSensitivity")
                .ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");
        }

        private void CreateExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial()
        {
            Alter.Table("ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial").AddColumn("Deleted").AsByte()
                .Nullable();
            Alter.Table("ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial").AddColumn("DeletedDate")
                .AsDateTime2().Nullable();
            Alter.Table("ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial").AddColumn("ImportId").AsInt32()
                .Nullable();

            Create.ForeignKey().FromTable("ExhaustiveSearchInstanceTrialInstanceActivationFunctionTrial")
                .ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");
        }

        private void CreateExhaustiveSearchInstanceTrialInstanceSensitivity()
        {
            Alter.Table("ExhaustiveSearchInstanceTrialInstanceSensitivity").AddColumn("Deleted").AsByte().Nullable();
            Alter.Table("ExhaustiveSearchInstanceTrialInstanceSensitivity").AddColumn("DeletedDate").AsDateTime2()
                .Nullable();
            Alter.Table("ExhaustiveSearchInstanceTrialInstanceSensitivity").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("ExhaustiveSearchInstanceTrialInstanceSensitivity").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");
        }

        private void CreateExhaustiveSearchInstanceTrialInstanceTopologyTrial()
        {
            Alter.Table("ExhaustiveSearchInstanceTrialInstanceTopologyTrial").AddColumn("Deleted").AsByte().Nullable();
            Alter.Table("ExhaustiveSearchInstanceTrialInstanceTopologyTrial").AddColumn("DeletedDate").AsDateTime2()
                .Nullable();
            Alter.Table("ExhaustiveSearchInstanceTrialInstanceTopologyTrial").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("ExhaustiveSearchInstanceTrialInstanceTopologyTrial").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");
        }

        private void CreateExhaustiveSearchInstancePromotedTrialInstanceRoc()
        {
            Alter.Table("ExhaustiveSearchInstancePromotedTrialInstanceRoc").AddColumn("Deleted").AsByte().Nullable();
            Alter.Table("ExhaustiveSearchInstancePromotedTrialInstanceRoc").AddColumn("DeletedDate").AsDateTime2()
                .Nullable();
            Alter.Table("ExhaustiveSearchInstancePromotedTrialInstanceRoc").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("ExhaustiveSearchInstancePromotedTrialInstanceRoc").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");
        }

        private void CreateExhaustiveSearchInstancePromotedTrialInstancePredictedActual()
        {
            Alter.Table("ExhaustiveSearchInstancePromotedTrialInstancePredictedActual").AddColumn("Deleted").AsByte()
                .Nullable();
            Alter.Table("ExhaustiveSearchInstancePromotedTrialInstancePredictedActual").AddColumn("DeletedDate")
                .AsDateTime2().Nullable();
            Alter.Table("ExhaustiveSearchInstancePromotedTrialInstancePredictedActual").AddColumn("ImportId").AsInt32()
                .Nullable();

            Create.ForeignKey().FromTable("ExhaustiveSearchInstancePromotedTrialInstancePredictedActual")
                .ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");
        }

        private void CreateExhaustiveSearchInstancePromotedTrialInstance()
        {
            Alter.Table("ExhaustiveSearchInstancePromotedTrialInstance").AddColumn("Deleted").AsByte().Nullable();
            Alter.Table("ExhaustiveSearchInstancePromotedTrialInstance").AddColumn("DeletedDate").AsDateTime2().Nullable();
            Alter.Table("ExhaustiveSearchInstancePromotedTrialInstance").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("ExhaustiveSearchInstancePromotedTrialInstance").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");
        }

        private void CreateExhaustiveSearchInstanceTrialInstanceVariable()
        {
            Alter.Table("ExhaustiveSearchInstanceTrialInstanceVariable").AddColumn("Deleted").AsByte().Nullable();
            Alter.Table("ExhaustiveSearchInstanceTrialInstanceVariable").AddColumn("DeletedDate").AsDateTime2().Nullable();
            Alter.Table("ExhaustiveSearchInstanceTrialInstanceVariable").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("ExhaustiveSearchInstanceTrialInstanceVariable").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");
        }

        private void CreateExhaustiveSearchInstanceVariable()
        {
            Alter.Table("ExhaustiveSearchInstanceVariable").AddColumn("Deleted").AsByte().Nullable();
            Alter.Table("ExhaustiveSearchInstanceVariable").AddColumn("DeletedDate").AsDateTime2().Nullable();
            Alter.Table("ExhaustiveSearchInstanceVariable").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("ExhaustiveSearchInstanceVariable").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");
        }

        private void CreateExhaustiveSearchInstanceTrialInstance()
        {
            Alter.Table("ExhaustiveSearchInstanceTrialInstance").AddColumn("Deleted").AsByte().Nullable();
            Alter.Table("ExhaustiveSearchInstanceTrialInstance").AddColumn("DeletedDate").AsDateTime2().Nullable();
            Alter.Table("ExhaustiveSearchInstanceTrialInstance").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("ExhaustiveSearchInstanceTrialInstance").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");
        }

        private void CreateExhaustiveSearchInstanceData()
        {
            Create.Table("ExhaustiveSearchInstanceData")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("ExhaustiveSearchInstanceId").AsInt32().Nullable()
                .WithColumn("Dependent").AsFloat().Nullable()
                .WithColumn("Anomaly").AsFloat().Nullable()
                .WithColumn("Independent").AsCustom("float[]").Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("ExhaustiveSearchInstanceData").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Create.ForeignKey().FromTable("ExhaustiveSearchInstanceData").ForeignColumn("ExhaustiveSearchInstanceId")
                .ToTable("ExhaustiveSearchInstance").PrimaryColumn("Id");
        }

        private void CreateExhaustiveSearchInstance()
        {
            Alter.Table("ExhaustiveSearchInstance").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("ExhaustiveSearchInstance").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Create.Index().OnTable("ExhaustiveSearchInstance").OnColumn("Guid").Ascending();
        }

        private void CreateEntityAnalysisModel()
        {
            Alter.Table("EntityAnalysisModel").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModel").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Create.Index().OnTable("EntityAnalysisModel").OnColumn("Guid").Ascending();
        }

        private void CreateImport()
        {
            Create.Table("Import")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("TenantRegistryId").AsInt32().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("Bytes").AsBinary().Nullable()
                .WithColumn("InError").AsByte().Nullable()
                .WithColumn("ErrorStack").AsString().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CompletedDate").AsDateTime2().Nullable()
                .WithColumn("ExportVersion").AsInt32().Nullable()
                .WithColumn("ExportGuid").AsGuid().Nullable();

            Create.Index().OnTable("Import").OnColumn("Guid").Ascending();

            Create.ForeignKey().FromTable("Import").ForeignColumn("TenantRegistryId")
                .ToTable("TenantRegistry").PrimaryColumn("Id");
        }

        private void CreateExport()
        {
            Create.Table("Export")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("TenantRegistryId").AsInt32().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("Bytes").AsBinary().Nullable()
                .WithColumn("EncryptedBytes").AsBinary().Nullable()
                .WithColumn("InError").AsByte().Nullable()
                .WithColumn("ErrorStack").AsString().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CompletedDate").AsDateTime2().Nullable()
                .WithColumn("Version").AsInt32().Nullable();

            Create.Index().OnTable("Export").OnColumn("Guid").Ascending();

            Create.ForeignKey().FromTable("Export").ForeignColumn("TenantRegistryId")
                .ToTable("TenantRegistry").PrimaryColumn("Id");
        }

        private void CreateExportPeek()
        {
            Create.Table("ExportPeek")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("TenantRegistryId").AsInt32().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("Yaml").AsString().Nullable()
                .WithColumn("InError").AsByte().Nullable()
                .WithColumn("ErrorStack").AsString().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CompletedDate").AsDateTime2().Nullable();

            Create.Index().OnTable("ExportPeek").OnColumn("Guid").Ascending();

            Create.ForeignKey().FromTable("ExportPeek").ForeignColumn("TenantRegistryId")
                .ToTable("TenantRegistry").PrimaryColumn("Id");
        }

        private void DropExhaustiveSearchInstancePromotedTrialInstanceVariableHistogram()
        {
            Delete.Table("ExhaustiveSearchInstancePromotedTrialInstanceVariableHistogram");
        }

        private void CreateEntityAnalysisModelSuppression()
        {
            Alter.Table("EntityAnalysisModelSuppression").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("EntityAnalysisModelSuppression").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelSuppression").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Alter.Table("EntityAnalysisModelSuppression").AddColumn("UpdatedDate").AsDateTime().Nullable();
            Alter.Table("EntityAnalysisModelSuppression").AddColumn("UpdatedUser").AsString().Nullable();

            Execute.Sql(
                """update "EntityAnalysisModelSuppression" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("EntityAnalysisModelSuppression").OnColumn("Guid").Ascending();

            Create.Table("EntityAnalysisModelSuppressionVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelSuppressionId").AsInt32().Nullable()
                .WithColumn("EntityAnalysisModelGuid").AsGuid().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("DeletedDate").AsDateTime().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("SuppressionKeyValue").AsString().Nullable()
                .WithColumn("SuppressionKey").AsString().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelSuppressionVersion")
                .ForeignColumn("EntityAnalysisModelSuppressionId")
                .ToTable("EntityAnalysisModelSuppression").PrimaryColumn("Id");
        }

        private void CreateEntityAnalysisModelActivationRuleSuppression()
        {
            Alter.Table("EntityAnalysisModelActivationRuleSuppression").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("EntityAnalysisModelActivationRuleSuppression").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelActivationRuleSuppression").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Alter.Table("EntityAnalysisModelActivationRuleSuppression").AddColumn("UpdatedDate").AsDateTime().Nullable();
            Alter.Table("EntityAnalysisModelActivationRuleSuppression").AddColumn("UpdatedUser").AsString().Nullable();

            Execute.Sql(
                """update "EntityAnalysisModelActivationRuleSuppression" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("EntityAnalysisModelActivationRuleSuppression").OnColumn("Guid").Ascending();

            Create.Table("EntityAnalysisModelActivationRuleSuppressionVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelActivationRuleSuppressionId").AsInt32().Nullable()
                .WithColumn("EntityAnalysisModelGuid").AsGuid().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("SuppressionKey").AsString().Nullable()
                .WithColumn("SuppressionKeyValue").AsString().Nullable()
                .WithColumn("EntityAnalysisModelActivationRuleName").AsString().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelActivationRuleSuppressionVersion")
                .ForeignColumn("EntityAnalysisModelActivationRuleSuppressionId")
                .ToTable("EntityAnalysisModelActivationRuleSuppression").PrimaryColumn("Id");
        }

        private void CreateRoleRegistryPermission()
        {
            Alter.Table("RoleRegistryPermission").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("RoleRegistryPermission").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("RoleRegistryPermission").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Execute.Sql(
                """update "RoleRegistryPermission" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("RoleRegistryPermission").OnColumn("Guid").Ascending();

            Create.Table("RoleRegistryPermissionVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("RoleRegistryPermissionId").AsInt32().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("RoleRegistryId").AsInt32().Nullable()
                .WithColumn("PermissionSpecificationId").AsInt32().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("Version").AsInt32().Nullable();

            Create.ForeignKey().FromTable("RoleRegistryPermissionVersion").ForeignColumn("RoleRegistryPermissionId")
                .ToTable("RoleRegistryPermission").PrimaryColumn("Id");
        }

        private void CreateUserRegistry()
        {
            Alter.Table("UserRegistry").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("UserRegistry").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("UserRegistry").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Alter.Table("UserRegistry").AddColumn("UpdatedDate").AsDateTime().Nullable();
            Alter.Table("UserRegistry").AddColumn("UpdatedUser").AsString().Nullable();

            Execute.Sql(
                """update "UserRegistry" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("UserRegistry").OnColumn("Guid").Ascending();

            Create.Table("UserRegistryVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("UserRegistryId").AsInt32().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("RoleRegistryId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Email").AsString().Nullable()
                .WithColumn("Password").AsString().Nullable()
                .WithColumn("PasswordExpiryDate").AsDateTime().Nullable()
                .WithColumn("PasswordCreatedDate").AsDateTime().Nullable()
                .WithColumn("FailedPasswordCount").AsInt32().Nullable()
                .WithColumn("LastLoginDate").AsDateTime().Nullable()
                .WithColumn("PasswordLocked").AsByte().Nullable()
                .WithColumn("PasswordLockedDate").AsDateTime().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("UserRegistryVersion").ForeignColumn("UserRegistryId")
                .ToTable("UserRegistry").PrimaryColumn("Id");
        }

        private void CreateRoleRegistry()
        {
            Alter.Table("RoleRegistry").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("RoleRegistry").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("RoleRegistry").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Execute.Sql(
                """update "RoleRegistry" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("RoleRegistry").OnColumn("Guid").Ascending();

            Create.Table("RoleRegistryVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("RoleRegistryId").AsInt32().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("DeletedDate").AsString().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("TenantRegistryId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("RoleRegistryVersion").ForeignColumn("RoleRegistryId")
                .ToTable("RoleRegistry").PrimaryColumn("Id");
        }

        private void DropUserRoleRegistry()
        {
            Delete.Table("UserRoleRegistry");
        }

        private void UpdateDataCaseFilterSelectJsonDataAndInstructCaseWorkflowFilterRebuild()
        {
            Execute.Sql("""

                                    update "CaseWorkflowFilter"
                                    set "SelectJson" =
                                            jsonb_set(
                                                    "SelectJson",
                                                    array ['rules', elem_index::text, 'field'],
                                                    '"\"CaseWorkflowStatus\".\"Guid\""'::jsonb,
                                                    true)
                                    from (select pos - 1 as elem_index
                                          from "CaseWorkflowFilter" s,
                                               jsonb_array_elements(s."SelectJson" #> '{rules}') with ordinality arr(elem, pos)
                                          where elem ->> 'id' = 'CaseWorkflowStatusId') sub;


                                    update "CaseWorkflowFilter"
                                    set "SelectJson" =
                                            jsonb_set(
                                                    "SelectJson",
                                                    array ['rules', elem_index::text, 'label'],
                                                    '"CaseWorkflowStatus"'::jsonb,
                                                    true)
                                    from (select pos - 1 as elem_index
                                          from "CaseWorkflowFilter" s,
                                               jsonb_array_elements(s."SelectJson" #> '{rules}') with ordinality arr(elem, pos)
                                          where elem ->> 'id' = 'CaseWorkflowStatusId') sub;

                                    update "CaseWorkflowFilter"
                                    set "SelectJson" =
                                            jsonb_set(
                                                    "SelectJson",
                                                    array ['rules', elem_index::text, 'id'],
                                                    '"CaseWorkflowStatus"'::jsonb, --Change this for new value
                                                    true)
                                    from (select pos - 1 as elem_index
                                          from "CaseWorkflowFilter" s,
                                               jsonb_array_elements(s."SelectJson" #> '{rules}') with ordinality arr(elem, pos)
                                          where elem ->> 'id' = 'CaseWorkflowStatusId') sub;

                                    update "CaseWorkflowFilter"
                                    set "FilterJson" =
                                            jsonb_set(
                                                    "FilterJson",
                                                    array ['rules', elem_index::text, 'field'],
                                                    '"\"CaseWorkflowStatus\".\"Guid\""'::jsonb,
                                                    true)
                                    from (select pos - 1 as elem_index
                                          from "CaseWorkflowFilter" s,
                                               jsonb_array_elements(s."FilterJson" #> '{rules}') with ordinality arr(elem, pos)
                                          where elem ->> 'id' = 'CaseWorkflowStatusId') sub;

                                    update "CaseWorkflowFilter"
                                    set "FilterJson" =
                                            jsonb_set(
                                                    "FilterJson",
                                                    array ['rules', elem_index::text, 'label'],
                                                    '"CaseWorkflowStatus"'::jsonb,
                                                    true)
                                    from (select pos - 1 as elem_index
                                          from "CaseWorkflowFilter" s,
                                               jsonb_array_elements(s."FilterJson" #> '{rules}') with ordinality arr(elem, pos)
                                          where elem ->> 'id' = 'CaseWorkflowStatusId') sub;

                                    update "CaseWorkflowFilter"
                                    set "FilterJson" =
                                            jsonb_set(
                                                    "FilterJson",
                                                    array ['rules', elem_index::text, 'value'],
                                                    guid::jsonb, --Change this for new value
                                                    true)
                                    from (select pos - 1 as elem_index,'"' || s."Guid"::text || '"' as guid
                                          from "CaseWorkflowFilter" f,"CaseWorkflowStatus" s,
                                               jsonb_array_elements(f."FilterJson" #> '{rules}') with ordinality arr(elem, pos)
                                          where elem ->> 'id' = 'CaseWorkflowStatusId'
                                            and s."Id" = f."Id") sub;

                                    update "CaseWorkflowFilter"
                                    set "FilterJson" =
                                            jsonb_set(
                                                    "FilterJson",
                                                    array ['rules', elem_index::text, 'id'],
                                                    '"CaseWorkflowStatus"'::jsonb, --Change this for new value
                                                    true)
                                    from (select pos - 1 as elem_index
                                          from "CaseWorkflowFilter" s,
                                               jsonb_array_elements(s."FilterJson" #> '{rules}') with ordinality arr(elem, pos)
                                          where elem ->> 'id' = 'CaseWorkflowStatusId') sub;

                                    create temporary table "CurrentSession"
                                    (
                                        "Id" int                           
                                    );

                                    insert into "CurrentSession"("Id")
                                    select max("Id")
                                    from "SessionCaseSearchCompiledSql"
                                    group by "CreatedUser";

                                    --Update field for select
                                    update "SessionCaseSearchCompiledSql"
                                    set "SelectJson" =
                                            jsonb_set(
                                                    "SelectJson",
                                                    array ['rules', elem_index::text, 'field'],
                                                    '"\"CaseWorkflowStatus\".\"Guid\""'::jsonb,
                                                    true)
                                    from (select pos - 1 as elem_index,"Id"
                                          from "SessionCaseSearchCompiledSql" s,
                                               jsonb_array_elements(s."SelectJson" #> '{rules}') with ordinality arr(elem, pos)
                                          where elem ->> 'id' = 'CaseWorkflowStatusId') sub,
                                         "CurrentSession" c where c."Id" = sub."Id";

                                    --Update label for select
                                    update "SessionCaseSearchCompiledSql"
                                    set "SelectJson" =
                                            jsonb_set(
                                                    "SelectJson",
                                                    array ['rules', elem_index::text, 'label'],
                                                    '"CaseWorkflowStatus"'::jsonb,
                                                    true)
                                    from (select pos - 1 as elem_index,"Id"
                                          from "SessionCaseSearchCompiledSql" s,
                                               jsonb_array_elements(s."SelectJson" #> '{rules}') with ordinality arr(elem, pos)
                                          where elem ->> 'id' = 'CaseWorkflowStatusId') sub,
                                         "CurrentSession" c where c."Id" = sub."Id";

                                    --Update id for select
                                    update "SessionCaseSearchCompiledSql"
                                    set "SelectJson" =
                                            jsonb_set(
                                                    "SelectJson",
                                                    array ['rules', elem_index::text, 'id'],
                                                    '"CaseWorkflowStatus"'::jsonb, --Change this for new value
                                                    true)
                                    from (select pos - 1 as elem_index,"Id"
                                          from "SessionCaseSearchCompiledSql" s,
                                               jsonb_array_elements(s."SelectJson" #> '{rules}') with ordinality arr(elem, pos)
                                          where elem ->> 'id' = 'CaseWorkflowStatusId') sub,
                                         "CurrentSession" c where c."Id" = sub."Id";

                                    --Update field for filter
                                    update "SessionCaseSearchCompiledSql"
                                    set "FilterJson" =
                                            jsonb_set(
                                                    "FilterJson",
                                                    array ['rules', elem_index::text, 'field'],
                                                    '"\"CaseWorkflowStatus\".\"Guid\""'::jsonb,
                                                    true)
                                    from (select pos - 1 as elem_index,"Id"
                                          from "SessionCaseSearchCompiledSql" s,
                                               jsonb_array_elements(s."FilterJson" #> '{rules}') with ordinality arr(elem, pos)
                                          where elem ->> 'id' = 'CaseWorkflowStatusId') sub,
                                         "CurrentSession" c where c."Id" = sub."Id";

                                    --Update label for filter
                                    update "SessionCaseSearchCompiledSql"
                                    set "FilterJson" =
                                            jsonb_set(
                                                    "FilterJson",
                                                    array ['rules', elem_index::text, 'label'],
                                                    '"CaseWorkflowStatus"'::jsonb,
                                                    true)
                                    from (select pos - 1 as elem_index,"Id"
                                          from "SessionCaseSearchCompiledSql" s,
                                               jsonb_array_elements(s."FilterJson" #> '{rules}') with ordinality arr(elem, pos)
                                          where elem ->> 'id' = 'CaseWorkflowStatusId') sub,
                                         "CurrentSession" c where c."Id" = sub."Id";

                                    --Update value for filter
                                    update "SessionCaseSearchCompiledSql"
                                    set "FilterJson" =
                                            jsonb_set(
                                                    "FilterJson",
                                                    array ['rules', elem_index::text, 'value'],
                                                    guid::jsonb, --Change this for new value
                                                    true)
                                    from (select pos - 1 as elem_index, '"' || s."Guid"::text || '"' as guid,f."Id"
                                          from "SessionCaseSearchCompiledSql" f,
                                               "CaseWorkflowStatus" s,
                                               jsonb_array_elements(f."FilterJson" #> '{rules}') with ordinality arr(elem, pos)
                                          where elem ->> 'id' = 'CaseWorkflowStatusId'
                                            and s."Id" = f."Id") sub,
                                         "CurrentSession" c where c."Id" = sub."Id";

                                    --Update id for filter
                                    update "SessionCaseSearchCompiledSql"
                                    set "FilterJson" =
                                            jsonb_set(
                                                    "FilterJson",
                                                    array ['rules', elem_index::text, 'id'],
                                                    '"CaseWorkflowStatus"'::jsonb, --Change this for new value
                                                    true)
                                    from (select pos - 1 as elem_index,"Id"
                                          from "SessionCaseSearchCompiledSql" s,
                                               jsonb_array_elements(s."FilterJson" #> '{rules}') with ordinality arr(elem, pos)
                                          where elem ->> 'id' = 'CaseWorkflowStatusId') sub,
                                         "CurrentSession" c where c."Id" = sub."Id";

                                    update "SessionCaseSearchCompiledSql" sub
                                    set "Rebuild" = 1
                                    FROM "CurrentSession" c where c."Id" = sub."Id";
                                
                        """);
        }

        private void CreateSessionCaseSearchCompiledSql()
        {
            Alter.Table("SessionCaseSearchCompiledSql").AddColumn("CaseWorkflowFilterGuid").AsGuid().Nullable();
            Alter.Table("SessionCaseSearchCompiledSql").AddColumn("Rebuild").AsByte().Nullable();
            Alter.Table("SessionCaseSearchCompiledSql").AddColumn("RebuildDate").AsDateTime().Nullable();
        }

        private void CreateExhaustiveSearchInstanceVersion()
        {
            Create.Table("ExhaustiveSearchInstanceVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("ExhaustiveSearchInstanceId").AsInt32().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("ResponsePayload").AsByte().Nullable()
                .WithColumn("ReportTable").AsByte().Nullable()
                .WithColumn("ModelsSinceBest").AsInt32().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Anomaly").AsByte().Nullable()
                .WithColumn("Filter").AsByte().Nullable()
                .WithColumn("AnomalyProbability").AsDouble().Nullable()
                .WithColumn("FilterJson").AsCustom("jsonb").Nullable()
                .WithColumn("FilterSql").AsString().Nullable()
                .WithColumn("FilterTokens").AsCustom("jsonb").Nullable()
                .WithColumn("StatusId").AsByte().Nullable()
                .WithColumn("Models").AsInt32().Nullable()
                .WithColumn("Score").AsDouble().Nullable()
                .WithColumn("TopologyComplexity").AsDouble().Nullable()
                .WithColumn("CompletedDate").AsDateTime().Nullable()
                .WithColumn("UpdatedDate").AsDateTime().Nullable()
                .WithColumn("Object").AsBinary().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("DeletedDate").AsDateTime().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("ExhaustiveSearchInstanceVersion").ForeignColumn("ExhaustiveSearchInstanceId")
                .ToTable("ExhaustiveSearchInstance").PrimaryColumn("Id");
        }

        private void UpdateDataInSessionCaseSearchCompiledSql()
        {
            Alter.Table("SessionCaseSearchCompiledSql").AddColumn("CaseWorkflowGuid").AsGuid().Nullable();

            Execute.Sql(
                """UPDATE "SessionCaseSearchCompiledSql" b SET "CaseWorkflowGuid" = a."Guid" FROM "CaseWorkflow" a WHERE a."Id" = b."CaseWorkflowId";""");

            Delete.Column("CaseWorkflowId").FromTable("SessionCaseSearchCompiledSql");
            Create.Index().OnTable("SessionCaseSearchCompiledSql").OnColumn("CaseWorkflowGuid").Ascending();
        }

        private void UpdateDataInEntityAnalysisModelDictionary()
        {
            Alter.Table("EntityAnalysisModelDictionary").AddColumn("EntityAnalysisModelGuid").AsGuid().Nullable();

            Execute.Sql(
                """UPDATE "EntityAnalysisModelDictionary" b SET "EntityAnalysisModelGuid" = a."Guid" FROM "EntityAnalysisModel" a WHERE a."Id" = b."EntityAnalysisModelId";""");

            Delete.Column("EntityAnalysisModelId").FromTable("EntityAnalysisModelDictionary");
            Create.Index().OnTable("EntityAnalysisModelDictionary").OnColumn("EntityAnalysisModelGuid").Ascending();
        }

        private void UpdateDataInEntityAnalysisModelList()
        {
            Alter.Table("EntityAnalysisModelList").AddColumn("EntityAnalysisModelGuid").AsGuid().Nullable();

            Execute.Sql(
                """UPDATE "EntityAnalysisModelList" b SET "EntityAnalysisModelGuid" = a."Guid" FROM "EntityAnalysisModel" a WHERE a."Id" = b."EntityAnalysisModelId";""");

            Delete.Column("EntityAnalysisModelId").FromTable("EntityAnalysisModelList");
            Create.Index().OnTable("EntityAnalysisModelList").OnColumn("EntityAnalysisModelGuid").Ascending();
        }

        private void UpdateEntityAnalysisModelAsynchronousQueueBalance()
        {
            Alter.Table("EntityAnalysisModelAsynchronousQueueBalance").AddColumn("EntityAnalysisModelGuid").AsGuid()
                .Nullable();

            Execute.Sql(
                """UPDATE "EntityAnalysisModelAsynchronousQueueBalance" b SET "EntityAnalysisModelGuid" = a."Guid" FROM "EntityAnalysisModel" a WHERE a."Id" = b."EntityAnalysisModelId";""");

            Delete.Column("EntityAnalysisModelId").FromTable("EntityAnalysisModelAsynchronousQueueBalance");
            Create.Index().OnTable("EntityAnalysisModelAsynchronousQueueBalance").OnColumn("EntityAnalysisModelGuid")
                .Ascending();
        }

        private void UpdateEntityAnalysisModelProcessingCounter()
        {
            Alter.Table("EntityAnalysisModelProcessingCounter").AddColumn("EntityAnalysisModelGuid").AsGuid().Nullable();

            Execute.Sql(
                """UPDATE "EntityAnalysisModelProcessingCounter" b SET "EntityAnalysisModelGuid" = a."Guid" FROM "EntityAnalysisModel" a WHERE a."Id" = b."EntityAnalysisModelId";""");

            Delete.Column("EntityAnalysisModelId").FromTable("EntityAnalysisModelProcessingCounter");
            Create.Index().OnTable("EntityAnalysisModelProcessingCounter").OnColumn("EntityAnalysisModelGuid").Ascending();
        }

        private void UpdateEntityAnalysisModelSearchKeyCalculationInstance()
        {
            Alter.Table("EntityAnalysisModelSearchKeyCalculationInstance").AddColumn("EntityAnalysisModelGuid").AsGuid()
                .Nullable();

            Execute.Sql(
                """UPDATE "EntityAnalysisModelSearchKeyCalculationInstance" b SET "EntityAnalysisModelGuid" = a."Guid" FROM "EntityAnalysisModel" a WHERE a."Id" = b."EntityAnalysisModelId";""");

            Delete.Column("EntityAnalysisModelId").FromTable("EntityAnalysisModelSearchKeyCalculationInstance");
            Create.Index().OnTable("EntityAnalysisModelSearchKeyCalculationInstance").OnColumn("EntityAnalysisModelGuid")
                .Ascending();
        }

        private void UpdateDataInEntityAnalysisModelSuppression()
        {
            Alter.Table("EntityAnalysisModelSuppression").AddColumn("EntityAnalysisModelGuid").AsGuid().Nullable();

            Execute.Sql(
                """UPDATE "EntityAnalysisModelSuppression" b SET "EntityAnalysisModelGuid" = a."Guid" FROM "EntityAnalysisModel" a WHERE a."Id" = b."EntityAnalysisModelId";""");

            Delete.Column("EntityAnalysisModelId").FromTable("EntityAnalysisModelSuppression");
            Create.Index().OnTable("EntityAnalysisModelSuppression").OnColumn("EntityAnalysisModelGuid").Ascending();
        }

        private void UpdateDataInEntityAnalysisModelActivationRuleSuppression()
        {
            Alter.Table("EntityAnalysisModelActivationRuleSuppression").AddColumn("EntityAnalysisModelGuid").AsGuid()
                .Nullable();

            Execute.Sql(
                """UPDATE "EntityAnalysisModelActivationRuleSuppression" b SET "EntityAnalysisModelGuid" = a."Guid" FROM "EntityAnalysisModel" a WHERE a."Id" = b."EntityAnalysisModelId";""");

            Delete.Column("EntityAnalysisModelId").FromTable("EntityAnalysisModelActivationRuleSuppression");
            Create.Index().OnTable("EntityAnalysisModelActivationRuleSuppression").OnColumn("EntityAnalysisModelGuid")
                .Ascending();
        }

        private void UpdateDataInVisualisationRegistry()
        {
            Alter.Table("CaseWorkflow").AddColumn("VisualisationRegistryGuid").AsGuid().Nullable();

            Execute.Sql(
                """UPDATE "CaseWorkflow" b SET "VisualisationRegistryGuid" = a."Guid" FROM "VisualisationRegistry" a WHERE a."Id" = b."VisualisationRegistryId";""");

            Delete.Column("VisualisationRegistryId").FromTable("CaseWorkflow");
            Create.Index().OnTable("CaseWorkflow").OnColumn("VisualisationRegistryGuid").Ascending();
        }

        private void UpdateDataInEntityAnalysisModelActivationRule()
        {
            Alter.Table("EntityAnalysisModelActivationRule").AddColumn("EntityAnalysisModelTtlCounterGuid").AsGuid()
                .Nullable();

            Execute.Sql(
                """update "EntityAnalysisModelActivationRule" set "EntityAnalysisModelTtlCounterGuid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Execute.Sql(
                """UPDATE "EntityAnalysisModelActivationRule" b SET "EntityAnalysisModelTtlCounterGuid" = a."Guid" FROM "EntityAnalysisModelTtlCounter" a WHERE a."Id" = b."EntityAnalysisModelTtlCounterId";""");

            Create.Index().OnTable("EntityAnalysisModelActivationRule").OnColumn("EntityAnalysisModelTtlCounterGuid")
                .Ascending();

            Delete.Column("EntityAnalysisModelTtlCounterId").FromTable("EntityAnalysisModelActivationRule");

            Alter.Table("EntityAnalysisModelActivationRule").AddColumn("EntityAnalysisModelGuidTtlCounter").AsGuid()
                .Nullable();

            Execute.Sql(
                """update "EntityAnalysisModelActivationRule" set "EntityAnalysisModelGuidTtlCounter" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Execute.Sql(
                """UPDATE "EntityAnalysisModelActivationRule" b SET "EntityAnalysisModelGuidTtlCounter" = a."Guid" FROM "EntityAnalysisModel" a WHERE a."Id" = b."EntityAnalysisModelIdTtlCounter";""");

            Create.Index().OnTable("EntityAnalysisModelActivationRule").OnColumn("EntityAnalysisModelGuidTtlCounter")
                .Ascending();

            Delete.Column("EntityAnalysisModelIdTtlCounter").FromTable("EntityAnalysisModelActivationRule");

            Alter.Table("EntityAnalysisModelActivationRule").AddColumn("CaseWorkflowStatusGuid").AsGuid().Nullable();

            Execute.Sql(
                """update "EntityAnalysisModelActivationRule" set "CaseWorkflowStatusGuid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Execute.Sql(
                """UPDATE "EntityAnalysisModelActivationRule" b SET "CaseWorkflowStatusGuid" = a."Guid" FROM "CaseWorkflowStatus" a WHERE a."Id" = b."CaseWorkflowStatusId";""");

            Delete.Column("CaseWorkflowStatusId").FromTable("EntityAnalysisModelActivationRule");
            Create.Index().OnTable("EntityAnalysisModelActivationRule").OnColumn("CaseWorkflowStatusGuid").Ascending();

            Alter.Table("EntityAnalysisModelActivationRule").AddColumn("CaseWorkflowGuid").AsGuid().Nullable();

            Execute.Sql(
                """update "EntityAnalysisModelActivationRule" set "CaseWorkflowGuid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Execute.Sql(
                """UPDATE "EntityAnalysisModelActivationRule" b SET "CaseWorkflowGuid" = a."Guid" FROM "CaseWorkflow" a WHERE a."Id" = b."CaseWorkflowId";""");

            Delete.Column("CaseWorkflowId").FromTable("EntityAnalysisModelActivationRule");
            Create.Index().OnTable("EntityAnalysisModelActivationRule").OnColumn("CaseWorkflowGuid").Ascending();
        }

        private void UpdateDataInCase()
        {
            Alter.Table("Case").AddColumn("CaseWorkflowGuid").AsGuid().Nullable();

            Execute.Sql(
                """UPDATE "Case" b SET "CaseWorkflowGuid" = a."Guid" FROM "CaseWorkflow" a WHERE a."Id" = b."CaseWorkflowId";""");

            Delete.Column("CaseWorkflowId").FromTable("Case");
            Create.Index().OnTable("Case").OnColumn("CaseWorkflowGuid").Ascending();

            Alter.Table("Case").AddColumn("CaseWorkflowStatusGuid").AsGuid().Nullable();

            Execute.Sql(
                """UPDATE "Case" b SET "CaseWorkflowStatusGuid" = a."Guid" FROM "CaseWorkflowStatus" a WHERE a."Id" = b."CaseWorkflowStatusId";""");

            Delete.Column("CaseWorkflowStatusId").FromTable("Case");
            Create.Index().OnTable("Case").OnColumn("CaseWorkflowStatusGuid").Ascending();
        }

        private void CreateVisualisationRegistryParameter()
        {
            Alter.Table("VisualisationRegistryParameter").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("VisualisationRegistryParameter").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("VisualisationRegistryParameter").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Execute.Sql(
                """update "VisualisationRegistryParameter" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("VisualisationRegistryParameter").OnColumn("Guid").Ascending();

            Create.Table("VisualisationRegistryParameterVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("VisualisationRegistryParameterId").AsInt32().Nullable()
                .WithColumn("VisualisationRegistryId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("DataTypeId").AsByte().Nullable()
                .WithColumn("DefaultValue").AsString().Nullable()
                .WithColumn("Required").AsByte().Nullable()
                .WithColumn("Guid").AsGuid().Nullable();

            Create.ForeignKey().FromTable("VisualisationRegistryParameterVersion")
                .ForeignColumn("VisualisationRegistryParameterId")
                .ToTable("VisualisationRegistryParameter").PrimaryColumn("Id");
        }

        private void CreateVisualisationRegistryDatasource()
        {
            Alter.Table("VisualisationRegistryDatasource").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("VisualisationRegistryDatasource").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("VisualisationRegistryDatasource").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Execute.Sql(
                """update "VisualisationRegistryDatasource" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("VisualisationRegistryDatasource").OnColumn("Guid").Ascending();

            Create.Table("VisualisationRegistryDatasourceVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("VisualisationRegistryDatasourceId").AsInt32().Nullable()
                .WithColumn("VisualisationRegistryId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("VisualisationTypeId").AsByte().Nullable()
                .WithColumn("Command").AsString().Nullable()
                .WithColumn("VisualisationText").AsString().Nullable()
                .WithColumn("Priority").AsInt32().Nullable()
                .WithColumn("IncludeGrid").AsByte().Nullable()
                .WithColumn("IncludeDisplay").AsByte().Nullable()
                .WithColumn("ColumnSpan").AsInt32().Nullable()
                .WithColumn("RowSpan").AsInt32().Nullable()
                .WithColumn("Guid").AsGuid().Nullable();

            Create.ForeignKey().FromTable("VisualisationRegistryDatasourceVersion")
                .ForeignColumn("VisualisationRegistryDatasourceId")
                .ToTable("VisualisationRegistryDatasource").PrimaryColumn("Id");
        }

        private void CreateEntityAnalysisModelDictionaryKvp()
        {
            Alter.Table("EntityAnalysisModelDictionaryKvp").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("EntityAnalysisModelDictionaryKvp").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelDictionaryKvp").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Alter.Table("EntityAnalysisModelDictionaryKvp").AddColumn("UpdatedDate").AsDateTime().Nullable();
            Alter.Table("EntityAnalysisModelDictionaryKvp").AddColumn("UpdatedUser").AsString().Nullable();

            Execute.Sql(
                """update "EntityAnalysisModelDictionaryKvp" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("EntityAnalysisModelDictionaryKvp").OnColumn("Guid").Ascending();

            Create.Table("EntityAnalysisModelDictionaryKvpVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelDictionaryKvpId").AsInt32().Nullable()
                .WithColumn("EntityAnalysisModelDictionaryId").AsInt32().Nullable()
                .WithColumn("KvpKey").AsString().Nullable()
                .WithColumn("KvpValue").AsDouble().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("UpdatedDate").AsDateTime().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelDictionaryKvpVersion")
                .ForeignColumn("EntityAnalysisModelDictionaryKvpId")
                .ToTable("EntityAnalysisModelDictionaryKvp").PrimaryColumn("Id");
        }

        private void CreateEntityAnalysisModelListValue()
        {
            Alter.Table("EntityAnalysisModelListValue").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("EntityAnalysisModelListValue").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelListValue").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Alter.Table("EntityAnalysisModelListValue").AddColumn("UpdatedDate").AsDateTime().Nullable();
            Alter.Table("EntityAnalysisModelListValue").AddColumn("UpdatedUser").AsString().Nullable();

            Execute.Sql(
                """update "EntityAnalysisModelListValue" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("EntityAnalysisModelListValue").OnColumn("Guid").Ascending();

            Create.Table("EntityAnalysisModelListValueVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelListValueId").AsInt32().Nullable()
                .WithColumn("EntityAnalysisModelListId").AsInt32().Nullable()
                .WithColumn("ListValue").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("UpdatedDate").AsDateTime().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelListValueVersion")
                .ForeignColumn("EntityAnalysisModelListValueId")
                .ToTable("EntityAnalysisModelListValue").PrimaryColumn("Id");
        }

        private void CreateCaseWorkflowFilter()
        {
            Alter.Table("CaseWorkflowFilter").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("CaseWorkflowFilter").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("CaseWorkflowFilter").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Execute.Sql(
                """update "CaseWorkflowFilter" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("CaseWorkflowFilter").OnColumn("Guid").Ascending();

            Create.Table("CaseWorkflowFilterVersion")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CaseWorkflowFilterId").AsInt32().Nullable()
                .WithColumn("CaseWorkflowId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("FilterSql").AsString().Nullable()
                .WithColumn("FilterJson").AsCustom("jsonb").Nullable()
                .WithColumn("SelectJson").AsCustom("jsonb").Nullable()
                .WithColumn("FilterTokens").AsString().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("VisualisationRegistryId").AsInt32().Nullable()
                .WithColumn("Guid").AsGuid().Nullable();

            Create.ForeignKey().FromTable("CaseWorkflowFilterVersion").ForeignColumn("CaseWorkflowFilterId")
                .ToTable("CaseWorkflowFilter").PrimaryColumn("Id");
        }

        private void CreateCaseWorkflowMacro()
        {
            Alter.Table("CaseWorkflowMacro").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("CaseWorkflowMacro").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("CaseWorkflowMacro").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Execute.Sql(
                """update "CaseWorkflowMacro" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("CaseWorkflowMacro").OnColumn("Guid").Ascending();

            Create.Table("CaseWorkflowMacroVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("CaseWorkflowMacroId").AsInt32().Nullable()
                .WithColumn("CaseWorkflowId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("Javascript").AsString().Nullable()
                .WithColumn("ImageLocation").AsString().Nullable()
                .WithColumn("EnableHttpEndpoint").AsByte().Nullable()
                .WithColumn("HttpEndpoint").AsString().Nullable()
                .WithColumn("HttpEndpointTypeId").AsByte().Nullable()
                .WithColumn("EnableNotification").AsByte().Nullable()
                .WithColumn("NotificationTypeId").AsByte().Nullable()
                .WithColumn("NotificationDestination").AsString().Nullable()
                .WithColumn("NotificationSubject").AsString().Nullable()
                .WithColumn("NotificationBody").AsString().Nullable()
                .WithColumn("Guid").AsGuid().Nullable();

            Create.ForeignKey().FromTable("CaseWorkflowMacroVersion").ForeignColumn("CaseWorkflowMacroId")
                .ToTable("CaseWorkflowMacro").PrimaryColumn("Id");
        }

        private void CreateCaseWorkflowDisplay()
        {
            Alter.Table("CaseWorkflowDisplay").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("CaseWorkflowDisplay").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("CaseWorkflowDisplay").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Execute.Sql(
                """update "CaseWorkflowDisplay" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("CaseWorkflowDisplay").OnColumn("Guid").Ascending();

            Create.Table("CaseWorkflowDisplayVersion")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CaseWorkflowDisplayId").AsInt32().Nullable()
                .WithColumn("CaseWorkflowId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("Html").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("Guid").AsGuid().Nullable();

            Create.ForeignKey().FromTable("CaseWorkflowDisplayVersion").ForeignColumn("CaseWorkflowDisplayId")
                .ToTable("CaseWorkflowDisplay").PrimaryColumn("Id");
        }

        private void CreateCaseWorkflowAction()
        {
            Alter.Table("CaseWorkflowAction").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("CaseWorkflowAction").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("CaseWorkflowAction").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Execute.Sql(
                """update "CaseWorkflowAction" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("CaseWorkflowAction").OnColumn("Guid").Ascending();

            Create.Table("CaseWorkflowActionVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("CaseWorkflowActionId").AsInt32().Nullable()
                .WithColumn("CaseWorkflowId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("EnableHttpEndpoint").AsByte().Nullable()
                .WithColumn("HttpEndpoint").AsString().Nullable()
                .WithColumn("HttpEndpointTypeId").AsByte().Nullable()
                .WithColumn("EnableNotification").AsByte().Nullable()
                .WithColumn("NotificationTypeId").AsByte().Nullable()
                .WithColumn("NotificationDestination").AsString().Nullable()
                .WithColumn("NotificationSubject").AsString().Nullable()
                .WithColumn("NotificationBody").AsString().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("Guid").AsGuid().Nullable();

            Create.ForeignKey().FromTable("CaseWorkflowActionVersion").ForeignColumn("CaseWorkflowActionId")
                .ToTable("CaseWorkflowAction").PrimaryColumn("Id");
        }

        private void CreateCaseWorkflowForm()
        {
            Alter.Table("CaseWorkflowForm").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("CaseWorkflowForm").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("CaseWorkflowForm").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Execute.Sql(
                """update "CaseWorkflowForm" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("CaseWorkflowForm").OnColumn("Guid").Ascending();

            Create.Table("CaseWorkflowFormVersion")
                .WithColumn("Id").AsInt64().PrimaryKey().Identity()
                .WithColumn("CaseWorkflowFormId").AsInt32().Nullable()
                .WithColumn("CaseWorkflowId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("Html").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("EnableHttpEndpoint").AsByte().Nullable()
                .WithColumn("HttpEndpoint").AsString().Nullable()
                .WithColumn("HttpEndpointTypeId").AsByte().Nullable()
                .WithColumn("EnableNotification").AsByte().Nullable()
                .WithColumn("NotificationTypeId").AsByte().Nullable()
                .WithColumn("NotificationDestination").AsString().Nullable()
                .WithColumn("NotificationSubject").AsString().Nullable()
                .WithColumn("NotificationBody").AsString().Nullable()
                .WithColumn("Guid").AsGuid().Nullable();

            Create.ForeignKey().FromTable("CaseWorkflowFormVersion").ForeignColumn("CaseWorkflowFormId")
                .ToTable("CaseWorkflowForm").PrimaryColumn("Id");
        }

        private void CreateCaseWorkflowStatus()
        {
            Alter.Table("CaseWorkflowStatus").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("CaseWorkflowStatus").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("CaseWorkflowStatus").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Alter.Table("CaseWorkflowStatusVersion").AddColumn("Guid").AsGuid().Nullable();

            Execute.Sql(
                """update "CaseWorkflowStatus" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("CaseWorkflowStatus").OnColumn("Guid").Ascending();
        }

        private void CreateCaseWorkflowXPath()
        {
            Alter.Table("CaseWorkflowXPath").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("CaseWorkflowXPath").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("CaseWorkflowXPath").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Execute.Sql(
                """update "CaseWorkflowXPath" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("CaseWorkflowXPath").OnColumn("Guid").Ascending();
        }

        private void CreateCaseWorkflowXPathVersion()
        {
            Create.Table("CaseWorkflowXPathVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("CaseWorkflowXPathId").AsInt32().Nullable()
                .WithColumn("CaseWorkflowId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("XPath").AsString().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("UpdatedDate").AsDateTime2().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("ConditionalRegularExpressionFormatting").AsByte().Nullable()
                .WithColumn("ConditionalFormatForeColor").AsString().Nullable()
                .WithColumn("ConditionalFormatBackColor").AsString().Nullable()
                .WithColumn("RegularExpression").AsString().Nullable()
                .WithColumn("ForeRowColorScope").AsString().Nullable()
                .WithColumn("BackRowColorScope").AsString().Nullable()
                .WithColumn("Drill").AsByte().Nullable()
                .WithColumn("BoldLineFormatForeColor").AsString().Nullable()
                .WithColumn("BoldLineFormatBackColor").AsString().Nullable()
                .WithColumn("BoldLineMatched").AsByte().Nullable()
                .WithColumn("Guid").AsGuid().Nullable();

            Create.ForeignKey().FromTable("CaseWorkflowXPathVersion").ForeignColumn("CaseWorkflowXPathId")
                .ToTable("CaseWorkflowXPath").PrimaryColumn("Id");
        }

        private void CreateVisualisationRegistry()
        {
            Alter.Table("VisualisationRegistry").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("VisualisationRegistry").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("VisualisationRegistry").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Alter.Table("VisualisationRegistryVersion").AddColumn("Guid").AsGuid().Nullable();

            Execute.Sql(
                """update "VisualisationRegistry" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("VisualisationRegistry").OnColumn("Guid").Ascending();
        }

        private void CreateEntityAnalysisModelDictionary()
        {
            Alter.Table("EntityAnalysisModelDictionary").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("EntityAnalysisModelDictionary").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelDictionary").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Alter.Table("EntityAnalysisModelDictionary").AddColumn("UpdatedDate").AsDateTime().Nullable();
            Alter.Table("EntityAnalysisModelDictionary").AddColumn("UpdatedUser").AsString().Nullable();

            Execute.Sql(
                """update "EntityAnalysisModelDictionary" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("EntityAnalysisModelDictionary").OnColumn("Guid").Ascending();

            Create.Table("EntityAnalysisModelDictionaryVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelDictionaryId").AsInt32().Nullable()
                .WithColumn("EntityAnalysisModelGuid").AsGuid().Nullable()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("DataName").AsString().Nullable()
                .WithColumn("ResponsePayload").AsByte().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("UpdatedDate").AsDateTime().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelDictionaryVersion")
                .ForeignColumn("EntityAnalysisModelDictionaryId")
                .ToTable("EntityAnalysisModelDictionary").PrimaryColumn("Id");
        }

        private void CreateEntityAnalysisModelList()
        {
            Alter.Table("EntityAnalysisModelList").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("EntityAnalysisModelList").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelList").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Alter.Table("EntityAnalysisModelList").AddColumn("UpdatedDate").AsDateTime().Nullable();
            Alter.Table("EntityAnalysisModelList").AddColumn("UpdatedUser").AsString().Nullable();

            Execute.Sql(
                """update "EntityAnalysisModelList" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("EntityAnalysisModelList").OnColumn("Guid").Ascending();

            Create.Table("EntityAnalysisModelListVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelListId").AsInt32().Nullable()
                .WithColumn("EntityAnalysisModelGuid").AsGuid().Nullable()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("UpdatedDate").AsDateTime().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelListVersion").ForeignColumn("EntityAnalysisModelListId")
                .ToTable("EntityAnalysisModelList").PrimaryColumn("Id");
        }

        private void CreateCaseWorkflow()
        {
            Alter.Table("CaseWorkflow").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("CaseWorkflow").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("CaseWorkflow").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Alter.Table("CaseWorkflowVersion").AddColumn("Guid").AsGuid().Nullable();

            Execute.Sql(
                """update "CaseWorkflow" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("CaseWorkflow").OnColumn("Guid").Ascending();
        }

        private void CreateEntityAnalysisModelActivationRule()
        {
            Alter.Table("EntityAnalysisModelActivationRule").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("EntityAnalysisModelActivationRule").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelActivationRule").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Alter.Table("EntityAnalysisModelActivationRule").AddColumn("UpdatedDate").AsDateTime().Nullable();
            Alter.Table("EntityAnalysisModelActivationRule").AddColumn("UpdatedUser").AsString().Nullable();

            Execute.Sql(
                """update "EntityAnalysisModelActivationRule" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("EntityAnalysisModelActivationRule").OnColumn("Guid").Ascending();

            Create.Table("EntityAnalysisModelActivationRuleVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelActivationRuleId").AsInt32().Nullable()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("BuilderRuleScript").AsString().Nullable()
                .WithColumn("Json").AsCustom("jsonb").Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("ResponseElevation").AsFloat().Nullable()
                .WithColumn("CaseWorkflowGuid").AsGuid().Nullable()
                .WithColumn("EnableCaseWorkflow").AsByte().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("EntityAnalysisModelTtlCounterGuid").AsGuid().Nullable()
                .WithColumn("EntityAnalysisModelGuidTtlCounter").AsGuid().Nullable()
                .WithColumn("ResponsePayload").AsByte().Nullable()
                .WithColumn("EnableTtlCounter").AsByte().Nullable()
                .WithColumn("ResponseElevationContent").AsString().Nullable()
                .WithColumn("SendToActivationWatcher").AsByte().Nullable()
                .WithColumn("ResponseElevationForeColor").AsString().Nullable()
                .WithColumn("ResponseElevationBackColor").AsString().Nullable()
                .WithColumn("CaseWorkflowStatusGuid").AsGuid().Nullable()
                .WithColumn("ActivationSample").AsDouble().Nullable()
                .WithColumn("ActivationCounter").AsInt64().Nullable()
                .WithColumn("ActivationCounterDate").AsDateTime2().Nullable()
                .WithColumn("ResponseElevationRedirect").AsString().Nullable()
                .WithColumn("ReviewStatusId").AsByte().Nullable()
                .WithColumn("ReportTable").AsByte().Nullable()
                .WithColumn("EnableNotification").AsByte().Nullable()
                .WithColumn("NotificationTypeId").AsByte().Nullable()
                .WithColumn("NotificationDestination").AsString().Nullable()
                .WithColumn("NotificationSubject").AsString().Nullable()
                .WithColumn("NotificationBody").AsString().Nullable()
                .WithColumn("CoderRuleScript").AsString().Nullable()
                .WithColumn("RuleScriptTypeId").AsString().Nullable()
                .WithColumn("EnableResponseElevation").AsByte().Nullable()
                .WithColumn("CaseKey").AsString().Nullable()
                .WithColumn("ResponseElevationKey").AsString().Nullable()
                .WithColumn("EnableBypass").AsByte().Nullable()
                .WithColumn("BypassSuspendInterval").AsString().Nullable()
                .WithColumn("BypassSuspendValue").AsInt32().Nullable()
                .WithColumn("BypassSuspendSample").AsDouble().Nullable()
                .WithColumn("Visible").AsByte().Nullable()
                .WithColumn("EnableReprocessing").AsByte().Nullable()
                .WithColumn("EnableSuppression").AsByte().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("UpdatedDate").AsDateTime().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelActivationRuleVersion")
                .ForeignColumn("EntityAnalysisModelActivationRuleId")
                .ToTable("EntityAnalysisModelActivationRule").PrimaryColumn("Id");
        }

        private void CreateEntityAnalysisModelHttpAdaptation()
        {
            Alter.Table("EntityAnalysisModelHttpAdaptation").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("EntityAnalysisModelHttpAdaptation").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelHttpAdaptation").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Alter.Table("EntityAnalysisModelHttpAdaptation").AddColumn("UpdatedDate").AsDateTime().Nullable();
            Alter.Table("EntityAnalysisModelHttpAdaptation").AddColumn("UpdatedUser").AsString().Nullable();

            Execute.Sql(
                """update "EntityAnalysisModelHttpAdaptation" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("EntityAnalysisModelHttpAdaptation").OnColumn("Guid").Ascending();

            Create.Table("EntityAnalysisModelHttpAdaptationVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelHttpAdaptationId").AsInt32().Nullable()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("ResponsePayload").AsByte().Nullable()
                .WithColumn("ReportTable").AsByte().Nullable()
                .WithColumn("HttpEndpoint").AsString().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("UpdatedDate").AsDateTime().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelHttpAdaptationVersion")
                .ForeignColumn("EntityAnalysisModelHttpAdaptationId")
                .ToTable("EntityAnalysisModelHttpAdaptation").PrimaryColumn("Id");
        }

        private void CreateEntityAnalysisModelAbstractionCalculation()
        {
            Alter.Table("EntityAnalysisModelAbstractionCalculation").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("EntityAnalysisModelAbstractionCalculation").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelAbstractionCalculation").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Alter.Table("EntityAnalysisModelAbstractionCalculation").AddColumn("UpdatedDate").AsDateTime().Nullable();
            Alter.Table("EntityAnalysisModelAbstractionCalculation").AddColumn("UpdatedUser").AsString().Nullable();

            Execute.Sql(
                """update "EntityAnalysisModelAbstractionCalculation" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("EntityAnalysisModelAbstractionCalculation").OnColumn("Guid").Ascending();

            Create.Table("EntityAnalysisModelAbstractionCalculationVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelAbstractionCalculationId").AsInt32().Nullable()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("EntityAnalysisModelAbstractionNameLeft").AsString().Nullable()
                .WithColumn("EntityAnalysisModelAbstractionNameRight").AsString().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("ResponsePayload").AsByte().Nullable()
                .WithColumn("AbstractionCalculationTypeId").AsByte().Nullable()
                .WithColumn("ReportTable").AsByte().Nullable()
                .WithColumn("FunctionScript").AsString().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("UpdatedDate").AsDateTime().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelAbstractionCalculationVersion")
                .ForeignColumn("EntityAnalysisModelAbstractionCalculationId")
                .ToTable("EntityAnalysisModelAbstractionCalculation").PrimaryColumn("Id");
        }

        private void CreateEntityAnalysisModelAbstractionRule()
        {
            Alter.Table("EntityAnalysisModelAbstractionRule").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("EntityAnalysisModelAbstractionRule").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelAbstractionRule").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Alter.Table("EntityAnalysisModelAbstractionRule").AddColumn("UpdatedDate").AsDateTime().Nullable();
            Alter.Table("EntityAnalysisModelAbstractionRule").AddColumn("UpdatedUser").AsString().Nullable();

            Execute.Sql(
                """update "EntityAnalysisModelAbstractionRule" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("EntityAnalysisModelAbstractionRule").OnColumn("Guid").Ascending();

            Create.Table("EntityAnalysisModelAbstractionRuleVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelAbstractionRuleId").AsInt32().Nullable()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("BuilderRuleScript").AsString().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("SearchKey").AsString().Nullable()
                .WithColumn("SearchFunctionTypeId").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("SearchInterval").AsString().Nullable()
                .WithColumn("SearchValue").AsInt32().Nullable()
                .WithColumn("SearchFunctionKey").AsString().Nullable()
                .WithColumn("Json").AsCustom("jsonb").Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("Search").AsByte().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("DeletedDate").AsDateTime().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("ResponsePayload").AsByte().Nullable()
                .WithColumn("ReportTable").AsByte().Nullable()
                .WithColumn("Offset").AsByte().Nullable()
                .WithColumn("OffsetTypeId").AsByte().Nullable()
                .WithColumn("OffsetValue").AsInt32().Nullable()
                .WithColumn("CoderRuleScript").AsString().Nullable()
                .WithColumn("RuleScriptTypeId").AsByte().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("UpdatedDate").AsDateTime().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelAbstractionRuleVersion")
                .ForeignColumn("EntityAnalysisModelAbstractionRuleId")
                .ToTable("EntityAnalysisModelAbstractionRule").PrimaryColumn("Id");
        }

        private void CreateEntityAnalysisModelTtlCounter()
        {
            Alter.Table("EntityAnalysisModelTtlCounter").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("EntityAnalysisModelTtlCounter").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelTtlCounter").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Alter.Table("EntityAnalysisModelTtlCounter").AddColumn("UpdatedDate").AsDateTime().Nullable();
            Alter.Table("EntityAnalysisModelTtlCounter").AddColumn("UpdatedUser").AsString().Nullable();

            Execute.Sql(
                """update "EntityAnalysisModelTtlCounter" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("EntityAnalysisModelTtlCounter").OnColumn("Guid").Ascending();
        }

        private void CreateEntityAnalysisModelSanction()
        {
            Alter.Table("EntityAnalysisModelSanction").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("EntityAnalysisModelSanction").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelSanction").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Alter.Table("EntityAnalysisModelSanction").AddColumn("UpdatedDate").AsDateTime().Nullable();
            Alter.Table("EntityAnalysisModelSanction").AddColumn("UpdatedUser").AsString().Nullable();

            Execute.Sql(
                """update "EntityAnalysisModelSanction" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("EntityAnalysisModelSanction").OnColumn("Guid").Ascending();

            Create.Table("EntityAnalysisModelSanctionVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelSanctionId").AsInt32().Nullable()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("MultipartStringDataName").AsString().Nullable()
                .WithColumn("Distance").AsByte().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("ResponsePayload").AsByte().Nullable()
                .WithColumn("ReportTable").AsByte().Nullable()
                .WithColumn("CacheValue").AsInt32().Nullable()
                .WithColumn("CacheInterval").AsString().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("UpdatedDate").AsDateTime().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelSanctionVersion")
                .ForeignColumn("EntityAnalysisModelSanctionId")
                .ToTable("EntityAnalysisModelSanction").PrimaryColumn("Id");
        }

        private void CreateEntityAnalysisModelTag()
        {
            Alter.Table("EntityAnalysisModelTag").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("EntityAnalysisModelTag").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelTag").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Alter.Table("EntityAnalysisModelTag").AddColumn("UpdatedDate").AsDateTime().Nullable();
            Alter.Table("EntityAnalysisModelTag").AddColumn("UpdatedUser").AsString().Nullable();

            Execute.Sql(
                """update "EntityAnalysisModelTag" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("EntityAnalysisModelTag").OnColumn("Guid").Ascending();

            Create.Table("EntityAnalysisModelTagVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelTagId").AsInt32().Nullable()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("ReportTable").AsByte().Nullable()
                .WithColumn("ResponsePayload").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("UpdatedDate").AsDateTime().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelTagVersion").ForeignColumn("EntityAnalysisModelTagId")
                .ToTable("EntityAnalysisModelTag").PrimaryColumn("Id");
        }

        private void CreateEntityAnalysisModelGatewayRule()
        {
            Alter.Table("EntityAnalysisModelGatewayRule").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("EntityAnalysisModelGatewayRule").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelGatewayRule").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Alter.Table("EntityAnalysisModelGatewayRule").AddColumn("UpdatedDate").AsDateTime().Nullable();
            Alter.Table("EntityAnalysisModelGatewayRule").AddColumn("UpdatedUser").AsString().Nullable();

            Execute.Sql(
                """update "EntityAnalysisModelGatewayRule" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("EntityAnalysisModelGatewayRule").OnColumn("Guid").Ascending();

            Create.Table("EntityAnalysisModelGatewayRuleVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelGatewayRuleId").AsInt32().Nullable()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("Priority").AsByte().Nullable()
                .WithColumn("BuilderRuleScript").AsString().Nullable()
                .WithColumn("Json").AsCustom("jsonb").Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("MaxResponseElevation").AsDouble().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("ActivationCounter").AsInt64().Nullable()
                .WithColumn("ActivationCounterDate").AsDateTime2().Nullable()
                .WithColumn("CoderRuleScript").AsString().Nullable()
                .WithColumn("RuleScriptTypeId").AsByte().Nullable()
                .WithColumn("GatewaySample").AsDouble().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("UpdatedDate").AsDateTime().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelGatewayRuleVersion")
                .ForeignColumn("EntityAnalysisModelGatewayRuleId")
                .ToTable("EntityAnalysisModelGatewayRule").PrimaryColumn("Id");
        }

        private void CreateEntityAnalysisModelInlineScript()
        {
            Alter.Table("EntityAnalysisModelInlineScript").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("EntityAnalysisModelInlineScript").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelInlineScript").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Alter.Table("EntityAnalysisModelInlineScript").AddColumn("UpdatedDate").AsDateTime().Nullable();
            Alter.Table("EntityAnalysisModelInlineScript").AddColumn("UpdatedUser").AsString().Nullable();

            Execute.Sql(
                """update "EntityAnalysisModelInlineScript" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("EntityAnalysisModelInlineScript").OnColumn("Guid").Ascending();

            Create.Table("EntityAnalysisModelInlineScriptVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelInlineScriptId").AsInt32().Nullable()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("EntityAnalysisInlineScriptId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("UpdatedDate").AsDateTime().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelInlineScriptVersion")
                .ForeignColumn("EntityAnalysisModelInlineScriptId")
                .ToTable("EntityAnalysisModelInlineScript").PrimaryColumn("Id");
        }

        private void CreateEntityAnalysisModelInlineFunction()
        {
            Alter.Table("EntityAnalysisModelInlineFunction").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("EntityAnalysisModelInlineFunction").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelInlineFunction").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Alter.Table("EntityAnalysisModelInlineFunction").AddColumn("UpdatedDate").AsDateTime().Nullable();
            Alter.Table("EntityAnalysisModelInlineFunction").AddColumn("UpdatedUser").AsString().Nullable();

            Execute.Sql(
                """update "EntityAnalysisModelInlineFunction" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("EntityAnalysisModelInlineFunction").OnColumn("Guid").Ascending();

            Create.Table("EntityAnalysisModelInlineFunctionVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelInlineFunctionId").AsInt32().Nullable()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("FunctionScript").AsString().Nullable()
                .WithColumn("ReturnDataTypeId").AsInt32().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("DeletedDate").AsDateTime2().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("ResponsePayload").AsByte().Nullable()
                .WithColumn("ReportTable").AsByte().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("UpdatedDate").AsDateTime().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelInlineFunctionVersion")
                .ForeignColumn("EntityAnalysisModelInlineFunctionId")
                .ToTable("EntityAnalysisModelInlineFunction").PrimaryColumn("Id");
        }

        private void CreateEntityAnalysisModelRequestXpathVersion()
        {
            Alter.Table("EntityAnalysisModelRequestXpath").AddColumn("Guid").AsGuid().Nullable();
            Alter.Table("EntityAnalysisModelRequestXpath").AddColumn("UpdatedDate").AsDateTime().Nullable();
            Alter.Table("EntityAnalysisModelRequestXpath").AddColumn("UpdatedUser").AsString().Nullable();

            Alter.Table("EntityAnalysisModelRequestXpath").AddColumn("ImportId").AsInt32().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelRequestXpath").ForeignColumn("ImportId")
                .ToTable("Import").PrimaryColumn("Id");

            Execute.Sql(
                """update "EntityAnalysisModelRequestXpath" set "Guid" = gen_random_uuid() where "Deleted" = 0 or "Deleted" is null;""");

            Create.Index().OnTable("EntityAnalysisModelRequestXpath").OnColumn("Guid").Ascending();

            Create.Table("EntityAnalysisModelRequestXpathVersion")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("EntityAnalysisModelRequestXpathId").AsInt32().Nullable()
                .WithColumn("EntityAnalysisModelId").AsInt32().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("DataTypeId").AsByte().Nullable()
                .WithColumn("XPath").AsString().Nullable()
                .WithColumn("Active").AsByte().Nullable()
                .WithColumn("Locked").AsByte().Nullable()
                .WithColumn("SearchKey").AsByte().Nullable()
                .WithColumn("CreatedDate").AsDateTime().Nullable()
                .WithColumn("CreatedUser").AsString().Nullable()
                .WithColumn("InheritedId").AsInt32().Nullable()
                .WithColumn("Version").AsInt32().Nullable()
                .WithColumn("DeletedDate").AsDateTime().Nullable()
                .WithColumn("DeletedUser").AsString().Nullable()
                .WithColumn("Deleted").AsByte().Nullable()
                .WithColumn("SearchKeyCache").AsByte().Nullable()
                .WithColumn("SearchKeyCacheInterval").AsString().Nullable()
                .WithColumn("SearchKeyCacheValue").AsInt32().Nullable()
                .WithColumn("ResponsePayload").AsByte().Nullable()
                .WithColumn("SearchKeyCacheTtlInterval").AsString().Nullable()
                .WithColumn("SearchKeyCacheTtlValue").AsInt32().Nullable()
                .WithColumn("PayloadLocationTypeId").AsByte().Nullable()
                .WithColumn("SearchKeyCacheFetchLimit").AsInt32().Nullable()
                .WithColumn("ReportTable").AsInt32().Nullable()
                .WithColumn("SearchKeyCacheSample").AsInt32().Nullable()
                .WithColumn("DefaultValue").AsString().Nullable()
                .WithColumn("EnableSuppression").AsByte().Nullable()
                .WithColumn("Guid").AsGuid().Nullable()
                .WithColumn("UpdatedDate").AsDateTime().Nullable()
                .WithColumn("UpdatedUser").AsString().Nullable()
                .WithColumn("SearchKeyTtlInterval").AsString().Nullable()
                .WithColumn("SearchKeyTtlIntervalValue").AsString().Nullable()
                .WithColumn("SearchKeyFetchLimit").AsInt32().Nullable();

            Create.ForeignKey().FromTable("EntityAnalysisModelRequestXpathVersion")
                .ForeignColumn("EntityAnalysisModelRequestXpathId")
                .ToTable("EntityAnalysisModelRequestXpath").PrimaryColumn("Id");
        }

        public override void Down()
        {
        }
    }
}