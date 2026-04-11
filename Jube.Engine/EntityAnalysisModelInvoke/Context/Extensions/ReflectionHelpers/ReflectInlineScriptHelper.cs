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

namespace Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.ReflectionHelpers
{
    using System;
    using System.Threading.Tasks;
    using Data.Poco;
    using EntityAnalysisModelInlineScript=EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelInlineScript;

    public static class ReflectInlineScriptHelper
    {
        public static async Task<bool> ExecuteAsync(EntityAnalysisModelInlineScript.EntityAnalysisModelInlineScript entityAnalysisModelInlineScript, Context context)
        {
            var instance = entityAnalysisModelInlineScript.ActivatorDelegate();

            try
            {
                if (!await entityAnalysisModelInlineScript.ExecuteAsyncDelegate(instance, context))
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                context.Log.Info($"Error executing Inline Script {entityAnalysisModelInlineScript.Id}:", ex);
                return true;
            }

            foreach (var prop in entityAnalysisModelInlineScript.EntityAnalysisModelInlineScriptPropertyAttributes)
            {
                try
                {
                    if (instance == null)
                    {
                        continue;
                    }

                    var value = prop.Value.GetValueDelegate(instance);

                    var underlyingType = Nullable.GetUnderlyingType(prop.Value.PropertyType);
                    var isNullable = underlyingType != null;
                    var effectiveType = underlyingType ?? prop.Value.PropertyType;
                    
                    if (isNullable && value == null)
                    {
                        continue;
                    }

                    if (effectiveType != typeof(string)
                        && effectiveType != typeof(int)
                        && effectiveType != typeof(bool)
                        && effectiveType != typeof(DateTime)
                        && effectiveType != typeof(double))
                    {
                        continue;
                    }

                    switch (value)
                    {
                        case string s:
                            context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd(prop.Key, s);

                            if (prop.Value.ReportTable)
                            {
                                context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                                {
                                    ProcessingTypeId = 1,
                                    Key = prop.Key,
                                    KeyValueString = value == null ? null : Convert.ToString(value),
                                    EntityAnalysisModelInstanceEntryGuid =
                                        context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                                });
                            }

                            break;

                        case int i:
                            context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd(prop.Key, i);

                            if (prop.Value.ReportTable)
                            {
                                context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                                {
                                    ProcessingTypeId = 1,
                                    Key = prop.Key,
                                    KeyValueInteger = i,
                                    EntityAnalysisModelInstanceEntryGuid =
                                        context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                                });
                            }

                            break;

                        case byte b:
                            context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd(prop.Key, b);

                            if (prop.Value.ReportTable)
                            {
                                context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                                {
                                    ProcessingTypeId = 1,
                                    Key = prop.Key,
                                    KeyValueBoolean = b,
                                    EntityAnalysisModelInstanceEntryGuid =
                                        context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                                });
                            }

                            break;

                        case double d:
                            context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd(prop.Key, d);

                            if (prop.Value.ReportTable)
                            {
                                context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                                {
                                    ProcessingTypeId = 1,
                                    Key = prop.Key,
                                    KeyValueFloat = d,
                                    EntityAnalysisModelInstanceEntryGuid =
                                        context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                                });
                            }

                            break;

                        case DateTime dt:
                            context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd(prop.Key, dt);

                            if (prop.Value.ReportTable)
                            {
                                context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                                {
                                    ProcessingTypeId = 1,
                                    Key = prop.Key,
                                    KeyValueDate = dt,
                                    EntityAnalysisModelInstanceEntryGuid =
                                        context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                                });
                            }

                            break;

                        default:
                            context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd(prop.Key, value.ToString());

                            if (prop.Value.ReportTable)
                            {
                                context.EntityAnalysisModelInstanceEntryPayload.ArchiveKeys.Add(new ArchiveKey
                                {
                                    ProcessingTypeId = 1,
                                    Key = prop.Key,
                                    KeyValueString = value.ToString(),
                                    EntityAnalysisModelInstanceEntryGuid =
                                        context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid
                                });
                            }

                            break;
                    }
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    context.Log.Error(ex.ToString());
                }
            }

            return true;
        }
    }
}
