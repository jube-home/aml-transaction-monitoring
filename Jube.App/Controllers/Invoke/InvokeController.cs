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
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Cache.Redis.Callback;
    using Data.Extension;
    using Dto;
    using Dto.Sanctions;
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
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Newtonsoft.Json.Linq;
    using SanctionEntryDto=Dto.Sanctions.SanctionEntryDto;

    [Authorize]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class InvokeController : Controller
    {
        private readonly DynamicEnvironment dynamicEnvironment;
        private readonly Engine engine;
        private readonly ILog log;
        private readonly string userName;

        public InvokeController(ILog log, DynamicEnvironment dynamicEnvironment, IHttpContextAccessor httpContextAccessor,
            Engine engine = null)
        {
            this.engine = engine;
            this.log = log;
            this.dynamicEnvironment = dynamicEnvironment;

            if (this.engine != null)
            {
                Interlocked.Increment(ref this.engine.Context.Counters.HttpCounterAllRequests);
            }

            if (httpContextAccessor.HttpContext?.User.Identity != null)
            {
                userName = httpContextAccessor.HttpContext.User.Identity.Name;
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
        public Task<ActionResult<SanctionSearchResponseDto>> SanctionAsync(string multiPartString, int distance,
            double? maxDistanceRatio, double? maxCoverageRatio)
        {
            try
            {
                if (!dynamicEnvironment.AppSettings("EnablePublicInvokeController").Equals("True", StringComparison.OrdinalIgnoreCase)
                    || !dynamicEnvironment.AppSettings("EnableEngine").Equals("True", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult<ActionResult<SanctionSearchResponseDto>>(NotFound());
                }

                if (!engine.Context.Ready)
                {
                    return Task.FromResult<ActionResult<SanctionSearchResponseDto>>(StatusCode(503));
                }

                Interlocked.Increment(ref engine.Context.Counters.HttpCounterSanction);

                var effectiveMaxDistanceRatio = maxDistanceRatio ??
                                                LevenshteinDistance.ParseNullableDistanceRatio(dynamicEnvironment.AppSettings("SanctionsLevenshteinMaxDistanceRatio"));

                var effectiveMaxCoverageRatio = maxCoverageRatio ??
                                                LevenshteinDistance.ParseNullableCoverageRatio(dynamicEnvironment.AppSettings("SanctionsLevenshteinMaxCoverageRatio"));

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Sanction Fetch: Reached Sanction Get controller with distance of {distance}, max distance ratio of {effectiveMaxDistanceRatio}, max coverage ratio of {effectiveMaxCoverageRatio} and string of {multiPartString}.");
                }

                var sanctionEntryReturns = new LevenshteinDistance(effectiveMaxDistanceRatio, effectiveMaxCoverageRatio)
                    .CheckMultipartString(multiPartString, distance, engine.Context.Sanctions.SanctionsEntries, engine.Context.Sanctions.SanctionsStopTokens);

                var entries = sanctionEntryReturns
                    .Select(sanctionEntryReturn => new SanctionEntryDto
                    {
                        Reference = sanctionEntryReturn.SanctionEntry.SanctionEntryReference,
                        Value = String.Join(' ', sanctionEntryReturn.SanctionEntry.SanctionElementValue),
                        SanctionEntrySourceId = sanctionEntryReturn.SanctionEntry.SanctionEntrySourceId,
                        Source = engine.Context.Sanctions.SanctionsSources.TryGetValue(sanctionEntryReturn.SanctionEntry
                            .SanctionEntrySourceId, out var source)
                            ? source.Name
                            : "Missing",
                        Distance = sanctionEntryReturn.LevenshteinDistance,
                        Id = sanctionEntryReturn.SanctionEntry.SanctionEntryId
                    })
                    .ToList();

                var groupBySource = sanctionEntryReturns
                    .GroupBy(sanctionEntryReturn => sanctionEntryReturn.SanctionEntry.SanctionEntrySourceId)
                    .Select(sourceGroup =>
                    {
                        var sourceMatches = sourceGroup.ToList();

                        return new SanctionSourceAggregationDto
                        {
                            SourceId = sourceGroup.Key,
                            SourceName = engine.Context.Sanctions.SanctionsSources.TryGetValue(sourceGroup.Key, out var source)
                                ? source.Name
                                : "Missing",
                            Sum = SanctionAggregationCalculator.CalculateSum(sourceMatches),
                            Average = SanctionAggregationCalculator.CalculateAverage(sourceMatches),
                            Count = SanctionAggregationCalculator.CalculateCount(sourceMatches),
                            Max = SanctionAggregationCalculator.CalculateMax(sourceMatches),
                            Min = SanctionAggregationCalculator.CalculateMin(sourceMatches),
                            First = SanctionAggregationCalculator.CalculateFirst(sourceMatches),
                            Last = SanctionAggregationCalculator.CalculateLast(sourceMatches),
                            Confidence = SanctionAggregationCalculator.CalculateConfidence(sourceMatches)
                        };
                    })
                    .OrderBy(sourceAggregation => sourceAggregation.SourceName)
                    .ToList();

                var total = new SanctionAggregationDto
                {
                    Sum = SanctionAggregationCalculator.CalculateSum(sanctionEntryReturns),
                    Average = SanctionAggregationCalculator.CalculateAverage(sanctionEntryReturns),
                    Count = SanctionAggregationCalculator.CalculateCount(sanctionEntryReturns),
                    Max = SanctionAggregationCalculator.CalculateMax(sanctionEntryReturns),
                    Min = SanctionAggregationCalculator.CalculateMin(sanctionEntryReturns),
                    First = SanctionAggregationCalculator.CalculateFirst(sanctionEntryReturns),
                    Last = SanctionAggregationCalculator.CalculateLast(sanctionEntryReturns),
                    Confidence = SanctionAggregationCalculator.CalculateConfidence(sanctionEntryReturns)
                };

                return Task.FromResult<ActionResult<SanctionSearchResponseDto>>(new SanctionSearchResponseDto
                {
                    Aggregations = new SanctionAggregationsDto
                    {
                        Total = total,
                        BySource = groupBySource
                    },
                    Entries = entries
                });
            }
            catch (Exception ex)
            {
                log.Error($"Sanction Fetch: Has seen an error as {ex}. Returning 500.");

                Interlocked.Increment(ref engine.Context.Counters.HttpCounterAllError);
                return Task.FromResult<ActionResult<SanctionSearchResponseDto>>(StatusCode(500));
            }
        }

        [HttpPut("Archive/Tag")]
        [ProducesResponseType(typeof(ValidationResult), (int)HttpStatusCode.BadRequest)]
        public Task<ActionResult> EntityAnalysisModelInstanceEntryGuidAsync([FromBody] ArchiveTagDto model)
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
                        $" name {model.Tag}.");
                }

                Interlocked.Increment(ref engine.Context.Counters.HttpCounterTag);

                var tag = new TagMessage
                {
                    Tag = model.Tag,
                    EntityAnalysisModelInstanceEntryGuid = model.EntityAnalysisModelInstanceEntryGuid,
                    UserName = userName
                };

                engine.Context.ConcurrentQueues.PendingTagging.Enqueue(tag);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        "Tagging: Controller has put tag in queue with guid " +
                        $"{tag.EntityAnalysisModelInstanceEntryGuid} and tags {model.Tag}.  Returning Ok.");
                }

                return Task.FromResult<ActionResult>(Ok());
            }
            catch (Exception ex)
            {
                log.Error(
                    "Tagging: An error has been created while tagging guid " +
                    $"{model.EntityAnalysisModelInstanceEntryGuid} as {ex}.");

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
                                return new EmptyResult();
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
