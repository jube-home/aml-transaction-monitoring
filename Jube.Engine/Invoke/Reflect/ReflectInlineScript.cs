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

namespace Jube.Engine.Invoke.Reflect
{
    using System;
    using Dictionary;
    using log4net;
    using Model;

    public static class ReflectInlineScript
    {
        public static void Execute(EntityAnalysisModelInlineScript entityAnalysisModelInlineScript,
            ref DictionaryNoBoxing dataPayload,
            ref DictionaryNoBoxing responsePayload, ILog log)
        {
            object[] args = [dataPayload, log];
            entityAnalysisModelInlineScript.PreProcessingMethodInfo.Invoke(entityAnalysisModelInlineScript.ActivatedObject,
                args);

            foreach (var p in entityAnalysisModelInlineScript.InlineScriptType.GetProperties())
            {
                try
                {
                    if (entityAnalysisModelInlineScript.ActivatedObject == null)
                    {
                        continue;
                    }

                    foreach (var customAttributeData in p.CustomAttributes)
                    {
                        if (customAttributeData.AttributeType.Name.Contains("Latitude"))
                        {
                            if (!dataPayload.ContainsKey(p.Name))
                            {
                                dataPayload.TryAdd("Latitude", dataPayload[p.Name].AsDouble());
                            }
                        }
                        else if (customAttributeData.AttributeType.Name.Contains("Longitude"))
                        {
                            if (!dataPayload.ContainsKey(p.Name))
                            {
                                dataPayload.TryAdd("Longitude", dataPayload[p.Name].AsDouble());
                            }
                        }
                        else if (customAttributeData.AttributeType.Name.Contains("ResponsePayload"))
                        {
                            if (responsePayload.ContainsKey(p.Name))
                            {
                                continue;
                            }
                            
                            if (p.PropertyType == typeof(int))
                            {
                                responsePayload.TryAdd(p.Name, dataPayload[p.Name].AsInt());
                            }

                            else if (p.PropertyType == typeof(string))
                            {
                                responsePayload.TryAdd(p.Name, dataPayload[p.Name].AsString());
                            }

                            else if (p.PropertyType == typeof(double))
                            {
                                responsePayload.TryAdd(p.Name, dataPayload[p.Name].AsDouble());
                            }

                            else if (p.PropertyType == typeof(DateTime))
                            {
                                responsePayload.TryAdd(p.Name, dataPayload[p.Name].AsDateTime());
                            }

                            else if (p.PropertyType == typeof(bool))
                            {
                                responsePayload.TryAdd(p.Name, dataPayload[p.Name].AsBool());
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.Error(ex.ToString());
                }
            }
        }
    }
}
