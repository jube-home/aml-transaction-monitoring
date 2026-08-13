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

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.ActivationRules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Cache;
    using EntityAnalysisModelManager.EntityAnalysisModel;
    using EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
    using Jube.Extensions;
    using TaskCancellation.TaskHelper;

    public static class ActivationRuleTtlCounterExtensions
    {
        public static async Task ActivationRuleTtlCounterAsync(this Context context,
            EntityAnalysisModelActivationRule evaluateActivationRule,
            Dictionary<int, EntityAnalysisModel> availableModels, CacheService cacheService)
        {
            if (!evaluateActivationRule.EnableTtlCounter || context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelReprocessingRuleInstanceId.HasValue)
            {
                return;
            }

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} is incrementing TTL counter {evaluateActivationRule.EntityAnalysisModelTtlCounterGuid} as this is enabled in the activation rule.");
            }

            var found = false;
            foreach (var (_, value) in
                     from targetTtlCounterModelKvp in availableModels
                     where evaluateActivationRule.EntityAnalysisModelGuidTtlCounter ==
                           targetTtlCounterModelKvp.Value.Instance.Guid
                     select targetTtlCounterModelKvp)
            {
                var addedEntityAnalysisModelTtlCounters = new List<Guid>();
                foreach (var foundTtlCounter in value.Collections.ModelTtlCounters)
                {
                    if (evaluateActivationRule.EntityAnalysisModelTtlCounterGuid == foundTtlCounter.Guid)
                    {
                        try
                        {
                            if (context.Log.IsInfoEnabled)
                            {
                                context.Log.Info(
                                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has matched the name in the activation rule to the TTL counters loaded for {context.EntityAnalysisModel.Instance.Name} in model id {value.Instance.Id}.");
                            }

                            if (context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey(foundTtlCounter.TtlCounterDataName))
                            {
                                if (context.Log.IsInfoEnabled)
                                {
                                    context.Log.Info(
                                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} found a value a value for TTL counter name {foundTtlCounter.Name} as {context.EntityAnalysisModelInstanceEntryPayload.Payload[foundTtlCounter.TtlCounterDataName]}.");
                                }

                                if (value.Flags.EnableTtlCounter)
                                {
                                    if (evaluateActivationRule.EntityAnalysisModelGuidTtlCounter == value.Instance.Guid)
                                    {
                                        if (addedEntityAnalysisModelTtlCounters.Contains(foundTtlCounter.Guid))
                                        {
                                            context.Log.Info(
                                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has built a TTL Counter insert payload of TTLCounterName as {foundTtlCounter.Name}, TTLCounterDataName as {foundTtlCounter.TtlCounterDataName} and TTLCounterDataNameValue as {context.EntityAnalysisModelInstanceEntryPayload.Payload[foundTtlCounter.TtlCounterDataName]} can only be incremented once during an evaluation of an activation rule.");

                                            continue;
                                        }

                                        if (context.Environment.AppSettings("ActivationRuleIdempotency").Equals("True", StringComparison.OrdinalIgnoreCase))
                                        {
                                            if (!await cacheService.CacheTtlCounterIdempotencyRepository.CheckAndClaimIdempotencyAsync(context.EntityAnalysisModel.Instance.TenantRegistryId,
                                                    context.EntityAnalysisModel.Instance.Guid,
                                                    context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid,
                                                    foundTtlCounter.Guid))
                                            {
                                                if (context.Log.IsInfoEnabled)
                                                {
                                                    context.Log.Info(
                                                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has built a TTL Counter insert payload of TTLCounterName as {foundTtlCounter.Name}, TTLCounterDataName as {foundTtlCounter.TtlCounterDataName} and TTLCounterDataNameValue as {context.EntityAnalysisModelInstanceEntryPayload.Payload[foundTtlCounter.TtlCounterDataName]} but failed idempotency check.");
                                                }

                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            if (context.Log.IsInfoEnabled)
                                            {
                                                context.Log.Info(
                                                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has built a TTL Counter insert payload of TTLCounterName as {foundTtlCounter.Name}, TTLCounterDataName as {foundTtlCounter.TtlCounterDataName} and TTLCounterDataNameValue as {context.EntityAnalysisModelInstanceEntryPayload.Payload[foundTtlCounter.TtlCounterDataName]} won't check ActivationRuleIdempotency.");
                                            }
                                        }

                                        if (context.Log.IsInfoEnabled)
                                        {
                                            context.Log.Info(
                                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has built a TTL Counter insert payload of TTLCounterName as {foundTtlCounter.Name}, TTLCounterDataName as {foundTtlCounter.TtlCounterDataName} and TTLCounterDataNameValue as {context.EntityAnalysisModelInstanceEntryPayload.Payload[foundTtlCounter.TtlCounterDataName]}.  Is about to insert the entry.");
                                        }

                                        double incrementValue = 1;
                                        if (foundTtlCounter.EnableSum)
                                        {
                                            incrementValue = context.EntityAnalysisModelInstanceEntryPayload.Payload[foundTtlCounter.TtlCounterDataValue];

                                            if (context.Log.IsInfoEnabled)
                                            {
                                                context.Log.Info(
                                                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has built a TTL Counter insert payload of TTLCounterName as {foundTtlCounter.Name}, TTLCounterDataName as {foundTtlCounter.TtlCounterDataName} and TTLCounterDataNameValue as {context.EntityAnalysisModelInstanceEntryPayload.Payload[foundTtlCounter.TtlCounterDataName]}.  Has incremented based on sum for value {foundTtlCounter.TtlCounterDataValue} with a increment value of {incrementValue}.");
                                            }
                                        }

                                        if (!foundTtlCounter.EnableLiveForever)
                                        {
                                            var resolution = foundTtlCounter.ResolutionInterval switch
                                            {
                                                "n" => context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate.Floor(TimeSpan.FromMinutes(1)),
                                                "h" => context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate.Floor(TimeSpan.FromHours(1)),
                                                "d" => context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate.Floor(TimeSpan.FromDays(1)),
                                                _ => context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate.Floor(TimeSpan.FromMinutes(1))
                                            };

                                            if (context.Log.IsInfoEnabled)
                                            {
                                                context.Log.Info(
                                                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has built a TTL Counter insert payload of TTLCounterName as {foundTtlCounter.Name}, TTLCounterDataName as {foundTtlCounter.TtlCounterDataName} and TTLCounterDataNameValue as {context.EntityAnalysisModelInstanceEntryPayload.Payload[foundTtlCounter.TtlCounterDataName]}.  Is about to insert the entry with a resolution of {resolution}.");
                                            }

                                            context.PendingWriteTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.CacheTtlCounterEntryUpsertAsync, async () => await cacheService.CacheTtlCounterEntryRepository.UpsertAsync(
                                                context.EntityAnalysisModel.Instance.TenantRegistryId, evaluateActivationRule.EntityAnalysisModelGuidTtlCounter,
                                                foundTtlCounter.TtlCounterDataName,
                                                context.EntityAnalysisModelInstanceEntryPayload.Payload[foundTtlCounter.TtlCounterDataName]
                                                    .AsString(),
                                                foundTtlCounter.Guid,
                                                resolution, incrementValue).ConfigureAwait(false)));
                                        }
                                        else
                                        {
                                            if (context.Log.IsInfoEnabled)
                                            {
                                                context.Log.Info(
                                                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has built a TTL Counter insert payload of TTLCounterName as {foundTtlCounter.Name}, TTLCounterDataName as {foundTtlCounter.TtlCounterDataName} and TTLCounterDataNameValue as {context.EntityAnalysisModelInstanceEntryPayload.Payload[foundTtlCounter.TtlCounterDataName]} is set to live forever so no entry has been made to wind back counters.");
                                            }
                                        }

                                        if (!context.EntityAnalysisModelInstanceEntryPayload.Payload.TryGetValue(foundTtlCounter.TtlCounterDataName, out var payloadValue))
                                        {
                                            continue;
                                        }

                                        context.PendingWriteTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.CacheTtlCounterEntryIncrementAsync, async () => await cacheService.CacheTtlCounterRepository
                                            .IncrementTtlCounterCacheAsync(context.EntityAnalysisModel.Instance.TenantRegistryId,
                                                context.EntityAnalysisModel.Instance.Guid,
                                                foundTtlCounter.TtlCounterDataName,
                                                payloadValue.AsString(),
                                                foundTtlCounter.Guid, incrementValue,
                                                context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate
                                            ).ConfigureAwait(false)));

                                        if (context.EntityAnalysisModelInstanceEntryPayload.TtlCounter.TryGetValue(foundTtlCounter.Name, out var ttlCounterValue))
                                        {
                                            ttlCounterValue += incrementValue;
                                            context.EntityAnalysisModelInstanceEntryPayload.TtlCounter[foundTtlCounter.Name] = ttlCounterValue;
                                        }
                                        else
                                        {
                                            context.EntityAnalysisModelInstanceEntryPayload.TtlCounter.Add(foundTtlCounter.Name, incrementValue);
                                        }
                                    }
                                }
                                else
                                {
                                    if (context.Log.IsInfoEnabled)
                                    {
                                        context.Log.Info(
                                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} cannot create a TTL counter for name {value.Instance.Name} as TTL Counter Storage is disabled for the model id {value.Instance.Id}.");
                                    }
                                }

                                addedEntityAnalysisModelTtlCounters.Add(foundTtlCounter.Guid);
                            }
                            else
                            {
                                if (context.Log.IsInfoEnabled)
                                {
                                    context.Log.Info(
                                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} could not find a value for TTL counter name {foundTtlCounter.Name}.");
                                }
                            }
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            if (context.Log.IsInfoEnabled)
                            {
                                context.Log.Info(
                                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} error performing insertion on match for a TTL Counter by name of {foundTtlCounter.Name} and id of {foundTtlCounter.Id} with exception message of {ex.Message}.");
                            }
                        }

                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has matched the name in the activation rule to the TTL counters loaded for {context.EntityAnalysisModel.Instance.Name} and has finished processing.");
                        }

                        found = true;
                    }

                    if (found)
                    {
                        break;
                    }
                }

                if (found)
                {
                    break;
                }
            }
        }
    }
}
