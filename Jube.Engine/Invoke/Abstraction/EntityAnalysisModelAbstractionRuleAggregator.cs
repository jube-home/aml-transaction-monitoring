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

namespace Jube.Engine.Invoke.Abstraction
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Accord.Statistics;
    using Dictionary;
    using Helpers;
    using log4net;
    using Model;
    using Model.Processing.Payload;

    public static class EntityAnalysisModelAbstractionRuleAggregator
    {
        public static double Aggregate(EntityAnalysisModelInstanceEntryPayload payload,
            Dictionary<int, List<DictionaryNoBoxing>> abstractionRuleMatches,
            EntityAnalysisModelAbstractionRule abstractionRule, ILog log)
        {
            double abstractionValue = 0;
            try
            {
                if (log.IsDebugEnabled)
                {
                    log.Debug(
                        $"Abstraction Aggregation: The aggregator has been called for GUID payload {payload.EntityAnalysisModelInstanceGuid} to aggregate {abstractionRuleMatches.Count} on Abstraction rule {abstractionRule.Id}.");
                }

                if (abstractionRuleMatches.TryGetValue(abstractionRule.Id, out var matches))
                {
                    var matchesCount = matches.Count;

                    var (skip, fetch) = abstractionRule.EnableOffset
                        ? abstractionRule.OffsetType switch
                        {
                            0 => (0, matchesCount),
                            1 => (abstractionRule.OffsetValue, 1),
                            2 => (matchesCount - (1 + abstractionRule.OffsetValue), 1),
                            3 => (abstractionRule.OffsetValue, matchesCount - abstractionRule.OffsetValue),
                            4 => (matchesCount - abstractionRule.OffsetValue, abstractionRule.OffsetValue),
                            _ => (0, matchesCount)
                        }
                        : (0, matchesCount);

                    var rangeCacheDocumentsList = matches.Skip(skip).Take(fetch).ToList();

                    if (log.IsDebugEnabled)
                    {
                        log.Debug(
                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id}.");
                    }

                    switch (abstractionRule.AbstractionRuleAggregationFunctionType)
                    {
                        case 1:// Count
                            if (log.IsDebugEnabled)
                            {
                                log.Debug(
                                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will count the entries in the collection.");
                            }

                            abstractionValue = rangeCacheDocumentsList.Count;

                            if (log.IsDebugEnabled)
                            {
                                log.Debug(
                                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} and{abstractionRule.Id} has a count value of {abstractionValue}.");
                            }

                            break;
                        case 2:// Distinct Count
                        {
                            if (log.IsDebugEnabled)
                            {
                                log.Debug(
                                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will distinct count the entries in the collection.");
                            }

                            var distinctSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                            foreach (var cacheDocumentEntry in rangeCacheDocumentsList)
                            foreach (var (key, value) in cacheDocumentEntry)
                            {
                                if (!String.Equals(key, abstractionRule.SearchFunctionKey,
                                        StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }
                                
                                distinctSet.Add(value.ToString());
                                break;
                            }

                            abstractionValue = distinctSet.Count;

                            if (log.IsDebugEnabled)
                            {
                                log.Debug(
                                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} and{abstractionRule.Id} has a distinct count value of {abstractionValue}.");
                            }

                            break;
                        }
                        case 12:// Same Count
                        {
                            if (log.IsDebugEnabled)
                            {
                                log.Debug(
                                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will same count the entries in the collection.");
                            }

                            foreach (var cacheDocumentEntry in rangeCacheDocumentsList)
                            foreach (var (key, value) in cacheDocumentEntry)
                            {
                                if (!String.Equals(key,
                                        abstractionRule.SearchFunctionKey,
                                        StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }

                                if (String.Equals(payload.Payload[key].ToString(), value.ToString(),
                                        StringComparison.OrdinalIgnoreCase))
                                {
                                    abstractionValue += 1;
                                }

                                break;
                            }

                            if (log.IsDebugEnabled)
                            {
                                log.Debug(
                                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} and{abstractionRule.Id} has a same count value of {abstractionValue}.");
                            }

                            break;
                        }
                        default:
                        {
                            var cacheDocumentsList = rangeCacheDocumentsList.ToList();

                            switch (abstractionRule.AbstractionRuleAggregationFunctionType)
                            {
                                case 13:// Raw
                                    if (log.IsDebugEnabled)
                                    {
                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will raw value the entries in the collection.");
                                    }

                                    try
                                    {
                                        var lastElement = cacheDocumentsList[^1];
                                        if (lastElement.TryGetValue(abstractionRule.SearchFunctionKey, out var value))
                                        {
                                            abstractionValue = value.AsDouble();

                                            if (log.IsDebugEnabled)
                                            {
                                                log.Debug(
                                                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} found the key for raw value {abstractionRule.SearchFunctionKey}.");
                                            }
                                        }
                                        else
                                        {
                                            abstractionValue = 0;

                                            if (log.IsDebugEnabled)
                                            {
                                                log.Debug(
                                                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} Could not find the key for raw value {abstractionRule.SearchFunctionKey}.");
                                            }
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        abstractionValue = 0;
                                    }

                                    if (log.IsDebugEnabled)
                                    {
                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} and{abstractionRule.Id} has a raw value of {abstractionValue}.");
                                    }

                                    break;
                                case 16:// Since
                                {
                                    if (log.IsDebugEnabled)
                                    {
                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will Since date the entries in the collection.");
                                    }

                                    var lastMatch = matches[^1];

                                    if (lastMatch.TryGetValue(abstractionRule.SearchFunctionKey, out var currentDateValue) &&
                                        DateTime.TryParse(currentDateValue.ToString(), out var sinceCurrentDate))
                                    {
                                        if (log.IsDebugEnabled)
                                        {
                                            log.Debug(
                                                $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} is using the {abstractionRule.SearchFunctionKey} for the Current Date Value.");
                                        }
                                    }
                                    else
                                    {
                                        sinceCurrentDate = lastMatch["CreatedDate"];

                                        if (log.IsDebugEnabled)
                                        {
                                            log.Debug(
                                                $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} is using the CreatedDate for the Current Date Value.");
                                        }
                                    }

                                    var lastElement = cacheDocumentsList[^1];

                                    if (lastElement.TryGetValue(abstractionRule.SearchFunctionKey, out var testDateValue) &&
                                        DateTime.TryParse(testDateValue.ToString(), out var sinceTestDate))
                                    {
                                        if (log.IsDebugEnabled)
                                        {
                                            log.Debug(
                                                $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} is using the {abstractionRule.SearchFunctionKey} for the Test Date Value.");
                                        }
                                    }
                                    else
                                    {
                                        sinceTestDate = lastElement["CreatedDate"];

                                        if (log.IsDebugEnabled)
                                        {
                                            log.Debug(
                                                $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} is using the CreatedDate for the Test Date Value.");
                                        }
                                    }

                                    abstractionValue = abstractionRule.AbstractionRuleAggregationFunctionIntervalType switch
                                    {
                                        "s" => DateHelper.DateDiff(DateHelper.DateInterval.Second, sinceTestDate, sinceCurrentDate),
                                        "h" => DateHelper.DateDiff(DateHelper.DateInterval.Hour, sinceTestDate, sinceCurrentDate),
                                        "m" => DateHelper.DateDiff(DateHelper.DateInterval.Minute, sinceTestDate, sinceCurrentDate),
                                        "d" => DateHelper.DateDiff(DateHelper.DateInterval.Day, sinceTestDate, sinceCurrentDate),
                                        _ => 0
                                    };

                                    if (log.IsDebugEnabled)
                                    {
                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} and{abstractionRule.Id} has a since date value of {abstractionValue}.");
                                    }

                                    break;
                                }
                                default:
                                {
                                    if (log.IsDebugEnabled)
                                    {
                                        log.Debug(
                                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found matches for {abstractionRule.Id} and will use Extreme stats on an array.");
                                    }

                                    var values = new double[cacheDocumentsList.Count];
                                    for (var i = 0; i < cacheDocumentsList.Count; i++)
                                    {
                                        var document = cacheDocumentsList[i];
                                        if (!document.TryGetValue(abstractionRule.SearchFunctionKey, out var value))
                                        {
                                            continue;
                                        }
                                        try
                                        {
                                            values[i] = value.AsDouble();
                                        }
                                        catch (Exception ex)
                                        {
                                            if (log.IsInfoEnabled)
                                            {
                                                log.Info(
                                                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found error on {abstractionRule.Id} as {ex}.");
                                            }
                                        }
                                    }

                                    abstractionValue = abstractionRule.AbstractionRuleAggregationFunctionType switch
                                    {
                                        3 => values.Sum(),
                                        4 => values.Mean(),
                                        5 => values.Median(),
                                        6 => values.Kurtosis(),
                                        7 => values.Skewness(),
                                        8 => values.StandardDeviation(),
                                        9 => values.StandardDeviation() + values.Mean(),
                                        10 or 12 => values.StandardDeviation() * 2 + values.Mean(),
                                        11 => values.Mode(),
                                        14 => values.Max(),
                                        15 => values.Min(),
                                        _ => 0
                                    };

                                    break;
                                }
                            }

                            break;
                        }
                    }
                }
                else
                {
                    abstractionValue = 0;

                    if (log.IsDebugEnabled)
                    {
                        log.Debug(
                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} has found no matches for {abstractionRule.Id} returned zero.");
                    }
                }

                if (Double.IsNaN(abstractionValue) || Double.IsInfinity(abstractionValue))
                {
                    abstractionValue = 0;

                    if (log.IsDebugEnabled)
                    {
                        log.Debug(
                            $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} seems to be a NaN or Infinity. Swapped to Zero.");
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error(
                    $"Abstraction Aggregation: payload GUID {payload.EntityAnalysisModelInstanceGuid} for {abstractionRule.Id} is in error as{ex}.");

                abstractionValue = 0;
            }

            return abstractionValue;
        }
    }
}
