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
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.EntityAnalysisModelTtlCounter;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.EntityAnalysisModelTtlCounter;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.EntityAnalysisModelTtlCounter;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.EntityAnalysisModelTtlCounter
{
    using TtlCounterPoco = Data.Poco.EntityAnalysisModelTtlCounter;

    public sealed class EntityAnalysisModelTtlCounterService
    {
        private const int MaxListTake = 200;
        private static readonly int[] permissions = [12];
        private static readonly int[] parentReadPermissions = [12, 17];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly EntityAnalysisModelTtlCounterRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly EntityAnalysisModelTtlCounterDtoValidator validator;

        private EntityAnalysisModelTtlCounterService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings)
        {
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            repository = new EntityAnalysisModelTtlCounterRepository(dbContext, userName);
            validator = new EntityAnalysisModelTtlCounterDtoValidator(repository, strings);
        }

        public static Task<EntityAnalysisModelTtlCounterService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelTtlCounterService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelTtlCounterResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                    log.Warn("EntityAnalysisModelTtlCounter.Create: no authenticated user; refusing.");

                throw new NotAuthenticatedException(strings[EntityAnalysisModelTtlCounterResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                    log.Warn(
                        $"EntityAnalysisModelTtlCounter.Create: user '{userName}' resolves to no tenant; refusing.");

                throw new NotAuthenticatedException(strings[EntityAnalysisModelTtlCounterResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelTtlCounterService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every TTL Counter visible to the calling user's tenant. Unbounded -- intended for " +
                     "the administrative page, not for agent tooling (use the bounded list operation instead).")]
        public async Task<List<EntityAnalysisModelTtlCounterDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelTtlCounter", "List", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelTtlCounter.List: entry user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelTtlCounter.List", permissions);
                var dtos = EntityAnalysisModelTtlCounterMapper.ToDto(await repository.GetAsync(token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                    log.Debug($"EntityAnalysisModelTtlCounter.List: {dtos.Count} rows user={userName}");

                return dtos;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelTtlCounter.List: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelTtlCounter.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the TTL Counters belonging to the given Model, ordered by Id, scoped to the " +
                     "calling user's tenant. Used to populate the Activation Rule TTL Counter picker.")]
        [ServiceOperation("EntityAnalysisModelTtlCounterGetByEntityAnalysisModelId", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<EntityAnalysisModelTtlCounterDto>> GetByEntityAnalysisModelIdAsync(
            [Description("Numeric identifier of the parent Model.")]
            int entityAnalysisModelId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelTtlCounter", "ListByEntityAnalysisModelId",
                userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"EntityAnalysisModelTtlCounter.ListByEntityAnalysisModelId: entry entityAnalysisModelId={entityAnalysisModelId} user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelTtlCounter.ListByEntityAnalysisModelId", parentReadPermissions);
                var dtos = EntityAnalysisModelTtlCounterMapper.ToDto(await repository
                    .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                    log.Debug(
                        $"EntityAnalysisModelTtlCounter.ListByEntityAnalysisModelId: {dtos.Count} rows entityAnalysisModelId={entityAnalysisModelId} user={userName}");

                return dtos;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                if (log.IsDebugEnabled)
                    log.Debug(
                        $"EntityAnalysisModelTtlCounter.ListByEntityAnalysisModelId: cancelled entityAnalysisModelId={entityAnalysisModelId} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelTtlCounter.ListByEntityAnalysisModelId: unexpected failure entityAnalysisModelId={entityAnalysisModelId} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Lists the TTL Counters belonging to the Model identified by the given Guid, scoped to the " +
                     "calling user's tenant. Used to populate the Activation Rule TTL Counter picker when only " +
                     "the parent Model's Guid is known.")]
        [ServiceOperation("EntityAnalysisModelTtlCounterGetByEntityAnalysisModelGuid", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<EntityAnalysisModelTtlCounterDto>> GetByEntityAnalysisModelGuidAsync(
            [Description("Guid of the parent Model.")]
            Guid guid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelTtlCounter", "ListByEntityAnalysisModelGuid",
                userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"EntityAnalysisModelTtlCounter.ListByEntityAnalysisModelGuid: entry guid={guid} user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelTtlCounter.ListByEntityAnalysisModelGuid",
                    parentReadPermissions);
                var dtos = EntityAnalysisModelTtlCounterMapper.ToDto(await repository
                    .GetByEntityAnalysisModelGuidAsync(guid, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                    log.Debug(
                        $"EntityAnalysisModelTtlCounter.ListByEntityAnalysisModelGuid: {dtos.Count} rows guid={guid} user={userName}");

                return dtos;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                if (log.IsDebugEnabled)
                    log.Debug(
                        $"EntityAnalysisModelTtlCounter.ListByEntityAnalysisModelGuid: cancelled guid={guid} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelTtlCounter.ListByEntityAnalysisModelGuid: unexpected failure guid={guid} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Returns one TTL Counter by its numeric identifier, scoped to the calling user's tenant. " +
                     "Returns null when the row does not exist or is not visible to the caller.")]
        [ServiceOperation("EntityAnalysisModelTtlCounterGet", OperationKind.Read, Idempotent = true)]
        public async Task<EntityAnalysisModelTtlCounterDto?> GetByIdAsync(
            [Description("Numeric identifier of the TTL Counter.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelTtlCounter", "Get", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelTtlCounter.Get: entry id={id} user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelTtlCounter.Get", permissions);
                var ttlCounter = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                if (ttlCounter == null)
                {
                    if (log.IsDebugEnabled)
                        log.Debug(
                            $"EntityAnalysisModelTtlCounter.Get: id={id} not found or not visible to tenant user={userName}");

                    return null;
                }

                op.Entity(ttlCounter.Id);
                return EntityAnalysisModelTtlCounterMapper.ToDto(ttlCounter);
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                if (log.IsDebugEnabled)
                    log.Debug($"EntityAnalysisModelTtlCounter.Get: cancelled id={id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelTtlCounter.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Lists TTL Counters for the caller's tenant, ordered by id, capped at 'take' rows (max " +
                     "200). If 'more' is true, call again with 'afterId' set to the last returned Id to " +
                     "continue.")]
        [ServiceOperation("EntityAnalysisModelTtlCounterList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<EntityAnalysisModelTtlCounterDto>> ListAsync(
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelTtlCounter", "ListPaged", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"EntityAnalysisModelTtlCounter.ListPaged: entry take={clampedTake} afterId={afterId} user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelTtlCounter.ListPaged", permissions);

                var ordered = (await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(o => o.Id)
                    .Where(w => !afterId.HasValue || w.Id > afterId.Value)
                    .ToList();

                var page = ordered.Take(clampedTake).ToList();

                op.Rows(page.Count);

                return new PagedResult<EntityAnalysisModelTtlCounterDto>(
                    EntityAnalysisModelTtlCounterMapper.ToDto(page));
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                if (log.IsDebugEnabled)
                    log.Debug($"EntityAnalysisModelTtlCounter.ListPaged: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelTtlCounter.ListPaged: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Registers a new TTL Counter under a Model in the caller's tenant. Not idempotent -- " +
                     "calling twice creates two rows.")]
        [ServiceOperation("EntityAnalysisModelTtlCounterCreate", OperationKind.Write, Idempotent = false)]
        public async Task<TtlCounterPoco> InsertAsync(
            [Description("The TTL Counter to create.")]
            EntityAnalysisModelTtlCounterDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelTtlCounter", "Create", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelTtlCounter.Create: entry user={userName} name={model?.Name}");

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("EntityAnalysisModelTtlCounter.Create", permissions);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                        log.Warn($"EntityAnalysisModelTtlCounter.Create: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");

                    throw new DtoValidationException(results);
                }

                var saved = await repository.InsertAsync(EntityAnalysisModelTtlCounterMapper.ToPoco(model), token)
                    .ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                    log.Info(
                        $"EntityAnalysisModelTtlCounter.Create: created Id={saved.Id} name={saved.Name} user={userName}");

                return saved;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (DtoValidationException)
            {
                op.Outcome("invalid");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelTtlCounter.Create: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelTtlCounter.Create: unexpected failure user={userName} name={model?.Name}",
                    ex);
                throw;
            }
        }

        [Description("Updates an existing TTL Counter in the caller's tenant, identified by its Id. Idempotent " +
                     "-- repeating the same update has no further effect beyond incrementing Version.")]
        [ServiceOperation("EntityAnalysisModelTtlCounterUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<TtlCounterPoco> UpdateAsync(
            [Description("The TTL Counter to update. Id selects the row; identity/tenant/audit fields are " +
                         "server-owned and ignored.")]
            EntityAnalysisModelTtlCounterDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelTtlCounter", "Update", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelTtlCounter.Update: entry id={model?.Id} user={userName}");

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("EntityAnalysisModelTtlCounter.Update", permissions);

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                        log.Warn($"EntityAnalysisModelTtlCounter.Update: validation failed id={model.Id} " +
                                 $"user={userName} props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");

                    throw new DtoValidationException(results);
                }

                TtlCounterPoco saved;
                try
                {
                    saved = await repository.UpdateAsync(EntityAnalysisModelTtlCounterMapper.ToPoco(model), token)
                        .ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelTtlCounter.Update: id={model.Id} not found, locked, deleted, or not visible to tenant user={userName}");

                    throw new NotFoundException("The TTL Counter was not found.", ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                    log.Info(
                        $"EntityAnalysisModelTtlCounter.Update: Id={saved.Id} version->{saved.Version} user={userName}");

                return saved;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (DtoValidationException)
            {
                op.Outcome("invalid");
                throw;
            }
            catch (NotFoundException)
            {
                op.Outcome("notfound");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                if (log.IsDebugEnabled)
                    log.Debug($"EntityAnalysisModelTtlCounter.Update: cancelled id={model?.Id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelTtlCounter.Update: unexpected failure id={model?.Id} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Deletes a TTL Counter in the caller's tenant by its Id. Reversible at the data level, but " +
                     "treat as destructive -- the TTL Counter immediately stops accumulating and being " +
                     "recallable by any Activation Rule that references it.")]
        [ServiceOperation("EntityAnalysisModelTtlCounterDelete", OperationKind.Delete, Idempotent = true,
            Destructive = true)]
        public async Task DeleteAsync(
            [Description("Numeric identifier of the TTL Counter to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelTtlCounter", "Delete", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelTtlCounter.Delete: entry id={id} user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelTtlCounter.Delete", permissions);

                try
                {
                    await repository.DeleteAsync(id, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelTtlCounter.Delete: id={id} not found, locked, already deleted, or not visible to tenant user={userName}");

                    throw new NotFoundException("The TTL Counter was not found.", ex);
                }

                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                    log.Info($"EntityAnalysisModelTtlCounter.Delete: soft-deleted Id={id} user={userName}");
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (NotFoundException)
            {
                op.Outcome("notfound");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                if (log.IsDebugEnabled)
                    log.Debug($"EntityAnalysisModelTtlCounter.Delete: cancelled id={id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelTtlCounter.Delete: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        private void EnsurePermitted(string op, int[] requiredSpecifications)
        {
            if (permissionValidation.Validate(requiredSpecifications)) return;

            if (log.IsWarnEnabled)
                log.Warn(
                    $"{op}: permission denied user={userName} specs=[{string.Join(",", requiredSpecifications)}]");

            throw new ForbiddenException(strings[EntityAnalysisModelTtlCounterResources.PermissionDenied],
                requiredSpecifications);
        }
    }
}