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
    using AutoMapper.Internal;
    using Data.Repository;
    using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
    using Jube.Engine.EntityAnalysisModelManager.Helpers;
    using Jube.Engine.Models;
    using Parser;
    using Parser.Compiler;

    public static class SyncEntityAnalysisModelAbstractionCalculationExtensions
    {
        public static async Task<Context> SyncEntityAnalysisModelAbstractionCalculationsAsync(this Context context)
        {
            try
            {
                context.Services.Parser.EntityAnalysisModelAbstractionCalculations = [];

                foreach (var (key, value) in context.EntityAnalysisModels.ActiveEntityAnalysisModels)
                {
                    context.Services.CancellationToken.ThrowIfCancellationRequested();

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Checking if model {key} is started for the purpose determining adding Abstraction Calculations.");
                    }

                    var repository = new EntityAnalysisModelAbstractionCalculationRepository(context.Services.DbContext);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Executing EntityAnalysisModelAbstractionCalculationRepository.GetByEntityAnalysisModelId for entity model key of {key}.");
                    }

                    var records = await repository.GetByEntityAnalysisModelIdOrderByIdDescAsync(key, context.Services.CancellationToken).ConfigureAwait(false);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug("Returned all Calculation Field from the database.");
                    }

                    var shadowEntityAnalysisModelAbstractionCalculation =
                        new List<EntityAnalysisModelAbstractionCalculation>();

                    foreach (var record in records)
                    {
                        context.Services.CancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Abstraction Calculation ID {record.Id} returned for model {key}.");
                            }

                            if (record.Active == 1)
                            {
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Abstraction Calculation ID {record.Id} returned for model {key} is active.");
                                }

                                var entityAnalysisModelAbstractionCalculation =
                                    new EntityAnalysisModelAbstractionCalculation
                                    {
                                        Id = record.Id
                                    };

                                if (record.Name == null)
                                {
                                    entityAnalysisModelAbstractionCalculation.Name =
                                        $"s{entityAnalysisModelAbstractionCalculation.Id}";

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set DEFAULT Name as {entityAnalysisModelAbstractionCalculation.Name}.");
                                    }
                                }
                                else
                                {
                                    entityAnalysisModelAbstractionCalculation.Name = record.Name.Replace(" ", "_");

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set Name as {entityAnalysisModelAbstractionCalculation.Name}.");
                                    }
                                }

                                if (record.EntityAnalysisModelAbstractionNameLeft != null)
                                {
                                    entityAnalysisModelAbstractionCalculation.EntityAnalysisModelAbstractionNameLeft =
                                        record.EntityAnalysisModelAbstractionNameLeft;

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set Entity Analysis Model Abstraction Name Left as {entityAnalysisModelAbstractionCalculation.EntityAnalysisModelAbstractionNameLeft}.");
                                    }
                                }
                                else
                                {
                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set DEFAULT,  missing, Entity Analysis Model Abstraction Name Left as {entityAnalysisModelAbstractionCalculation.EntityAnalysisModelAbstractionNameLeft}.");
                                    }
                                }

                                if (record.EntityAnalysisModelAbstractionNameRight != null)
                                {
                                    entityAnalysisModelAbstractionCalculation.EntityAnalysisModelAbstractionNameRight =
                                        record.EntityAnalysisModelAbstractionNameRight;

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set Entity Analysis Model Abstraction Name Right as {entityAnalysisModelAbstractionCalculation.EntityAnalysisModelAbstractionNameRight}.");
                                    }
                                }
                                else
                                {
                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set DEFAULT,  missing, Entity Analysis Model Abstraction Name Right as {entityAnalysisModelAbstractionCalculation.EntityAnalysisModelAbstractionNameRight}.");
                                    }
                                }

                                if (!record.ResponsePayload.HasValue)
                                {
                                    entityAnalysisModelAbstractionCalculation.ResponsePayload = false;

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set DEFAULT Response Payload as {entityAnalysisModelAbstractionCalculation.ResponsePayload}.");
                                    }
                                }
                                else
                                {
                                    entityAnalysisModelAbstractionCalculation.ResponsePayload = record.ResponsePayload == 1;

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set Response Payload as {entityAnalysisModelAbstractionCalculation.ResponsePayload}.");
                                    }
                                }

                                if (!record.ReportTable.HasValue)
                                {
                                    entityAnalysisModelAbstractionCalculation.ReportTable = false;

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set DEFAULT Promote Report Table as {entityAnalysisModelAbstractionCalculation.ReportTable}.");
                                    }
                                }
                                else
                                {
                                    entityAnalysisModelAbstractionCalculation.ReportTable =
                                        record.ReportTable == 1;

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set Promote Report Table as {entityAnalysisModelAbstractionCalculation.ReportTable}.");
                                    }
                                }

                                if (!record.AbstractionCalculationTypeId.HasValue)
                                {
                                    entityAnalysisModelAbstractionCalculation.AbstractionCalculationTypeId = 1;

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set DEFAULT Calculation Type as {entityAnalysisModelAbstractionCalculation.AbstractionCalculationTypeId}.");
                                    }
                                }
                                else
                                {
                                    entityAnalysisModelAbstractionCalculation.AbstractionCalculationTypeId =
                                        record.AbstractionCalculationTypeId.Value;

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Model {key} and Deviation {entityAnalysisModelAbstractionCalculation.Id} set Calculation Type as {entityAnalysisModelAbstractionCalculation.AbstractionCalculationTypeId}.");
                                    }
                                }

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
                                        entityAnalysisModelAbstractionCalculation.FunctionScript =
                                            parsedRule.ParsedRuleText;
                                        hasRuleScript = true;

                                        if (context.Services.Log.IsDebugEnabled)
                                        {
                                            context.Services.Log.Debug(
                                                $"Entity Start: Entity Model {key} and calculation {entityAnalysisModelAbstractionCalculation.Id} set  script as {entityAnalysisModelAbstractionCalculation.FunctionScript}.");
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
                                    activationRuleScript.Append("Public Class CalculationRule\r\n");
                                    activationRuleScript.Append(
                                        "Public Shared Function Match(Data As DictionaryNoBoxing(Of String),TTLCounter As PooledDictionary(Of String, Double),Abstraction As PooledDictionary(Of string,double),List as Dictionary(Of String,List(Of String)),KVP As PooledDictionary(Of String, Double),Log as ILog) As Double\r\n");
                                    activationRuleScript.Append("Dim Matched as Double\r\n");
                                    activationRuleScript.Append("Try\r\n");
                                    activationRuleScript.Append(entityAnalysisModelAbstractionCalculation.FunctionScript +
                                                                "\r\n");
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
                                            $"Entity Start: {key} and Calculation {entityAnalysisModelAbstractionCalculation.Id} class wrapped as {activationRuleScript}.");
                                    }

                                    var activationRuleScriptHash = HashHelper.GetHash(activationRuleScript.ToString());

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: {key} and Calculation {entityAnalysisModelAbstractionCalculation.Id} has been hashed to {activationRuleScriptHash}, will now check if it is in the hash cache.");
                                    }

                                    if (context.Caching.HashCacheAssembly.TryGetValue(activationRuleScriptHash, out var valueHash))
                                    {
                                        if (context.Services.Log.IsDebugEnabled)
                                        {
                                            context.Services.Log.Debug(
                                                $"Entity Start: {key} and Calculation {entityAnalysisModelAbstractionCalculation.Id} has been hashed to {activationRuleScriptHash} and been located in the hash cache to be assigned to a delegate.");
                                        }

                                        entityAnalysisModelAbstractionCalculation.FunctionCalculationCompile =
                                            valueHash;

                                        var classType =
                                            entityAnalysisModelAbstractionCalculation.FunctionCalculationCompile.GetType(
                                                "CalculationRule");
                                        var methodInfo = classType.GetMethod("Match");
                                        entityAnalysisModelAbstractionCalculation.FunctionCalculationCompileDelegate =
                                            (EntityAnalysisModelAbstractionCalculation.Match)Delegate.CreateDelegate(
                                                typeof(EntityAnalysisModelAbstractionCalculation.Match), methodInfo);

                                        await repository.UpdateCompileStatusAsync(entityAnalysisModelAbstractionCalculation.Id,
                                            true, null, context.Services.CancellationToken).ConfigureAwait(false);

                                        if (context.Services.Log.IsDebugEnabled)
                                        {
                                            context.Services.Log.Debug(
                                                $"Entity Start: {key} and Calculation {entityAnalysisModelAbstractionCalculation.Id} has been hashed to {activationRuleScriptHash}, assigned to a delegate from the hash cache and added to a shadow list of Activation Rules.");
                                        }
                                    }
                                    else
                                    {
                                        if (context.Services.Log.IsDebugEnabled)
                                        {
                                            context.Services.Log.Debug(
                                                $"Entity Start: {key} and Calculation {entityAnalysisModelAbstractionCalculation.Id} has been hashed to {activationRuleScriptHash} and has not been located in the hash cache, hence it will be compiled.");
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
                                                $"Entity Start: {key} and Abstraction Rule Model {entityAnalysisModelAbstractionCalculation.Id} has been hashed to {activationRuleScriptHash} and compiled with {compile.Errors}.");
                                        }

                                        if (compile.Errors == null)
                                        {
                                            if (context.Services.Log.IsDebugEnabled)
                                            {
                                                context.Services.Log.Debug(
                                                    $"Entity Start: {key} and Calculation {entityAnalysisModelAbstractionCalculation.Id} has been hashed to {activationRuleScriptHash} and has been compiled,  it will now be allocated to a delegate.");
                                            }

                                            entityAnalysisModelAbstractionCalculation.FunctionCalculationCompile =
                                                compile.CompiledAssembly;

                                            var classType =
                                                entityAnalysisModelAbstractionCalculation.FunctionCalculationCompile
                                                    .GetType("CalculationRule");
                                            var methodInfo = classType.GetMethod("Match");
                                            entityAnalysisModelAbstractionCalculation.FunctionCalculationCompileDelegate =
                                                (EntityAnalysisModelAbstractionCalculation.Match)
                                                Delegate.CreateDelegate(
                                                    typeof(EntityAnalysisModelAbstractionCalculation.Match), methodInfo);

                                            context.Caching.HashCacheAssembly.TryAdd(activationRuleScriptHash, compile.CompiledAssembly);
                                            context.Caching.HashCacheAssemblyMetadata.TryAdd(activationRuleScriptHash,
                                                new HashCacheAssemblyPayload(compile.CompiledAssemblyBytes, compile.CompiledAssemblyBinary, activationRuleScript.ToString()));

                                            await repository.UpdateCompileStatusAsync(entityAnalysisModelAbstractionCalculation.Id,
                                                true, null, context.Services.CancellationToken).ConfigureAwait(false);

                                            if (context.Services.Log.IsDebugEnabled)
                                            {
                                                context.Services.Log.Debug(
                                                    $"Entity Start: {key} and Calculation {entityAnalysisModelAbstractionCalculation.Id} has been hashed to {activationRuleScriptHash} and has been compiled,  allocated to a delegate and added to a shadow list of Calculations.");
                                            }
                                        }
                                        else
                                        {
                                            await repository.UpdateCompileStatusAsync(entityAnalysisModelAbstractionCalculation.Id,
                                                false, compile.ErrorsSummary, context.Services.CancellationToken).ConfigureAwait(false);

                                            if (context.Services.Log.IsDebugEnabled)
                                            {
                                                context.Services.Log.Debug(
                                                    $"Entity Start: {key} and Calculation {entityAnalysisModelAbstractionCalculation.Id} has been hashed to {activationRuleScriptHash} but has failed to load.");
                                            }
                                        }
                                    }
                                }

                                shadowEntityAnalysisModelAbstractionCalculation.Add(
                                    entityAnalysisModelAbstractionCalculation);

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Calculation {entityAnalysisModelAbstractionCalculation.Id} has been added to a shadow list of Abstraction Calculations.");
                                }

                                context.Services.Parser.EntityAnalysisModelAbstractionCalculations.TryAdd(
                                    entityAnalysisModelAbstractionCalculation.Name);

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Calculation {entityAnalysisModelAbstractionCalculation.Id} has added {entityAnalysisModelAbstractionCalculation.Name} to context.Parser.");
                                }
                            }
                            else
                            {
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Model {key} and Calculation {record.Id} has not been added to a shadow list of Abstraction Calculations as it is not active.");
                                }
                            }
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            context.Services.Log.Error(
                                $"Entity Start: Abstraction Calculation ID {record.Id} returned for model {key} has created an error as {ex}.");

                            await repository.UpdateCompileStatusAsync(record.Id, false, ex.Message,
                                context.Services.CancellationToken).ConfigureAwait(false);
                        }
                    }

                    value.Collections.EntityAnalysisModelAbstractionCalculations =
                        shadowEntityAnalysisModelAbstractionCalculation;

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Model {key} has overwritten the current Abstraction Calculations with the shadow list of Abstraction Calculations.");
                    }
                }

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug("Entity Start: Completed adding Abstraction Calculations to entity models.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"SyncEntityAnalysisModelAbstractionCalculationsAsync: has produced an error {ex}");

                await new EntityAnalysisModelSynchronisationErrorRepository(context.Services.DbContext)
                    .InsertAsync(EntityAnalysisModelSynchronisationErrorRepository.EntityAnalysisModelSynchronisationErrorStepEnum.AbstractionCalculation, ex.ToString(),
                        context.Services.CancellationToken).ConfigureAwait(false);
            }

            return context;
        }
    }
}
