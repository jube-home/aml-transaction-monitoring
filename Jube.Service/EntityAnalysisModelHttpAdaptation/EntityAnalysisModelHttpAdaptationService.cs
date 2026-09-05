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
using Jube.Dto.EntityAnalysisModelHttpAdaptation;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.EntityAnalysisModelHttpAdaptation;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.EntityAnalysisModelHttpAdaptation;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.EntityAnalysisModelHttpAdaptation
{
    using HttpAdaptationPoco = Data.Poco.EntityAnalysisModelHttpAdaptation;

    public sealed class EntityAnalysisModelHttpAdaptationService
    {
        private const int MaxListTake = 200;
        private static readonly int[] listPermissions = [15];
        private static readonly int[] readPermissions = [15];
        private static readonly int[] writePermissions = [15];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly EntityAnalysisModelHttpAdaptationRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly EntityAnalysisModelHttpAdaptationDtoValidator validator;

        private EntityAnalysisModelHttpAdaptationService(DbContext dbContext, string userName,
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
            repository = new EntityAnalysisModelHttpAdaptationRepository(dbContext, userName);
            validator = new EntityAnalysisModelHttpAdaptationDtoValidator(repository, strings);
        }

        public static Task<EntityAnalysisModelHttpAdaptationService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelHttpAdaptationService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelHttpAdaptationResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                    log.Warn("EntityAnalysisModelHttpAdaptation.Create: no authenticated user; refusing.");

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelHttpAdaptationResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                    log.Warn(
                        $"EntityAnalysisModelHttpAdaptation.Create: user '{userName}' resolves to no tenant; refusing.");

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelHttpAdaptationResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelHttpAdaptationService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every HTTP Adaptation visible to the calling user's tenant. Unbounded -- intended " +
                     "for the administrative page, not for agent tooling (use the bounded list operation " +
                     "instead).")]
        public async Task<List<EntityAnalysisModelHttpAdaptationDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelHttpAdaptation", "List", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelHttpAdaptation.List: entry user={userName}");

            try
            {
                EnsurePermitted(listPermissions, "EntityAnalysisModelHttpAdaptation.List");
                var dtos = EntityAnalysisModelHttpAdaptationMapper.ToDto(await repository.GetAsync(token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                    log.Debug($"EntityAnalysisModelHttpAdaptation.List: {dtos.Count} rows user={userName}");

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
                    log.Debug($"EntityAnalysisModelHttpAdaptation.List: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelHttpAdaptation.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists HTTP Adaptations belonging to the given Model, ordered by Id, scoped to the " +
                     "calling user's tenant.")]
        [ServiceOperation("EntityAnalysisModelHttpAdaptationGetByEntityAnalysisModelId", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<EntityAnalysisModelHttpAdaptationDto>> GetByEntityAnalysisModelIdAsync(
            [Description("Numeric identifier of the parent Model.")]
            int entityAnalysisModelId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelHttpAdaptation", "ListByEntityAnalysisModelId",
                userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"EntityAnalysisModelHttpAdaptation.ListByEntityAnalysisModelId: entry entityAnalysisModelId={entityAnalysisModelId} user={userName}");

            try
            {
                EnsurePermitted(readPermissions, "EntityAnalysisModelHttpAdaptation.ListByEntityAnalysisModelId");
                var dtos = EntityAnalysisModelHttpAdaptationMapper.ToDto(await repository
                    .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                    log.Debug(
                        $"EntityAnalysisModelHttpAdaptation.ListByEntityAnalysisModelId: {dtos.Count} rows entityAnalysisModelId={entityAnalysisModelId} user={userName}");

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
                        $"EntityAnalysisModelHttpAdaptation.ListByEntityAnalysisModelId: cancelled entityAnalysisModelId={entityAnalysisModelId} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelHttpAdaptation.ListByEntityAnalysisModelId: unexpected failure entityAnalysisModelId={entityAnalysisModelId} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Returns one HTTP Adaptation by its numeric identifier, scoped to the calling user's " +
                     "tenant. Returns null when the row does not exist or is not visible to the caller.")]
        [ServiceOperation("EntityAnalysisModelHttpAdaptationGet", OperationKind.Read, Idempotent = true)]
        public async Task<EntityAnalysisModelHttpAdaptationDto?> GetByIdAsync(
            [Description("Numeric identifier of the HTTP Adaptation.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelHttpAdaptation", "Get", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelHttpAdaptation.Get: entry id={id} user={userName}");

            try
            {
                EnsurePermitted(readPermissions, "EntityAnalysisModelHttpAdaptation.Get");
                var httpAdaptation = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                if (httpAdaptation == null)
                {
                    if (log.IsDebugEnabled)
                        log.Debug(
                            $"EntityAnalysisModelHttpAdaptation.Get: id={id} not found or not visible to tenant user={userName}");

                    return null;
                }

                op.Entity(httpAdaptation.Id);
                return EntityAnalysisModelHttpAdaptationMapper.ToDto(httpAdaptation);
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
                    log.Debug($"EntityAnalysisModelHttpAdaptation.Get: cancelled id={id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelHttpAdaptation.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Lists HTTP Adaptations for the caller's tenant, ordered by id, capped at 'take' rows " +
                     "(max 200). If 'more' is true, call again with 'afterId' set to the last returned Id to " +
                     "continue.")]
        [ServiceOperation("EntityAnalysisModelHttpAdaptationList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<EntityAnalysisModelHttpAdaptationDto>> ListAsync(
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelHttpAdaptation", "ListPaged", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"EntityAnalysisModelHttpAdaptation.ListPaged: entry take={clampedTake} afterId={afterId} user={userName}");

            try
            {
                EnsurePermitted(listPermissions, "EntityAnalysisModelHttpAdaptation.ListPaged");

                var ordered = (await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(o => o.Id)
                    .Where(w => !afterId.HasValue || w.Id > afterId.Value)
                    .ToList();

                var page = ordered.Take(clampedTake).ToList();

                op.Rows(page.Count);

                return new PagedResult<EntityAnalysisModelHttpAdaptationDto>(
                    EntityAnalysisModelHttpAdaptationMapper.ToDto(page));
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
                    log.Debug($"EntityAnalysisModelHttpAdaptation.ListPaged: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelHttpAdaptation.ListPaged: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Registers a new HTTP Adaptation under a Model in the caller's tenant. Not idempotent -- " +
                     "calling twice creates two rows.")]
        [ServiceOperation("EntityAnalysisModelHttpAdaptationCreate", OperationKind.Write, Idempotent = false)]
        public async Task<HttpAdaptationPoco> InsertAsync(
            [Description("The HTTP Adaptation to create.")]
            EntityAnalysisModelHttpAdaptationDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelHttpAdaptation", "Create", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelHttpAdaptation.Create: entry user={userName} name={model?.Name}");

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted(writePermissions, "EntityAnalysisModelHttpAdaptation.Create");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                        log.Warn($"EntityAnalysisModelHttpAdaptation.Create: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");

                    throw new DtoValidationException(results);
                }

                var saved = await repository
                    .InsertAsync(EntityAnalysisModelHttpAdaptationMapper.ToPoco(model), token)
                    .ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                    log.Info(
                        $"EntityAnalysisModelHttpAdaptation.Create: created Id={saved.Id} name={saved.Name} " +
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
                    log.Debug($"EntityAnalysisModelHttpAdaptation.Create: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelHttpAdaptation.Create: unexpected failure user={userName} " +
                          $"name={model?.Name}", ex);
                throw;
            }
        }

        [Description("Updates an existing HTTP Adaptation in the caller's tenant, identified by its Id. " +
                     "Idempotent -- repeating the same update has no further effect beyond incrementing Version.")]
        [ServiceOperation("EntityAnalysisModelHttpAdaptationUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<HttpAdaptationPoco> UpdateAsync(
            [Description("The HTTP Adaptation to update. Id selects the row; identity/tenant/audit fields are " +
                         "server-owned and ignored.")]
            EntityAnalysisModelHttpAdaptationDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelHttpAdaptation", "Update", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelHttpAdaptation.Update: entry id={model?.Id} user={userName}");

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted(writePermissions, "EntityAnalysisModelHttpAdaptation.Update");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                        log.Warn($"EntityAnalysisModelHttpAdaptation.Update: validation failed id={model.Id} " +
                                 $"user={userName} props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");

                    throw new DtoValidationException(results);
                }

                HttpAdaptationPoco saved;
                try
                {
                    saved = await repository
                        .UpdateAsync(EntityAnalysisModelHttpAdaptationMapper.ToPoco(model), token)
                        .ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelHttpAdaptation.Update: id={model.Id} not found, locked, deleted, or not visible to tenant user={userName}");

                    throw new NotFoundException("The HTTP Adaptation was not found.", ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                    log.Info(
                        $"EntityAnalysisModelHttpAdaptation.Update: Id={saved.Id} version->{saved.Version} user={userName}");

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
                    log.Debug($"EntityAnalysisModelHttpAdaptation.Update: cancelled id={model?.Id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelHttpAdaptation.Update: unexpected failure id={model?.Id} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Deletes an HTTP Adaptation in the caller's tenant by its Id. Reversible at the data " +
                     "level, but treat as destructive -- the HTTP Adaptation immediately stops being recalled " +
                     "on transaction invocation.")]
        [ServiceOperation("EntityAnalysisModelHttpAdaptationDelete", OperationKind.Delete, Idempotent = true,
            Destructive = true)]
        public async Task DeleteAsync(
            [Description("Numeric identifier of the HTTP Adaptation to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelHttpAdaptation", "Delete", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelHttpAdaptation.Delete: entry id={id} user={userName}");

            try
            {
                EnsurePermitted(writePermissions, "EntityAnalysisModelHttpAdaptation.Delete");

                try
                {
                    await repository.DeleteAsync(id, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelHttpAdaptation.Delete: id={id} not found, locked, already deleted, or not visible to tenant user={userName}");

                    throw new NotFoundException("The HTTP Adaptation was not found.", ex);
                }

                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                    log.Info($"EntityAnalysisModelHttpAdaptation.Delete: soft-deleted Id={id} user={userName}");
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
                    log.Debug($"EntityAnalysisModelHttpAdaptation.Delete: cancelled id={id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelHttpAdaptation.Delete: unexpected failure id={id} user={userName}",
                    ex);
                throw;
            }
        }

        private void EnsurePermitted(int[] specs, string op)
        {
            if (permissionValidation.Validate(specs)) return;

            if (log.IsWarnEnabled)
                log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", specs)}]");

            throw new ForbiddenException(strings[EntityAnalysisModelHttpAdaptationResources.PermissionDenied],
                specs);
        }
    }
}