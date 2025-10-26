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

namespace Jube.App.Controllers.Invoke
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Cache;
    using Data.Cache.Postgres.Callback;
    using Data.Extension;
    using Dto;
    using DynamicEnvironment;
    using Engine;
    using Engine.Invoke;
    using Engine.Model;
    using Engine.Model.Archive;
    using FluentValidation.Results;
    using log4net;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json.Linq;
    using RabbitMQ.Client;

    [Route("api/[controller]")]
    [Produces("application/json")]
    public class InvokeController : Controller
    {
        private readonly DynamicEnvironment dynamicEnvironment;
        private readonly Program engine;
        private readonly ILog log;
        private readonly ConcurrentQueue<EntityAnalysisModelInvoke> pendingEntityInvoke;
        private readonly IModel rabbitMqChannel;
        private readonly CacheService redisDatabase;
        private readonly Random seeded;

        public InvokeController(ILog log,
            Random seeded, DynamicEnvironment dynamicEnvironment,
            ConcurrentQueue<EntityAnalysisModelInvoke> pendingEntityInvoke,
            Program engine = null,
            IModel rabbitMqChannel = null,
            CacheService redisDatabase = null)
        {
            this.engine = engine;
            this.log = log;
            this.seeded = seeded;
            this.dynamicEnvironment = dynamicEnvironment;
            this.pendingEntityInvoke = pendingEntityInvoke;
            this.rabbitMqChannel = rabbitMqChannel;
            this.redisDatabase = redisDatabase;
            if (this.engine != null)
            {
                this.engine.HttpCounterAllRequests += 1;
            }
        }

        [HttpGet("EntityAnalysisModel/Callback/{guid:Guid}")]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult> EntityAnalysisModelCallback(Guid guid, int? timeout)
        {
            try
            {
                if (!dynamicEnvironment.AppSettings("EnablePublicInvokeController")
                        .Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    return await Task.FromResult<ActionResult>(Forbid()).ConfigureAwait(false);
                }

                timeout ??= 3000;

                engine.HttpCounterCallback += 1;

                var sw = new Stopwatch();
                sw.Start();

                var spinWait = new SpinWait();
                while (true)
                {
                    engine.EntityAnalysisModelManager.PendingCallbacks.TryGetValue(guid, out var value);

                    if (value != null)
                    {
                        var cacheCallbackRepository = new CacheCallbackRepository(
                            dynamicEnvironment.AppSettings("ConnectionString"),
                            log);

                        await cacheCallbackRepository.DeleteAsync(guid).ConfigureAwait(false);

                        Response.ContentType = "application/json";
                        Response.ContentLength = value.Payload.Length;
                        return await Task.FromResult<ActionResult>(Ok(value.Payload)).ConfigureAwait(false);
                    }

                    if (sw.ElapsedMilliseconds > timeout)
                    {
                        return await Task.FromResult<ActionResult>(NotFound()).ConfigureAwait(false);
                    }

                    spinWait.SpinOnce();
                }
            }
            catch (Exception ex)
            {
                log.Error($"Callback Fetch: Has seen an error as {ex}. Returning 500.");

                engine.HttpCounterCallback += 1;
                return await Task.FromResult<ActionResult>(StatusCode(500)).ConfigureAwait(false);
            }
        }

        [HttpGet("Sanction")]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public ActionResult<List<SanctionEntryDto>> Sanction(string multiPartString, int distance)
        {
            try
            {
                if (!dynamicEnvironment.AppSettings("EnablePublicInvokeController")
                        .Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }

                if (!engine.SanctionsHasLoadedForStartup)
                {
                    return NotFound();
                }

                engine.HttpCounterSanction += 1;

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Sanction Fetch: Reached Sanction Get controller with distance of {distance} and string of {multiPartString}.");
                }

                return engine.HttpHandlerSanctions(multiPartString, distance)
                    .Select(sanctionEntryReturn => new SanctionEntryDto
                    {
                        Reference = sanctionEntryReturn.SanctionEntryDto.SanctionEntryReference,
                        Value = String.Join(' ', sanctionEntryReturn.SanctionEntryDto.SanctionElementValue),
                        Source = engine.SanctionSources.TryGetValue(sanctionEntryReturn.SanctionEntryDto
                            .SanctionEntrySourceId, out var source)
                            ? source.Name
                            : "Missing",
                        Distance = sanctionEntryReturn.LevenshteinDistance,
                        Id = sanctionEntryReturn.SanctionEntryDto.SanctionEntryId
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                log.Error($"Sanction Fetch: Has seen an error as {ex}. Returning 500.");

                engine.HttpCounterAllError += 1;
                return StatusCode(500);
            }
        }

        [HttpPut("Archive/Tag")]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public ActionResult EntityAnalysisModelInstanceEntryGuid([FromBody] TagRequestDto model)
        {
            try
            {
                if (!dynamicEnvironment.AppSettings("EnablePublicInvokeController")
                        .Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }

                if (!engine.EntityModelsHasLoadedForStartup)
                {
                    return NotFound();
                }

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Tagging: Controller has Put request with guid {model.EntityAnalysisModelInstanceEntryGuid}," +
                        $" name {model.Name} and value {model.Value}.");
                }

                engine.HttpCounterTag += 1;

                var entityAnalysisModelGuid = Guid.Parse(model.EntityAnalysisModelGuid);
                foreach (var (_, value) in
                         from modelKvp in engine.EntityAnalysisModelManager.ActiveEntityAnalysisModels
                         where entityAnalysisModelGuid == modelKvp.Value.Guid
                         select modelKvp)
                {
                    if (value.EntityAnalysisModelTags.Find(w => w.Name == model.Name) == null)
                    {
                        return BadRequest();
                    }

                    var tag = new Tag
                    {
                        Name = model.Name,
                        Value = model.Value,
                        EntityAnalysisModelInstanceEntryGuid = Guid.Parse(model.EntityAnalysisModelInstanceEntryGuid),
                        EntityAnalysisModelId = value.Id
                    };

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            "HTTP Handler Entity: GUID matched for Requested Model GUID " +
                            $"{tag.EntityAnalysisModelInstanceEntryGuid} and model {tag.EntityAnalysisModelId}.");
                    }

                    engine.PendingTagging.Enqueue(tag);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            "Tagging: Controller has put tag in queue with guid " +
                            $"{tag.EntityAnalysisModelInstanceEntryGuid}, model {tag.EntityAnalysisModelId}, " +
                            $"name {model.Name} and value {model.Value}.  Returning Ok.");
                    }

                    return Ok();
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                log.Error(
                    "Tagging: An error has been created while tagging guid " +
                    $"{model.EntityAnalysisModelInstanceEntryGuid} " +
                    $"and model {model.EntityAnalysisModelGuid} as {ex}.");

                engine.HttpCounterAllError += 1;

                return StatusCode(500);
            }
        }
#pragma warning disable ASP0018
        // ReSharper disable once RouteTemplates.RouteParameterIsNotPassedToMethod
        [HttpPost("EntityAnalysisModel/{guid}")]
        // ReSharper disable once RouteTemplates.RouteParameterIsNotPassedToMethod
        [HttpPost("EntityAnalysisModel/{guid}/{async}")]
#pragma warning restore ASP0018
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        // ReSharper disable once RouteTemplates.MethodMissingRouteParameters
        public async Task<ActionResult> EntityAnalysisModelGuidAsync()
        {
            try
            {
                if (!dynamicEnvironment.AppSettings("EnablePublicInvokeController")
                        .Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }

                if (engine is not { EntityModelsHasLoadedForStartup: true })
                {
                    return NotFound();
                }

                engine.HttpCounterModel += 1;

                var ms = new MemoryStream();
                await Request.Body.CopyToAsync(ms).ConfigureAwait(false);

                try
                {
                    var guid = Guid.Parse(Request.RouteValues["guid"].AsString());

                    var async = false;
                    if (Request.RouteValues.ContainsKey("async"))
                    {
                        async = Request.RouteValues["async"].AsString()
                            .Equals("Async", StringComparison.OrdinalIgnoreCase);
                        engine.HttpCounterModelAsync += 1;
                    }

                    EntityAnalysisModel entityAnalysisModel = null;
                    foreach (var (_, value) in
                             from modelKvp in engine.EntityAnalysisModelManager.ActiveEntityAnalysisModels
                             where guid == modelKvp.Value.Guid
                             select modelKvp)
                    {
                        entityAnalysisModel = value;

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"HTTP Handler Entity: GUID matched for Requested Model GUID {guid}.  Model id is {entityAnalysisModel.Id}.");
                        }

                        break;
                    }

                    if (entityAnalysisModel != null)
                    {
                        if (!entityAnalysisModel.Started)
                        {
                            return StatusCode(204);
                        }

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"HTTP Handler Entity: GUID payload {guid} model id is {entityAnalysisModel.Id} will now begin payload parsing.");
                        }

                        var entityModelInvoke = new EntityAnalysisModelInvoke(log, dynamicEnvironment,
                            rabbitMqChannel, redisDatabase, engine.PendingNotification, seeded,
                            engine.EntityAnalysisModelManager.ActiveEntityAnalysisModels);

                        if (Request.ContentLength != null)
                        {
                            if (!(Request.ContentLength > 0))
                            {
                                return BadRequest("Content body is zero length.");
                            }

                            await entityModelInvoke.ParseAndInvoke(entityAnalysisModel, ms, async,
                                Request.ContentLength.Value,
                                pendingEntityInvoke).ConfigureAwait(false);

                            if (entityModelInvoke.InError)
                            {
                                return BadRequest(entityModelInvoke.ErrorMessage);
                            }

                            Response.ContentType = "application/json";
                            Response.ContentLength = entityModelInvoke.ResponseJson.Length;

                            return Ok(entityModelInvoke.ResponseJson);
                        }

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                "HTTP Handler Entity: Json content body is zero.");
                        }

                        return BadRequest("Content body is zero length.");
                    }

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            $"HTTP Handler Entity: Could not locate the model for Guid {guid}.");
                    }

                    return NotFound();
                }
                catch (Exception)
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                log.Error(
                    $"HTTP Handler Entity: Error as {ex}.  Returning 500.");

                if (engine != null)
                {
                    engine.HttpCounterAllError += 1;
                }

                return StatusCode(500);
            }
        }

#pragma warning disable ASP0018
        [HttpPost("ExhaustiveSearchInstance/{guid}")]
#pragma warning restore ASP0018
        // ReSharper disable once RouteTemplates.MethodMissingRouteParameters
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<double>> ExhaustiveSearchInstanceAsync()
        {
            try
            {
                if (!dynamicEnvironment.AppSettings("EnablePublicInvokeController")
                        .Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }

                if (!engine.EntityModelsHasLoadedForStartup)
                {
                    return NotFound();
                }

                engine.HttpCounterExhaustive += 1;

                var ms = new MemoryStream();
                await Request.Body.CopyToAsync(ms).ConfigureAwait(false);

                var guid = Request.RouteValues["guid"].AsString();

                if (log.IsInfoEnabled)
                {
                    log.Info($"Exhaustive Recall:  Recall received for {guid}.  Invoking handler.");
                }

                var value = Math.Round(engine.ThreadPoolCallBackHttpHandlerExhaustive(
                    Guid.Parse(guid),
                    JObject.Parse(Encoding.UTF8.GetString(ms.ToArray()))), 2);

                if (log.IsInfoEnabled)
                {
                    log.Info($"Exhaustive Recall:  Has invoked the handler and returned a value of {value}.  Returning.");
                }

                return value;
            }
            catch (Exception ex)
            {
                log.Error($"Exhaustive Recall:  An error has been raised as {ex}.  Returning 500.");

                engine.HttpCounterAllError += 1;
                return StatusCode(500);
            }
        }

        [HttpPost("ExampleFraudScoreLocalEndpoint")]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult<double>> ExampleFraudScoreLocalEndpointAsync()
        {
            try
            {
                if (!dynamicEnvironment.AppSettings("EnablePublicInvokeController")
                        .Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }

                var ms = new MemoryStream();
                await Request.Body.CopyToAsync(ms).ConfigureAwait(false);

                if (log.IsInfoEnabled)
                {
                    log.Info("Example FraudScore Local Endpoint Recall:  Recall received.");
                }

                var jObject = JObject.Parse(Encoding.UTF8.GetString(ms.ToArray()));

                var responseCodeVolumeRatio = jObject.SelectToken("$.ResponseCodeEqual0Volume");

                if (log.IsInfoEnabled)
                {
                    log.Info($"Example FraudScore Local Endpoint Recall:  Json parsed as {jObject}.  " +
                             "This endpoint will just echo back the sqrt of the ResponseCodeVolumeRatio element." +
                             " More typically this would be an R endpoint and it would recall a variety of models.");
                }

                if (responseCodeVolumeRatio != null)
                {
                    return Math.Sqrt(responseCodeVolumeRatio.ToObject<double>());
                }

                return 0;
            }
            catch (Exception ex)
            {
                log.Error(
                    $"Example FraudScore Local Endpoint Recall:  An error has been raised as {ex}.  Returning 500.");

                return StatusCode(500);
            }
        }
    }
}
