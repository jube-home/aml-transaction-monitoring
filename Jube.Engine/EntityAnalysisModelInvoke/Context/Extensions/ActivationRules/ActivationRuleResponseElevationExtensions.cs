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
    using System.Threading;
    using EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
    using Models.Payload.EntityAnalysisModelInstanceEntryPayload;

    public static class ActivationRuleResponseElevationExtensions
    {
        public static void ProcessResponseElevation(this Context context,
            EntityAnalysisModelActivationRule evaluateActivationRule, ref double responseElevationHighWaterMark,
            bool suppressed)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} " +
                    $"and model {context.EntityAnalysisModel.Instance.Id} will begin processing of response elevation for activation " +
                    $"rule {evaluateActivationRule.Id}. " +
                    $"Current high water mark on response elevation is {responseElevationHighWaterMark}.");
            }

            if (suppressed || !evaluateActivationRule.EnableResponseElevation)
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} response elevation for activation rule {evaluateActivationRule.Id} is suppressed or disabled and will not be evaluated.");
                }

                return;
            }

            if (responseElevationHighWaterMark > evaluateActivationRule.ResponseElevation)
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} response elevation for activation rule {evaluateActivationRule.Id} is less than the current largest Response Elevation {context.EntityAnalysisModelInstanceEntryPayload.ResponseElevation} which is {responseElevationHighWaterMark} and will not be processed.");
                }

                return;
            }

            responseElevationHighWaterMark = evaluateActivationRule.ResponseElevation;

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} response elevation for activation rule {evaluateActivationRule.Id} is the current largest Response Elevation {context.EntityAnalysisModelInstanceEntryPayload.ResponseElevation} but is less than the new value of {responseElevationHighWaterMark} so it will be elevated.  Will be tested against certain other model constraints.");
            }

            if (responseElevationHighWaterMark > context.EntityAnalysisModel.Counters.MaxResponseElevation)
            {
                context.EntityAnalysisModelInstanceEntryPayload.ResponseElevation.Value =
                    context.EntityAnalysisModel.Counters.MaxResponseElevation;

                Interlocked.Increment(ref context.EntityAnalysisModel.Counters.ResponseElevationValueLimitCounter);

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} the response elevation exceeds the maximum allowed in the model of {context.EntityAnalysisModel.Counters.MaxResponseElevation}, so has been truncated to {context.EntityAnalysisModel.Counters.MaxResponseElevation} and the Response Elevation Value Limit Counter incremented.");
                }
            }
            else
            {
                if (responseElevationHighWaterMark > context.EntityAnalysisModelInstanceEntryPayload.ResponseElevationLimit)
                {
                    context.EntityAnalysisModelInstanceEntryPayload.ResponseElevation.Value =
                        context.EntityAnalysisModelInstanceEntryPayload.ResponseElevationLimit;

                    Interlocked.Increment(ref context.EntityAnalysisModel.Counters.ResponseElevationFrequencyLimitCounter);

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} the response elevation exceeds the maximum allowed in the gateway rule of {context.EntityAnalysisModelInstanceEntryPayload.ResponseElevationLimit}, so has been truncated and the Response Elevation Value Gateway Limit counter incremented.");
                    }
                }
                else
                {
                    context.EntityAnalysisModelInstanceEntryPayload.ResponseElevation.Value =
                        responseElevationHighWaterMark;

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} the response elevation has tested the limits and the response elevation is being carried forward as {responseElevationHighWaterMark}.");
                    }
                }
            }

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} response elevation for activation rule {evaluateActivationRule.Id} is being tested against the current limits and cap to zero if exceeded.");
            }

            context.EntityAnalysisModelInstanceEntryPayload.ResponseElevation.Content =
                evaluateActivationRule.ResponseElevationContent;

            context.EntityAnalysisModelInstanceEntryPayload.ResponseElevation.Redirect =
                evaluateActivationRule.ResponseElevationRedirect;

            context.EntityAnalysisModelInstanceEntryPayload.ResponseElevation.BackColor =
                evaluateActivationRule.ResponseElevationBackColor;

            context.EntityAnalysisModelInstanceEntryPayload.ResponseElevation.ForeColor =
                evaluateActivationRule.ResponseElevationForeColor;

            context.EntityAnalysisModelInstanceEntryPayload.ResponseElevation.Content =
                evaluateActivationRule.ResponseElevationContent;

            context.EntityAnalysisModelInstanceEntryPayload.ResponseElevation.Redirect =
                evaluateActivationRule.ResponseElevationRedirect;

            context.EntityAnalysisModelInstanceEntryPayload.ResponseElevation.ForeColor =
                evaluateActivationRule.ResponseElevationForeColor;

            context.EntityAnalysisModelInstanceEntryPayload.ResponseElevation.BackColor =
                evaluateActivationRule.ResponseElevationBackColor;

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} response elevation for activation rule {evaluateActivationRule.Id} updated the response elevation to {context.EntityAnalysisModelInstanceEntryPayload.ResponseElevation}.");
            }

            if (context.EntityAnalysisModel.Flags.EnableResponseElevationLimit)
            {
                context.EntityAnalysisModel.ConcurrentQueues.ResponseElevationEntries.Enqueue(new ResponseElevation
                {
                    CreatedDate = DateTime.Now,
                    Value = context.EntityAnalysisModelInstanceEntryPayload.ResponseElevation.Value
                });

                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} has noted the response elevation date on the counter queue. There are {context.EntityAnalysisModel.ConcurrentQueues.ActivationWatcherCountJournal.Count} in queue.");
                }
            }
            else
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel.Instance.Id} does not have response elevation limit enabled,  so has not noted the response elevation date.");
                }
            }
        }
    }
}
