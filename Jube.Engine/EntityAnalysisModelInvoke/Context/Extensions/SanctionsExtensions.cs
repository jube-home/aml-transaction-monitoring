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

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Cache;
    using Cache.Redis.Models;
    using EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
    using Sanctions;
    using Sanctions.Models;
    using TaskCancellation.TaskHelper;

    public static class SanctionsExtensions
    {
        public static async Task<Context> ExecuteSanctionsAsync(this Context context)
        {
            await IterateAndProcessAsync(context, context.EntityAnalysisModel.Services.CacheService).ConfigureAwait(false);
            StorePerformanceFromStopwatch(context);

            return context;
        }

        private static void StorePerformanceFromStopwatch(Context context)
        {

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.SanctionsAsync =
                (int)(context.Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has finished sanctions processing.");
            }
        }

        private static async Task IterateAndProcessAsync(Context context, CacheService cacheService)
        {

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is starting Sanctions processing.");
            }

            foreach (var entityAnalysisModelSanction in context.EntityAnalysisModel.Collections.EntityAnalysisModelSanctions)
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name}.");
                }

                try
                {
                    if (context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey(entityAnalysisModelSanction.MultipartStringDataName))
                    {
                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is about to look for Sanctions Match in the Cache.");
                        }

                        var multiPartStringValue = context.EntityAnalysisModelInstanceEntryPayload.Payload
                            [entityAnalysisModelSanction.MultipartStringDataName].AsString();

                        var sanction = await LookupFromCacheAsync(context, cacheService, multiPartStringValue, entityAnalysisModelSanction).ConfigureAwait(false);
                        var foundCacheSanctionsAndNotExpired = false;

                        if (sanction != null)
                        {
                            foundCacheSanctionsAndNotExpired = TestIfSanctionHasExpiredAndFound(context, sanction, multiPartStringValue, entityAnalysisModelSanction);
                        }
                        else
                        {
                            if (context.Log.IsInfoEnabled)
                            {
                                context.Log.Info(
                                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has extracted multi part string name value as {multiPartStringValue} cache is not available.");
                            }
                        }

                        if (foundCacheSanctionsAndNotExpired)
                        {
                            if (sanction.Value.HasValue)
                            {
                                AddToResponses(context, entityAnalysisModelSanction, sanction.Value.Value, multiPartStringValue);

                                context.Log.Info(
                                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has extracted multi part string name value as {multiPartStringValue} from cache and is continuing to the next configured Sanctions check.");

                                continue;
                            }

                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has extracted multi part string name value as {multiPartStringValue} from cache but the value is null.");
                        }

                        var aggregateLevenshteinDistance = CalculateSanctionAndUpsertCache(context, cacheService,
                            entityAnalysisModelSanction, multiPartStringValue, entityAnalysisModelSanction.AggregationTypeId);

                        if (!aggregateLevenshteinDistance.HasValue)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has extracted multi part string name value as {multiPartStringValue} does not have a value which means no match.");

                            continue;
                        }

                        AddToResponses(context, entityAnalysisModelSanction, aggregateLevenshteinDistance.Value, multiPartStringValue);

                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has extracted multi part string name value as {multiPartStringValue} recalculated as {aggregateLevenshteinDistance} and is continuing to the next configured Sanctions check.");

                        continue;
                    }

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} but could not find it in the payload.");
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    context.Log.Error(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} has seen an error in sanctions checking as {ex}.");
                }
            }
        }

        private static double? CalculateSanctionAndUpsertCache(Context context, CacheService cacheService,
            EntityAnalysisModelSanction entityAnalysisModelSanction, string multiPartStringValue, byte aggregationTypeId)
        {
            var sanctionEntryReturns = FindAllDistanceMatches(context, entityAnalysisModelSanction, multiPartStringValue);

            double? aggregateLevenshteinDistance = null;
            if (sanctionEntryReturns.Count == 0)
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} found no matches.");
                }
            }
            else
            {
                aggregateLevenshteinDistance = aggregationTypeId switch
                {
                    1 => CalculateSumDistance(context, entityAnalysisModelSanction, sanctionEntryReturns),
                    2 => CalculateAverageDistance(context, entityAnalysisModelSanction, sanctionEntryReturns),
                    3 => CalculateCountDistance(context, entityAnalysisModelSanction, sanctionEntryReturns),
                    4 => CalculateMaxDistance(context, entityAnalysisModelSanction, sanctionEntryReturns),
                    5 => CalculateMinDistance(context, entityAnalysisModelSanction, sanctionEntryReturns),
                    6 => CalculateFirstDistance(context, entityAnalysisModelSanction, sanctionEntryReturns),
                    7 => CalculateLastDistance(context, entityAnalysisModelSanction, sanctionEntryReturns),
                    8 => CalculateSanctionConfidence(context, entityAnalysisModelSanction, sanctionEntryReturns),
                    _ => CalculateAverageDistance(context, entityAnalysisModelSanction, sanctionEntryReturns)
                };

                if (aggregateLevenshteinDistance.HasValue)
                {
                    AddToResponses(context, entityAnalysisModelSanction, aggregateLevenshteinDistance.Value, multiPartStringValue);
                }
                else
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} found no matches when calculating distance.");
                }
            }

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has constructed a cache payload as Distance of {aggregateLevenshteinDistance}, MultiPartString of {multiPartStringValue} and a created date of now.  Will upset it in cache.");
            }

            UpsertSanctionInCache(context, cacheService, multiPartStringValue, entityAnalysisModelSanction, aggregateLevenshteinDistance);

            return aggregateLevenshteinDistance;
        }

        private static void UpsertSanctionInCache(Context context, CacheService cacheService,
            string multiPartStringValue, EntityAnalysisModelSanction entityAnalysisModelSanction, double? aggregateLevenshteinDistance)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is about to insert cache payload.");
            }

            context.PendingWriteTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.CacheSanctionInsertAsync, async () => await cacheService.CacheSanctionRepository.InsertAsync(
                context.EntityAnalysisModel.Instance.TenantRegistryId,
                context.EntityAnalysisModel.Instance.Guid,
                multiPartStringValue,
                entityAnalysisModelSanction.Distance, aggregateLevenshteinDistance).ConfigureAwait(false)));

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has inserted cache payload.");
            }
        }

        private static List<SanctionEntryReturn> FindAllDistanceMatches(Context context, EntityAnalysisModelSanction entityAnalysisModelSanction, string multiPartStringValue)
        {

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and is about to execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance}.");
            }

            var maxDistanceRatio = entityAnalysisModelSanction.MaxDistanceRatio ??
                                   LevenshteinDistance.ParseNullableDistanceRatio(context.Environment.AppSettings("SanctionsLevenshteinMaxDistanceRatio"));

            var maxCoverageRatio = entityAnalysisModelSanction.MaxCoverageRatio ??
                                   LevenshteinDistance.ParseNullableCoverageRatio(context.Environment.AppSettings("SanctionsLevenshteinMaxCoverageRatio"));

            var sanctionEntryReturns = new LevenshteinDistance(maxDistanceRatio, maxCoverageRatio).CheckMultipartString(
                multiPartStringValue,
                entityAnalysisModelSanction.Distance, context.EntityAnalysisModel.Dependencies.SanctionsEntries, context.EntityAnalysisModel.Dependencies.SanctionsStopTokens);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} and found {sanctionEntryReturns.Count} matches.");
            }
            return sanctionEntryReturns;
        }

        private static double? CalculateSumDistance(Context context, EntityAnalysisModelSanction entityAnalysisModelSanction, List<SanctionEntryReturn> sanctionEntryReturns)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} is about to calculate the sum.");
            }

            var sumLevenshteinDistance = SanctionAggregationCalculator.CalculateSum(sanctionEntryReturns);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} has a sum of {sumLevenshteinDistance}.");
            }

            return sumLevenshteinDistance;
        }

        private static double? CalculateFirstDistance(Context context, EntityAnalysisModelSanction entityAnalysisModelSanction, List<SanctionEntryReturn> sanctionEntryReturns)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} is about to calculate the first.");
            }

            var firstLevenshteinDistance = SanctionAggregationCalculator.CalculateFirst(sanctionEntryReturns);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} has a first of {firstLevenshteinDistance}.");
            }

            return firstLevenshteinDistance;
        }

        private static double? CalculateLastDistance(Context context, EntityAnalysisModelSanction entityAnalysisModelSanction, List<SanctionEntryReturn> sanctionEntryReturns)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} is about to calculate the last.");
            }

            var lastLevenshteinDistance = SanctionAggregationCalculator.CalculateLast(sanctionEntryReturns);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} has a last of {lastLevenshteinDistance}.");
            }

            return lastLevenshteinDistance;
        }

        private static double? CalculateMinDistance(Context context, EntityAnalysisModelSanction entityAnalysisModelSanction, List<SanctionEntryReturn> sanctionEntryReturns)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} is about to calculate the min.");
            }

            var minLevenshteinDistance = SanctionAggregationCalculator.CalculateMin(sanctionEntryReturns);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} has a min of {minLevenshteinDistance}.");
            }

            return minLevenshteinDistance;
        }

        private static double? CalculateMaxDistance(Context context, EntityAnalysisModelSanction entityAnalysisModelSanction, List<SanctionEntryReturn> sanctionEntryReturns)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} is about to calculate the max.");
            }

            var maxLevenshteinDistance = SanctionAggregationCalculator.CalculateMax(sanctionEntryReturns);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} has a max of {maxLevenshteinDistance}.");
            }

            return maxLevenshteinDistance;
        }

        private static double? CalculateCountDistance(Context context, EntityAnalysisModelSanction entityAnalysisModelSanction, List<SanctionEntryReturn> sanctionEntryReturns)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} is about to calculate the count.");
            }

            var countLevenshteinDistance = SanctionAggregationCalculator.CalculateCount(sanctionEntryReturns);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} has a count of {countLevenshteinDistance}.");
            }

            return countLevenshteinDistance;
        }

        private static double? CalculateAverageDistance(Context context, EntityAnalysisModelSanction entityAnalysisModelSanction, List<SanctionEntryReturn> sanctionEntryReturns)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} is about to calculate the average.");
            }

            var averageLevenshteinDistance = SanctionAggregationCalculator.CalculateAverage(sanctionEntryReturns);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} and finished execute the fuzzy logic with a distance of {entityAnalysisModelSanction.Distance} has calculated average as {averageLevenshteinDistance}.");
            }

            return averageLevenshteinDistance;
        }

        private static double? CalculateSanctionConfidence(
            Context context,
            EntityAnalysisModelSanction entityAnalysisModelSanction,
            List<SanctionEntryReturn> sanctionEntryReturns)
        {
            LogInfo(context, entityAnalysisModelSanction, "is about to calculate confidence");

            var confidence = SanctionAggregationCalculator.CalculateConfidence(sanctionEntryReturns);

            LogInfo(context, entityAnalysisModelSanction,
                confidence.HasValue
                    ? $"across {sanctionEntryReturns?.Count} candidates confidence calculated as {confidence}"
                    : "has no candidate returns, confidence cannot be calculated");

            return confidence;
        }

        private static void LogInfo(
            Context context, EntityAnalysisModelSanction entityAnalysisModelSanction, string message)
        {
            if (!context.Log.IsInfoEnabled)
            {
                return;
            }

            context.Log.Info(
                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} "
                + $"and model {context.EntityAnalysisModel.Instance.Id} is evaluating Sanctions {entityAnalysisModelSanction.Name} "
                + $"with a distance of {entityAnalysisModelSanction.Distance} {message}.");
        }

        private static void AddToResponses(Context context, EntityAnalysisModelSanction entityAnalysisModelSanction, double value, string multiPartStringValue)
        {
            if (!context.EntityAnalysisModelInstanceEntryPayload.Sanction.TryAdd(entityAnalysisModelSanction.Name, value))
            {
                return;
            }
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has extracted multi part string name value as {multiPartStringValue} is adding cache value of {value} to processing. Reprocessing will not take place.");
            }
        }

        private static bool TestIfSanctionHasExpiredAndFound(Context context, CacheSanction sanction, string multiPartStringValue, EntityAnalysisModelSanction entityAnalysisModelSanction)
        {

            bool foundCacheSanctions;
            var deleteLineCacheKeys = sanction.CreatedDate;

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has extracted multi part string name value as {multiPartStringValue} has a cache interval of {entityAnalysisModelSanction.CacheInterval} and value of {entityAnalysisModelSanction.CacheValue}.");
            }

            deleteLineCacheKeys = entityAnalysisModelSanction.CacheInterval switch
            {
                's' => deleteLineCacheKeys.AddSeconds(
                    entityAnalysisModelSanction.CacheValue),
                'n' => deleteLineCacheKeys.AddMinutes(
                    entityAnalysisModelSanction.CacheValue),
                'h' => deleteLineCacheKeys.AddHours(entityAnalysisModelSanction.CacheValue),
                _ => deleteLineCacheKeys.AddDays(entityAnalysisModelSanction.CacheValue)
            };

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has extracted multi part string name value as {multiPartStringValue} has an expiry date of {deleteLineCacheKeys}");
            }

            if (deleteLineCacheKeys <= DateTime.UtcNow)
            {
                foundCacheSanctions = false;

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has extracted multi part string name value as {multiPartStringValue} cache is not available because of expiration.");
                }
            }
            else
            {
                foundCacheSanctions = true;

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has extracted multi part string name value as {multiPartStringValue} cache is available.");
                }
            }
            return foundCacheSanctions;
        }

        private static async Task<CacheSanction> LookupFromCacheAsync(Context context,
            CacheService cacheService, string multiPartStringValue, EntityAnalysisModelSanction entityAnalysisModelSanction)
        {

            var sanction = await cacheService.CacheSanctionRepository
                .GetByMultiPartStringDistanceThresholdAsync(
                    context.EntityAnalysisModel.Instance.TenantRegistryId,
                    context.EntityAnalysisModel.Instance.Guid, multiPartStringValue,
                    entityAnalysisModelSanction.Distance
                ).ConfigureAwait(false);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has extracted multi part string name value as {multiPartStringValue} and has found sanction as {sanction != null}.");
            }
            return sanction;
        }
    }
}
