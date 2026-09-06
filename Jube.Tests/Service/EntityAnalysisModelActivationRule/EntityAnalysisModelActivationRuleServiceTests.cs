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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.EntityAnalysisModelActivationRule;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.EntityAnalysisModelActivationRule;
using Jube.Service.Exceptions.EntityAnalysisModelActivationRule;
using Jube.Service.Observability;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.EntityAnalysisModelActivationRule
{
    using EntityAnalysisModelActivationRuleService = EntityAnalysisModelActivationRuleService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisModelActivationRuleServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdIds = [];
        private readonly List<int> createdModelIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var id in createdIds)
            {
                await dbContext.GetTable<EntityAnalysisModelActivationRuleVersion>()
                    .Where(w => w.EntityAnalysisModelActivationRuleId == id).DeleteAsync();
                await dbContext.GetTable<Data.Poco.EntityAnalysisModelActivationRule>().Where(w => w.Id == id)
                    .DeleteAsync();
            }

            foreach (var modelId in createdModelIds)
            {
                await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == modelId)
                    .DeleteAsync();
                await dbContext.EntityAnalysisModel.Where(w => w.Id == modelId).DeleteAsync();
            }
        }

        private static Task<EntityAnalysisModelActivationRuleService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return EntityAnalysisModelActivationRuleService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task<int> CreateParentModelAsync(DbContext dbContext, string createdUser)
        {
            var repository = new EntityAnalysisModelRepository(dbContext, createdUser);
            var saved = await repository.InsertAsync(new Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0
            }).ConfigureAwait(false);

            createdModelIds.Add(saved.Id);
            return saved.Id;
        }

        private static EntityAnalysisModelActivationRuleDto NewDto(int entityAnalysisModelId, string name)
        {
            return new EntityAnalysisModelActivationRuleDto
            {
                EntityAnalysisModelId = entityAnalysisModelId,
                Name = name,
                BuilderRuleScript = "If (Payload.CurrencyAmount > 0) Then\n   Return True\nEnd If",
                Json = "{\"valid\":true,\"condition\":\"AND\",\"rules\":[]}",
                CoderRuleScript = "Return True",
                RuleScriptTypeId = 1,
                ReviewStatusId = 0,
                ActivationSample = 1,
                Priority = 0
            };
        }

        private static string UniqueName(string label)
        {
            return $"{DatabaseFixture.Prefix}{label}{Guid.NewGuid():N}"[..40];
        }

        [Fact]
        public async Task InsertPersistsAndReturnsVersionOneAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("Insert")));
            createdIds.Add(saved.Id);

            saved.Id.Should().BeGreaterThan(0);
            saved.Version.Should().Be(1);
            saved.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            saved.CreatedDate.Should().NotBeNull();
            saved.CreatedDate!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task GetAllReturnsCreatedRowAndGetByIdReturnsItAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var name = UniqueName("GetAll");
            var saved = await service.InsertAsync(NewDto(modelId, name));
            createdIds.Add(saved.Id);

            var all = await service.GetAsync();
            all.Should().Contain(d => d.Id == saved.Id && d.Name == name);

            var byId = await service.GetByIdAsync(saved.Id);
            byId.Should().NotBeNull();
            byId!.Name.Should().Be(name);
        }

        [Fact]
        public async Task GetByEntityAnalysisModelIdReturnsOnlyRowsForThatModelAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelAId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var modelBId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var savedA = await service.InsertAsync(NewDto(modelAId, UniqueName("ByModelA")));
            createdIds.Add(savedA.Id);
            var savedB = await service.InsertAsync(NewDto(modelBId, UniqueName("ByModelB")));
            createdIds.Add(savedB.Id);

            var forModelA = await service.GetByEntityAnalysisModelIdAsync(modelAId);
            forModelA.Should().Contain(d => d.Id == savedA.Id);
            forModelA.Should().NotContain(d => d.Id == savedB.Id);
        }

        [Fact]
        public async Task UpdateIncrementsVersionAndWritesAuditRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("Update")));
            createdIds.Add(saved.Id);

            var dto = NewDto(modelId, saved.Name);
            dto.Id = saved.Id;
            dto.Priority = 5;

            var updated = await service.UpdateAsync(dto);

            updated.Version.Should().Be(2);
            updated.Priority.Should().Be(5);
            updated.Guid.Should().Be(saved.Guid);

            var auditRows = await dbContext.GetTable<EntityAnalysisModelActivationRuleVersion>()
                .Where(w => w.EntityAnalysisModelActivationRuleId == saved.Id).CountAsync();
            auditRows.Should().Be(1);
        }

        [Fact]
        public async Task DeleteSoftDeletesAndRowDisappearsFromReadsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("Delete")));
            createdIds.Add(saved.Id);

            await service.DeleteAsync(saved.Id);

            var byId = await service.GetByIdAsync(saved.Id);
            byId.Should().BeNull();

            var all = await service.GetAsync();
            all.Should().NotContain(d => d.Id == saved.Id);
        }

        [Fact]
        public async Task EveryMethodThrowsForbiddenWhenPermissionMissingAndWritesNoRowAsync()
        {
            await using var writerDb = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(writerDb, fx.Seed.UserWithPermission);
            var writer = await BuildServiceAsync(writerDb, fx.Seed.UserWithPermission);
            var seedRow = await writer.InsertAsync(NewDto(modelId, UniqueName("Forbidden")));
            createdIds.Add(seedRow.Id);

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var beforeCount = await dbContext.GetTable<Data.Poco.EntityAnalysisModelActivationRule>()
                .CountAsync(w => w.Name!.StartsWith(DatabaseFixture.Prefix));

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                service.InsertAsync(NewDto(modelId, UniqueName("Denied"))));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(seedRow.Id));

            var updateDto = NewDto(modelId, seedRow.Name);
            updateDto.Id = seedRow.Id;
            await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(updateDto));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(seedRow.Id));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.ResetCounterAsync(seedRow.Id));

            var afterCount = await dbContext.GetTable<Data.Poco.EntityAnalysisModelActivationRule>()
                .CountAsync(w => w.Name!.StartsWith(DatabaseFixture.Prefix));
            afterCount.Should().Be(beforeCount);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task NullOrBlankUserNameThrowsNotAuthenticatedBeforeAnyQueryAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();

            var ex = await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                EntityAnalysisModelActivationRuleService.CreateAsync(dbContext, userName, log, localizers,
                    new NullServiceChangeBus(), TestLog.NoOp));

            ex.Code.Should().Be("NotAuthenticated");
            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task UserWithNoUserInTenantRowThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UserNoTenant));
        }

        [Fact]
        public async Task UnknownUserThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UnknownUser));
        }

        [Fact]
        public async Task UserInTenantBCannotGetUpdateOrDeleteTenantARowAsync()
        {
            await using var ownerDb = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(ownerDb, fx.Seed.UserWithPermission);
            var owner = await BuildServiceAsync(ownerDb, fx.Seed.UserWithPermission);
            var saved = await owner.InsertAsync(NewDto(modelId, UniqueName("Isolation")));
            createdIds.Add(saved.Id);

            await using var dbContext = fx.GetDbContext();
            var otherTenant = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var byId = await otherTenant.GetByIdAsync(saved.Id);
            byId.Should().BeNull();

            var updateDto = NewDto(modelId, saved.Name);
            updateDto.Id = saved.Id;
            await Assert.ThrowsAsync<NotFoundException>(() => otherTenant.UpdateAsync(updateDto));
            await Assert.ThrowsAsync<NotFoundException>(() => otherTenant.DeleteAsync(saved.Id));

            var stillThere = await owner.GetByIdAsync(saved.Id);
            stillThere.Should().NotBeNull();
        }

        [Fact]
        public async Task GetAllAsTenantBNeverContainsTenantARowsAsync()
        {
            await using var ownerDb = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(ownerDb, fx.Seed.UserWithPermission);
            var owner = await BuildServiceAsync(ownerDb, fx.Seed.UserWithPermission);
            var saved = await owner.InsertAsync(NewDto(modelId, UniqueName("TenantAOnly")));
            createdIds.Add(saved.Id);

            await using var dbContext = fx.GetDbContext();
            var otherTenant = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var all = await otherTenant.GetAsync();

            all.Should().NotContain(d => d.Id == saved.Id);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task NameRequiredRejectsNullEmptyOrWhitespaceAsync(string? name)
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, name!);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e =>
                e.PropertyName == nameof(EntityAnalysisModelActivationRuleDto.Name) &&
                e.ErrorCode == "NameNotEmpty");
        }

        [Fact]
        public async Task NameOverMaximumLengthIsRejectedWithErrorCodeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, new string('n', 257));

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "NameMaximumLength");
        }

        [Fact]
        public async Task DuplicateNameWithinSameModelFailsButDifferentModelSucceedsAsync()
        {
            var name = UniqueName("Dup");

            await using var dbContext = fx.GetDbContext();
            var modelAId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var modelBId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var first = await service.InsertAsync(NewDto(modelAId, name));
            createdIds.Add(first.Id);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.InsertAsync(NewDto(modelAId, name)));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "NameDuplicate");

            var second = await service.InsertAsync(NewDto(modelBId, name));
            createdIds.Add(second.Id);
        }

        [Fact]
        public async Task InvalidEntityAnalysisModelIdIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(0, UniqueName("BadModelId"));

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "EntityAnalysisModelIdInvalid");
        }

        [Fact]
        public async Task InvalidReviewStatusIdIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, UniqueName("BadReview"));
            dto.ReviewStatusId = 99;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "ReviewStatusIdInvalid");
        }

        [Fact]
        public async Task InvalidRuleScriptTypeIdIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, UniqueName("BadRuleType"));
            dto.RuleScriptTypeId = 99;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "RuleScriptTypeIdInvalid");
        }

        [Fact]
        public async Task BuilderRuleScriptRequiredRejectsEmptyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, UniqueName("NoBuilder"));
            dto.RuleScriptTypeId = 1;
            dto.BuilderRuleScript = "";

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "BuilderRuleScriptNotEmpty");
        }

        [Fact]
        public async Task JsonRequiredRejectsEmptyWhenBuilderModeSelectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, UniqueName("NoJson"));
            dto.RuleScriptTypeId = 1;
            dto.Json = "";

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "JsonNotEmpty");
        }

        [Fact]
        public async Task CoderRuleScriptRequiredRejectsEmptyWhenCoderModeSelectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, UniqueName("NoCoder"));
            dto.RuleScriptTypeId = 2;
            dto.CoderRuleScript = "";

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "CoderRuleScriptNotEmpty");
        }

        [Fact]
        public async Task BuilderModeDoesNotRequireCoderRuleScriptAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, UniqueName("BuilderOnly"));
            dto.RuleScriptTypeId = 1;
            dto.CoderRuleScript = "";

            var saved = await service.InsertAsync(dto);
            createdIds.Add(saved.Id);
        }

        [Fact]
        public async Task CoderModeDoesNotRequireBuilderRuleScriptOrJsonAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, UniqueName("CoderOnly"));
            dto.RuleScriptTypeId = 2;
            dto.BuilderRuleScript = "";
            dto.Json = "";

            var saved = await service.InsertAsync(dto);
            createdIds.Add(saved.Id);
        }

        [Fact]
        public async Task CaseWorkflowFieldsRequiredWhenEnableCaseWorkflowTrueAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, UniqueName("NoCaseFields"));
            dto.EnableCaseWorkflow = true;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "CaseWorkflowGuidNotEmpty");
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "CaseWorkflowStatusGuidNotEmpty");
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "CaseKeyNotEmpty");
        }

        [Fact]
        public async Task CaseWorkflowFieldsNotRequiredWhenEnableCaseWorkflowFalseAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, UniqueName("CaseOff"));
            dto.EnableCaseWorkflow = false;

            var saved = await service.InsertAsync(dto);
            createdIds.Add(saved.Id);
        }

        [Fact]
        public async Task BypassFieldsValidatedOnlyWhenCaseWorkflowAndBypassEnabledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, UniqueName("BadBypass"));
            dto.EnableCaseWorkflow = true;
            dto.CaseWorkflowGuid = Guid.NewGuid();
            dto.CaseWorkflowStatusGuid = Guid.NewGuid();
            dto.CaseKey = "AccountId";
            dto.EnableBypass = true;
            dto.BypassSuspendSample = 1.5;
            dto.BypassSuspendInterval = 'x';
            dto.BypassSuspendValue = -1;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "BypassSuspendSampleRange");
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "BypassSuspendIntervalInvalid");
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "BypassSuspendValueRange");
        }

        [Fact]
        public async Task ResponseElevationFieldsRequiredWhenEnabledAndSentToActivationWatcherAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, UniqueName("NoResponseElevationFields"));
            dto.EnableResponseElevation = true;
            dto.SendToActivationWatcher = true;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "ResponseElevationKeyNotEmpty");
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "ResponseElevationForeColorNotEmpty");
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "ResponseElevationBackColorNotEmpty");
        }

        [Fact]
        public async Task ResponseElevationColorsMustBeHexAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, UniqueName("BadColors"));
            dto.EnableResponseElevation = true;
            dto.SendToActivationWatcher = true;
            dto.ResponseElevationKey = "AccountId";
            dto.ResponseElevationForeColor = "blue";
            dto.ResponseElevationBackColor = "red";

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "ResponseElevationForeColorInvalid");
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "ResponseElevationBackColorInvalid");
        }

        [Fact]
        public async Task ResponseElevationFieldsNotRequiredWhenNotSentToActivationWatcherAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, UniqueName("ElevationNoWatcher"));
            dto.EnableResponseElevation = true;
            dto.SendToActivationWatcher = false;

            var saved = await service.InsertAsync(dto);
            createdIds.Add(saved.Id);
        }

        [Fact]
        public async Task NotificationDestinationRequiredWhenEnableNotificationTrueAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, UniqueName("NoNotificationDestination"));
            dto.EnableNotification = true;
            dto.NotificationTypeId = 1;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "NotificationDestinationNotEmpty");
        }

        [Fact]
        public async Task TtlCounterFieldsRequiredWhenEnableTtlCounterTrueAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, UniqueName("NoTtlCounterFields"));
            dto.EnableTtlCounter = true;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "EntityAnalysisModelGuidTtlCounterNotEmpty");
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "EntityAnalysisModelTtlCounterGuidNotEmpty");
        }

        [Theory]
        [InlineData(-0.1)]
        [InlineData(1.1)]
        public async Task ActivationSampleMustBeBetweenZeroAndOneAsync(double sample)
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, UniqueName("BadSample"));
            dto.ActivationSample = sample;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "ActivationSampleRange");
        }

        [Fact]
        public async Task PriorityCannotBeNegativeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, UniqueName("BadPriority"));
            dto.Priority = -1;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "PriorityRange");
        }

        [Fact]
        public async Task InsertWithApprovedByReviewWithoutPermissionThrowsReviewStatusApprovalExceptionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermissionNoApproveByReview);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermissionNoApproveByReview);
            var dto = NewDto(modelId, UniqueName("NoApprove"));
            dto.ReviewStatusId = 4;

            var ex = await Assert.ThrowsAsync<ReviewStatusApprovalException>(() => service.InsertAsync(dto));
            ex.Code.Should().Be("PermissionDenied");
            ex.PropertyName.Should().Be("Permission");
        }

        [Fact]
        public async Task InsertWithApprovedByReviewWithPermissionSucceedsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, UniqueName("Approve"));
            dto.ReviewStatusId = 4;

            var saved = await service.InsertAsync(dto);
            createdIds.Add(saved.Id);

            saved.ReviewStatusId.Should().Be(4);
        }

        [Fact]
        public async Task UpdateWithApprovedByReviewWithoutPermissionThrowsReviewStatusApprovalExceptionAsync()
        {
            await using var ownerDb = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(ownerDb, fx.Seed.UserWithPermissionNoApproveByReview);
            var owner = await BuildServiceAsync(ownerDb, fx.Seed.UserWithPermissionNoApproveByReview);
            var saved = await owner.InsertAsync(NewDto(modelId, UniqueName("NoApproveUpdate")));
            createdIds.Add(saved.Id);

            var dto = NewDto(modelId, saved.Name);
            dto.Id = saved.Id;
            dto.ReviewStatusId = 4;

            var ex = await Assert.ThrowsAsync<ReviewStatusApprovalException>(() => owner.UpdateAsync(dto));
            ex.Code.Should().Be("PermissionDenied");
            ex.PropertyName.Should().Be("Permission");
        }

        [Fact]
        public async Task TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = NewDto(modelId, UniqueName("Tamper"));
            dto.CreatedUser = "someone-else";
            dto.Version = 999;

            var saved = await service.InsertAsync(dto);
            createdIds.Add(saved.Id);

            saved.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            saved.Version.Should().Be(1);
        }

        [Fact]
        public async Task TamperedIdentityAndAuditFieldsOnUpdateHaveNoEffectAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("TamperUpdate")));
            createdIds.Add(saved.Id);

            var dto = NewDto(modelId, saved.Name);
            dto.Id = saved.Id;
            dto.CreatedUser = "someone-else";
            dto.Version = 999;
            dto.UpdatedUser = "also-tampered";

            var updated = await service.UpdateAsync(dto);
            updated.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            updated.Guid.Should().Be(saved.Guid);
            updated.Version.Should().Be(2);
            updated.UpdatedUser.Should().BeNull();
        }

        [Fact]
        public async Task UpdateOfLockedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("Locked")));
            createdIds.Add(saved.Id);

            await dbContext.GetTable<Data.Poco.EntityAnalysisModelActivationRule>()
                .Where(w => w.Id == saved.Id).Set(s => s.Locked, (byte)1).UpdateAsync();

            var dto = NewDto(modelId, saved.Name);
            dto.Id = saved.Id;
            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task UpdateOfSoftDeletedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("PreDeleted")));
            createdIds.Add(saved.Id);

            await service.DeleteAsync(saved.Id);

            var dto = NewDto(modelId, saved.Name);
            dto.Id = saved.Id;
            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task UpdateOfNeverExistedIdThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = NewDto(modelId, UniqueName("Missing"));
            dto.Id = int.MaxValue - 1;

            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task DeleteOfMissingOrAlreadyDeletedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(int.MaxValue - 1));

            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("DoubleDelete")));
            createdIds.Add(saved.Id);
            await service.DeleteAsync(saved.Id);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(saved.Id));
        }

        [Fact]
        public async Task ActiveAndLockedRoundTripBoolToByteAndBackAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(modelId, UniqueName("ActiveLocked"));
            dto.Active = true;

            var saved = await service.InsertAsync(dto);
            createdIds.Add(saved.Id);

            var fetched = await service.GetByIdAsync(saved.Id);
            fetched!.Active.Should().BeTrue();
            fetched.Locked.Should().BeFalse();
        }

        [Fact]
        public async Task ResetCounterZeroesActivationAndEvaluationCountersAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("ResetMe")));
            createdIds.Add(saved.Id);

            await dbContext.GetTable<Data.Poco.EntityAnalysisModelActivationRule>()
                .Where(w => w.Id == saved.Id)
                .Set(s => s.ActivationCounter, 5L)
                .Set(s => s.EvaluationCounter, 10L)
                .UpdateAsync();

            await service.ResetCounterAsync(saved.Id);

            var fetched = await service.GetByIdAsync(saved.Id);
            fetched!.ActivationCounter.Should().Be(0);
            fetched.EvaluationCounter.Should().Be(0);
        }

        [Fact]
        public async Task ResetCounterOfMissingRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NotFoundException>(() => service.ResetCounterAsync(int.MaxValue - 1));
        }

        [Fact]
        public async Task GetByIdForMissingIdReturnsNullNotExceptionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetByIdAsync(int.MaxValue - 1);
            result.Should().BeNull();
        }

        [Fact]
        public async Task PreCancelledTokenOnReadThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync(cts.Token));
        }

        [Fact]
        public async Task PermissionDeniedLogsWarnNotErrorAndGatesHoldAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(1));

            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains(fx.Seed.UserWithoutPermission));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task SuccessfulInsertLogsExactlyOneInfoAndReadsLogNoInfoAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("LogInsert")));
            createdIds.Add(saved.Id);

            log.Entries.Count(e => e.Level == "INFO").Should().Be(1);

            var infoCountBeforeRead = log.Entries.Count(e => e.Level == "INFO");
            await service.GetAsync();
            log.Entries.Count(e => e.Level == "INFO").Should().Be(infoCountBeforeRead);
        }

        [Fact]
        public async Task WithGatesDisabledHappyPathRecordsNoDebugInfoOrWarnAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var log = new TestLog(false);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("Gated")));
            createdIds.Add(saved.Id);

            log.Entries.Should().BeEmpty();
        }

        [Fact]
        public async Task FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync()
        {
            var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            await dbContext.DisposeAsync();

            await Assert.ThrowsAnyAsync<Exception>(() => service.GetAsync());

            var errorEntry = log.Entries.Should().ContainSingle(e => e.Level == "ERROR").Subject;
            errorEntry.Exception.Should().NotBeNull();
        }

        [Fact]
        public async Task EachCallEmitsOneSpanWithOutcomeTagAsync()
        {
            var activities = new List<Activity>();
            using var listener = new ActivityListener();
            listener.ShouldListenTo = s => s.Name == ServiceDiagnostics.Name;
            listener.Sample = (ref _) => ActivitySamplingResult.AllData;
            listener.ActivityStopped = activities.Add;
            ActivitySource.AddActivityListener(listener);

            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("Span")));
            createdIds.Add(saved.Id);

            var createSpan = activities.Should()
                .ContainSingle(a => a.OperationName == "EntityAnalysisModelActivationRule.Create").Subject;
            createSpan.GetTagItem("jube.outcome").Should().Be("ok");
            createSpan.GetTagItem("jube.entity.id").Should().Be(saved.Id);
        }

        [Fact]
        public async Task EachCallRecordsOneDurationMeasurementAsync()
        {
            using var collector = new MetricCollector<double>(ServiceDiagnostics.OperationDuration);

            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("Metric")));
            createdIds.Add(saved.Id);

            collector.GetMeasurementSnapshot().Should().ContainSingle(m => (string)m.Tags["operation"]! == "Create");
        }

        [Fact]
        public async Task AuditLogGetsExactlyOneLineIncludingReadsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            await service.GetAsync();

            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("op=List");
        }

        [Fact]
        public async Task InsertPublishesExactlyOneCreatedEventAsync()
        {
            var serviceChangeBus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: serviceChangeBus);

            var saved = await service.InsertAsync(NewDto(modelId, UniqueName("Reactive")));
            createdIds.Add(saved.Id);

            serviceChangeBus.Published.Should().ContainSingle();
            serviceChangeBus.Published[0].Kind.Should().Be(ServiceChangeKind.Created);
            serviceChangeBus.Published[0].EntityId.Should().Be(saved.Id);
        }

        [Fact]
        public async Task ReadsAndFailuresPublishNothingAsync()
        {
            var serviceChangeBus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: serviceChangeBus);

            await service.GetAsync();
            await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.InsertAsync(NewDto(modelId, "")));

            serviceChangeBus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueRegistersUniquePascalCaseNoUnderscoreNames()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();
            names.Should().OnlyHaveUniqueItems();
            names.Should().OnlyContain(n => !n.Contains('_'));
            names.Should().Contain(
            [
                "EntityAnalysisModelActivationRuleList", "EntityAnalysisModelActivationRuleGet",
                "EntityAnalysisModelActivationRuleGetByEntityAnalysisModelId",
                "EntityAnalysisModelActivationRuleCreate", "EntityAnalysisModelActivationRuleUpdate",
                "EntityAnalysisModelActivationRuleDelete", "EntityAnalysisModelActivationRuleReset"
            ]);
        }

        [Fact]
        public async Task ListAsyncClampsTakeAndPaginatesDeterministicallyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var modelId = await CreateParentModelAsync(dbContext, fx.Seed.UserWithPermission);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            for (var i = 0; i < 3; i++)
            {
                var saved = await service.InsertAsync(NewDto(modelId, UniqueName($"Page{i}")));
                createdIds.Add(saved.Id);
            }

            var page = await service.ListAsync(2);
            page.Items.Count.Should().BeLessThanOrEqualTo(2);

            var oversized = await service.ListAsync(10_000);
            oversized.Items.Count.Should().BeLessThanOrEqualTo(200);
        }

        private sealed class CapturingBus : IServiceChangeBus
        {
            public readonly List<ServiceChangeEvent> Published = [];

            public Task PublishAsync(ServiceChangeEvent change, CancellationToken token = default)
            {
                Published.Add(change);
                return Task.CompletedTask;
            }

            public IDisposable Subscribe(Func<ServiceChangeEvent, Task> handler)
            {
                return NoopSubscription.Instance;
            }

            private sealed class NoopSubscription : IDisposable
            {
                public static readonly NoopSubscription Instance = new();

                public void Dispose()
                {
                }
            }
        }
    }
}