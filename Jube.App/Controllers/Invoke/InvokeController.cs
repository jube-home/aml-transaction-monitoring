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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Cache.Redis.Callback;
    using Data.Extension;
    using Dto;
    using DynamicEnvironment;
    using Engine;
    using Engine.BackgroundTasks.TaskStarters.Models;
    using Engine.EntityAnalysisModelInvoke;
    using Engine.EntityAnalysisModelInvoke.Exceptions;
    using Engine.Exhaustive.Extensions;
    using Engine.Sanctions;
    using FluentValidation.Results;
    using log4net;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json.Linq;
    using SanctionEntryDto=Dto.SanctionEntryDto;

    [Authorize]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class InvokeController : Controller
    {
        private readonly DynamicEnvironment dynamicEnvironment;
        private readonly Engine engine;
        private readonly ILog log;

        public InvokeController(ILog log, DynamicEnvironment dynamicEnvironment,
            Engine engine = null)
        {
            this.engine = engine;
            this.log = log;
            this.dynamicEnvironment = dynamicEnvironment;
            if (this.engine != null)
            {
                Interlocked.Increment(ref this.engine.Context.Counters.HttpCounterAllRequests);
            }
        }

        [HttpGet("EntityAnalysisModel/Callback/{guid:Guid}")]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public async Task<ActionResult> EntityAnalysisModelCallbackAsync(Guid guid, int? timeout, CancellationToken token = default)
        {
            try
            {
                if (!dynamicEnvironment.AppSettings("EnablePublicInvokeController")
                        .Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    return await Task.FromResult<ActionResult>(NotFound()).ConfigureAwait(false);
                }

                Interlocked.Increment(ref engine.Context.Counters.HttpCounterCallback);

                var tcs = engine.Context.Services.CacheService.CacheCallbackPublishSubscribe.Callbacks.GetOrAdd(guid, _ => new TaskCompletionSource<Callback>(TaskCreationOptions.RunContinuationsAsynchronously));
                var callback = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(timeout ?? 30000), token);

                await engine.Context.Services.CacheService
                    .CacheCallbackPublishSubscribe
                    .DeleteAsync(guid, token).ConfigureAwait(false);

                return File(callback.Payload, "application/json");

            }
            catch (TimeoutException)
            {
                return StatusCode(408);
            }
            catch (Exception ex)
            {
                log.Error($"Callback Fetch: Has seen an error as {ex}. Returning 500.");

                Interlocked.Increment(ref engine.Context.Counters.HttpCounterCallback);
                return await Task.FromResult<ActionResult>(StatusCode(500)).ConfigureAwait(false);
            }
        }

        [HttpGet("Sanction")]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public Task<ActionResult<List<SanctionEntryDto>>> SanctionAsync(string multiPartString, int distance)
        {
            try
            {
                if (!dynamicEnvironment.AppSettings("EnablePublicInvokeController")
                        .Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult<ActionResult<List<SanctionEntryDto>>>(NotFound());
                }

                if (!engine.Context.Ready)
                {
                    return Task.FromResult<ActionResult<List<SanctionEntryDto>>>(StatusCode(503));
                }

                Interlocked.Increment(ref engine.Context.Counters.HttpCounterSanction);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Sanction Fetch: Reached Sanction Get controller with distance of {distance} and string of {multiPartString}.");
                }

                return Task.FromResult<ActionResult<List<SanctionEntryDto>>>(
                    LevenshteinDistance.CheckMultipartString(multiPartString, distance, engine.Context.Sanctions.SanctionsEntries)
                        .Select(sanctionEntryReturn => new SanctionEntryDto
                        {
                            Reference = sanctionEntryReturn.SanctionEntry.SanctionEntryReference,
                            Value = String.Join(' ', sanctionEntryReturn.SanctionEntry.SanctionElementValue),
                            Source = engine.Context.Sanctions.SanctionsSources.TryGetValue(sanctionEntryReturn.SanctionEntry
                                .SanctionEntrySourceId, out var source)
                                ? source.Name
                                : "Missing",
                            Distance = sanctionEntryReturn.LevenshteinDistance,
                            Id = sanctionEntryReturn.SanctionEntry.SanctionEntryId
                        })
                        .ToList());
            }
            catch (Exception ex)
            {
                log.Error($"Sanction Fetch: Has seen an error as {ex}. Returning 500.");

                Interlocked.Increment(ref engine.Context.Counters.HttpCounterAllError);
                return Task.FromResult<ActionResult<List<SanctionEntryDto>>>(StatusCode(500));
            }
        }

        [HttpPut("Archive/Tag")]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public Task<ActionResult> EntityAnalysisModelInstanceEntryGuidAsync([FromBody] TagRequestDto model)
        {
            try
            {
                if (!dynamicEnvironment.AppSettings("EnablePublicInvokeController")
                        .Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult<ActionResult>(NotFound());
                }

                if (!engine.Context.Ready)
                {
                    return Task.FromResult<ActionResult>(StatusCode(503));
                }

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Tagging: Controller has Put request with guid {model.EntityAnalysisModelInstanceEntryGuid}," +
                        $" name {model.Name} and value {model.Value}.");
                }

                Interlocked.Increment(ref engine.Context.Counters.HttpCounterTag);

                var foundModels = engine.Context.Tasks.EntityAnalysisModelManager.Context.EntityAnalysisModels.ActiveEntityAnalysisModels
                    .Where(modelKvp => Guid.Parse(model.EntityAnalysisModelGuid) == modelKvp.Value.Instance.Guid).ToList();

                if (foundModels.Count == 0)
                {
                    return Task.FromResult<ActionResult>(NotFound());
                }

                foreach (var (_, value) in foundModels)
                {
                    if (!value.Collections.Users.Contains(User.Identity?.Name))
                    {
                        continue;
                    }

                    if (value.Collections.EntityAnalysisModelTags.Find(w => w.Name == model.Name) == null)
                    {
                        return Task.FromResult<ActionResult>(BadRequest());
                    }

                    var tag = new Tag
                    {
                        Name = model.Name,
                        Value = model.Value,
                        EntityAnalysisModelInstanceEntryGuid = Guid.Parse(model.EntityAnalysisModelInstanceEntryGuid),
                        EntityAnalysisModelId = value.Instance.Id
                    };

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            "HTTP Handler Entity: GUID matched for Requested Model GUID " +
                            $"{tag.EntityAnalysisModelInstanceEntryGuid} and model {tag.EntityAnalysisModelId}.");
                    }

                    engine.Context.ConcurrentQueues.PendingTagging.Enqueue(tag);

                    if (log.IsInfoEnabled)
                    {
                        log.Info(
                            "Tagging: Controller has put tag in queue with guid " +
                            $"{tag.EntityAnalysisModelInstanceEntryGuid}, model {tag.EntityAnalysisModelId}, " +
                            $"name {model.Name} and value {model.Value}.  Returning Ok.");
                    }

                    return Task.FromResult<ActionResult>(Ok());
                }

                return Task.FromResult<ActionResult>(Forbid());
            }
            catch (Exception ex)
            {
                log.Error(
                    "Tagging: An error has been created while tagging guid " +
                    $"{model.EntityAnalysisModelInstanceEntryGuid} " +
                    $"and model {model.EntityAnalysisModelGuid} as {ex}.");

                engine.Context.Counters.HttpCounterAllError += 1;

                return Task.FromResult<ActionResult>(StatusCode(500));
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
                    return NotFound();
                }

                if (engine.Context is not { Ready: true })
                {
                    return StatusCode(503);
                }

                Interlocked.Increment(ref engine.Context.Counters.HttpCounterModel);

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
                        Interlocked.Increment(ref engine.Context.Counters.HttpCounterModelAsync);
                    }

                    var foundModels = engine.Context.Tasks.EntityAnalysisModelManager.Context.EntityAnalysisModels.ActiveEntityAnalysisModels
                        .Where(modelKvp => guid == modelKvp.Value.Instance.Guid).ToList();

                    if (!foundModels.Any())
                    {
                        return NotFound();
                    }

                    foreach (var (_, value) in foundModels)
                    {
                        if (!value.Collections.Users.Contains(User.Identity?.Name))
                        {
                            continue;
                        }

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"HTTP Handler Entity: GUID matched for Requested Model GUID {guid}.  Model id is {value.Instance.Id}.");
                        }

                        if (log.IsInfoEnabled)
                        {
                            log.Info(
                                $"HTTP Handler Entity: GUID payload {guid} model id is {value.Instance.Id} will now begin payload parsing.");
                        }

                        if (Request.ContentLength != null)
                        {
                            try
                            {
                                var context = await EntityAnalysisModelInvoke.InvokeAsync(
                                    value,
                                    ms, Int32.Parse(dynamicEnvironment.AppSettings("MaxInvokeControllerRequestBytes")),
                                    async).ConfigureAwait(false);

                                var bytes = context.EntityAnalysisModelInstanceEntryPayload.ResponseJson.Length > 0 ? context.EntityAnalysisModelInstanceEntryPayload.ResponseJson : context.EntityAnalysisModelInstanceEntryPayload.ArchiveJson;
                                Response.ContentType = "application/json";
                                Response.ContentLength = bytes.Length;
                                await Response.Body.WriteAsync(bytes);
                            }
                            catch (ExceededBytesException)
                            {
                                return BadRequest("Exceeded the maximum allowed bytes in POST body.");
                            }
                            catch (ExceededQueueLengthException)
                            {
                                return StatusCode(429, "Too many asynchronous requests in the queue.");
                            }
                            catch (ReferenceDateInFutureException)
                            {
                                return BadRequest("Reference Date can't be in the future.");
                            }
                            catch (ZeroBytesException)
                            {
                                return BadRequest("Empty POST body.");
                            }
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

                    return Forbid();
                }
                catch (Exception ex)
                {
                    return StatusCode(500, ex.Message);
                }
            }
            catch (Exception ex)
            {
                log.Error(
                    $"HTTP Handler Entity: Error as {ex}.  Returning 500.");

                if (engine != null)
                {
                    engine.Context.Counters.HttpCounterAllError += 1;
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
                    return NotFound();
                }

                if (!engine.Context.Ready)
                {
                    return StatusCode(503);
                }

                Interlocked.Increment(ref engine.Context.Counters.HttpCounterExhaustive);

                var ms = new MemoryStream();
                await Request.Body.CopyToAsync(ms).ConfigureAwait(false);

                var guid = Guid.Parse(Request.RouteValues["guid"].AsString());

                if (log.IsInfoEnabled)
                {
                    log.Info($"Exhaustive Recall:  Recall received for {guid}.  Invoking handler.");
                }

                var foundExhaustive = engine.Context.Tasks.EntityAnalysisModelManager.Context.EntityAnalysisModels.ActiveEntityAnalysisModels
                    .Where(w => w.Value.Collections.ExhaustiveModels.Any(a => a.Guid == guid)).ToList();

                if (foundExhaustive.Count == 0)
                {
                    return NotFound();
                }

                foreach (var (_, value) in foundExhaustive)
                {
                    if (!value.Collections.Users.Contains(User.Identity?.Name))
                    {
                        continue;
                    }

                    var response = Math.Round(engine.Context.RecallExhaustive(
                        guid,
                        JObject.Parse(Encoding.UTF8.GetString(ms.ToArray()))), 2);

                    if (log.IsInfoEnabled)
                    {
                        log.Info($"Exhaustive Recall:  Has invoked the handler and returned a value of {value}.  Returning.");
                    }

                    return response;
                }

                return Forbid();
            }
            catch (Exception ex)
            {
                log.Error($"Exhaustive Recall:  An error has been raised as {ex}.  Returning 500.");

                engine.Context.Counters.HttpCounterAllError += 1;
                return StatusCode(500);
            }
        }
    }
}
