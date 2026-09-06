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
using Jube.Dto.EntityAnalysisModelActivationRule;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.EntityAnalysisModelActivationRule;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using Jube.Validations.EntityAnalysisModelActivationRule;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.EntityAnalysisModelActivationRule
{
    using ActivationRulePoco = Data.Poco.EntityAnalysisModelActivationRule;

    public sealed class EntityAnalysisModelActivationRuleService
    {
        private const int MaxListTake = 200;
        private const int ApprovedByReviewStatusId = 4;
        private static readonly int[] listPermissions = [17];
        private static readonly int[] readPermissions = [17];
        private static readonly int[] writePermissions = [17];
        private static readonly int[] approveByReviewPermissions = [41];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly EntityAnalysisModelActivationRuleRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;
        private readonly EntityAnalysisModelActivationRuleDtoValidator validator;

        private EntityAnalysisModelActivationRuleService(DbContext dbContext, string userName,
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
            repository = new EntityAnalysisModelActivationRuleRepository(dbContext, userName);
            validator = new EntityAnalysisModelActivationRuleDtoValidator(repository, strings);
        }

        public static Task<EntityAnalysisModelActivationRuleService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<EntityAnalysisModelActivationRuleService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityAnalysisModelActivationRuleResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                    log.Warn("EntityAnalysisModelActivationRule.Create: no authenticated user; refusing.");

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelActivationRuleResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                    log.Warn(
                        $"EntityAnalysisModelActivationRule.Create: user '{userName}' resolves to no tenant; refusing.");

                throw new NotAuthenticatedException(
                    strings[EntityAnalysisModelActivationRuleResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelActivationRuleService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists every Activation Rule visible to the calling user's tenant. Unbounded -- intended " +
                     "for the administrative page, not for agent tooling (use the bounded list operation " +
                     "instead).")]
        public async Task<List<EntityAnalysisModelActivationRuleDto>> GetAsync(CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelActivationRule", "List", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelActivationRule.List: entry user={userName}");

            try
            {
                EnsurePermitted(listPermissions, "EntityAnalysisModelActivationRule.List");
                var dtos = EntityAnalysisModelActivationRuleMapper.ToDto(await repository.GetAsync(token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                    log.Debug($"EntityAnalysisModelActivationRule.List: {dtos.Count} rows user={userName}");

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
                if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelActivationRule.List: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelActivationRule.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Activation Rules belonging to the given Model, ordered by Id, scoped to the " +
                     "calling user's tenant.")]
        [ServiceOperation("EntityAnalysisModelActivationRuleGetByEntityAnalysisModelId", OperationKind.Read,
            Idempotent = true)]
        public async Task<List<EntityAnalysisModelActivationRuleDto>> GetByEntityAnalysisModelIdAsync(
            [Description("Numeric identifier of the parent Model.")]
            int entityAnalysisModelId,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelActivationRule", "ListByEntityAnalysisModelId",
                userName, tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"EntityAnalysisModelActivationRule.ListByEntityAnalysisModelId: entry entityAnalysisModelId={entityAnalysisModelId} user={userName}");

            try
            {
                EnsurePermitted(readPermissions, "EntityAnalysisModelActivationRule.ListByEntityAnalysisModelId");
                var dtos = EntityAnalysisModelActivationRuleMapper.ToDto(await repository
                    .GetByEntityAnalysisModelIdOrderByIdDescAsync(entityAnalysisModelId, token)
                    .ConfigureAwait(false));
                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                    log.Debug(
                        $"EntityAnalysisModelActivationRule.ListByEntityAnalysisModelId: {dtos.Count} rows entityAnalysisModelId={entityAnalysisModelId} user={userName}");

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
                        $"EntityAnalysisModelActivationRule.ListByEntityAnalysisModelId: cancelled entityAnalysisModelId={entityAnalysisModelId} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelActivationRule.ListByEntityAnalysisModelId: unexpected failure entityAnalysisModelId={entityAnalysisModelId} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Returns one Activation Rule by its numeric identifier, scoped to the calling user's " +
                     "tenant. Returns null when the row does not exist or is not visible to the caller.")]
        [ServiceOperation("EntityAnalysisModelActivationRuleGet", OperationKind.Read, Idempotent = true)]
        public async Task<EntityAnalysisModelActivationRuleDto?> GetByIdAsync(
            [Description("Numeric identifier of the Activation Rule.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelActivationRule", "Get", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled) log.Debug($"EntityAnalysisModelActivationRule.Get: entry id={id} user={userName}");

            try
            {
                EnsurePermitted(readPermissions, "EntityAnalysisModelActivationRule.Get");
                var activationRule = await repository.GetByIdAsync(id, token).ConfigureAwait(false);
                if (activationRule == null)
                {
                    if (log.IsDebugEnabled)
                        log.Debug(
                            $"EntityAnalysisModelActivationRule.Get: id={id} not found or not visible to tenant user={userName}");

                    return null;
                }

                op.Entity(activationRule.Id);
                return EntityAnalysisModelActivationRuleMapper.ToDto(activationRule);
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
                    log.Debug($"EntityAnalysisModelActivationRule.Get: cancelled id={id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelActivationRule.Get: unexpected failure id={id} user={userName}", ex);
                throw;
            }
        }

        [Description("Lists Activation Rules for the caller's tenant, ordered by id, capped at 'take' rows " +
                     "(max 200). If 'more' is true, call again with 'afterId' set to the last returned Id to " +
                     "continue.")]
        [ServiceOperation("EntityAnalysisModelActivationRuleList", OperationKind.Read, Idempotent = true)]
        public async Task<PagedResult<EntityAnalysisModelActivationRuleDto>> ListAsync(
            [Description("Maximum number of rows to return; clamped to 200.")]
            int take = 50,
            [Description("When set, only rows with an Id greater than this value are returned (keyset paging).")]
            int? afterId = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelActivationRule", "ListPaged", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            if (log.IsDebugEnabled)
                log.Debug(
                    $"EntityAnalysisModelActivationRule.ListPaged: entry take={clampedTake} afterId={afterId} user={userName}");

            try
            {
                EnsurePermitted(listPermissions, "EntityAnalysisModelActivationRule.ListPaged");

                var ordered = (await repository.GetAsync(token).ConfigureAwait(false))
                    .OrderBy(o => o.Id)
                    .Where(w => !afterId.HasValue || w.Id > afterId.Value)
                    .ToList();

                var page = ordered.Take(clampedTake).ToList();

                op.Rows(page.Count);

                return new PagedResult<EntityAnalysisModelActivationRuleDto>(
                    EntityAnalysisModelActivationRuleMapper.ToDto(page));
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
                    log.Debug($"EntityAnalysisModelActivationRule.ListPaged: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelActivationRule.ListPaged: unexpected failure user={userName}", ex);
                throw;
            }
        }

        [Description("Registers a new Activation Rule under a Model in the caller's tenant. Not idempotent -- " +
                     "calling twice creates two rows. Setting ReviewStatusId to 4 (Approved by Review) requires " +
                     "the caller to additionally hold the Allow Approved By Review permission.")]
        [ServiceOperation("EntityAnalysisModelActivationRuleCreate", OperationKind.Write, Idempotent = false)]
        public async Task<ActivationRulePoco> InsertAsync(
            [Description("The Activation Rule to create.")]
            EntityAnalysisModelActivationRuleDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelActivationRule", "Create", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelActivationRule.Create: entry user={userName} name={model?.Name}");

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted(writePermissions, "EntityAnalysisModelActivationRule.Create");
                EnsureApprovedByReviewPermitted(model, "EntityAnalysisModelActivationRule.Create");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                        log.Warn($"EntityAnalysisModelActivationRule.Create: validation failed user={userName} " +
                                 $"props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");

                    throw new DtoValidationException(results);
                }

                var saved = await repository
                    .InsertAsync(EntityAnalysisModelActivationRuleMapper.ToPoco(model), token)
                    .ConfigureAwait(false);

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Created();

                if (log.IsInfoEnabled)
                    log.Info(
                        $"EntityAnalysisModelActivationRule.Create: created Id={saved.Id} name={saved.Name} " +
                        $"user={userName}");

                return saved;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (ReviewStatusApprovalException)
            {
                op.Outcome("invalid");
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
                    log.Debug($"EntityAnalysisModelActivationRule.Create: cancelled user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelActivationRule.Create: unexpected failure user={userName} " +
                          $"name={model?.Name}", ex);
                throw;
            }
        }

        [Description("Updates an existing Activation Rule in the caller's tenant, identified by its Id. " +
                     "Idempotent -- repeating the same update has no further effect beyond incrementing Version. " +
                     "Setting ReviewStatusId to 4 (Approved by Review) requires the caller to additionally hold " +
                     "the Allow Approved By Review permission.")]
        [ServiceOperation("EntityAnalysisModelActivationRuleUpdate", OperationKind.Write, Idempotent = true)]
        public async Task<ActivationRulePoco> UpdateAsync(
            [Description("The Activation Rule to update. Id selects the row; identity/tenant/audit fields are " +
                         "server-owned and ignored.")]
            EntityAnalysisModelActivationRuleDto? model,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelActivationRule", "Update", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelActivationRule.Update: entry id={model?.Id} user={userName}");

            try
            {
                ArgumentNullException.ThrowIfNull(model);
                EnsurePermitted(writePermissions, "EntityAnalysisModelActivationRule.Update");
                EnsureApprovedByReviewPermitted(model, "EntityAnalysisModelActivationRule.Update");

                var results = await validator.ValidateAsync(model, token).ConfigureAwait(false);
                if (!results.IsValid)
                {
                    if (log.IsWarnEnabled)
                        log.Warn($"EntityAnalysisModelActivationRule.Update: validation failed id={model.Id} " +
                                 $"user={userName} props=[{string.Join(",", results.Errors.Select(e => e.PropertyName).Distinct())}]");

                    throw new DtoValidationException(results);
                }

                ActivationRulePoco saved;
                try
                {
                    saved = await repository
                        .UpdateAsync(EntityAnalysisModelActivationRuleMapper.ToPoco(model), token)
                        .ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelActivationRule.Update: id={model.Id} not found, locked, deleted, or not visible to tenant user={userName}");

                    throw new NotFoundException("The Activation Rule was not found.", ex);
                }

                op.Entity(saved.Id);
                op.Version(saved.Version.GetValueOrDefault());
                op.Updated();

                if (log.IsInfoEnabled)
                    log.Info(
                        $"EntityAnalysisModelActivationRule.Update: Id={saved.Id} version->{saved.Version} user={userName}");

                return saved;
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (ReviewStatusApprovalException)
            {
                op.Outcome("invalid");
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
                    log.Debug($"EntityAnalysisModelActivationRule.Update: cancelled id={model?.Id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error(
                    $"EntityAnalysisModelActivationRule.Update: unexpected failure id={model?.Id} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Deletes an Activation Rule in the caller's tenant by its Id. Reversible at the data " +
                     "level, but treat as destructive -- the Activation Rule immediately stops being evaluated " +
                     "on transaction invocation.")]
        [ServiceOperation("EntityAnalysisModelActivationRuleDelete", OperationKind.Delete, Idempotent = true,
            Destructive = true)]
        public async Task DeleteAsync(
            [Description("Numeric identifier of the Activation Rule to delete.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelActivationRule", "Delete", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelActivationRule.Delete: entry id={id} user={userName}");

            try
            {
                EnsurePermitted(writePermissions, "EntityAnalysisModelActivationRule.Delete");

                try
                {
                    await repository.DeleteAsync(id, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelActivationRule.Delete: id={id} not found, locked, already deleted, or not visible to tenant user={userName}");

                    throw new NotFoundException("The Activation Rule was not found.", ex);
                }

                op.Entity(id);
                op.Deleted();

                if (log.IsInfoEnabled)
                    log.Info($"EntityAnalysisModelActivationRule.Delete: soft-deleted Id={id} user={userName}");
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
                    log.Debug($"EntityAnalysisModelActivationRule.Delete: cancelled id={id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelActivationRule.Delete: unexpected failure id={id} user={userName}",
                    ex);
                throw;
            }
        }

        [Description("Resets the ActivationCounter and EvaluationCounter of an Activation Rule in the caller's " +
                     "tenant to zero. Idempotent, but treat as destructive -- the counter history is not " +
                     "reversible.")]
        [ServiceOperation("EntityAnalysisModelActivationRuleReset", OperationKind.Write, Idempotent = true,
            Destructive = true)]
        public async Task ResetCounterAsync(
            [Description("Numeric identifier of the Activation Rule whose counters are to be reset.")]
            int id,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("EntityAnalysisModelActivationRule", "Reset", userName,
                tenantRegistryId, auditLog, log, serviceChangeBus);
            if (log.IsDebugEnabled)
                log.Debug($"EntityAnalysisModelActivationRule.Reset: entry id={id} user={userName}");

            try
            {
                EnsurePermitted(writePermissions, "EntityAnalysisModelActivationRule.Reset");

                try
                {
                    await repository.ResetCounterAsync(id, token).ConfigureAwait(false);
                }
                catch (KeyNotFoundException ex)
                {
                    if (log.IsWarnEnabled)
                        log.Warn(
                            $"EntityAnalysisModelActivationRule.Reset: id={id} not found, locked, deleted, or not visible to tenant user={userName}");

                    throw new NotFoundException("The Activation Rule was not found.", ex);
                }

                op.Entity(id);
                op.Updated();

                if (log.IsInfoEnabled)
                    log.Info($"EntityAnalysisModelActivationRule.Reset: counters reset Id={id} user={userName}");
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
                    log.Debug($"EntityAnalysisModelActivationRule.Reset: cancelled id={id} user={userName}");

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"EntityAnalysisModelActivationRule.Reset: unexpected failure id={id} user={userName}",
                    ex);
                throw;
            }
        }

        private void EnsurePermitted(int[] specs, string op)
        {
            if (permissionValidation.Validate(specs)) return;

            if (log.IsWarnEnabled)
                log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", specs)}]");

            throw new ForbiddenException(strings[EntityAnalysisModelActivationRuleResources.PermissionDenied],
                specs);
        }

        private void EnsureApprovedByReviewPermitted(EntityAnalysisModelActivationRuleDto model, string op)
        {
            if (model.ReviewStatusId != ApprovedByReviewStatusId) return;

            if (permissionValidation.Validate(approveByReviewPermissions)) return;

            if (log.IsWarnEnabled) log.Warn($"{op}: Approved by Review requested without permission user={userName}");

            throw new ReviewStatusApprovalException(
                strings[EntityAnalysisModelActivationRuleResources.PermissionDeniedApproveByReview], "Permission");
        }
    }
}