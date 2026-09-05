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
using Jube.Dto.ExhaustiveSearchInstance;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.ExhaustiveSearchInstance;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.ExhaustiveSearchInstance;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.ExhaustiveSearchInstance
{
    using ExhaustiveSearchInstancePoco = Data.Poco.ExhaustiveSearchInstance;

    public sealed class ExhaustiveSearchInstanceService
    {
        private const int MaxListTake = 200;
        private static readonly int[] listPermissions = [16];
        private static readonly int[] readPermissions = [16];
        private static readonly int[] writePermissions = [16];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly ExhaustiveSearchInstanceRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly ExhaustiveSearchInstanceDtoValidator validator;

        private ExhaustiveSearchInstanceService(DbContext dbContext, string userName,
            int tenantRegistryId, PermissionValidation permissionValidation, ILog log, ILog auditLog,
            IServiceChangeBus serviceChangeBus, IStringLocalizer strings)
        {
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            repository = new ExhaustiveSearchInstanceRepository(dbContext, userName);
            validator = new ExhaustiveSearchInstanceDtoValidator(repository, strings);
        }

        public static Task<ExhaustiveSearchInstanceService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<ExhaustiveSearchInstanceService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(ExhaustiveSearchInstanceResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                    log.Warn("ExhaustiveSearchInstance.Create: no authenticated user; refusing.");

                throw new NotAuthenticatedException(strings[ExhaustiveSearchInstanceResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                    log.Warn($"ExhaustiveSearchInstance.Create: user '{userName}' resolves to no tenant; refusing.");

                throw new NotAuthenticatedException(strings[ExhaustiveSearchInstanceResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new ExhaustiveSearchInstanceService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every Exhaustive Adaptation visible to the calling user's tenant. Unbounded -- " +
                     "intended for the administrative page, not for agent tooling (use the bounded list " +
                     "operation instead).")]
        public async Task<List<ExhaustiveSearchInstanceDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("ExhaustiveSearchInstance", "List", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled) log.Debug($"ExhaustiveSearchInstance.List: entry user={userName}");

            try
            {
                EnsurePermitted(listPermissions, "ExhaustiveSearchInstance.List");
                var dtos = ExhaustiveSearchInstanceMapper.ToDto(await repository.GetAsync(token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                    log.Debug($"ExhaustiveSearchInstance.List: {dtos.Count} rows user={userName}");

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
                    log.Debug($"ExhaustiveSearchInstance.List: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"ExhaustiveSearchInstance.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Exhaustive Adaptations belonging to the given Model, ordered by Id, scoped to the " +
                     "calling user's tenant.")]
        [ServiceOperation("ExhaustiveSearchInstanceGetByEntityAnalysisModelId", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<ExhaustiveSearchInstanceDto>> GetByEntityAnalysisModelIdAsync(
            [Description("Numeric identifier of the parent Model.")]
            int entityAnalysisModelId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("ExhaustiveSearchInstance", "ListByEntityAnalysisModelId",
                userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"ExhaustiveSearchInstance.ListByEntityAnalysisModelId: entry entityAnalysisModelId={entityAnalysisModelId} user={userName}");

            try
            {
                EnsurePermitted(readPermissions, "ExhaustiveSearchInstance.ListByEntityAnalysisModelId");
                var dtos = ExhaustiveSearchInstanceMapper.ToDto(await repository
                    .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                    log.Debug(
                        $"ExhaustiveSearchInstance.ListByEntityAnalysisModelId: {dtos.Count} rows entityAnalysisModelId={entityAnalysisModelId} user={userName}");

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
                        $"ExhaustiveSearchInstance.ListByEntityAnalysisModelId: cancelled entityAnalysisModelId={entityAnalysisModelId} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"ExhaustiveSearchInstance.ListByEntityAnalysisModelId: unexpected failure entityAnalysisModelId={entityAnalysisModelId} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Returns one Exhaustive Adaptation by its numeric identifier, scoped to the calling " +
                     "user's tenant. Returns null when the row does not exist or is not visible to the caller.")]
        [ServiceOperation("ExhaustiveSearchInstanceGet", OperationKind.Read, Idempotent = true)]
        public async Task<ExhaustiveSearchInstanceDto?> GetByIdAsync(
            [Description("Numeric identifier of the Exhaustive Adaptation.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("ExhaustiveSearchInstance", "Get", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"ExhaustiveSearchInstance.Get: entry id={id} user={userName}");

            try
            {
                EnsurePermitted(readPermissions, "ExhaustiveSearchInstance.Get");
                var exhaustiveSearchInstance = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                if (exhaustiveSearchInstance == null)
                {
                    if (log.IsDebugEnabled)
                        log.Debug(
                            $"ExhaustiveSearchInstance.Get: id={id} not found or not visible to tenant user={userName}");

                    return null;
                }

                op.Entity(exhaustiveSearchInstance.Id);
                return ExhaustiveSearchInstanceMapper.ToDto(exhaustiveSearchInstance);
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
                    log.Debug($"ExhaustiveSearchInstance.Get: cancelled id={id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"ExhaustiveSearchInstance.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Exhaustive Adaptations for the caller's tenant, ordered by id, capped at 'take' " +
                     "rows (max 200). If 'more' is true, call again with 'afterId' set to the last returned Id " +
                     "to continue.")]
        [ServiceOperation("ExhaustiveSearchInstanceList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<ExhaustiveSearchInstanceDto>> ListAsync(
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("ExhaustiveSearchInstance", "ListPaged", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"ExhaustiveSearchInstance.ListPaged: entry take={clampedTake} afterId={afterId} user={userName}");

            try
            {
                EnsurePermitted(listPermissions, "ExhaustiveSearchInstance.ListPaged");

                var ordered = (await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(o => o.Id)
                    .Where(w => !afterId.HasValue || w.Id > afterId.Value)
                    .ToList();

                var page = ordered.Take(clampedTake).ToList();

                op.Rows(page.Count);

                return new PagedResult<ExhaustiveSearchInstanceDto>(ExhaustiveSearchInstanceMapper.ToDto(page));
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
                    log.Debug($"ExhaustiveSearchInstance.ListPaged: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"ExhaustiveSearchInstance.ListPaged: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Registers a new Exhaustive Adaptation under a Model in the caller's tenant, initially in " +
                     "status Awaiting Server, ready to be picked up for training by the background engine. Not " +
                     "idempotent -- calling twice creates two rows.")]
        [ServiceOperation("ExhaustiveSearchInstanceCreate", OperationKind.Write, Idempotent = false)]
        public async Task<ExhaustiveSearchInstancePoco> InsertAsync(
            [Description("The Exhaustive Adaptation to create.")]
            ExhaustiveSearchInstanceDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("ExhaustiveSearchInstance", "Create", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"ExhaustiveSearchInstance.Create: entry user={userName} name={model?.Name}");

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted(writePermissions, "ExhaustiveSearchInstance.Create");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                        log.Warn($"ExhaustiveSearchInstance.Create: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");

                    throw new DtoValidationException(results);
                }

                var saved = await repository
                    .InsertAsync(ExhaustiveSearchInstanceMapper.ToPoco(model), token)
                    .ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                    log.Info(
                        $"ExhaustiveSearchInstance.Create: created Id={saved.Id} name={saved.Name} " +
                        $"user={userName}");

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
                if (log.IsDebugEnabled)
                    log.Debug($"ExhaustiveSearchInstance.Create: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"ExhaustiveSearchInstance.Create: unexpected failure user={userName} " +
                          $"name={model?.Name}", ex);
                throw;
            }
        }

        [Description("Updates an existing Exhaustive Adaptation in the caller's tenant, identified by its Id. " +
                     "Only possible while its training StatusId is still 'Awaiting Server' (0) -- once picked " +
                     "up for training the row can no longer be updated. Idempotent -- repeating the same " +
                     "update has no further effect beyond incrementing Version.")]
        [ServiceOperation("ExhaustiveSearchInstanceUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<ExhaustiveSearchInstancePoco> UpdateAsync(
            [Description("The Exhaustive Adaptation to update. Id selects the row; identity/tenant/audit/" +
                         "training-status fields are server-owned and ignored.")]
            ExhaustiveSearchInstanceDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("ExhaustiveSearchInstance", "Update", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"ExhaustiveSearchInstance.Update: entry id={model?.Id} user={userName}");

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted(writePermissions, "ExhaustiveSearchInstance.Update");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                        log.Warn($"ExhaustiveSearchInstance.Update: validation failed id={model.Id} " +
                                 $"user={userName} props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");

                    throw new DtoValidationException(results);
                }

                ExhaustiveSearchInstancePoco saved;
                try
                {
                    saved = await repository
                        .UpdateAsync(ExhaustiveSearchInstanceMapper.ToPoco(model), token)
                        .ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"ExhaustiveSearchInstance.Update: id={model.Id} not found, locked, deleted, already " +
                            $"picked up for training, or not visible to tenant user={userName}");

                    throw new NotFoundException("The Exhaustive Adaptation was not found.", ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                    log.Info(
                        $"ExhaustiveSearchInstance.Update: Id={saved.Id} version->{saved.Version} user={userName}");

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
                    log.Debug($"ExhaustiveSearchInstance.Update: cancelled id={model?.Id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"ExhaustiveSearchInstance.Update: unexpected failure id={model?.Id} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Requests that an in-progress or queued training run for this Exhaustive Adaptation stop " +
                     "at the next safe checkpoint, identified by its Guid. A no-op (still returns success) when " +
                     "the Guid does not resolve to a visible, not-yet-stopped row -- see the migration report.")]
        [ServiceOperation("ExhaustiveSearchInstanceStop", OperationKind.Write, Idempotent = true)]
        public async Task StopAsync(
            [Description("Guid of the Exhaustive Adaptation to stop.")]
            Guid guid,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("ExhaustiveSearchInstance", "Stop", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"ExhaustiveSearchInstance.Stop: entry guid={guid} user={userName}");

            try
            {
                EnsurePermitted(writePermissions, "ExhaustiveSearchInstance.Stop");

                await repository.StopAsync(guid, token).ConfigureAwait(false);

                op.Updated();

                if (log.IsInfoEnabled)
                    log.Info($"ExhaustiveSearchInstance.Stop: requested stop guid={guid} user={userName}");
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
                    log.Debug($"ExhaustiveSearchInstance.Stop: cancelled guid={guid} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"ExhaustiveSearchInstance.Stop: unexpected failure guid={guid} user={userName}", ex);
                throw;
            }
        }

        [Description("Deletes an Exhaustive Adaptation in the caller's tenant by its Id. Reversible at the " +
                     "data level, but treat as destructive -- the Exhaustive Adaptation immediately stops " +
                     "being recalled on transaction invocation.")]
        [ServiceOperation("ExhaustiveSearchInstanceDelete", OperationKind.Delete, Idempotent = true,
            Destructive = true)]
        public async Task DeleteAsync(
            [Description("Numeric identifier of the Exhaustive Adaptation to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("ExhaustiveSearchInstance", "Delete", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"ExhaustiveSearchInstance.Delete: entry id={id} user={userName}");

            try
            {
                EnsurePermitted(writePermissions, "ExhaustiveSearchInstance.Delete");

                try
                {
                    await repository.DeleteAsync(id, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"ExhaustiveSearchInstance.Delete: id={id} not found, locked, already deleted, or not visible to tenant user={userName}");

                    throw new NotFoundException("The Exhaustive Adaptation was not found.", ex);
                }

                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                    log.Info($"ExhaustiveSearchInstance.Delete: soft-deleted Id={id} user={userName}");
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
                    log.Debug($"ExhaustiveSearchInstance.Delete: cancelled id={id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"ExhaustiveSearchInstance.Delete: unexpected failure id={id} user={userName}",
                    ex);
                throw;
            }
        }

        private void EnsurePermitted(int[] specs, string op)
        {
            if (permissionValidation.Validate(specs)) return;

            if (log.IsWarnEnabled)
                log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", specs)}]");

            throw new ForbiddenException(strings[ExhaustiveSearchInstanceResources.PermissionDenied], specs);
        }
    }
}
