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
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.EntityAnalysisModelRequestXPath;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.EntityAnalysisModelRequestXPath;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.EntityAnalysisModelRequestXPath;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.EntityAnalysisModelRequestXPath
{
    using RequestXPathPoco = EntityAnalysisModelRequestXpath;
    using RequestXPathRepository = EntityAnalysisModelRequestXPathRepository;

    public sealed class EntityAnalysisModelRequestXPathService
    {
        private const int MaxListTake = 200;
        private static readonly int[] listPermissions = [7];
        private static readonly int[] readPermissions = [7];
        private static readonly int[] readByEntityAnalysisModelPermissions = [7, 13];
        private static readonly int[] readByDataTypePermissions = [7, 12];
        private static readonly int[] writePermissions = [7];
        private static readonly int[] suppressionPermissions = [2];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly RequestXPathRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly EntityAnalysisModelRequestXPathDtoValidator validator;

        private EntityAnalysisModelRequestXPathService(DbContext dbContext, string userName, int tenantRegistryId,
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
            repository = new RequestXPathRepository(dbContext, userName);
            validator = new EntityAnalysisModelRequestXPathDtoValidator(repository, strings);
        }

        public static Task<EntityAnalysisModelRequestXPathService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelRequestXPathService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelRequestXPathResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                    log.Warn("EntityAnalysisModelRequestXPath.Create: no authenticated user; refusing.");

                throw new NotAuthenticatedException(strings[EntityAnalysisModelRequestXPathResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                    log.Warn(
                        $"EntityAnalysisModelRequestXPath.Create: user '{userName}' resolves to no tenant; refusing.");

                throw new NotAuthenticatedException(strings[EntityAnalysisModelRequestXPathResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelRequestXPathService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every Request XPath visible to the calling user's tenant. Unbounded -- intended for " +
                     "the administrative page, not for agent tooling (use the bounded list operation instead).")]
        public async Task<List<EntityAnalysisModelRequestXPathDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelRequestXPath", "List", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelRequestXPath.List: entry user={userName}");

            try
            {
                EnsurePermitted(listPermissions, "EntityAnalysisModelRequestXPath.List");
                var dtos = EntityAnalysisModelRequestXPathMapper.ToDto(await repository.GetAsync(token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                    log.Debug($"EntityAnalysisModelRequestXPath.List: {dtos.Count} rows user={userName}");

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
                if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelRequestXPath.List: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelRequestXPath.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Request XPaths belonging to the given Model, ordered by Id, scoped to the calling " +
                     "user's tenant.")]
        [ServiceOperation("EntityAnalysisModelRequestXPathGetByEntityAnalysisModelId", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<EntityAnalysisModelRequestXPathDto>> GetByEntityAnalysisModelIdAsync(
            [Description("Numeric identifier of the parent Model.")]
            int entityAnalysisModelId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelRequestXPath", "ListByEntityAnalysisModelId",
                userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"EntityAnalysisModelRequestXPath.ListByEntityAnalysisModelId: entry entityAnalysisModelId={entityAnalysisModelId} user={userName}");

            try
            {
                EnsurePermitted(readByEntityAnalysisModelPermissions,
                    "EntityAnalysisModelRequestXPath.ListByEntityAnalysisModelId");
                var dtos = EntityAnalysisModelRequestXPathMapper.ToDto(await repository
                    .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                    log.Debug(
                        $"EntityAnalysisModelRequestXPath.ListByEntityAnalysisModelId: {dtos.Count} rows entityAnalysisModelId={entityAnalysisModelId} user={userName}");

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
                        $"EntityAnalysisModelRequestXPath.ListByEntityAnalysisModelId: cancelled entityAnalysisModelId={entityAnalysisModelId} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelRequestXPath.ListByEntityAnalysisModelId: unexpected failure entityAnalysisModelId={entityAnalysisModelId} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Lists Request XPaths reachable from the given Case Workflow's Model, scoped to the calling " +
                     "user's tenant.")]
        [ServiceOperation("EntityAnalysisModelRequestXPathGetByCasesWorkflowId", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<EntityAnalysisModelRequestXPathDto>> GetByCasesWorkflowIdAsync(
            [Description("Numeric identifier of the Case Workflow.")]
            int casesWorkflowId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelRequestXPath", "ListByCasesWorkflowId", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"EntityAnalysisModelRequestXPath.ListByCasesWorkflowId: entry casesWorkflowId={casesWorkflowId} user={userName}");

            try
            {
                EnsurePermitted(readPermissions, "EntityAnalysisModelRequestXPath.ListByCasesWorkflowId");
                var dtos = EntityAnalysisModelRequestXPathMapper.ToDto(await repository
                    .GetByCasesWorkflowIdAsync(casesWorkflowId, token).ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                    log.Debug(
                        $"EntityAnalysisModelRequestXPath.ListByCasesWorkflowId: {dtos.Count} rows casesWorkflowId={casesWorkflowId} user={userName}");

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
                        $"EntityAnalysisModelRequestXPath.ListByCasesWorkflowId: cancelled casesWorkflowId={casesWorkflowId} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelRequestXPath.ListByCasesWorkflowId: unexpected failure casesWorkflowId={casesWorkflowId} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Lists Request XPaths flagged for Suppression, scoped to the calling user's tenant.")]
        [ServiceOperation("EntityAnalysisModelRequestXPathGetBySuppressionKey", OperationKind.Read, Idempotent = true)]
        public async Task<List<EntityAnalysisModelRequestXPathDto>> GetBySuppressionKeyAsync(
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelRequestXPath", "ListBySuppressionKey", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelRequestXPath.ListBySuppressionKey: entry user={userName}");

            try
            {
                EnsurePermitted(suppressionPermissions, "EntityAnalysisModelRequestXPath.ListBySuppressionKey");
                var dtos = EntityAnalysisModelRequestXPathMapper.ToDto(await repository.GetBySuppressionKeysAsync(token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                    log.Debug(
                        $"EntityAnalysisModelRequestXPath.ListBySuppressionKey: {dtos.Count} rows user={userName}");

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
                    log.Debug($"EntityAnalysisModelRequestXPath.ListBySuppressionKey: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelRequestXPath.ListBySuppressionKey: unexpected failure user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Lists String, Integer or Float Request XPaths belonging to the given Model, scoped to the " +
                     "calling user's tenant. The dataTypeId parameter is accepted for HTTP contract parity with the " +
                     "legacy endpoint but is not used -- the result is always filtered to String, Integer and " +
                     "Float (data type ids 1, 2 and 3).")]
        [ServiceOperation("EntityAnalysisModelRequestXPathGetByEntityAnalysisModelIdByDataType", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<EntityAnalysisModelRequestXPathDto>> GetByEntityAnalysisModelIdByDataTypeAsync(
            [Description("Numeric identifier of the parent Model.")]
            int entityAnalysisModelId,
            [Description("Accepted for HTTP contract parity with the legacy endpoint; not used by this operation.")]
            int dataTypeId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelRequestXPath",
                "ListByEntityAnalysisModelIdByDataType", userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"EntityAnalysisModelRequestXPath.ListByEntityAnalysisModelIdByDataType: entry entityAnalysisModelId={entityAnalysisModelId} user={userName}");

            try
            {
                EnsurePermitted(readByDataTypePermissions,
                    "EntityAnalysisModelRequestXPath.ListByEntityAnalysisModelIdByDataType");
                var dtos = EntityAnalysisModelRequestXPathMapper.ToDto(await repository
                    .GetByEntityAnalysisModelIdByDataTypeAsync(entityAnalysisModelId, token, 1, 2, 3)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                    log.Debug(
                        $"EntityAnalysisModelRequestXPath.ListByEntityAnalysisModelIdByDataType: {dtos.Count} rows entityAnalysisModelId={entityAnalysisModelId} user={userName}");

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
                        $"EntityAnalysisModelRequestXPath.ListByEntityAnalysisModelIdByDataType: cancelled entityAnalysisModelId={entityAnalysisModelId} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelRequestXPath.ListByEntityAnalysisModelIdByDataType: unexpected failure entityAnalysisModelId={entityAnalysisModelId} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Returns one Request XPath by its numeric identifier, scoped to the calling user's tenant. " +
                     "Returns null when the row does not exist or is not visible to the caller.")]
        [ServiceOperation("EntityAnalysisModelRequestXPathGet", OperationKind.Read, Idempotent = true)]
        public async Task<EntityAnalysisModelRequestXPathDto?> GetByIdAsync(
            [Description("Numeric identifier of the Request XPath.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelRequestXPath", "Get", userName, tenantRegistryId,
                auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelRequestXPath.Get: entry id={id} user={userName}");

            try
            {
                EnsurePermitted(readPermissions, "EntityAnalysisModelRequestXPath.Get");
                var requestXPath = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                if (requestXPath == null)
                {
                    if (log.IsDebugEnabled)
                        log.Debug(
                            $"EntityAnalysisModelRequestXPath.Get: id={id} not found or not visible to tenant user={userName}");

                    return null;
                }

                op.Entity(requestXPath.Id);
                return EntityAnalysisModelRequestXPathMapper.ToDto(requestXPath);
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
                    log.Debug($"EntityAnalysisModelRequestXPath.Get: cancelled id={id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelRequestXPath.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Request XPaths for the caller's tenant, ordered by id, capped at 'take' rows " +
                     "(max 200). If 'more' is true, call again with 'afterId' set to the last returned Id to " +
                     "continue.")]
        [ServiceOperation("EntityAnalysisModelRequestXPathList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<EntityAnalysisModelRequestXPathDto>> ListAsync(
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelRequestXPath", "ListPaged", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"EntityAnalysisModelRequestXPath.ListPaged: entry take={clampedTake} afterId={afterId} user={userName}");

            try
            {
                EnsurePermitted(listPermissions, "EntityAnalysisModelRequestXPath.ListPaged");

                var ordered = (await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(o => o.Id)
                    .Where(w => !afterId.HasValue || w.Id > afterId.Value)
                    .ToList();

                var page = ordered.Take(clampedTake).ToList();

                op.Rows(page.Count);

                return new PagedResult<EntityAnalysisModelRequestXPathDto>(
                    EntityAnalysisModelRequestXPathMapper.ToDto(page));
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
                    log.Debug($"EntityAnalysisModelRequestXPath.ListPaged: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelRequestXPath.ListPaged: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Creates a new Request XPath under a Model in the caller's tenant. Not idempotent -- calling " +
                     "twice creates two rows.")]
        [ServiceOperation("EntityAnalysisModelRequestXPathCreate", OperationKind.Write, Idempotent = false)]
        public async Task<RequestXPathPoco> InsertAsync(
            [Description("The Request XPath to create.")]
            EntityAnalysisModelRequestXPathDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelRequestXPath", "Create", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelRequestXPath.Create: entry user={userName} name={model?.Name}");

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted(writePermissions, "EntityAnalysisModelRequestXPath.Create");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                        log.Warn($"EntityAnalysisModelRequestXPath.Create: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");

                    throw new DtoValidationException(results);
                }

                var saved = await repository
                    .InsertIncrementCacheIndexIdAsync(EntityAnalysisModelRequestXPathMapper.ToPoco(model), token)
                    .ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                    log.Info($"EntityAnalysisModelRequestXPath.Create: created Id={saved.Id} name={saved.Name} " +
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
                if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelRequestXPath.Create: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelRequestXPath.Create: unexpected failure user={userName} " +
                          $"name={model?.Name}", ex);
                throw;
            }
        }

        [Description("Updates an existing Request XPath in the caller's tenant, identified by its Id. Idempotent " +
                     "-- repeating the same update has no further effect beyond incrementing Version.")]
        [ServiceOperation("EntityAnalysisModelRequestXPathUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<RequestXPathPoco> UpdateAsync(
            [Description(
                "The Request XPath to update. Id selects the row; identity/tenant/audit fields are server-owned " +
                "and ignored.")]
            EntityAnalysisModelRequestXPathDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelRequestXPath", "Update", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelRequestXPath.Update: entry id={model?.Id} user={userName}");

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted(writePermissions, "EntityAnalysisModelRequestXPath.Update");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                        log.Warn($"EntityAnalysisModelRequestXPath.Update: validation failed id={model.Id} " +
                                 $"user={userName} props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");

                    throw new DtoValidationException(results);
                }

                RequestXPathPoco saved;
                try
                {
                    saved = await repository.UpdateAsync(EntityAnalysisModelRequestXPathMapper.ToPoco(model), token)
                        .ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelRequestXPath.Update: id={model.Id} not found, locked, deleted, or not visible to tenant user={userName}");

                    throw new NotFoundException("The Request XPath was not found.", ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                    log.Info(
                        $"EntityAnalysisModelRequestXPath.Update: Id={saved.Id} version->{saved.Version} user={userName}");

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
                    log.Debug($"EntityAnalysisModelRequestXPath.Update: cancelled id={model?.Id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelRequestXPath.Update: unexpected failure id={model?.Id} user={userName}", ex);
                throw;
            }
        }

        [Description("Deletes a Request XPath in the caller's tenant by its Id. Reversible at the data level, but " +
                     "treat as destructive -- the field immediately stops being extracted via the API.")]
        [ServiceOperation("EntityAnalysisModelRequestXPathDelete", OperationKind.Delete, Idempotent = true,
            Destructive = true)]
        public async Task DeleteAsync(
            [Description("Numeric identifier of the Request XPath to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelRequestXPath", "Delete", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelRequestXPath.Delete: entry id={id} user={userName}");

            try
            {
                EnsurePermitted(writePermissions, "EntityAnalysisModelRequestXPath.Delete");

                try
                {
                    await repository.DeleteAsync(id, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelRequestXPath.Delete: id={id} not found, locked, already deleted, or not visible to tenant user={userName}");

                    throw new NotFoundException("The Request XPath was not found.", ex);
                }

                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                    log.Info($"EntityAnalysisModelRequestXPath.Delete: soft-deleted Id={id} user={userName}");
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
                    log.Debug($"EntityAnalysisModelRequestXPath.Delete: cancelled id={id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelRequestXPath.Delete: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        private void EnsurePermitted(int[] specs, string op)
        {
            if (permissionValidation.Validate(specs)) return;

            if (log.IsWarnEnabled)
                log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", specs)}]");

            throw new ForbiddenException(strings[EntityAnalysisModelRequestXPathResources.PermissionDenied], specs);
        }
    }
}