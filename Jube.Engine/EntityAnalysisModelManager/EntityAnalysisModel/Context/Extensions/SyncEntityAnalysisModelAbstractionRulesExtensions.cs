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

    public static class SyncEntityAnalysisModelAbstractionRulesExtensions
    {
        public static async Task<Context> SyncEntityAnalysisModelAbstractionRulesAsync(this Context context)
        {
            try
            {
                context.Services.Parser.EntityAnalysisModelsAbstractionRule = [];

                foreach (var (key, value) in context.EntityAnalysisModels.ActiveEntityAnalysisModels)
                {
                    context.Services.CancellationToken.ThrowIfCancellationRequested();

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Looping through active models {key} is started for the purpose adding the Abstraction Rules.");
                    }

                    var repository = new EntityAnalysisModelAbstractionRuleRepository(context.Services.DbContext);

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Executing EntityAnalysisModelAbstractionRuleRepository.GetByEntityAnalysisModelId for entity model key of {key}.");
                    }

                    var records = await repository.GetByEntityAnalysisModelIdOrderByIdDescAsync(key, context.Services.CancellationToken).ConfigureAwait(false);

                    var shadowEntityModelAbstractionRule = new List<EntityAnalysisModelAbstractionRule>();
                    var shadowDistinctSearchKeys = value.Collections.DistinctSearchKeys;

                    foreach (var record in records)
                    {
                        context.Services.CancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Abstraction Rules ID {record.Id} returned for model {key}.");
                            }

                            if (record.Active != 1)
                            {
                                continue;
                            }

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Abstraction Rules ID {record.Id} returned for model {key} is active.");
                            }

                            var modelAbstractionRule = new EntityAnalysisModelAbstractionRule
                            {
                                Id = record.Id
                            };

                            if (record.Name != null)
                            {
                                modelAbstractionRule.Name = record.Name.Replace(" ", "_");

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Name value as {modelAbstractionRule.Name}.");
                                }
                            }
                            else
                            {
                                modelAbstractionRule.Name =
                                    $"Abstraction_Rule_{modelAbstractionRule.Id}";

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Name value as {modelAbstractionRule.Name}.");
                                }
                            }

                            if (record.RuleScriptTypeId.HasValue)
                            {
                                modelAbstractionRule.RuleScriptTypeId = record.RuleScriptTypeId.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Rule Script Type ID value as {modelAbstractionRule.RuleScriptTypeId}.");
                                }
                            }
                            else
                            {
                                modelAbstractionRule.RuleScriptTypeId = 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Rule Script Type ID value as {modelAbstractionRule.RuleScriptTypeId}.");
                                }
                            }

                            if (record.SearchKey != null)
                            {
                                modelAbstractionRule.SearchKey = record.SearchKey;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Grouping Key value as {modelAbstractionRule.SearchKey}.");
                                }
                            }
                            else
                            {
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Grouping Key value as {modelAbstractionRule.SearchKey}.");
                                }
                            }

                            if (record.ReportTable.HasValue)
                            {
                                modelAbstractionRule.ReportTable = record.ReportTable == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Promote Report Table value as {modelAbstractionRule.ReportTable}.");
                                }
                            }
                            else
                            {
                                modelAbstractionRule.ReportTable = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Promote Report Table value as {modelAbstractionRule.ReportTable}.");
                                }
                            }

                            if (record.ResponsePayload.HasValue)
                            {
                                modelAbstractionRule.ResponsePayload = record.ResponsePayload == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Response Payload value as {modelAbstractionRule.ResponsePayload}.");
                                }
                            }
                            else
                            {
                                modelAbstractionRule.ResponsePayload = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Response Payload value as {modelAbstractionRule.ResponsePayload}.");
                                }
                            }

                            if (record.SearchFunctionKey != null)
                            {
                                modelAbstractionRule.SearchFunctionKey = record.SearchFunctionKey;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Grouping Function Key value as {modelAbstractionRule.SearchFunctionKey}.");
                                }
                            }
                            else
                            {
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Grouping Function Key value as {modelAbstractionRule.SearchFunctionKey}.");
                                }
                            }

                            if (record.Search.HasValue)
                            {
                                modelAbstractionRule.Search = record.Search.Value == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction Rule Search Extrapolation Type value is greater than 1, {modelAbstractionRule.Search}.  Checking to see if already added to the distinct search keys available to this abstraction rule.");
                                }

                                foreach (var requestXPath in value.Collections.EntityAnalysisModelRequestXPaths)
                                {
                                    context.Services.CancellationToken.ThrowIfCancellationRequested();

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} checking {requestXPath.Name}.");
                                    }

                                    if (requestXPath.Name != modelAbstractionRule.SearchKey)
                                    {
                                        continue;
                                    }

                                    var distinctSearchKey = new DistinctSearchKey();

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} matched {requestXPath.Name}.");
                                    }

                                    distinctSearchKey.SearchKeyCacheInterval =
                                        requestXPath.SearchKeyCacheInterval;
                                    distinctSearchKey.SearchKeyCacheValue =
                                        requestXPath.SearchKeyCacheValue;
                                    distinctSearchKey.SearchKeyCache = requestXPath.SearchKeyCache;
                                    distinctSearchKey.SearchKeyCacheFetchLimit =
                                        requestXPath.SearchKeyCacheFetchLimit;
                                    distinctSearchKey.SearchKey = modelAbstractionRule.SearchKey;
                                    distinctSearchKey.SearchKeyCacheSample = requestXPath.SearchKeyCacheSample;
                                    distinctSearchKey.SearchKeyTtlInterval = requestXPath.SearchKeyTtlInterval;
                                    distinctSearchKey.SearchKeyTtlIntervalValue =
                                        requestXPath.SearchKeyTtlIntervalValue;
                                    distinctSearchKey.SearchKeyFetchLimit = requestXPath.SearchKeyFetchLimit;

                                    if (!shadowDistinctSearchKeys.ContainsKey(modelAbstractionRule
                                            .SearchKey))
                                    {
                                        shadowDistinctSearchKeys.Add(distinctSearchKey.SearchKey,
                                            distinctSearchKey);

                                        if (context.Services.Log.IsDebugEnabled)
                                        {
                                            context.Services.Log.Debug(
                                                $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction Rule Search Extrapolation Type value is greater than 1, {modelAbstractionRule.Search}.  Not added to distinct search keys,  adding.");
                                        }
                                    }
                                    else
                                    {
                                        shadowDistinctSearchKeys[distinctSearchKey.SearchKey] = distinctSearchKey;

                                        if (context.Services.Log.IsDebugEnabled)
                                        {
                                            context.Services.Log.Debug(
                                                $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction Rule Search Extrapolation Type value is greater than 1, {modelAbstractionRule.Search}.  Already exists.  Updating.");
                                        }
                                    }

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} matched {requestXPath.Name} and added the grouping key to the distinct list being used by the rule.  Will not check any further in the available XPath.");
                                    }

                                    break;
                                }
                            }
                            else
                            {
                                modelAbstractionRule.Search = true;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Abstraction Rule Search Extrapolation Type value as {modelAbstractionRule.Search}.");
                                }
                            }

                            if (record.Offset.HasValue)
                            {
                                modelAbstractionRule.EnableOffset = record.Offset.Value == 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction Enable Offset value as {modelAbstractionRule.EnableOffset}.");
                                }
                            }
                            else
                            {
                                modelAbstractionRule.EnableOffset = false;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Enable Offset value as {modelAbstractionRule.EnableOffset}.");
                                }
                            }

                            if (record.OffsetTypeId.HasValue)
                            {
                                modelAbstractionRule.OffsetType = record.OffsetTypeId.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction Offset Type value as {modelAbstractionRule.OffsetType}.");
                                }
                            }
                            else
                            {
                                modelAbstractionRule.OffsetType = 0;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Offset Type value as {modelAbstractionRule.OffsetType}.");
                                }
                            }

                            if (record.OffsetValue.HasValue)
                            {
                                modelAbstractionRule.OffsetValue = record.OffsetValue.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction Offset value as {modelAbstractionRule.OffsetValue}.");
                                }
                            }
                            else
                            {
                                modelAbstractionRule.OffsetValue = 0;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Offset value as {modelAbstractionRule.OffsetValue}.");
                                }
                            }

                            if (record.SearchInterval != null)
                            {
                                modelAbstractionRule.AbstractionRuleAggregationFunctionIntervalType =
                                    record.SearchInterval;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction Rule Aggregation Function Interval Type value as {modelAbstractionRule.AbstractionRuleAggregationFunctionIntervalType}.");
                                }
                            }
                            else
                            {
                                modelAbstractionRule.AbstractionRuleAggregationFunctionIntervalType = "d";

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Abstraction Rule Aggregation Function Interval Type value as {modelAbstractionRule.AbstractionRuleAggregationFunctionIntervalType}.");
                                }
                            }

                            if (record.SearchInterval != null)
                            {
                                modelAbstractionRule.AbstractionHistoryIntervalValue =
                                    record.SearchValue;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction Rule Aggregation Function Interval Value value as {modelAbstractionRule.AbstractionRuleAggregationFunctionIntervalType}.");
                                }
                            }
                            else
                            {
                                modelAbstractionRule.AbstractionHistoryIntervalValue = 0;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set DEFAULT Abstraction Rule Aggregation Function Interval Value value as {modelAbstractionRule.AbstractionRuleAggregationFunctionIntervalType}.");
                                }
                            }

                            if (record.SearchFunctionTypeId.HasValue)
                            {
                                modelAbstractionRule.AbstractionRuleAggregationFunctionType =
                                    record.SearchFunctionTypeId.Value;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction Rule Aggregation Function Type Value as {modelAbstractionRule.AbstractionRuleAggregationFunctionType}.");
                                }
                            }
                            else
                            {
                                modelAbstractionRule.AbstractionRuleAggregationFunctionType = 1;

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule {modelAbstractionRule.Id} set Abstraction History Search Function Type Id as {modelAbstractionRule.AbstractionRuleAggregationFunctionType}.");
                                }
                            }

                            context.Services.CancellationToken.ThrowIfCancellationRequested();

                            var hasRuleScript = false;
                            var parsedRule = new ParsedRule
                            {
                                ErrorSpans = []
                            };

                            if (record.BuilderRuleScript != null && modelAbstractionRule.RuleScriptTypeId == 1)
                            {
                                parsedRule.OriginalRuleText = record.BuilderRuleScript;
                                parsedRule = context.Services.Parser.TranslateFromDotNotation(parsedRule, true);
                                parsedRule = context.Services.Parser.Parse(parsedRule);

                                if (parsedRule.ErrorSpans.Count == 0)
                                {
                                    modelAbstractionRule.AbstractionRuleScript = parsedRule.ParsedRuleText;
                                    hasRuleScript = true;

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} set builder script as {modelAbstractionRule.AbstractionRuleScript}.");
                                    }
                                }
                            }
                            else if (record.CoderRuleScript != null && modelAbstractionRule.RuleScriptTypeId == 2)
                            {
                                parsedRule.OriginalRuleText = record.CoderRuleScript;
                                parsedRule = context.Services.Parser.TranslateFromDotNotation(parsedRule);
                                parsedRule = context.Services.Parser.Parse(parsedRule);

                                if (parsedRule.ErrorSpans.Count == 0)
                                {
                                    modelAbstractionRule.AbstractionRuleScript = parsedRule.ParsedRuleText;

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} set coder script as {modelAbstractionRule.AbstractionRuleScript}.");
                                    }

                                    hasRuleScript = true;
                                }
                            }

                            if (modelAbstractionRule.Search)
                            {
                                if (shadowDistinctSearchKeys.ContainsKey(modelAbstractionRule.SearchKey))
                                {
                                    if (!shadowDistinctSearchKeys[modelAbstractionRule.SearchKey]
                                            .SelectedPayloadData.ContainsKey(modelAbstractionRule.SearchFunctionKey))
                                    {
                                        if (context.Services.Log.IsDebugEnabled)
                                        {
                                            context.Services.Log.Debug(
                                                $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} has recognised {modelAbstractionRule.SearchFunctionKey} as being required of the select given function key with a cast of float.");
                                        }

                                        shadowDistinctSearchKeys[modelAbstractionRule.SearchKey]
                                            .SelectedPayloadData.Add(modelAbstractionRule.SearchFunctionKey,
                                                new SelectedPayloadData
                                                {
                                                    Name = modelAbstractionRule.SearchFunctionKey,
                                                    DatabaseCast = "::float8",
                                                    DefaultValue = "0"
                                                }
                                            );
                                    }
                                    else
                                    {
                                        if (context.Services.Log.IsDebugEnabled)
                                        {
                                            context.Services.Log.Debug(
                                                $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} has recognised {modelAbstractionRule.SearchFunctionKey} already exists for select as function key.");
                                        }
                                    }

                                    foreach (var selectedPayloadData in parsedRule.SelectedPayloadData)
                                    {
                                        context.Services.CancellationToken.ThrowIfCancellationRequested();

                                        if (!shadowDistinctSearchKeys[modelAbstractionRule.SearchKey]
                                                .SelectedPayloadData.ContainsKey(selectedPayloadData.Value.Name))
                                        {
                                            if (context.Services.Log.IsDebugEnabled)
                                            {
                                                context.Services.Log.Debug(
                                                    $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} has recognised {selectedPayloadData.Value.Name} as being required of the select with a cast of {selectedPayloadData.Value.DatabaseCast}.");
                                            }

                                            shadowDistinctSearchKeys[modelAbstractionRule.SearchKey]
                                                .SelectedPayloadData.Add(selectedPayloadData.Value.Name,
                                                    selectedPayloadData.Value);
                                        }
                                        else
                                        {
                                            if (context.Services.Log.IsDebugEnabled)
                                            {
                                                context.Services.Log.Debug(
                                                    $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} has recognised {selectedPayloadData.Value.Name} already exists for select.");
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} has looked for search key {modelAbstractionRule.SearchKey} but it is not there.");
                                    }
                                }
                            }
                            else
                            {
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} does not need to compile data into a search key as it is not a search abstraction rule.");
                                }
                            }

                            if (!hasRuleScript)
                            {
                                continue;
                            }

                            var abstractionRuleScript = new StringBuilder();
                            abstractionRuleScript.Append("Imports System.IO\r\n");
                            abstractionRuleScript.Append("Imports log4net\r\n");
                            abstractionRuleScript.Append("Imports System.Net\r\n");
                            abstractionRuleScript.Append("Imports System.Collections.Generic\r\n");
                            abstractionRuleScript.Append("Imports Jube.Dictionary\r\n");
                            abstractionRuleScript.Append("Imports Jube.Dictionary.Extensions\r\n");
                            abstractionRuleScript.Append("Imports System\r\n");
                            abstractionRuleScript.Append("Public Class AbstractionRule\r\n");
                            abstractionRuleScript.Append(
                                "Public Shared Function Match(Data As DictionaryNoBoxing(Of String),List as Dictionary(Of String,List(Of String)),KVP as PooledDictionary(of String,Double),Log as ILog) As Boolean\r\n");
                            abstractionRuleScript.Append("Dim Matched as Boolean\r\n");
                            abstractionRuleScript.Append("Try\r\n");
                            abstractionRuleScript.Append(modelAbstractionRule.AbstractionRuleScript + "\r\n");
                            abstractionRuleScript.Append("Catch ex As Exception\r\n");
                            abstractionRuleScript.Append("Log.Info(ex.ToString)\r\n");
                            abstractionRuleScript.Append("End Try\r\n");
                            abstractionRuleScript.Append("Return Matched\r\n");
                            abstractionRuleScript.Append("\r\n");
                            abstractionRuleScript.Append("End Function\r\n");
                            abstractionRuleScript.Append("End Class\r\n");

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} set class as {abstractionRuleScript}.");
                            }

                            var abstractionRuleScriptHash = HashHelper.GetHash(abstractionRuleScript.ToString());

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} calculated hash as {abstractionRuleScriptHash}.  Checking if in hash cache.");
                            }

                            if (context.Caching.HashCacheAssembly.TryGetValue(abstractionRuleScriptHash, out var valueHash))
                            {
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} hash {abstractionRuleScriptHash} is in the hash cache,  will create the delegate from this.");
                                }

                                modelAbstractionRule.AbstractionRuleCompile =
                                    valueHash;
                                var classType =
                                    modelAbstractionRule.AbstractionRuleCompile.GetType("AbstractionRule");
                                var methodInfo = classType.GetMethod("Match");
                                modelAbstractionRule.AbstractionRuleCompileDelegate =
                                    (EntityAnalysisModelAbstractionRule.Match)Delegate.CreateDelegate(
                                        typeof(EntityAnalysisModelAbstractionRule.Match), methodInfo);
                                shadowEntityModelAbstractionRule.Add(modelAbstractionRule);

                                await repository.UpdateCompileStatusAsync(modelAbstractionRule.Id, true, null,
                                    context.Services.CancellationToken).ConfigureAwait(false);

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} hash {abstractionRuleScriptHash} created delegate.");
                                }
                            }
                            else
                            {
                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} hash {abstractionRuleScriptHash} is not in the hash cache,  will proceed to compile.");
                                }

                                var compile = new Compile();
                                compile.CompileCode(abstractionRuleScript.ToString(), context.Services.Log,
                                [
                                    Path.Combine(context.Paths.BinaryPath ?? throw new InvalidOperationException(), "log4net.dll"),
                                    Path.Combine(context.Paths.BinaryPath, "Jube.Dictionary.dll")
                                ], Compile.Language.Vb);

                                if (context.Services.Log.IsDebugEnabled)
                                {
                                    context.Services.Log.Debug(
                                        $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} hash {abstractionRuleScriptHash} has been compiled with {compile.Errors}.");
                                }

                                if (compile.Errors == null)
                                {
                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} hash {abstractionRuleScriptHash} has been compiled without error and will proceed to create delegate.");
                                    }

                                    modelAbstractionRule.AbstractionRuleCompile = compile.CompiledAssembly;

                                    var classType =
                                        modelAbstractionRule.AbstractionRuleCompile.GetType("AbstractionRule");
                                    var methodInfo = classType.GetMethod("Match");

                                    modelAbstractionRule.AbstractionRuleCompileDelegate =
                                        (EntityAnalysisModelAbstractionRule.Match)Delegate.CreateDelegate(
                                            typeof(EntityAnalysisModelAbstractionRule.Match), methodInfo);

                                    context.Caching.HashCacheAssembly.TryAdd(abstractionRuleScriptHash, compile.CompiledAssembly);
                                    context.Caching.HashCacheAssemblyMetadata.TryAdd(abstractionRuleScriptHash,
                                        new HashCacheAssemblyPayload(compile.CompiledAssemblyBytes, compile.CompiledAssemblyBinary, abstractionRuleScript.ToString()));

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} hash {abstractionRuleScriptHash} has created delegate and added it to the hash cache.");
                                    }

                                    shadowEntityModelAbstractionRule.Add(modelAbstractionRule);

                                    await repository.UpdateCompileStatusAsync(modelAbstractionRule.Id, true, null,
                                        context.Services.CancellationToken).ConfigureAwait(false);

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} hash {abstractionRuleScriptHash} is being added to the shadow list of Abstraction Rules.");
                                    }
                                }
                                else
                                {
                                    await repository.UpdateCompileStatusAsync(modelAbstractionRule.Id, false, compile.ErrorsSummary,
                                        context.Services.CancellationToken).ConfigureAwait(false);

                                    if (context.Services.Log.IsDebugEnabled)
                                    {
                                        context.Services.Log.Debug(
                                            $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} hash {abstractionRuleScriptHash} has failed to load.");
                                    }
                                }
                            }

                            modelAbstractionRule.LogicHash =
                                HashHelper.GetHash(modelAbstractionRule.AbstractionRuleScript);

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} hash {modelAbstractionRule.LogicHash} has been attached to the rule to avoid duplication in execution of abstraction rules.");
                            }

                            context.Services.Parser.EntityAnalysisModelsAbstractionRule.TryAdd(modelAbstractionRule.Name);

                            if (context.Services.Log.IsDebugEnabled)
                            {
                                context.Services.Log.Debug(
                                    $"Entity Start: Entity Model {key} and Abstraction Rule Model {modelAbstractionRule.Id} name {modelAbstractionRule.Name} added to parser");
                            }
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            context.Services.Log.Error(
                                $"Entity Start: Abstraction Rules ID {record.Id} returned for model {key} has created an error as {ex}.");

                            await repository.UpdateCompileStatusAsync(record.Id, false, ex.Message,
                                context.Services.CancellationToken).ConfigureAwait(false);
                        }
                    }

                    foreach (var distinctSearchKey in shadowDistinctSearchKeys)
                    {
                        context.Services.CancellationToken.ThrowIfCancellationRequested();

                        distinctSearchKey.Value.SqlSelect =
                            $"select a.\"CreatedDate\",a.\"ReferenceDate\" AS \"{value.References.ReferenceDateName}\"";

                        foreach (var selectedPayloadData in distinctSearchKey.Value.SelectedPayloadData)
                        {
                            context.Services.CancellationToken.ThrowIfCancellationRequested();

                            distinctSearchKey.Value.SqlSelect +=
                                $",COALESCE((a.\"Json\" -> 'payload' ->> '{selectedPayloadData.Key}'){selectedPayloadData.Value.DatabaseCast}," +
                                $"{selectedPayloadData.Value.DefaultValue}) AS \"{selectedPayloadData.Key}\"";
                        }

                        distinctSearchKey.Value.SqlSelectFrom =
                            " from \"Archive\" a inner join \"EntityAnalysisModel\" e on a.\"EntityAnalysisModelId\" = e.\"Id\""
                            + " where e.\"Guid\" = uuid('"
                            + value.Instance.Guid + "') and a.\"Json\" -> 'payload' ->> (@key) = (@value) ";

                        distinctSearchKey.Value.SqlSelectOrderBy =
                            " order by a.\"ReferenceDate\" desc limit (@limit)";
                    }

                    value.Collections.ModelAbstractionRules = shadowEntityModelAbstractionRule;
                    value.Collections.DistinctSearchKeys = shadowDistinctSearchKeys;

                    if (context.Services.Log.IsDebugEnabled)
                    {
                        context.Services.Log.Debug(
                            $"Entity Start: Entity Model {key} and Abstraction Rule Model has replaced the Abstraction Rule list with the shadow values and closed the reader.");
                    }
                }

                if (context.Services.Log.IsDebugEnabled)
                {
                    context.Services.Log.Debug("Entity Start: Completed adding Abstraction Rules to entity models.");
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                context.Services.Log.Error($"SyncEntityAnalysisModelAbstractionRulesAsync: has produced an error {ex}");

                await new EntityAnalysisModelSynchronisationErrorRepository(context.Services.DbContext)
                    .InsertAsync(EntityAnalysisModelSynchronisationErrorRepository.EntityAnalysisModelSynchronisationErrorStepEnum.AbstractionRules, ex.ToString(),
                        context.Services.CancellationToken).ConfigureAwait(false);
            }

            return context;
        }
    }
}
