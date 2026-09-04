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

using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Reactivity.Interfaces;

namespace Jube.Test.Service.EntityAnalysisModel
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Jube.Data.Context;
    using Jube.Data.Poco;
    using Jube.Dto.EntityAnalysisModel;
    using Jube.Service.Exceptions.EntityAnalysisModel;
    using Jube.Service.Observability;
    using Jube.Service.Reactivity;
    using Infrastructure;
    using LinqToDB;
    using log4net;
    using Microsoft.Extensions.Diagnostics.Metrics.Testing;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Xunit;
    using EntityAnalysisModelService = Jube.Service.EntityAnalysisModel.EntityAnalysisModelService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityAnalysisModelServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly List<int> createdIds = [];

        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            if (createdIds.Count == 0)
            {
                return;
            }

            await using var dbContext = fx.GetDbContext();
            foreach (var id in createdIds)
            {
                await dbContext.GetTable<EntityAnalysisModelVersion>().Where(w => w.EntityAnalysisModelId == id)
                    .DeleteAsync();
                await dbContext.EntityAnalysisModel.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<EntityAnalysisModelService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null) =>
            EntityAnalysisModelService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);

        private static EntityAnalysisModelDto NewDto(string name) => new()
        {
            Name = name,
            EntryName = "TxnId",
            EntryXPath = "$.TxnId",
            ReferenceDateName = "TxnDateTime",
            ReferenceDateXPath = "$.TxnDateTime",
            ReferenceDatePayloadLocationTypeId = 1,
            CacheTtlInterval = 'h',
            CacheFetchLimit = 100,
            CacheTtlIntervalValue = 1,
            MaxResponseElevation = 10,
            MaxResponseElevationInterval = 'd',
            MaxActivationWatcherInterval = 'd',
            ActivationWatcherSample = 1
        };

        private static string UniqueName(string label) => $"{DatabaseFixture.Prefix}{label}{Guid.NewGuid():N}"[..40];

        [Fact]
        public async Task InsertPersistsAndReturnsVersionOneAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(UniqueName("Insert")));
            createdIds.Add(saved.Id);

            saved.Id.Should().BeGreaterThan(0);
            saved.Version.Should().Be(1);
            saved.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            saved.Guid.Should().NotBe(Guid.Empty);
            saved.CreatedDate.Should().NotBeNull();
            saved.CreatedDate!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task GetAllReturnsCreatedRowAndGetByIdReturnsItAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var name = UniqueName("GetAll");
            var saved = await service.InsertAsync(NewDto(name));
            createdIds.Add(saved.Id);

            var all = await service.GetAsync();
            all.Should().Contain(d => d.Id == saved.Id && d.Name == name);

            var byId = await service.GetByIdAsync(saved.Id);
            byId.Should().NotBeNull();
            byId!.Name.Should().Be(name);
        }

        [Fact]
        public async Task UpdateIncrementsVersionAndWritesAuditRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(UniqueName("Update")));
            createdIds.Add(saved.Id);

            var dto = NewDto(saved.Name);
            dto.Id = saved.Id;
            dto.CacheFetchLimit = 250;

            var updated = await service.UpdateAsync(dto);

            updated.Version.Should().Be(2);
            updated.CacheFetchLimit.Should().Be(250);
            updated.Guid.Should().Be(saved.Guid);

            var auditRows = await dbContext.GetTable<EntityAnalysisModelVersion>()
                .Where(w => w.EntityAnalysisModelId == saved.Id).CountAsync();
            auditRows.Should().Be(1);
        }

        [Fact]
        public async Task UpdatePreservesOriginalCreatedAuditButSetsUpdatedUserAndDateAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(UniqueName("AuditTrail")));
            createdIds.Add(saved.Id);

            var dto = NewDto(saved.Name);
            dto.Id = saved.Id;
            var updated = await service.UpdateAsync(dto);

            updated.CreatedUser.Should().Be(saved.CreatedUser);
            updated.CreatedDate.Should().BeCloseTo(saved.CreatedDate!.Value, TimeSpan.FromSeconds(1));
            updated.UpdatedUser.Should().Be(fx.Seed.UserWithPermission);
            updated.UpdatedDate.Should().NotBeNull();
            updated.UpdatedDate!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task DeleteSoftDeletesAndRowDisappearsFromReadsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(UniqueName("Delete")));
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
            var writer = await BuildServiceAsync(writerDb, fx.Seed.UserWithPermission);
            var seedRow = await writer.InsertAsync(NewDto(UniqueName("Forbidden")));
            createdIds.Add(seedRow.Id);

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var beforeCount =
                await dbContext.EntityAnalysisModel.CountAsync(w => w.Name!.StartsWith(DatabaseFixture.Prefix));

            await Assert.ThrowsAsync<ForbiddenException>(() => service.InsertAsync(NewDto(UniqueName("Denied"))));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetByIdAsync(seedRow.Id));

            var updateDto = NewDto(seedRow.Name);
            updateDto.Id = seedRow.Id;
            await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateAsync(updateDto));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.DeleteAsync(seedRow.Id));

            var afterCount =
                await dbContext.EntityAnalysisModel.CountAsync(w => w.Name!.StartsWith(DatabaseFixture.Prefix));
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
                EntityAnalysisModelService.CreateAsync(dbContext, userName, log, localizers, new NullServiceChangeBus(),
                    TestLog.NoOp));

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
        public async Task LandlordUserBypassesPermissionChecksButStaysTenantScopedAsync()
        {
            await using var ownerDb = fx.GetDbContext();
            var owner = await BuildServiceAsync(ownerDb, fx.Seed.UserWithPermission);
            var savedByOwner = await owner.InsertAsync(NewDto(UniqueName("LandlordOtherTenant")));
            createdIds.Add(savedByOwner.Id);

            await using var dbContext = fx.GetDbContext();
            var landlord = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var savedByLandlord = await landlord.InsertAsync(NewDto(UniqueName("LandlordOwnTenant")));
            createdIds.Add(savedByLandlord.Id);
            savedByLandlord.Id.Should().BeGreaterThan(0);

            var otherTenantsRow = await landlord.GetByIdAsync(savedByOwner.Id);
            otherTenantsRow.Should().BeNull();
        }

        [Fact]
        public async Task LandlordGetAllNeverContainsOtherTenantsRowsAsync()
        {
            await using var ownerDb = fx.GetDbContext();
            var owner = await BuildServiceAsync(ownerDb, fx.Seed.UserWithPermission);
            var savedByOwner = await owner.InsertAsync(NewDto(UniqueName("LandlordListIsolation")));
            createdIds.Add(savedByOwner.Id);

            await using var dbContext = fx.GetDbContext();
            var landlord = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var all = await landlord.GetAsync();

            all.Should().NotContain(d => d.Id == savedByOwner.Id);
        }

        [Fact]
        public async Task UserInTenantBCannotGetUpdateOrDeleteTenantARowAsync()
        {
            await using var ownerDb = fx.GetDbContext();
            var owner = await BuildServiceAsync(ownerDb, fx.Seed.UserWithPermission);
            var saved = await owner.InsertAsync(NewDto(UniqueName("Isolation")));
            createdIds.Add(saved.Id);

            await using var dbContext = fx.GetDbContext();
            var otherTenant = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            var byId = await otherTenant.GetByIdAsync(saved.Id);
            byId.Should().BeNull();

            var updateDto = NewDto(saved.Name);
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
            var owner = await BuildServiceAsync(ownerDb, fx.Seed.UserWithPermission);
            var saved = await owner.InsertAsync(NewDto(UniqueName("TenantAOnly")));
            createdIds.Add(saved.Id);

            await using var dbContext = fx.GetDbContext();
            var otherTenant = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);
            var all = await otherTenant.GetAsync();

            all.Should().NotContain(d => d.Id == saved.Id);
        }

        [Fact]
        public async Task UserInBothTenantsGetsEachRowOnceFromGetAllAsync()
        {
            await using var aDb = fx.GetDbContext();
            var asTenantA = await BuildServiceAsync(aDb, fx.Seed.UserBothTenants);
            var savedA = await asTenantA.InsertAsync(NewDto(UniqueName("BothA")));
            createdIds.Add(savedA.Id);

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserBothTenants);
            var all = await service.GetAsync();

            all.Count(d => d.Id == savedA.Id).Should().Be(1);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task NameRequiredRejectsNullEmptyOrWhitespaceAsync(string? name)
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(name!);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e =>
                e.PropertyName == nameof(EntityAnalysisModelDto.Name) && e.ErrorCode == "NameNotEmpty");
        }

        [Fact]
        public async Task NameRequiredMessageResolvesPerCultureAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var originalCulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo("fr");
                var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(NewDto("")));
                ex.Result.Errors.Should().Contain(e =>
                    e.PropertyName == nameof(EntityAnalysisModelDto.Name) && e.ErrorMessage == "Un nom est requis.");
            }
            finally
            {
                CultureInfo.CurrentUICulture = originalCulture;
            }
        }

        [Fact]
        public async Task NameOverMaximumLengthIsRejectedWithErrorCodeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(new string('n', 257));

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "NameMaximumLength");
        }

        [Fact]
        public async Task DuplicateNameWithinSameTenantFailsButDifferentTenantSucceedsAsync()
        {
            var name = UniqueName("Dup");

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var first = await service.InsertAsync(NewDto(name));
            createdIds.Add(first.Id);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(NewDto(name)));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "NameDuplicate");

            await using var otherDb = fx.GetDbContext();
            var otherTenantService = await BuildServiceAsync(otherDb, fx.Seed.UserTenantB);
            var second = await otherTenantService.InsertAsync(NewDto(name));
            createdIds.Add(second.Id);
        }

        [Fact]
        public async Task DuplicateNameDifferingOnlyByCaseWithinSameTenantFailsAsync()
        {
            var name = UniqueName("CaseDup");

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var first = await service.InsertAsync(NewDto(name));
            createdIds.Add(first.Id);

            var ex = await Assert.ThrowsAsync<DtoValidationException>(
                () => service.InsertAsync(NewDto(name.ToUpperInvariant())));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "NameDuplicate");
        }

        [Fact]
        public async Task UpdateWithSameNameOnSameRowPassesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(UniqueName("SelfName")));
            createdIds.Add(saved.Id);

            var dto = NewDto(saved.Name);
            dto.Id = saved.Id;
            var updated = await service.UpdateAsync(dto);
            updated.Id.Should().Be(saved.Id);
        }

        [Fact]
        public async Task ReferenceDateXPathNotRequiredWhenPayloadLocationIsNowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = NewDto(UniqueName("Now"));
            dto.ReferenceDatePayloadLocationTypeId = 3;
            dto.ReferenceDateXPath = "";

            var saved = await service.InsertAsync(dto);
            createdIds.Add(saved.Id);
        }

        [Fact]
        public async Task ReferenceDateXPathRequiredWhenPayloadLocationIsBodyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = NewDto(UniqueName("Body"));
            dto.ReferenceDatePayloadLocationTypeId = 1;
            dto.ReferenceDateXPath = "";

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.PropertyName == nameof(EntityAnalysisModelDto.ReferenceDateXPath));
        }

        [Fact]
        public async Task InvalidReferenceDatePayloadLocationTypeIdIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(UniqueName("BadPayloadLoc"));
            dto.ReferenceDatePayloadLocationTypeId = 9;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "ReferenceDatePayloadLocationTypeIdInvalid");
        }

        [Fact]
        public async Task InvalidCacheTtlIntervalIsRejectedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(UniqueName("BadInterval"));
            dto.CacheTtlInterval = 'x';

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(dto));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "CacheTtlIntervalInvalid");
        }

        [Fact]
        public async Task ActivationWatcherSampleOutOfRangeOnlyRejectedWhenWatcherEnabledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var disabled = NewDto(UniqueName("SampleOff"));
            disabled.EnableActivationWatcher = false;
            disabled.ActivationWatcherSample = 5;
            var savedDisabled = await service.InsertAsync(disabled);
            createdIds.Add(savedDisabled.Id);

            var enabled = NewDto(UniqueName("SampleOn"));
            enabled.EnableActivationWatcher = true;
            enabled.ActivationWatcherSample = 5;
            enabled.MaxActivationWatcherInterval = 'd';
            enabled.MaxActivationWatcherValue = 1;
            enabled.MaxActivationWatcherThreshold = 100;

            var ex = await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(enabled));
            ex.Result.Errors.Should().Contain(e => e.ErrorCode == "ActivationWatcherSampleRange");
        }

        [Fact]
        public async Task TamperedIdentityAndAuditFieldsOnInsertHaveNoEffectAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = NewDto(UniqueName("Tamper"));
            dto.CreatedUser = "someone-else";
            dto.Guid = Guid.NewGuid();
            dto.Version = 999;

            var saved = await service.InsertAsync(dto);
            createdIds.Add(saved.Id);

            saved.CreatedUser.Should().Be(fx.Seed.UserWithPermission);
            saved.Version.Should().Be(1);
            saved.Guid.Should().NotBe(dto.Guid);
        }

        [Fact]
        public async Task TamperedIdentityAndAuditFieldsOnUpdateHaveNoEffectAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var saved = await service.InsertAsync(NewDto(UniqueName("TamperUpdate")));
            createdIds.Add(saved.Id);

            var dto = NewDto(saved.Name);
            dto.Id = saved.Id;
            dto.CreatedUser = "someone-else";
            dto.Guid = Guid.NewGuid();
            dto.Version = 999;
            dto.UpdatedUser = "also-tampered";

            var updated = await service.UpdateAsync(dto);

            updated.CreatedUser.Should().Be(saved.CreatedUser);
            updated.Guid.Should().Be(saved.Guid);
            updated.Version.Should().Be(2);
            updated.UpdatedUser.Should().Be(fx.Seed.UserWithPermission);
        }

        [Fact]
        public async Task UpdateOfLockedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(UniqueName("Locked")));
            createdIds.Add(saved.Id);

            await dbContext.EntityAnalysisModel.Where(w => w.Id == saved.Id).Set(s => s.Locked, (byte)1).UpdateAsync();

            var dto = NewDto(saved.Name);
            dto.Id = saved.Id;
            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task UpdateOfSoftDeletedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(UniqueName("PreDeleted")));
            createdIds.Add(saved.Id);

            await service.DeleteAsync(saved.Id);

            var dto = NewDto(saved.Name);
            dto.Id = saved.Id;
            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task UpdateOfNeverExistedIdThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = NewDto(UniqueName("Missing"));
            dto.Id = Int32.MaxValue - 1;

            await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(dto));
        }

        [Fact]
        public async Task DeleteOfMissingOrAlreadyDeletedRowThrowsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(Int32.MaxValue - 1));

            var saved = await service.InsertAsync(NewDto(UniqueName("DoubleDelete")));
            createdIds.Add(saved.Id);
            await service.DeleteAsync(saved.Id);

            await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(saved.Id));
        }

        [Fact]
        public async Task ActiveAndLockedRoundTripBoolToByteAndBackAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(UniqueName("ActiveLocked"));
            dto.Active = true;

            var saved = await service.InsertAsync(dto);
            createdIds.Add(saved.Id);

            var fetched = await service.GetByIdAsync(saved.Id);
            fetched!.Active.Should().BeTrue();
            fetched.Locked.Should().BeFalse();
        }

        [Fact]
        public async Task FieldsWithNoBackingPocoColumnAreAlwaysDefaultOnReadAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var dto = NewDto(UniqueName("Orphan"));
            dto.EnableElasticsearchArchive = true;
            dto.EntryPayloadLocationTypeId = 1;

            var saved = await service.InsertAsync(dto);
            createdIds.Add(saved.Id);

            var fetched = await service.GetByIdAsync(saved.Id);
            fetched!.EnableElasticsearchArchive.Should().BeFalse();
            fetched.EntryPayloadLocationTypeId.Should().Be(0);
        }

        [Fact]
        public async Task GetByIdForMissingIdReturnsNullNotExceptionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetByIdAsync(Int32.MaxValue - 1);
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
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            var saved = await service.InsertAsync(NewDto(UniqueName("LogInsert")));
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
            var log = new TestLog(enabled: false);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            var saved = await service.InsertAsync(NewDto(UniqueName("Gated")));
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
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(UniqueName("Span")));
            createdIds.Add(saved.Id);

            var createSpan = activities.Should().ContainSingle(a => a.OperationName == "EntityAnalysisModel.Create")
                .Subject;
            createSpan.GetTagItem("jube.outcome").Should().Be("ok");
            createSpan.GetTagItem("jube.entity.id").Should().Be(saved.Id);
        }

        [Fact]
        public async Task EachCallRecordsOneDurationMeasurementAsync()
        {
            using var collector = new MetricCollector<double>(ServiceDiagnostics.OperationDuration);

            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var saved = await service.InsertAsync(NewDto(UniqueName("Metric")));
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

        private sealed class CapturingBus : IServiceChangeBus
        {
            public readonly List<ServiceChangeEvent> Published = [];

            public Task PublishAsync(ServiceChangeEvent change, CancellationToken token = default)
            {
                Published.Add(change);
                return Task.CompletedTask;
            }

            public IDisposable Subscribe(Func<ServiceChangeEvent, Task> handler) => NoopSubscription.Instance;

            private sealed class NoopSubscription : IDisposable
            {
                public static readonly NoopSubscription Instance = new();

                public void Dispose()
                {
                }
            }
        }

        [Fact]
        public async Task InsertPublishesExactlyOneCreatedEventAsync()
        {
            var serviceChangeBus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: serviceChangeBus);

            var saved = await service.InsertAsync(NewDto(UniqueName("Reactive")));
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
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: serviceChangeBus);

            await service.GetAsync();
            await Assert.ThrowsAsync<DtoValidationException>(() => service.InsertAsync(NewDto("")));

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
                "EntityAnalysisModelList", "EntityAnalysisModelGet", "EntityAnalysisModelCreate",
                "EntityAnalysisModelUpdate", "EntityAnalysisModelDelete"
            ]);
        }

        [Fact]
        public async Task ListAsyncClampsTakeAndPaginatesDeterministicallyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            for (var i = 0; i < 3; i++)
            {
                var saved = await service.InsertAsync(NewDto(UniqueName($"Page{i}")));
                createdIds.Add(saved.Id);
            }

            var page = await service.ListAsync(take: 2);
            page.Items.Count.Should().BeLessThanOrEqualTo(2);

            var oversized = await service.ListAsync(take: 10_000);
            oversized.Items.Count.Should().BeLessThanOrEqualTo(200);
        }
    }
}