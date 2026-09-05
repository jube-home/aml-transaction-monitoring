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
using Jube.Dto.EntityAnalysisModelTag;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.EntityAnalysisModelTag;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.EntityAnalysisModelTag;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.EntityAnalysisModelTag
{
    using TagPoco = Data.Poco.EntityAnalysisModelTag;

    public sealed class EntityAnalysisModelTagService
    {
        private const int MaxListTake = 200;
        private static readonly int[] permissions = [37];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly EntityAnalysisModelTagRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly EntityAnalysisModelTagDtoValidator validator;

        private EntityAnalysisModelTagService(DbContext dbContext, string userName, int tenantRegistryId,
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
            repository = new EntityAnalysisModelTagRepository(dbContext, userName);
            validator = new EntityAnalysisModelTagDtoValidator(repository, strings);
        }

        public static Task<EntityAnalysisModelTagService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelTagService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelTagResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                    log.Warn("EntityAnalysisModelTag.Create: no authenticated user; refusing.");

                throw new NotAuthenticatedException(strings[EntityAnalysisModelTagResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                    log.Warn($"EntityAnalysisModelTag.Create: user '{userName}' resolves to no tenant; refusing.");

                throw new NotAuthenticatedException(strings[EntityAnalysisModelTagResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelTagService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every Tag visible to the calling user's tenant. Unbounded -- intended for the " +
                     "administrative page, not for agent tooling (use the bounded list operation instead).")]
        public async Task<List<EntityAnalysisModelTagDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelTag", "List", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelTag.List: entry user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelTag.List");
                var dtos = EntityAnalysisModelTagMapper.ToDto(await repository.GetAsync(token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelTag.List: {dtos.Count} rows user={userName}");

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
                if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelTag.List: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelTag.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists the Tags belonging to the given Model that are Active, ordered by Name, scoped to " +
                     "the calling user's tenant. Used to populate Tag pickers (e.g. Case tagging).")]
        [ServiceOperation("EntityAnalysisModelTagGetByEntityAnalysisModelId", OperationKind.Read, Idempotent = true)]
        public async Task<List<EntityAnalysisModelTagDto>> GetByEntityAnalysisModelIdAsync(
            [Description("Numeric identifier of the parent Model.")]
            int entityAnalysisModelId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelTag", "ListByEntityAnalysisModelId",
                userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"EntityAnalysisModelTag.ListByEntityAnalysisModelId: entry entityAnalysisModelId={entityAnalysisModelId} user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelTag.ListByEntityAnalysisModelId");
                var dtos = EntityAnalysisModelTagMapper.ToDto(await repository
                    .GetByEntityAnalysisModelIdOrderByNameActiveOnlyAsync(entityAnalysisModelId, token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                    log.Debug(
                        $"EntityAnalysisModelTag.ListByEntityAnalysisModelId: {dtos.Count} rows entityAnalysisModelId={entityAnalysisModelId} user={userName}");

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
                        $"EntityAnalysisModelTag.ListByEntityAnalysisModelId: cancelled entityAnalysisModelId={entityAnalysisModelId} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelTag.ListByEntityAnalysisModelId: unexpected failure entityAnalysisModelId={entityAnalysisModelId} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Returns one Tag by its numeric identifier, scoped to the calling user's tenant. Returns " +
                     "null when the row does not exist or is not visible to the caller.")]
        [ServiceOperation("EntityAnalysisModelTagGet", OperationKind.Read, Idempotent = true)]
        public async Task<EntityAnalysisModelTagDto?> GetByIdAsync(
            [Description("Numeric identifier of the Tag.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelTag", "Get", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelTag.Get: entry id={id} user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelTag.Get");
                var tag = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                if (tag == null)
                {
                    if (log.IsDebugEnabled)
                        log.Debug(
                            $"EntityAnalysisModelTag.Get: id={id} not found or not visible to tenant user={userName}");

                    return null;
                }

                op.Entity(tag.Id);
                return EntityAnalysisModelTagMapper.ToDto(tag);
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelTag.Get: cancelled id={id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelTag.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Tags for the caller's tenant, ordered by id, capped at 'take' rows (max 200). If " +
                     "'more' is true, call again with 'afterId' set to the last returned Id to continue.")]
        [ServiceOperation("EntityAnalysisModelTagList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<EntityAnalysisModelTagDto>> ListAsync(
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelTag", "ListPaged", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"EntityAnalysisModelTag.ListPaged: entry take={clampedTake} afterId={afterId} user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelTag.ListPaged");

                var ordered = (await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(o => o.Id)
                    .Where(w => !afterId.HasValue || w.Id > afterId.Value)
                    .ToList();

                var page = ordered.Take(clampedTake).ToList();

                op.Rows(page.Count);

                return new PagedResult<EntityAnalysisModelTagDto>(EntityAnalysisModelTagMapper.ToDto(page));
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelTag.ListPaged: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelTag.ListPaged: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Registers a new Tag under a Model in the caller's tenant. Not idempotent -- calling twice " +
                     "creates two rows.")]
        [ServiceOperation("EntityAnalysisModelTagCreate", OperationKind.Write, Idempotent = false)]
        public async Task<TagPoco> InsertAsync(
            [Description("The Tag to create.")] EntityAnalysisModelTagDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelTag", "Create", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelTag.Create: entry user={userName} name={model?.Name}");

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("EntityAnalysisModelTag.Create");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                        log.Warn($"EntityAnalysisModelTag.Create: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");

                    throw new DtoValidationException(results);
                }

                var saved = await repository.InsertAsync(EntityAnalysisModelTagMapper.ToPoco(model), token)
                    .ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                    log.Info($"EntityAnalysisModelTag.Create: created Id={saved.Id} name={saved.Name} user={userName}");

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
                if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelTag.Create: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelTag.Create: unexpected failure user={userName} name={model?.Name}",
                    ex);
                throw;
            }
        }

        [Description("Updates an existing Tag in the caller's tenant, identified by its Id. Idempotent -- " +
                     "repeating the same update has no further effect beyond incrementing Version.")]
        [ServiceOperation("EntityAnalysisModelTagUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<TagPoco> UpdateAsync(
            [Description("The Tag to update. Id selects the row; identity/tenant/audit fields are server-owned " +
                         "and ignored.")]
            EntityAnalysisModelTagDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelTag", "Update", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelTag.Update: entry id={model?.Id} user={userName}");

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted("EntityAnalysisModelTag.Update");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                        log.Warn($"EntityAnalysisModelTag.Update: validation failed id={model.Id} " +
                                 $"user={userName} props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");

                    throw new DtoValidationException(results);
                }

                TagPoco saved;
                try
                {
                    saved = await repository.UpdateAsync(EntityAnalysisModelTagMapper.ToPoco(model), token)
                        .ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelTag.Update: id={model.Id} not found, locked, deleted, or not visible to tenant user={userName}");

                    throw new NotFoundException("The Tag was not found.", ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                    log.Info($"EntityAnalysisModelTag.Update: Id={saved.Id} version->{saved.Version} user={userName}");

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
                    log.Debug($"EntityAnalysisModelTag.Update: cancelled id={model?.Id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelTag.Update: unexpected failure id={model?.Id} user={userName}", ex);
                throw;
            }
        }

        [Description("Deletes a Tag in the caller's tenant by its Id. Reversible at the data level, but treat " +
                     "as destructive -- the Tag immediately stops being eligible for merge into the Archive " +
                     "payload.")]
        [ServiceOperation("EntityAnalysisModelTagDelete", OperationKind.Delete, Idempotent = true,
            Destructive = true)]
        public async Task DeleteAsync(
            [Description("Numeric identifier of the Tag to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelTag", "Delete", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelTag.Delete: entry id={id} user={userName}");

            try
            {
                EnsurePermitted("EntityAnalysisModelTag.Delete");

                try
                {
                    await repository.DeleteAsync(id, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelTag.Delete: id={id} not found, locked, already deleted, or not visible to tenant user={userName}");

                    throw new NotFoundException("The Tag was not found.", ex);
                }

                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                    log.Info($"EntityAnalysisModelTag.Delete: soft-deleted Id={id} user={userName}");
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
                if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelTag.Delete: cancelled id={id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelTag.Delete: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        private void EnsurePermitted(string op)
        {
            if (permissionValidation.Validate(permissions)) return;

            if (log.IsWarnEnabled)
                log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", permissions)}]");

            throw new ForbiddenException(strings[EntityAnalysisModelTagResources.PermissionDenied], permissions);
        }
    }
}