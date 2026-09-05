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
using FluentValidation.Results;
using Jube.Data.Context;
using Jube.Dto.EntityAnalysisModelRequestXPath;
using Jube.Service.EntityAnalysisModelRequestXPath;
using Jube.Service.Exceptions.EntityAnalysisModelRequestXPath;
using Jube.Service.Reactivity.Interfaces;
using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Localization;

namespace Jube.App.Endpoints
{
    public static class EntityAnalysisModelRequestXPathEndpoints
    {
        private const string Base = "/api/EntityAnalysisModelRequestXPath";

        public static void MapEntityAnalysisModelRequestXPathEndpoints(this IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup(Base)
                .RequireAuthorization()
                .WithTags("EntityAnalysisModelRequestXPath");

            group.MapGet("", GetAsync)
                .Produces<List<EntityAnalysisModelRequestXPathDto>>()
                .WithName("EntityAnalysisModelRequestXPathGetAll");

            group.MapGet("ByEntityAnalysisModelId/{entityAnalysisModelId:int}", GetByEntityAnalysisModelIdAsync)
                .Produces<List<EntityAnalysisModelRequestXPathDto>>()
                .WithName("EntityAnalysisModelRequestXPathGetByEntityAnalysisModelId");

            group.MapGet("ByCasesWorkflowId/{casesWorkflowId:int}", GetByCasesWorkflowIdAsync)
                .Produces<List<EntityAnalysisModelRequestXPathDto>>()
                .WithName("EntityAnalysisModelRequestXPathGetByCasesWorkflowId");

            group.MapGet("BySuppressionKey", GetBySuppressionKeyAsync)
                .Produces<List<EntityAnalysisModelRequestXPathDto>>()
                .WithName("EntityAnalysisModelRequestXPathGetBySuppressionKey");

            group.MapGet("ByEntityAnalysisModelId/{entityAnalysisModelId:int}/ByStringIntegerFloatDataType",
                    GetByEntityAnalysisModelIdByDataTypeAsync)
                .Produces<List<EntityAnalysisModelRequestXPathDto>>()
                .WithName("EntityAnalysisModelRequestXPathGetByEntityAnalysisModelIdByDataType");

            group.MapGet("{id:int}", GetByIdAsync)
                .Produces<EntityAnalysisModelRequestXPathDto>()
                .WithName("EntityAnalysisModelRequestXPathGetById");

            group.MapPost("", CreateAsync)
                .Produces((int)HttpStatusCode.OK)
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .WithName("EntityAnalysisModelRequestXPathCreate");

            group.MapPut("", UpdateAsync)
                .Produces((int)HttpStatusCode.OK)
                .Produces<ValidationResult>((int)HttpStatusCode.BadRequest)
                .Produces((int)HttpStatusCode.NoContent)
                .WithName("EntityAnalysisModelRequestXPathUpdate");

            group.MapDelete("{id:int}", DeleteAsync)
                .Produces((int)HttpStatusCode.OK)
                .Produces((int)HttpStatusCode.NoContent)
                .WithName("EntityAnalysisModelRequestXPathDelete");
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
                var service = await EntityAnalysisModelRequestXPathService.CreateAsync(dbContext, user, log,
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
                var service = await EntityAnalysisModelRequestXPathService.CreateAsync(dbContext, user, log,
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

        private static async Task<IResult> GetByCasesWorkflowIdAsync(
            int casesWorkflowId, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled) log.Debug($"GET {Base}/ByCasesWorkflowId/{casesWorkflowId}: entry user={user}");

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await EntityAnalysisModelRequestXPathService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);
                var result = await service.GetByCasesWorkflowIdAsync(casesWorkflowId, token);
                return TypedResults.Ok(result);
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                    log.Warn($"GET {Base}/ByCasesWorkflowId/{casesWorkflowId}: 403 (not authenticated) user={user}");

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled) log.Warn($"GET {Base}/ByCasesWorkflowId/{casesWorkflowId}: 403 user={user}");

                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                    log.Debug($"GET {Base}/ByCasesWorkflowId/{casesWorkflowId}: client cancelled user={user}");

                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}/ByCasesWorkflowId/{casesWorkflowId}: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> GetBySuppressionKeyAsync(
            HttpContext httpContext, ILog log, DynamicEnvironment.DynamicEnvironment dynamicEnvironment,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled) log.Debug($"GET {Base}/BySuppressionKey: entry user={user}");

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await EntityAnalysisModelRequestXPathService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);
                var result = await service.GetBySuppressionKeyAsync(token);
                return TypedResults.Ok(result);
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled) log.Warn($"GET {Base}/BySuppressionKey: 403 (not authenticated) user={user}");

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled) log.Warn($"GET {Base}/BySuppressionKey: 403 user={user}");

                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled) log.Debug($"GET {Base}/BySuppressionKey: client cancelled user={user}");

                throw;
            }
            catch (Exception e)
            {
                log.Error($"GET {Base}/BySuppressionKey: 500 user={user}", e);
                return TypedResults.StatusCode((int)HttpStatusCode.InternalServerError);
            }
        }

        private static async Task<IResult> GetByEntityAnalysisModelIdByDataTypeAsync(
            int entityAnalysisModelId, int dataTypeId, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled)
                log.Debug(
                    $"GET {Base}/ByEntityAnalysisModelId/{entityAnalysisModelId}/ByStringIntegerFloatDataType: entry user={user}");

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await EntityAnalysisModelRequestXPathService.CreateAsync(dbContext, user, log,
                    stringLocalizerFactory, serviceChangeBus, token);
                var result =
                    await service.GetByEntityAnalysisModelIdByDataTypeAsync(entityAnalysisModelId, dataTypeId, token);
                return TypedResults.Ok(result);
            }
            catch (NotAuthenticatedException)
            {
                if (log.IsWarnEnabled)
                    log.Warn(
                        $"GET {Base}/ByEntityAnalysisModelId/{entityAnalysisModelId}/ByStringIntegerFloatDataType: 403 (not authenticated) user={user}");

                return TypedResults.Forbid();
            }
            catch (ForbiddenException)
            {
                if (log.IsWarnEnabled)
                    log.Warn(
                        $"GET {Base}/ByEntityAnalysisModelId/{entityAnalysisModelId}/ByStringIntegerFloatDataType: 403 user={user}");

                return TypedResults.Forbid();
            }
            catch (OperationCanceledException)
            {
                if (log.IsDebugEnabled)
                    log.Debug(
                        $"GET {Base}/ByEntityAnalysisModelId/{entityAnalysisModelId}/ByStringIntegerFloatDataType: client cancelled user={user}");

                throw;
            }
            catch (Exception e)
            {
                log.Error(
                    $"GET {Base}/ByEntityAnalysisModelId/{entityAnalysisModelId}/ByStringIntegerFloatDataType: 500 user={user}",
                    e);
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
                var service = await EntityAnalysisModelRequestXPathService.CreateAsync(dbContext, user, log,
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
            [FromBody] EntityAnalysisModelRequestXPathDto model, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled) log.Debug($"POST {Base}: entry user={user}");

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await EntityAnalysisModelRequestXPathService.CreateAsync(dbContext, user, log,
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
            [FromBody] EntityAnalysisModelRequestXPathDto model, HttpContext httpContext, ILog log,
            DynamicEnvironment.DynamicEnvironment dynamicEnvironment, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token)
        {
            var user = httpContext.User.Identity?.Name;
            if (log.IsDebugEnabled) log.Debug($"PUT {Base}: entry user={user}");

            await using var dbContext = DataConnectionDbContext.GetResilientDbContextDataConnection(
                dynamicEnvironment.AppSettings("ConnectionString"), log);
            try
            {
                var service = await EntityAnalysisModelRequestXPathService.CreateAsync(dbContext, user, log,
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
                var service = await EntityAnalysisModelRequestXPathService.CreateAsync(dbContext, user, log,
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
    }
}