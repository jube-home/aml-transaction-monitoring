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

namespace Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading.Tasks;
    using Attributes.Events;
    using Attributes.Properties;
    using Data.Repository;
    using Interfaces;
    using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
    using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelInlineScript;
    using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelInlineScript.EntityAnalysisModelInlineScriptPropertyAttribute;
    using Jube.Engine.EntityAnalysisModelManager.Helpers;
    using Jube.Engine.Models;
    using Parser.Compiler;

    public static class SyncEntityAnalysisInlineScriptsExtensions
    {
        public static async Task<Context> SyncEntityAnalysisInlineScriptAsync(this Context context)
        {
            if (context.Services.Log.IsDebugEnabled)
            {
                context.Services.Log.Debug("Entity Start: Getting all Inline Scripts from Database.");
            }

            var repository = new EntityAnalysisInlineScriptRepository(context.Services.DbContext);

            if (context.Services.Log.IsDebugEnabled)
            {
                context.Services.Log.Debug(
                    "Entity Start: Executing EntityAnalysisInlineScriptRepository.Get.");
            }

            var records = await repository.GetAsync();

            foreach (var record in records)
            {
                try
                {
                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Found an inline script with the id of {record.Id} and will proceed to check if already have this inline script available.");
                    }

                    var inlineScript = context.EntityAnalysisModels.EntityAnalysisModelInlineScripts.Find(x => x.Id == record.Id);
                    if (inlineScript == null)
                    {
                        inlineScript = new EntityAnalysisModelInlineScript();

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Have not found an inline script in the available inline scripts, with the id of {record.Id} hence a new one will be created.");
                        }
                    }
                    else
                    {
                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Found an inline script in the available inline scripts, with the id of {record.Id} and this will be used.");
                        }
                    }

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Found an inline script {record.Id} has Created Date: {inlineScript.CreatedDate.HasValue} of {inlineScript.CreatedDate}. A check will be made to see if it has changed recently");
                    }

                    if ((!inlineScript.CreatedDate.HasValue ||
                         !(DateTime.SpecifyKind(Convert.ToDateTime(record.CreatedDate), DateTimeKind.Utc) > inlineScript.CreatedDate)) &&
                        inlineScript.CreatedDate.HasValue)
                    {
                        continue;
                    }

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Inline Script {record.Id} has changed recently or is new,  setting created date.");
                    }

                    inlineScript.CreatedDate = record.CreatedDate;
                    inlineScript.Id = record.Id;
                    inlineScript.InlineScriptCode = record.Code;
                    inlineScript.LanguageId = record.LanguageId ?? 1;

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Inline Script {record.Id} has rule script of {inlineScript.InlineScriptCode}.");
                    }

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Inline Script {record.Id} has method specification of {inlineScript.ClassName}.");
                    }

                    inlineScript.Name = record.Name;

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug($"Entity Start: Inline Script {record.Id} has name of {inlineScript.Name}.");
                    }

                    var dependencyArray = BuildDependencyArray(context, inlineScript.Dependencies);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Inline Script {record.Id} is being checked for dll dependencies.  Has created {dependencyArray.Length} dependencies.");
                    }

                    var inlineScriptHash = HashHelper.GetHash(inlineScript.InlineScriptCode);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Inline Script {record.Id} has been hashed to {inlineScriptHash} and the hash cache will now be checked.");
                    }

                    if (context.Caching.HashCacheAssembly.TryGetValue(inlineScriptHash, out var value))
                    {
                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has been hashed to {inlineScriptHash} and has been located in the hash cache,  this will be used.  Creating a delegate.");
                        }

                        inlineScript.InlineScriptCompile = value;
                        SetupInlineScriptDelegates(inlineScript);
                        SetupEvents(inlineScript);

                        await repository.UpdateCompileStatusAsync(record.Id, true, null,
                            context.Services.CancellationToken).ConfigureAwait(false);

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has been hashed to {inlineScriptHash} and has been located in the hash cache and allocated to a delegate.");
                        }
                    }
                    else
                    {
                        bool compiled;

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has been hashed to {inlineScriptHash} but has not been located in the hash cache.  The inline script will now be compiled.");
                        }

                        var compile = new Compile();
                        compile.CompileCode(inlineScript.InlineScriptCode, context.Services.Log,
                            dependencyArray,
                            inlineScript.LanguageId == 2 ? Compile.Language.CSharp : Compile.Language.Vb);

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has been compiled with {compile.Errors} errors.");
                        }

                        if (compile.Errors == null)
                        {
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Inline Script {record.Id} has been compiled with no errors and will now be allocated to a delegate.");
                            }

                            inlineScript.InlineScriptCompile = compile.CompiledAssembly;

                            var interfaceType = typeof(IInlineScript);
                            var implementations = inlineScript.InlineScriptCompile.GetExportedTypes()
                                .Where(t => interfaceType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

                            foreach (var type in implementations)
                            {
                                inlineScript.ClassName = type.FullName;
                                break;
                            }

                            inlineScript.InlineScriptType =
                                inlineScript.InlineScriptCompile.GetType(inlineScript.ClassName);

                            if (inlineScript.InlineScriptType == null)
                            {
                                context.Services.Log.Error(
                                    $"Entity Start: Could not compile inline script: {inlineScript.Id} did not fine class entry point for {inlineScript.ClassName}.");

                                continue;
                            }

                            SetupInlineScriptDelegates(inlineScript);
                            SetupEvents(inlineScript);

                            context.EntityAnalysisModels.EntityAnalysisModelInlineScripts.Add(inlineScript);
                            context.Caching.HashCacheAssembly.TryAdd(inlineScriptHash, compile.CompiledAssembly);
                            context.Caching.HashCacheAssemblyMetadata.TryAdd(inlineScriptHash,
                                new HashCacheAssemblyPayload(compile.CompiledAssemblyBytes, compile.CompiledAssemblyBinary, inlineScript.InlineScriptCode));
                            compiled = true;

                            await repository.UpdateCompileStatusAsync(record.Id, true, null,
                                context.Services.CancellationToken).ConfigureAwait(false);

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Inline Script {record.Id} has been compiled and allocated to a delegate.");
                            }
                        }
                        else
                        {
                            foreach (var error in compile.Errors)
                            {
                                context.Services.Log.Error(
                                    $"Entity Start: Could not compile inline script: {inlineScript.Id} with error: {error.ToString()}.");
                            }

                            await repository.UpdateCompileStatusAsync(record.Id, false, compile.ErrorsSummary,
                                context.Services.CancellationToken).ConfigureAwait(false);

                            compiled = false;
                        }

                        if (!compiled)
                        {
                            continue;
                        }

                        if (context.Services.Log.IsDebugEnabled)
                        {
                            context.Services.Log.Debug(
                                $"Entity Start: Inline Script {record.Id} has been compiled will now proceed to inspect the properties exposed by the class.");
                        }

                        foreach (var p in inlineScript.InlineScriptType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                        {
                            var entityAnalysisModelInlineScriptProperty = new EntityAnalysisModelInlineScriptPropertyAttribute
                            {
                                Name = p.Name,
                                ReportTable = p.GetCustomAttribute<ReportTable>() != null,
                                Latitude = p.GetCustomAttribute<Latitude>() != null,
                                Longitude = p.GetCustomAttribute<Longitude>() != null,
                                ResponsePayload = p.GetCustomAttribute<ResponsePayload>() != null,
                                CacheIndexId = p.GetCustomAttribute<CacheIndex>()?.Id,
                                GetValueDelegate = CompileGetValueDelegate(p),
                                PropertyType = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType
                            };

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Inline Script {record.Id} is inspecting property {p.Name} and looking for custom attributes.");
                            }

                            var searchKeyAttribute = p.GetCustomAttribute<SearchKey>();

                            if (searchKeyAttribute != null)
                            {
                                var distinctSearchKey = new DistinctSearchKey
                                {
                                    SearchKey = p.Name,
                                    SearchKeyTtlInterval = searchKeyAttribute.SearchKeyTtlInterval,
                                    SearchKeyTtlIntervalValue = searchKeyAttribute.SearchKeyTtlIntervalValue,
                                    SearchKeyFetchLimit = searchKeyAttribute.SearchKeyFetchLimit,
                                    SearchKeyCache = searchKeyAttribute.SearchKeyCache,
                                    SearchKeyCacheInterval = searchKeyAttribute.SearchKeyCacheInterval,
                                    SearchKeyCacheValue = searchKeyAttribute.SearchKeyCacheValue,
                                    SearchKeyCacheSample = searchKeyAttribute.SearchKeyCacheSample,
                                    SearchKeyCacheFetchLimit = searchKeyAttribute.SearchKeyFetchLimit,
                                    SearchKeyCacheTtlInterval = searchKeyAttribute.SearchKeyCacheTtlInterval,
                                    SearchKeyCacheTtlValue = searchKeyAttribute.SearchKeyCacheTtlValue
                                };

                                inlineScript.GroupingKeys.Add(distinctSearchKey);
                                entityAnalysisModelInlineScriptProperty.SearchKey = distinctSearchKey;
                            }

                            inlineScript.EntityAnalysisModelInlineScriptPropertyAttributes.Add(p.Name, entityAnalysisModelInlineScriptProperty);

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Inline Script {record.Id} is inspecting property {p.Name} and has found a Search Key and is adding the grouping key to the model.");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    context.Services.Log.Error(
                        $"Entity Start: Inline script with the id of {record.Id} has created an error {ex}.");

                    await repository.UpdateCompileStatusAsync(record.Id, false, ex.Message,
                        context.Services.CancellationToken).ConfigureAwait(false);
                }
            }

            if (context.Services.Log.IsDebugEnabled)
            {
                context.Services.Log.Debug("Entity Start:  Completed creating Inline Scripts and closed the reader.");
            }

            return context;
        }

        private static void SetupEvents(EntityAnalysisModelInlineScript inlineScript)
        {

            var shadowEntityAnalysisModelInlineScriptEventsToBeSorted = new List<EntityAnalysisModelInlineScriptEvent>();
            foreach (var attribute in inlineScript.PreProcessingMethodInfo.GetCustomAttributes())
            {
                var entityAnalysisModelInlineScriptEvent = new EntityAnalysisModelInlineScriptEvent();

                switch (attribute)
                {
                    case ActivationRuleOverrideEvent activationRuleOverrideEvent:
                        entityAnalysisModelInlineScriptEvent.EntityAnalysisModelInlineScriptEventType = EntityAnalysisModelInlineScriptEventTypeEnum.AbstractionRuleOverride;

                        if (activationRuleOverrideEvent.Guid != null)
                        {
                            entityAnalysisModelInlineScriptEvent.Guid = Guid.Parse(activationRuleOverrideEvent.Guid);
                        }

                        entityAnalysisModelInlineScriptEvent.Priority = activationRuleOverrideEvent.Priority;
                        entityAnalysisModelInlineScriptEvent.Name = activationRuleOverrideEvent.Name;

                        shadowEntityAnalysisModelInlineScriptEventsToBeSorted.Add(entityAnalysisModelInlineScriptEvent);
                        break;

                    case PayloadEvent payloadEvent:
                        entityAnalysisModelInlineScriptEvent.EntityAnalysisModelInlineScriptEventType = EntityAnalysisModelInlineScriptEventTypeEnum.Payload;

                        if (payloadEvent.Guid != null)
                        {
                            entityAnalysisModelInlineScriptEvent.Guid = Guid.Parse(payloadEvent.Guid);
                        }

                        entityAnalysisModelInlineScriptEvent.Priority = payloadEvent.Priority;
                        entityAnalysisModelInlineScriptEvent.Name = payloadEvent.Name;

                        shadowEntityAnalysisModelInlineScriptEventsToBeSorted.Add(entityAnalysisModelInlineScriptEvent);
                        break;
                }
            }

            inlineScript.EntityAnalysisModelInlineScriptEvents = shadowEntityAnalysisModelInlineScriptEventsToBeSorted.OrderBy(o => o.Priority).ToList();
        }

        private static Func<object> CompileActivatorDelegate(EntityAnalysisModelInlineScript inlineScript)
        {

            return Expression.Lambda<Func<object>>(
                Expression.Convert(
                    Expression.New(inlineScript.InlineScriptType),
                    typeof(object)
                )
            ).Compile();
        }

        private static Func<object, EntityAnalysisModelInvoke.Context.Context, Task<bool>> CompileMethodDelegate(Type type, MethodInfo methodInfo)
        {
            var instanceParam = Expression.Parameter(typeof(object), "instance");
            var contextParam = Expression.Parameter(typeof(EntityAnalysisModelInvoke.Context.Context), "context");
            var castInstance = Expression.Convert(instanceParam, type);
            var methodCall = Expression.Call(castInstance, methodInfo, contextParam);

            return Expression.Lambda<Func<object, EntityAnalysisModelInvoke.Context.Context, Task<bool>>>(
                methodCall,
                instanceParam,
                contextParam
            ).Compile();
        }

        private static Func<object, object> CompileGetValueDelegate(PropertyInfo p)
        {
            if (p.DeclaringType == null)
            {
                throw new ArgumentException($"Property '{p.Name}' has no DeclaringType.");
            }

            var instParam = Expression.Parameter(typeof(object), "instance");
            var castInstance = Expression.Convert(instParam, p.DeclaringType);
            var propertyAccess = Expression.Property(castInstance, p);
            var castResult = Expression.Convert(propertyAccess, typeof(object));

            return Expression.Lambda<Func<object, object>>(castResult, instParam).Compile();
        }

        private static void SetupInlineScriptDelegates(EntityAnalysisModelInlineScript inlineScript)
        {
            inlineScript.InlineScriptType =
                inlineScript.InlineScriptCompile.GetType(inlineScript.ClassName);

            if (inlineScript.InlineScriptType == null)
            {
                return;
            }

            inlineScript.PreProcessingMethodInfo =
                inlineScript.InlineScriptType.GetMethod("ExecuteAsync",
                    [typeof(EntityAnalysisModelInvoke.Context.Context)]);

            inlineScript.ActivatorDelegate = CompileActivatorDelegate(inlineScript);
            inlineScript.ExecuteAsyncDelegate = CompileMethodDelegate(
                inlineScript.InlineScriptType,
                inlineScript.PreProcessingMethodInfo);
        }

        private static string[] BuildDependencyArray(Context context, string dependencies)
        {
            var baseArray = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !String.IsNullOrEmpty(a.Location))
                .Select(a => a.Location)
                .ToArray();

            var alwaysInclude = new[]
            {
                ResolveDependencyPath(context, "Jube.Cryptography.dll")
            };

            if (String.IsNullOrEmpty(dependencies))
            {
                return baseArray.Concat(alwaysInclude).ToArray();
            }

            var additionalDeps = dependencies.Split(",".ToCharArray())
                .Select(file => ResolveDependencyPath(context, file))
                .ToArray();

            return baseArray.Concat(alwaysInclude).Concat(additionalDeps).ToArray();
        }

        private static string ResolveDependencyPath(Context context, string file)
        {
            var binaryPath = Path.Combine(context.Paths.BinaryPath, file);
            return File.Exists(binaryPath) ? binaryPath
                : Path.Combine(context.Paths.FrameworkPath, file);
        }
    }
}
