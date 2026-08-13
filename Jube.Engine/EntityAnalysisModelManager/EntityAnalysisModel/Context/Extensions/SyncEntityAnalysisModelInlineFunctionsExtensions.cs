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
    using System.Text;
    using System.Threading.Tasks;
    using Data.Repository;
    using Helpers;
    using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
    using Jube.Engine.EntityAnalysisModelManager.Helpers;
    using Jube.Engine.Models;
    using Parser;
    using Parser.Compiler;

    public static class SyncEntityAnalysisModelInlineFunctionsExtensions
    {
        public static async Task<Context> SyncEntityAnalysisModelInlineFunctionsAsync(this Context context)
        {
            var shadowEntityAnalysisModelsInlineFunctions = new Dictionary<string, int>();
            try
            {
                foreach (var (key, value) in context.EntityAnalysisModels.ActiveEntityAnalysisModels)
                {
                    context.Services.CancellationToken.ThrowIfCancellationRequested();

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Checking if model {key} is started for the purpose determining adding Inline Functions.");
                    }

                    var repository = new EntityAnalysisModelInlineFunctionRepository(context.Services.DbContext);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Executing EntityAnalysisModelInlineFunctionRepository.GetByEntityAnalysisModelId for entity model key of {key}.");
                    }

                    var records = await repository.GetByEntityAnalysisModelIdOrderByIdAsync(key, context.Services.CancellationToken).ConfigureAwait(false);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug("Returned all Inline Functions from the database.");
                    }

                    var shadowEntityAnalysisModelInlineFunctions = new List<EntityAnalysisModelInlineFunction>();
                    foreach (var record in records)
                    {
                        context.Services.CancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Inline Function ID {record.Id} returned for model {key}.");
                            }

                            if (record.Active.Value != 1)
                            {
                                continue;
                            }

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Inline_Function ID {record.Id} returned for model {key} is active.");
                            }

                            var entityAnalysisModelInlineFunction = new EntityAnalysisModelInlineFunction
                            {
                                Id = record.Id
                            };

                            if (record.Name == null)
                            {
                                entityAnalysisModelInlineFunction.Name =
                                    $"Inline_Function_{entityAnalysisModelInlineFunction.Id}";

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Function {entityAnalysisModelInlineFunction.Id} set DEFAULT Name as {entityAnalysisModelInlineFunction.Name}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelInlineFunction.Name = record.Name.Replace(" ", "_");

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Inline Function {entityAnalysisModelInlineFunction.Id} set Name as {entityAnalysisModelInlineFunction.Name}.");
                                }
                            }

                            if (!record.ResponsePayload.HasValue)
                            {
                                entityAnalysisModelInlineFunction.ResponsePayload = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Function {entityAnalysisModelInlineFunction.Id} set DEFAULT Response Payload as {entityAnalysisModelInlineFunction.ResponsePayload}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelInlineFunction.ResponsePayload = record.ResponsePayload.Value == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Function {entityAnalysisModelInlineFunction.Id} set Response Payload as {entityAnalysisModelInlineFunction.ResponsePayload}.");
                                }
                            }

                            if (!record.ReportTable.HasValue)
                            {
                                entityAnalysisModelInlineFunction.ReportTable = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Function Type {entityAnalysisModelInlineFunction.Id} set DEFAULT Promote Report Table as {entityAnalysisModelInlineFunction.ReportTable}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelInlineFunction.ReportTable = record.ReportTable.Value == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Function Type {entityAnalysisModelInlineFunction.Id} set Promote Report Table as {entityAnalysisModelInlineFunction.ReportTable}.");
                                }
                            }

                            if (!record.ReturnDataTypeId.HasValue)
                            {
                                entityAnalysisModelInlineFunction.ReturnDataTypeId = 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Function Type {entityAnalysisModelInlineFunction.Id} set DEFAULT Return Type as {entityAnalysisModelInlineFunction.ReturnDataTypeId}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelInlineFunction.ReturnDataTypeId = record.ReturnDataTypeId.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Function Type {entityAnalysisModelInlineFunction.Id} set Return Type as {entityAnalysisModelInlineFunction.ReturnDataTypeId}.");
                                }
                            }

                            if (!record.EncryptionId.HasValue)
                            {
                                entityAnalysisModelInlineFunction.EncryptionId = 0;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Function Type {entityAnalysisModelInlineFunction.Id} set DEFAULT Encryption Id as {entityAnalysisModelInlineFunction.EncryptionId}.");
                                }
                            }
                            else
                            {
                                entityAnalysisModelInlineFunction.EncryptionId = record.EncryptionId.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Function Type {entityAnalysisModelInlineFunction.Id} set Encryption Id as {entityAnalysisModelInlineFunction.EncryptionId}.");
                                }
                            }

                            shadowEntityAnalysisModelsInlineFunctions.TryAdd(entityAnalysisModelInlineFunction.Name,
                                entityAnalysisModelInlineFunction.ReturnDataTypeId);

                            context.Services.CancellationToken.ThrowIfCancellationRequested();

                            var hasRuleScript = false;
                            if (record.FunctionScript != null)
                            {
                                var parsedRule = new ParsedRule
                                {
                                    OriginalRuleText = record.FunctionScript,
                                    ErrorSpans = []
                                };
                                parsedRule = context.Services.Parser.TranslateFromDotNotation(parsedRule);
                                parsedRule = context.Services.Parser.Parse(parsedRule);

                                if (parsedRule.ErrorSpans.Count == 0)
                                {
                                    entityAnalysisModelInlineFunction.FunctionScript = parsedRule.ParsedRuleText;
                                    hasRuleScript = true;

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Entity Model {key} and calculation {entityAnalysisModelInlineFunction.Id} set  script as {entityAnalysisModelInlineFunction.FunctionScript}.");
                                    }
                                }
                                else
                                {
                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Entity Model {key} and calculation {entityAnalysisModelInlineFunction.Id} set soft parse security error for script as {entityAnalysisModelInlineFunction.FunctionScript}.");
                                    }
                                }
                            }

                            if (hasRuleScript)
                            {
                                var activationRuleScript = new StringBuilder();
                                activationRuleScript.Append("Imports System.IO\r\n");
                                activationRuleScript.Append("Imports log4net\r\n");
                                activationRuleScript.Append("Imports System.Net\r\n");
                                activationRuleScript.Append("Imports System.Collections.Generic\r\n");
                                activationRuleScript.Append("Imports Jube.Dictionary\r\n");
                                activationRuleScript.Append("Imports Jube.Dictionary.Extensions\r\n");
                                activationRuleScript.Append("Imports System\r\n");
                                activationRuleScript.Append("Public Class InlineFunction\r\n");
                                activationRuleScript.Append(
                                    "Public Shared Function Match(Data As DictionaryNoBoxing(Of String),List As Dictionary(Of String, List(Of String)),KVP As PooledDictionary(Of String, Double),Log as ILog) As Object\r\n");
                                activationRuleScript.Append("Dim Matched as Object = Nothing");
                                activationRuleScript.Append("\r\n");
                                activationRuleScript.Append("Try\r\n");
                                activationRuleScript.Append(entityAnalysisModelInlineFunction.FunctionScript + "\r\n");
                                activationRuleScript.Append("Catch ex As Exception\r\n");
                                activationRuleScript.Append("Log.Info(ex.ToString)\r\n");
                                activationRuleScript.Append("End Try\r\n");
                                activationRuleScript.Append("Return Matched\r\n");
                                activationRuleScript.Append("\r\n");
                                activationRuleScript.Append("End Function\r\n");
                                activationRuleScript.Append("End Class\r\n");

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: {key} and Function {entityAnalysisModelInlineFunction.Id} class wrapped as {activationRuleScript}.");
                                }

                                var activationRuleScriptHash = HashHelper.GetHash(activationRuleScript.ToString());

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: {key} and Function {entityAnalysisModelInlineFunction.Id} has been hashed to {activationRuleScriptHash}, will now check if it is in the hash cache.");
                                }

                                if (context.Caching.HashCacheAssembly.TryGetValue(activationRuleScriptHash, out var valueHash))
                                {
                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: {key} and Function {entityAnalysisModelInlineFunction.Id} has been hashed to {activationRuleScriptHash} and been located in the hash cache to be assigned to a delegate.");
                                    }

                                    entityAnalysisModelInlineFunction.FunctionCalculationCompile =
                                        valueHash;

                                    var classType =
                                        entityAnalysisModelInlineFunction.FunctionCalculationCompile.GetType(
                                            "InlineFunction");
                                    var methodInfo = classType.GetMethod("Match");
                                    entityAnalysisModelInlineFunction.FunctionCalculationCompileDelegate =
                                        (EntityAnalysisModelInlineFunction.Match)Delegate.CreateDelegate(
                                            typeof(EntityAnalysisModelInlineFunction.Match),
                                            methodInfo);

                                    shadowEntityAnalysisModelInlineFunctions.Add(entityAnalysisModelInlineFunction);

                                    await new EntityAnalysisModelInlineFunctionRepository(context.Services.DbContext)
                                        .UpdateCompileStatusAsync(entityAnalysisModelInlineFunction.Id, true, null,
                                            context.Services.CancellationToken).ConfigureAwait(false);

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: {key} and Function {entityAnalysisModelInlineFunction.Id} has been hashed to {activationRuleScriptHash}, assigned to a delegate from the hash cache and added to a shadow list of Inline Functions.");
                                    }
                                }
                                else
                                {
                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: {key} and Function {entityAnalysisModelInlineFunction.Id} has been hashed to {activationRuleScriptHash} and has not been located in the hash cache, hence it will be compiled.");
                                    }

                                    var compile = new Compile();
                                    compile.CompileCode(activationRuleScript.ToString(), context.Services.Log,
                                    [
                                        Path.Combine(context.Paths.BinaryPath ?? throw new InvalidOperationException(), "log4net.dll"),
                                        Path.Combine(context.Paths.BinaryPath, "Jube.Dictionary.dll")
                                    ], Compile.Language.Vb);

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: {key} and Abstraction Rule Model {entityAnalysisModelInlineFunction.Id} has been hashed to {activationRuleScriptHash} and compiled with {compile.Errors}.");
                                    }

                                    if (compile.Errors == null)
                                    {
                                        if (context.Services.Log.IsDebugEnabled)
                                        {
                                            context.Services.Log.Debug(
                                                $"Entity Start: {key} and Calculation {entityAnalysisModelInlineFunction.Id} has been hashed to {activationRuleScriptHash} and has been compiled,  it will now be allocated to a delegate.");
                                        }

                                        entityAnalysisModelInlineFunction.FunctionCalculationCompile =
                                            compile.CompiledAssembly;

                                        var classType =
                                            entityAnalysisModelInlineFunction.FunctionCalculationCompile.GetType(
                                                "InlineFunction");
                                        var methodInfo = classType.GetMethod("Match");
                                        entityAnalysisModelInlineFunction.FunctionCalculationCompileDelegate =
                                            (EntityAnalysisModelInlineFunction.Match)Delegate.CreateDelegate(
                                                typeof(EntityAnalysisModelInlineFunction.Match),
                                                methodInfo);

                                        context.Caching.HashCacheAssembly.TryAdd(activationRuleScriptHash, compile.CompiledAssembly);
                                        context.Caching.HashCacheAssemblyMetadata.TryAdd(activationRuleScriptHash,
                                            new HashCacheAssemblyPayload(compile.CompiledAssemblyBytes, compile.CompiledAssemblyBinary, activationRuleScript.ToString()));
                                        shadowEntityAnalysisModelInlineFunctions.Add(entityAnalysisModelInlineFunction);

                                        await new EntityAnalysisModelInlineFunctionRepository(context.Services.DbContext)
                                            .UpdateCompileStatusAsync(entityAnalysisModelInlineFunction.Id, true, null,
                                                context.Services.CancellationToken).ConfigureAwait(false);

                                        if (context.Services.Log.IsDebugEnabled)
                                        {
                                            context.Services.Log.Debug(
                                                $"Entity Start: {key} and Calculation {entityAnalysisModelInlineFunction.Id} has been hashed to {activationRuleScriptHash} and has been compiled,  allocated to a delegate and added to a shadow list of Inline Functions.");
                                        }
                                    }
                                    else
                                    {
                                        await new EntityAnalysisModelInlineFunctionRepository(context.Services.DbContext)
                                            .UpdateCompileStatusAsync(entityAnalysisModelInlineFunction.Id, false, compile.ErrorsSummary,
                                                context.Services.CancellationToken).ConfigureAwait(false);

                                        if (context.Services.Log.IsDebugEnabled)
                                        {
                                            context.Services.Log.Debug(
                                                $"Entity Start: {key} and Calculation {entityAnalysisModelInlineFunction.Id} has been hashed to {activationRuleScriptHash} but has failed to load.");
                                        }
                                    }
                                }
                            }

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Model {key} and Function {entityAnalysisModelInlineFunction.Id} has been added to a shadow list of Inline Functions.");
                            }
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            context.Services.Log.Error(
                                $"Entity Start: Inline Function ID {record.Id} returned for model {key} has created an error as {ex}.");

                            await new EntityAnalysisModelInlineFunctionRepository(context.Services.DbContext)
                                .UpdateCompileStatusAsync(record.Id, false, ex.Message,
                                    context.Services.CancellationToken).ConfigureAwait(false);
                        }
                    }

                    context.Services.Parser.EntityAnalysisModelsInlineFunctions = shadowEntityAnalysisModelsInlineFunctions;
                    value.Collections.EntityAnalysisModelInlineFunctions = shadowEntityAnalysisModelInlineFunctions;
                    value.References.PayloadInitialSize = DictionaryNoBoxingHelpers.CalculateInitialSize(value);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Model {key} has overwritten the current Inline Functions with the shadow list of Inline Functions.");
                    }
                }

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug("Entity Start: Completed adding Inline Functions to entity models.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"SyncEntityAnalysisModelInlineFunctionsAsync: has produced an error {ex}");

                await new EntityAnalysisModelSynchronisationErrorRepository(context.Services.DbContext)
                    .InsertAsync(EntityAnalysisModelSynchronisationErrorRepository.EntityAnalysisModelSynchronisationErrorStepEnum.InlineFunctions, ex.ToString(),
                        context.Services.CancellationToken).ConfigureAwait(false);
            }

            return context;
        }
    }
}
