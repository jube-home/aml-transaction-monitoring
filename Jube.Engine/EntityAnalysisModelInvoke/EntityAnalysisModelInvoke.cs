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

namespace Jube.Engine.EntityAnalysisModelInvoke
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Context.Extensions;
    using Dictionary;
    using Exceptions;
    using Extraction;
    using Models.Payload.AsyncInvocationCallbackToken;
    using Models.Payload.EntityAnalysisModelInstanceEntryPayload;
    using TaskCancellation.TaskHelper;
    using EntityAnalysisModel=EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

    public static class EntityAnalysisModelInvoke
    {
        public static async Task<Context.Context> InvokeAsync(EntityAnalysisModel entityAnalysisModel,
            MemoryStream inputStream, int? maxBytes = null,
            bool async = false)
        {
            if (maxBytes.HasValue)
            {
                CheckLengthGuardsForExceptions(inputStream, maxBytes.Value);
            }

            var extractor = new EntityAnalysisModelJsonExtractor(entityAnalysisModel, entityAnalysisModel.Dependencies.ActiveEntityAnalysisModels, entityAnalysisModel.Services.JubeEnvironment, entityAnalysisModel.Services.Log);
            var context = extractor.CreateContext(inputStream);
            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.Parse = (int)(context.Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);
            await CheckAsyncAndInvokeContextAsync(async, context).ConfigureAwait(false);

            return context;
        }

        public static Task InvokeAsync(EntityAnalysisModel entityAnalysisModel, DictionaryNoBoxing<string> dictionaryNoBoxing, int entityAnalysisModelReprocessingRuleInstanceId)
        {
            var extractor = new EntityAnalysisModelDictionaryNoBoxingExtractor(entityAnalysisModel, entityAnalysisModel.Dependencies.ActiveEntityAnalysisModels, entityAnalysisModel.Services.JubeEnvironment, entityAnalysisModel.Services.Log);
            var context = extractor.CreateContext(dictionaryNoBoxing, entityAnalysisModelReprocessingRuleInstanceId);
            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.Parse = (int)(context.Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);
            return InvokeAsync(context);

        }

        private static async Task CheckAsyncAndInvokeContextAsync(bool async, Context.Context context)
        {
            if (async && context.EntityAnalysisModel.ConcurrentQueues.PendingEntityInvoke != null)
            {
                context.EntityAnalysisModel.Services.Log.Info(
                    $"HTTP Handler Entity: GUID payload {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} model id is {context.EntityAnalysisModel.Instance.Id} will start asynchronous invocation.");

                EnqueueForAsyncInvocationOfContext(context, context.EntityAnalysisModel.ConcurrentQueues.PendingEntityInvoke);
            }
            else
            {
                if (context.EntityAnalysisModel.Services.Log.IsInfoEnabled)
                {
                    context.EntityAnalysisModel.Services.Log.Info(
                        $"HTTP Handler Entity: GUID payload {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} model id is {context.EntityAnalysisModel.Instance.Id} will start synchronous invocation.");
                }

                await InvokeAsync(context).ConfigureAwait(false);
            }

            if (context.EntityAnalysisModel.Services.Log.IsInfoEnabled)
            {
                context.EntityAnalysisModel.Services.Log.Info(
                    $"HTTP Handler Entity: GUID payload {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} model id is {context.EntityAnalysisModel.Instance.Id} has finished model invocation.");
            }
        }

        private static void CheckLengthGuardsForExceptions(MemoryStream inputStream, int maxBytes)
        {
            if (inputStream.Length == 0)
            {
                throw new ZeroBytesException();
            }

            if (inputStream.Length > maxBytes)
            {
                throw new ExceededBytesException();
            }
        }

        private static void EnqueueForAsyncInvocationOfContext(Context.Context context, ConcurrentQueue<Context.Context> pendingEntityInvoke)
        {
            if (pendingEntityInvoke.Count >=
                Int32.Parse(context.EntityAnalysisModel.Services.JubeEnvironment.AppSettings("MaximumModelInvokeAsyncQueue")))
            {
                throw new ExceededQueueLengthException();
            }

            context.Async = true;
            pendingEntityInvoke.Enqueue(context);

            var asyncInvocationCallbackToken = new AsyncInvocationCallbackToken
            {
                EntityAnalysisModelInstanceEntryGuid = context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
            };

            context.EntityAnalysisModelInstanceEntryPayload.ResponseJson = BuildJsonResponses.BuildFullJson(asyncInvocationCallbackToken, context.EntityAnalysisModel.JsonSerializationHelper.ArchiveJsonSerializer);
        }

        public static async Task InvokeAsync(Context.Context context)
        {
            try
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} has started invocation timer. " +
                        $"Will now update the reference date and check that it is not older than the latest stored, else exception.");
                }

                IncrementModelInvokeCounter(context);

                await context.CheckIntegrityAndUpsertAsync(context.EntityAnalysisModel.Services.CacheService).ConfigureAwait(false);

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} " +
                        $"has configured startup options.  The model invocation counter is {context.EntityAnalysisModel.Counters.ModelInvokeCounter} and " +
                        $"reprocessing is set to {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelReprocessingRuleInstanceId.HasValue}.  Will now proceed" +
                        $" create the Invoke Context and execute Inline Functions. " +
                        $"Has updated reference date for model {context.EntityAnalysisModel.Instance.Id} to {context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate}");
                }

                context.ExecuteInlineFunctions();

                await context.ExecuteInlineScriptsAsync();

                context.ExecuteGatewayRules();

                if (context.EntityAnalysisModelInstanceEntryPayload.MatchedGatewayRule)
                {
                    context.ExecuteCacheDbStorage(context.EntityAnalysisModel.Services.CacheService, context.EntityAnalysisModel.Collections.DistinctSearchKeys);
                    context.PendingReadTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.SanctionsAsync, async () => await context.ExecuteSanctionsAsync().ConfigureAwait(false)));
                    context.PendingReadTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.TtlCountersAsync, async () => await context.ExecuteTtlCountersAsync().ConfigureAwait(false)));
                    context.PendingReadTasks.Add(TaskHelper.MeasureTaskTimeAndMemoryAllocatedAsync(TaskType.AbstractionRulesWithSearchKeysAsync, async () => await context.ExecuteAbstractionRulesWithSearchKeysAsync().ConfigureAwait(false)));

                    await context.WaitReadTasksAsync().ConfigureAwait(false);

                    context.ExecuteAbstractionRulesWithoutSearchKeys();
                    context.ExecuteAbstractionCalculations();
                    context.ExecuteExhaustiveAdaptation();

                    await context.ExecuteHttpAdaptationsAsync().ConfigureAwait(false);

                    await context.ExecuteActivationsAsync().ConfigureAwait(false);
                }

                await context.WaitWriteTasksAsync().ConfigureAwait(false);
                await context.WriteResponseJsonAndQueueAsynchronousResponseMessageAsync().ConfigureAwait(false);
                await context.ActivationRuleBuildArchivePayloadAsync().ConfigureAwait(false);

                Interlocked.Add(ref context.EntityAnalysisModel.Counters.ModelTotalResponseTime, (int)(context.Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency));

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} all model invocation processing has completed.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException
                                       && ex is not ReferenceDateInFutureException
                                       && ex is not ExceededQueueLengthException
                                       && ex is not ZeroBytesException
                                       && ex is not ExceededBytesException)
            {
                context.Log.Error(
                    $"Entity Invoke: {context.EntityAnalysisModel.Instance.Id} has created a general error as {ex}.");
            }
        }

        private static void IncrementModelInvokeCounter(Context.Context context)
        {
            Interlocked.Increment(ref context.EntityAnalysisModel.Counters.ModelInvokeCounter);
        }
    }
}
