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
    using System.Diagnostics;
    using Cryptography;
    using Data.Poco;
    using ReflectionHelpers;
    using EntityAnalysisModelInlineFunction=EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelInlineFunction;

    public static class InlineFunctionsExtensions
    {
        public static Context ExecuteInlineFunctions(this Context context)
        {
            try
            {
                IterateAndProcess(context);
                FinaliseStopwatchValues(context);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel} has experienced an error invoking inline functions as {ex}.");
                }
            }

            return context;
        }

        private static void FinaliseStopwatchValues(Context context)
        {

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.ComputeTimes.InlineFunction = (int)(context.Stopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency);

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel} has passed inline functions.");
            }
        }

        private static void IterateAndProcess(Context context)
        {
            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel} is going to check for inline functions.");
            }

            foreach (var inlineFunction in context.EntityAnalysisModel.Collections.EntityAnalysisModelInlineFunctions)
            {
                if (context.Log.IsInfoEnabled)
                {
                    context.Log.Info(
                        $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel} is going to invoke inline function {inlineFunction.Id}.");
                }

                try
                {
                    var output = ReflectRuleHelper.Execute(inlineFunction, context.EntityAnalysisModel,
                        context.EntityAnalysisModelInstanceEntryPayload,
                        context.EntityAnalysisModelInstanceEntryPayload.Dictionary, context.Log);

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel} is has invoked inline function {inlineFunction.Id} and returned a value of {output}.");
                    }

                    PopulateAllValues(context, inlineFunction, output);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel} is has invoked inline function {inlineFunction.Id} has created an error as {ex}.");
                    }
                }
            }
        }

        private static void PopulateAllValues(Context context, EntityAnalysisModelInlineFunction inlineFunction, object output)
        {
            if (inlineFunction.ReturnDataTypeId == 1 && output != null)
            {
                output = inlineFunction.EncryptionId switch
                {
                    1 => context.EntityAnalysisModel.Services.AesEncryption.Encrypt(output.ToString() ?? "", IvMode.Deterministic),
                    2 => context.EntityAnalysisModel.Services.AesEncryption.Encrypt(output.ToString() ?? "", IvMode.Random),
                    _ => output
                };
            }

            var writtenToPayload = PopulateCachePayloadDocumentStore(context, inlineFunction, output);

            if (inlineFunction.ReturnDataTypeId == 1 && writtenToPayload)
            {
                context.EntityAnalysisModel.ResolveDictionaryValueForField(context.EntityAnalysisModelInstanceEntryPayload, context.Log, inlineFunction.Name);
            }

            if (inlineFunction.ReportTable)
            {
                PopulateArchiveKeys(context, inlineFunction, output);
            }
        }

        private static void PopulateArchiveKeys(Context context, EntityAnalysisModelInlineFunction inlineFunction, object output)
        {

            switch (inlineFunction.ReturnDataTypeId)
            {
                case 1:
                    context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                    {
                        ProcessingTypeId = 3,
                        Key = inlineFunction.Name,
                        KeyValueString = output == null ? null : Convert.ToString(output),
                        EntityAnalysisModelInstanceEntryGuid =
                            context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                    });

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel} is has invoked inline function {inlineFunction.Id} but has added to report payload as name {inlineFunction.Name} with value of {output} as string.");
                    }

                    break;
                case 2:
                    if (output != null)
                    {
                        context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                        {
                            ProcessingTypeId = 3,
                            Key = inlineFunction.Name,
                            KeyValueInteger = (int)output,
                            EntityAnalysisModelInstanceEntryGuid =
                                context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                        });

                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel} is has invoked inline function {inlineFunction.Id} but has added to report payload as name {inlineFunction.Name} with value of {output} as integer.");
                        }
                    }

                    break;
                case 3:
                    context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                    {
                        ProcessingTypeId = 3,
                        Key = inlineFunction.Name,
                        KeyValueFloat = Convert.ToDouble(output),
                        EntityAnalysisModelInstanceEntryGuid =
                            context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                    });

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel} is has invoked inline function {inlineFunction.Id} but has added to report payload as name {inlineFunction.Name} with value of {output} as double.");
                    }

                    break;
                case 4:
                    context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                    {
                        ProcessingTypeId = 3,
                        Key = inlineFunction.Name,
                        KeyValueDate = DateTime.SpecifyKind(Convert.ToDateTime(output), DateTimeKind.Utc),
                        EntityAnalysisModelInstanceEntryGuid =
                            context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                    });

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel} is has invoked inline function {inlineFunction.Id} but has added to report payload as name {inlineFunction.Name} with value of {output} as date.");
                    }

                    break;
                case 5:
                    context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                    {
                        ProcessingTypeId = 3,
                        Key = inlineFunction.Name,
                        KeyValueBoolean = Convert.ToByte(output),
                        EntityAnalysisModelInstanceEntryGuid =
                            context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                    });

                    if (context.Log.IsInfoEnabled)
                    {
                        context.Log.Info(
                            $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel} is has invoked inline function {inlineFunction.Id} but has added to report payload as name {inlineFunction.Name} with value of {output} as boolean.");
                    }

                    break;
            }
        }

        private static bool PopulateCachePayloadDocumentStore(Context context, EntityAnalysisModelInlineFunction inlineFunction, object output)
        {

            if (!context.EntityAnalysisModelInstanceEntryPayload.Payload.ContainsKey(inlineFunction.Name))
            {
                switch (inlineFunction.ReturnDataTypeId)
                {
                    case 1:
                        context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd(inlineFunction.Name, output.ToString());

                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel} is has invoked inline function {inlineFunction.Id} but has added to payload as name {inlineFunction.Name} with value of {output} as string.");
                        }

                        break;
                    case 2:
                        context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd(inlineFunction.Name, Convert.ToInt32(output));

                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel} is has invoked inline function {inlineFunction.Id} but has added to payload as name {inlineFunction.Name} with value of {output} as integer.");
                        }

                        break;
                    case 3:
                        context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd(inlineFunction.Name, Convert.ToDouble(output));

                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel} is has invoked inline function {inlineFunction.Id} but has added to payload as name {inlineFunction.Name} with value of {output} as double.");
                        }

                        break;
                    case 4:
                        context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd(
                            inlineFunction.Name,
                            DateTime.SpecifyKind(Convert.ToDateTime(output), DateTimeKind.Utc));

                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel} is has invoked inline function {inlineFunction.Id} but has added to payload as name {inlineFunction.Name} with value of {output} as date.");
                        }

                        break;
                    case 5:
                        context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd(inlineFunction.Name, Convert.ToBoolean(output));

                        if (context.Log.IsInfoEnabled)
                        {
                            context.Log.Info(
                                $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel} is has invoked inline function {inlineFunction.Id} but has added to payload as name {inlineFunction.Name} with value of {output} as boolean.");
                        }

                        break;
                }

                return true;
            }

            if (context.Log.IsInfoEnabled)
            {
                context.Log.Info(
                    $"Entity Invoke: GUID {context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid} and model {context.EntityAnalysisModel} is has invoked inline function {inlineFunction.Id} but has not added to payload as name {inlineFunction.Name} already exists.");
            }

            return false;
        }
    }
}
