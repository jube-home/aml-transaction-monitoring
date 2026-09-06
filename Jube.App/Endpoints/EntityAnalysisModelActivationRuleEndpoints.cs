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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Dto.EntityAnalysisModelActivationRule;
using Jube.Service.EntityAnalysisModelActivationRule;
using Jube.Service.Exceptions.EntityAnalysisModelActivationRule;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints
{
    public static class EntityAnalysisModelActivationRuleEndpoints
    {
        private const string Base = "/api/EntityAnalysisModelActivationRule";
        
        public static void MapEntityAnalysisModelActivationRuleEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("EntityAnalysisModelActivationRule");

            group.MapGet("", GetAsync)
                .Produces<List<EntityAnalysisModelActivationRuleDto>>()
                .WithName("EntityAnalysisModelActivationRuleGetAll");

            group.MapGet("ByEntityAnalysisModelId/{entityAnalysisModelId:int}", GetByEntityAnalysisModelIdAsync)
                .Produces<List<EntityAnalysisModelActivationRuleDto>>()
                .WithName("EntityAnalysisModelActivationRuleGetByEntityAnalysisModelId");

            group.MapGet("{id:int}", GetByIdAsync)
                .Produces<EntityAnalysisModelActivationRuleDto>()
                .WithName("EntityAnalysisModelActivationRuleGetById");

            group.MapPost("", CreateAsync)
                .Produces((int)HttpStatusCode.OK)
                .Produces((int)HttpStatusCode.BadRequest)
                .WithName("EntityAnalysisModelActivationRuleCreate");

            group.MapPut("", UpdateAsync)
                .Produces((int)HttpStatusCode.OK)
                .Produces((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NoContent)
                .WithName("EntityAnalysisModelActivationRuleUpdate");

            group.MapDelete("{id:int}", DeleteAsync)
                .Produces((int)HttpStatusCode.OK)
                .Produces((int)HttpStatusCode.NoContent)
                .WithName("EntityAnalysisModelActivationRuleDelete");

            group.MapPost("{id:int}/Reset", ResetCounterAsync)
                .Produces((int)HttpStatusCode.OK)
                .Produces((int)HttpStatusCode.NoContent)
                .WithName("EntityAnalysisModelActivationRuleReset");
        }

        private static async Task<IResult> GetAsync(
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled) log.Debug($"GET {Base}: entry user={user}");

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await EntityAnalysisModelActivationRuleService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);
                var result = await service.GetAsync(token);
                return TypedResults.Ok(result);
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled) log.Warn($"GET {Base}: 403 (not authenticated) user={user}");

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled) log.Warn($"GET {Base}: 403 user={user}");

                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled) log.Debug($"GET {Base}: client cancelled user={user}");

                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> GetByEntityAnalysisModelIdAsync(
            int entityAnalysisModelId, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
                log.Debug($"GET {Base}/ByEntityAnalysisModelId/{entityAnalysisModelId}: entry user={user}");

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await EntityAnalysisModelActivationRuleService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);
                var result = await service.GetByEntityAnalysisModelIdAsync(entityAnalysisModelId, token);
                return TypedResults.Ok(result);
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                    log.Warn(
                        $"GET {Base}/ByEntityAnalysisModelId/{entityAnalysisModelId}: 403 (not authenticated) user={user}");

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                    log.Warn($"GET {Base}/ByEntityAnalysisModelId/{entityAnalysisModelId}: 403 user={user}");

                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                    log.Debug(
                        $"GET {Base}/ByEntityAnalysisModelId/{entityAnalysisModelId}: client cancelled user={user}");

                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}/ByEntityAnalysisModelId/{entityAnalysisModelId}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> GetByIdAsync(
            int id, HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled) log.Debug($"GET {Base}/{id}: entry user={user}");

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await EntityAnalysisModelActivationRuleService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);
                var result = await service.GetByIdAsync(id, token);
                return TypedResults.Ok(result);
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled) log.Warn($"GET {Base}/{id}: 403 (not authenticated) user={user}");

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled) log.Warn($"GET {Base}/{id}: 403 user={user}");

                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled) log.Debug($"GET {Base}/{id}: client cancelled user={user}");

                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}/{id}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> CreateAsync(
            [FromBody] EntityAnalysisModelActivationRuleDto model, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled) log.Debug($"POST {Base}: entry user={user}");

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await EntityAnalysisModelActivationRuleService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);
                var result = await service.InsertAsync(model, token);
                return TypedResults.Ok(result);
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled) log.Warn($"POST {Base}: 403 (not authenticated) user={user}");

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled) log.Warn($"POST {Base}: 403 user={user}");

                return TypedResults.Forbid();
            }
            catch (ReviewStatusApprovalException ex)
            {
                if (log.IsWarnEnabled) log.Warn($"POST {Base}: 400 (approve-by-review denied) user={user}");

                return ApprovalDenied(ex);
            }
            catch (DtoValidationException ex)
            {
                if (log.IsWarnEnabled) log.Warn($"POST {Base}: 400 user={user} errors={ex.Result.Errors.Count}");

                return TypedResults.BadRequest(ex.Result);
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled) log.Debug($"POST {Base}: client cancelled user={user}");

                throw;
            }
            catch (Exception e)
            {
                log.Error($"POST {Base}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> UpdateAsync(
            [FromBody] EntityAnalysisModelActivationRuleDto model, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled) log.Debug($"PUT {Base}: entry user={user}");

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await EntityAnalysisModelActivationRuleService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);
                var result = await service.UpdateAsync(model, token);
                return TypedResults.Ok(result);
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled) log.Warn($"PUT {Base}: 403 (not authenticated) user={user}");

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled) log.Warn($"PUT {Base}: 403 user={user}");

                return TypedResults.Forbid();
            }
            catch (ReviewStatusApprovalException ex)
            {
                if (log.IsWarnEnabled) log.Warn($"PUT {Base}: 400 (approve-by-review denied) user={user}");

                return ApprovalDenied(ex);
            }
            catch (DtoValidationException ex)
            {
                if (log.IsWarnEnabled) log.Warn($"PUT {Base}: 400 user={user} errors={ex.Result.Errors.Count}");

                return TypedResults.BadRequest(ex.Result);
            }
            catch (NotFoundException)
            {
                if (log.IsWarnEnabled) log.Warn($"PUT {Base}: 204 (not found) user={user}");

                return TypedResults.StatusCode((int)HttpStatusCode.NoContent);
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled) log.Debug($"PUT {Base}: client cancelled user={user}");

                throw;
            }
            catch (Exception e)
            {
                log.Error($"PUT {Base}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> DeleteAsync(
            int id, HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled) log.Debug($"DELETE {Base}/{id}: entry user={user}");

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await EntityAnalysisModelActivationRuleService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);
                await service.DeleteAsync(id, token);
                return TypedResults.Ok();
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled) log.Warn($"DELETE {Base}/{id}: 403 (not authenticated) user={user}");

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled) log.Warn($"DELETE {Base}/{id}: 403 user={user}");

                return TypedResults.Forbid();
            }
            catch (NotFoundException)
            {
                if (log.IsWarnEnabled) log.Warn($"DELETE {Base}/{id}: 204 (not found) user={user}");

                return TypedResults.StatusCode((int)HttpStatusCode.NoContent);
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled) log.Debug($"DELETE {Base}/{id}: client cancelled user={user}");

                throw;
            }
            catch (Exception e)
            {
                log.Error($"DELETE {Base}/{id}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> ResetCounterAsync(
            int id, HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled) log.Debug($"POST {Base}/{id}/Reset: entry user={user}");

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await EntityAnalysisModelActivationRuleService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);
                await service.ResetCounterAsync(id, token);
                return TypedResults.Ok();
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled) log.Warn($"POST {Base}/{id}/Reset: 403 (not authenticated) user={user}");

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled) log.Warn($"POST {Base}/{id}/Reset: 403 user={user}");

                return TypedResults.Forbid();
            }
            catch (NotFoundException)
            {
                if (log.IsWarnEnabled) log.Warn($"POST {Base}/{id}/Reset: 204 (not found) user={user}");

                return TypedResults.StatusCode((int)HttpStatusCode.NoContent);
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled) log.Debug($"POST {Base}/{id}/Reset: client cancelled user={user}");

                throw;
            }
            catch (Exception e)
            {
                log.Error($"POST {Base}/{id}/Reset: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        // Reproduces the legacy controller's dedicated approve-by-review 400 body exactly -- a plain
        // { errors:[{ errorMessage, propertyName }] } shape, not a FluentValidation ValidationResult.
        private static IResult ApprovalDenied(ReviewStatusApprovalException ex)
        {
            return TypedResults.BadRequest(new
            {
                errors = new[]
                {
                    new
                    {
                        errorMessage = ex.Message,
                        propertyName = ex.PropertyName
                    }
                }
            });
        }
    }
}