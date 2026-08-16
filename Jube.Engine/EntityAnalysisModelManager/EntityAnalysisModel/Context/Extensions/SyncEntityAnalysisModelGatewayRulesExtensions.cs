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
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Data.Repository;
    using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
    using Jube.Engine.EntityAnalysisModelManager.Helpers;
    using Jube.Engine.Models;
    using Parser;
    using Parser.Compiler;

    public static class SyncEntityAnalysisModelGatewayRulesExtensions
    {
        public static async Task<Context> SyncEntityAnalysisModelGatewayRulesAsync(this Context context)
        {
            try
            {
                foreach (var (key, value) in context.EntityAnalysisModels.ActiveEntityAnalysisModels)
                {
                    context.Services.CancellationToken.ThrowIfCancellationRequested();

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Checking if model {key} is started for the purpose determining compiling the Gateway rules.");
                    }

                    var repository = new EntityAnalysisModelGatewayRuleRepository(context.Services.DbContext);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Executing EntityAnalysisModelGatewayRuleRepository.GetByEntityAnalysisModelIdOrderByPriorityAsync for entity model key of {key}.");
                    }

                    var records = await repository.GetByEntityAnalysisModelIdOrderByPriorityAsync(key, context.Services.CancellationToken).ConfigureAwait(false);

                    var shadowEntityModelGatewayRule = new List<EntityModelGatewayRule>();
                    foreach (var record in records)
                    {
                        context.Services.CancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Activation Rule ID {record.Id} returned for model {key}.");
                            }

                            if (record.Active != 1)
                            {
                                continue;
                            }

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Activation Rule ID {record.Id} returned for model {key} is active.");
                            }

                            var modelGatewayRule = new EntityModelGatewayRule
                            {
                                EntityAnalysisModelGatewayRuleId = record.Id
                            };

                            if (record.Name != null)
                            {
                                modelGatewayRule.Name = record.Name.Replace(" ", "_");

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Gateway Rule {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set Model Gateway Rule value set as {modelGatewayRule.Name}.");
                                }
                            }
                            else
                            {
                                modelGatewayRule.Name =
                                    $"Gateway_Rule_{modelGatewayRule.EntityAnalysisModelGatewayRuleId}";

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Gateway Rule {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set DEFAULT Model Gateway Rule value set as {modelGatewayRule.Name}.");
                                }
                            }

                            if (record.RuleScriptTypeId.HasValue)
                            {
                                modelGatewayRule.RuleScriptTypeId = record.RuleScriptTypeId.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Gateway Rule {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set Model Gateway Rule Script Type ID value set as {modelGatewayRule.RuleScriptTypeId}.");
                                }
                            }
                            else
                            {
                                modelGatewayRule.RuleScriptTypeId = 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Gateway Rule {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set DEFAULT Model Gateway Rule Script Type ID value set as {modelGatewayRule.RuleScriptTypeId}.");
                                }
                            }

                            if (record.MaxResponseElevation.HasValue)
                            {
                                modelGatewayRule.MaxResponseElevation = record.MaxResponseElevation.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Gateway Rule {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set Model Max Response Elevation value set as {modelGatewayRule.MaxResponseElevation}.");
                                }
                            }
                            else
                            {
                                modelGatewayRule.MaxResponseElevation = 0;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Gateway Rule {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set DEFAULT Model Max Response Elevation value set as {modelGatewayRule.MaxResponseElevation}.");
                                }
                            }

                            if (record.GatewaySample.HasValue)
                            {
                                modelGatewayRule.GatewaySample = record.GatewaySample.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Gateway Rule {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set Model Gateway Sample value set as {modelGatewayRule.GatewaySample}.");
                                }
                            }
                            else
                            {
                                modelGatewayRule.GatewaySample = 0;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Gateway Rule {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set DEFAULT Model Gateway Sample value set as {modelGatewayRule.GatewaySample}.");
                                }
                            }

                            if (record.Priority.HasValue)
                            {
                                modelGatewayRule.Priority = record.Priority.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Gateway Rule {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set Model Priority value set as {modelGatewayRule.Priority}.");
                                }
                            }
                            else
                            {
                                modelGatewayRule.Priority = 0;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Gateway Rule {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set DEFAULT Model Priority value set as {modelGatewayRule.Priority}.");
                                }
                            }

                            context.Services.CancellationToken.ThrowIfCancellationRequested();

                            var hasRuleScript = false;
                            if (record.BuilderRuleScript != null && modelGatewayRule.RuleScriptTypeId == 1)
                            {
                                var parsedRule = new ParsedRule
                                {
                                    OriginalRuleText = record.BuilderRuleScript,
                                    ErrorSpans = []
                                };
                                parsedRule = context.Services.Parser.TranslateFromDotNotation(parsedRule);
                                parsedRule = context.Services.Parser.Parse(parsedRule);

                                if (parsedRule.ErrorSpans.Count == 0)
                                {
                                    modelGatewayRule.GatewayRuleScript = parsedRule.ParsedRuleText;
                                    hasRuleScript = true;

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set builder script as {modelGatewayRule.GatewayRuleScript}.");
                                    }
                                }
                            }
                            else if (record.CoderRuleScript != null && modelGatewayRule.RuleScriptTypeId == 2)
                            {
                                var parsedRule = new ParsedRule
                                {
                                    OriginalRuleText = record.CoderRuleScript,
                                    ErrorSpans = []
                                };
                                parsedRule = context.Services.Parser.TranslateFromDotNotation(parsedRule);
                                parsedRule = context.Services.Parser.Parse(parsedRule);

                                if (parsedRule.ErrorSpans.Count == 0)
                                {
                                    modelGatewayRule.GatewayRuleScript = parsedRule.ParsedRuleText;
                                    hasRuleScript = true;

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set coder script as {modelGatewayRule.GatewayRuleScript}.");
                                    }
                                }
                            }

                            if (!hasRuleScript)
                            {
                                continue;
                            }

                            var gatewayRuleScript = new StringBuilder();
                            gatewayRuleScript.Append("Imports System.IO\r\n");
                            gatewayRuleScript.Append("Imports log4net\r\n");
                            gatewayRuleScript.Append("Imports System.Net\r\n");
                            gatewayRuleScript.Append("Imports System.Collections.Generic\r\n");
                            gatewayRuleScript.Append("Imports Jube.Dictionary\r\n");
                            gatewayRuleScript.Append("Imports Jube.Dictionary.Extensions\r\n");
                            gatewayRuleScript.Append("Imports System\r\n");
                            gatewayRuleScript.Append("Public Class GatewayRule\r\n");
                            gatewayRuleScript.Append(
                                "Public Shared Function Match(Data As DictionaryNoBoxing(Of String),List As Dictionary(Of String, List(Of String)),KVP As PooledDictionary(Of String, Double),Log as ILog) As Boolean\r\n");
                            gatewayRuleScript.Append("Dim Matched as Boolean\r\n");
                            gatewayRuleScript.Append("Try\r\n");
                            gatewayRuleScript.Append(modelGatewayRule.GatewayRuleScript + "\r\n");
                            gatewayRuleScript.Append("Catch ex As Exception\r\n");
                            gatewayRuleScript.Append("Log.Info(ex.ToString)\r\n");
                            gatewayRuleScript.Append("End Try\r\n");
                            gatewayRuleScript.Append("Return Matched\r\n");
                            gatewayRuleScript.Append("\r\n");
                            gatewayRuleScript.Append("End Function\r\n");
                            gatewayRuleScript.Append("End Class\r\n");

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} set class wrap as {gatewayRuleScript}.");
                            }

                            var gatewayRuleScriptHash = HashHelper.GetHash(gatewayRuleScript.ToString());

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} has been hashed to {gatewayRuleScriptHash} and will be checked against the hash cache.");
                            }

                            if (context.Caching.HashCacheAssembly.TryGetValue(gatewayRuleScriptHash, out var valueHash))
                            {
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} has been hashed to {gatewayRuleScriptHash} exists in the hash cache and will be allocated to a delegate.");
                                }

                                modelGatewayRule.GatewayRuleCompile = valueHash;

                                var classType = modelGatewayRule.GatewayRuleCompile.GetType("GatewayRule");
                                var methodInfo = classType.GetMethod("Match");
                                modelGatewayRule.GatewayRuleCompileDelegate =
                                    (EntityModelGatewayRule.Match)Delegate.CreateDelegate(
                                        typeof(EntityModelGatewayRule.Match), methodInfo);

                                shadowEntityModelGatewayRule.Add(modelGatewayRule);

                                await repository.UpdateCompileStatusAsync(record.Id, true, null,
                                    context.Services.CancellationToken).ConfigureAwait(false);

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} has been hashed to {gatewayRuleScriptHash} exists in the hash cache, has been allocated a to a delegate and placed in a shadow list of gateway rules.");
                                }
                            }
                            else
                            {
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} has been hashed to {gatewayRuleScriptHash} has not been found in the hash cache and will now be compiled.");
                                }

                                var compile = new Compile();
                                compile.CompileCode(gatewayRuleScript.ToString(), context.Services.Log,
                                [
                                    Path.Combine(context.Paths.BinaryPath ?? throw new InvalidOperationException(), "log4net.dll"),
                                    Path.Combine(context.Paths.BinaryPath, "Jube.Dictionary.dll")
                                ], Compile.Language.Vb);

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} has been hashed to {gatewayRuleScriptHash} has now been compiled with {compile.Errors} errors.");
                                }

                                if (compile.Errors == null)
                                {
                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} has been hashed to {gatewayRuleScriptHash} has now been compiled without error,  a delegate will now be allocated.");
                                    }

                                    modelGatewayRule.GatewayRuleCompile = compile.CompiledAssembly;

                                    var classType = modelGatewayRule.GatewayRuleCompile.GetType("GatewayRule");
                                    var methodInfo = classType.GetMethod("Match");
                                    modelGatewayRule.GatewayRuleCompileDelegate =
                                        (EntityModelGatewayRule.Match)Delegate.CreateDelegate(
                                            typeof(EntityModelGatewayRule.Match), methodInfo);
                                    shadowEntityModelGatewayRule.Add(modelGatewayRule);
                                    context.Caching.HashCacheAssembly.TryAdd(gatewayRuleScriptHash, compile.CompiledAssembly);
                                    context.Caching.HashCacheAssemblyMetadata.TryAdd(gatewayRuleScriptHash,
                                        new HashCacheAssemblyPayload(compile.CompiledAssemblyBytes, compile.CompiledAssemblyBinary, gatewayRuleScript.ToString()));

                                    await repository.UpdateCompileStatusAsync(record.Id, true, null,
                                        context.Services.CancellationToken).ConfigureAwait(false);

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} has been hashed to {gatewayRuleScriptHash} has now been compiled without error,  a delegate has been allocated,  added to hash cache and added to a shadow list of gateway rules.");
                                    }
                                }
                                else
                                {
                                    await repository.UpdateCompileStatusAsync(record.Id, false, compile.ErrorsSummary,
                                        context.Services.CancellationToken).ConfigureAwait(false);

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Model {key} and Gateway Rule Model {modelGatewayRule.EntityAnalysisModelGatewayRuleId} has been hashed to {gatewayRuleScriptHash} failed to load.");
                                    }
                                }
                            }
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            context.Services.Log.Error(
                                $"Entity Start: Activation Rule ID {record.Id} returned for model {key} has created an error as {ex}.");

                            await repository.UpdateCompileStatusAsync(record.Id, false, ex.Message,
                                context.Services.CancellationToken).ConfigureAwait(false);
                        }
                    }

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: {key} is being finished,  proceeding to update gateway rules and close off the cursor.");
                    }

                    var previousGatewayRulesById = value.Collections.ModelGatewayRules
                        .ToDictionary(r => r.EntityAnalysisModelGatewayRuleId);
                    foreach (var newGatewayRule in shadowEntityModelGatewayRule)
                    {
                        if (!previousGatewayRulesById.TryGetValue(newGatewayRule.EntityAnalysisModelGatewayRuleId,
                                out var previousGatewayRule))
                        {
                            continue;
                        }

                        newGatewayRule.EvaluationCounter =
                            Interlocked.Exchange(ref previousGatewayRule.EvaluationCounter, 0);
                        newGatewayRule.ActivationCounter =
                            Interlocked.Exchange(ref previousGatewayRule.ActivationCounter, 0);
                        newGatewayRule.ActivationCounterDate = previousGatewayRule.ActivationCounterDate;
                    }

                    value.Collections.ModelGatewayRules = shadowEntityModelGatewayRule;

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug($"Entity Start: {key} replaced Gateway Rule List with shadow gateway rules.");
                    }
                }

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug("Entity Start: Completed adding Gateway Rules to entity models.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"SyncEntityAnalysisModelGatewayRulesAsync: has produced an error {ex}");

                await new EntityAnalysisModelSynchronisationErrorRepository(context.Services.DbContext)
                    .InsertAsync(EntityAnalysisModelSynchronisationErrorRepository.EntityAnalysisModelSynchronisationErrorStepEnum.GatewayRules, ex.ToString(),
                        context.Services.CancellationToken).ConfigureAwait(false);
            }

            return context;
        }
    }
}
