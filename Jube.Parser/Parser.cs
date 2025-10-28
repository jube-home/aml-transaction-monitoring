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

namespace Jube.Parser
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using log4net;

    public class Parser
    {
        private readonly ILog log;
        private readonly List<string> ruleScriptTokens;
        public List<string> EntityAnalysisModelAbstractionCalculations;
        public Dictionary<string, EntityAnalysisModelRequestXPath> EntityAnalysisModelRequestXPaths;
        public List<string> EntityAnalysisModelsAbstractionRule;
        public List<string> EntityAnalysisModelsDictionaries;
        public List<string> EntityAnalysisModelsExhaustiveAdaptations;
        public List<string> EntityAnalysisModelsHttpAdaptations;
        public List<string> EntityAnalysisModelsLists;
        public List<string> EntityAnalysisModelsSanctions;
        public List<string> EntityAnalysisModelsTtlCounters;

        public Parser(ILog log,
            List<string> ruleScriptTokens
        )
        {
            this.log = log;
            this.ruleScriptTokens = ruleScriptTokens ?? [];

            if (!this.ruleScriptTokens.Contains("Return"))
            {
                this.ruleScriptTokens.Add("return");
            }

            if (!this.ruleScriptTokens.Contains("If"))
            {
                this.ruleScriptTokens.Add("if");
            }

            if (!this.ruleScriptTokens.Contains("Then"))
            {
                this.ruleScriptTokens.Add("then");
            }

            if (!this.ruleScriptTokens.Contains("End If"))
            {
                this.ruleScriptTokens.Add("End If");
            }

            if (!this.ruleScriptTokens.Contains("False"))
            {
                this.ruleScriptTokens.Add("False");
            }

            if (!this.ruleScriptTokens.Contains("true"))
            {
                this.ruleScriptTokens.Add("true");
            }

            if (!this.ruleScriptTokens.Contains("Payload"))
            {
                this.ruleScriptTokens.Add("Payload");
            }

            if (!this.ruleScriptTokens.Contains("Abstraction"))
            {
                this.ruleScriptTokens.Add("Abstraction");
            }

            if (!this.ruleScriptTokens.Contains("Activation"))
            {
                this.ruleScriptTokens.Add("Activation");
            }

            if (!this.ruleScriptTokens.Contains("ExhaustiveAdaptation"))
            {
                this.ruleScriptTokens.Add("ExhaustiveAdaptation");
            }

            if (!this.ruleScriptTokens.Contains("HttpAdaptation"))
            {
                this.ruleScriptTokens.Add("HttpAdaptation");
            }

            if (!this.ruleScriptTokens.Contains("Select"))
            {
                this.ruleScriptTokens.Add("Select");
            }

            if (!this.ruleScriptTokens.Contains("Case"))
            {
                this.ruleScriptTokens.Add("Case");
            }

            if (!this.ruleScriptTokens.Contains("End Select"))
            {
                this.ruleScriptTokens.Add("End Select");
            }

            if (!this.ruleScriptTokens.Contains("Contains"))
            {
                this.ruleScriptTokens.Add("Contains");
            }

            if (!this.ruleScriptTokens.Contains("Sanctions"))
            {
                this.ruleScriptTokens.Add("Sanctions");
            }

            if (!this.ruleScriptTokens.Contains("KVP"))
            {
                this.ruleScriptTokens.Add("KVP");
            }

            if (!this.ruleScriptTokens.Contains("List"))
            {
                this.ruleScriptTokens.Add("List");
            }

            if (!this.ruleScriptTokens.Contains("TTLCounter"))
            {
                this.ruleScriptTokens.Add("TTLCounter");
            }

            if (!this.ruleScriptTokens.Contains("String"))
            {
                this.ruleScriptTokens.Add("String");
            }

            if (!this.ruleScriptTokens.Contains("Double"))
            {
                this.ruleScriptTokens.Add("Double");
            }

            if (!this.ruleScriptTokens.Contains("Integer"))
            {
                this.ruleScriptTokens.Add("Integer");
            }

            if (!this.ruleScriptTokens.Contains("DateTime"))
            {
                this.ruleScriptTokens.Add("DateTime");
            }

            if (!this.ruleScriptTokens.Contains("AsString"))
            {
                this.ruleScriptTokens.Add("AsString");
            }

            if (!this.ruleScriptTokens.Contains("AsInt"))
            {
                this.ruleScriptTokens.Add("AsInt");
            }

            if (!this.ruleScriptTokens.Contains("AsDouble"))
            {
                this.ruleScriptTokens.Add("AsDouble");
            }

            if (!this.ruleScriptTokens.Contains("AsDateTime"))
            {
                this.ruleScriptTokens.Add("AsDateTime");
            }

            if (!this.ruleScriptTokens.Contains("AsBool"))
            {
                this.ruleScriptTokens.Add("AsBool");
            }

            if (!this.ruleScriptTokens.Contains("Data"))
            {
                this.ruleScriptTokens.Add("Data");
            }

            if (!this.ruleScriptTokens.Contains("Calculation"))
            {
                this.ruleScriptTokens.Add("Calculation");
            }

            if (!this.ruleScriptTokens.Contains("Not"))
            {
                this.ruleScriptTokens.Add("Not");
            }
            
            if (!this.ruleScriptTokens.Contains("AND"))
            {
                this.ruleScriptTokens.Add("AND");
            }
            
            if (!this.ruleScriptTokens.Contains("OR"))
            {
                this.ruleScriptTokens.Add("OR");
            }
        }

        public ParsedRule Parse(ParsedRule parsedRule)
        {
            try
            {
                var lines = parsedRule.ParsedRuleText.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
                var softParseFailed = false;
                var i = 0;
                foreach (var line in lines)
                {
                    if (!String.IsNullOrEmpty(line))
                    {
                        //Remove strings as they are allowed to have special characters
                        var matches = Regex.Matches(line, "\"(?:[^\"\\\\]|\\\\.)*\"");

                        string[] separator =
                            [",", " ", "(", ")", "=", ">", "<", ">=", "<=", "<>", ".", "_", "+", "-", "/", "*", "&"];
                        var tokens = matches.Aggregate(line, (current, match) => current.Replace(match.Value, ""))
                            .Split(separator, StringSplitOptions.RemoveEmptyEntries);
                        int j;
                        for (j = 0; j < tokens.Length; j++)
                        {
                            var valid = false;
                            if (NumericHelper.IsNumeric(tokens[j]))
                            {
                                valid = true;
                            }
                            else
                                //Loop around all permissions, matching up with the tokens that have been found in the rule text.
                            {
                                foreach (var ruleScriptToken in ruleScriptTokens)
                                {
                                    //There is a curious case of valid language tokens such as "End If," which is logically a single token, but would be read as two tokens.  As seen above, it is possible to store such logical tokens in the registry.
                                    var permissionTokensCount = ruleScriptToken.Split(" ".ToCharArray()).Length;
                                    //Find out how many tokens exist inside this token.

                                    var testToken = "";
                                    int f;
                                    //This joins up the token for matching based on the next number of tokens
                                    for (f = 0; f < permissionTokensCount; f++)
                                    {
                                        if (f > 0)
                                        {
                                            testToken += " ";
                                        }

                                        var extend = j + f;//We are adding the tokens that come after.
                                        if (extend < tokens.Length)
                                        {
                                            testToken += tokens[extend];
                                        }
                                        //A new test token has been constructed, for example, End If
                                    }

                                    if (String.Equals(ruleScriptToken, testToken,
                                            StringComparison.CurrentCultureIgnoreCase))
                                        //Check fof the test token (perhaps derived) matches.
                                    {
                                        valid = true;
                                    }
                                }
                            }

                            if (valid)
                            {
                                continue;//This would be enough to kill the routine, return false.
                            }

                            softParseFailed = true;

                            parsedRule.ErrorSpans.Add(new ErrorSpan
                            {
                                Line = i,
                                Message =
                                    $"Line {i + 1}: Security restricted token '{tokens[j]}'  has been discovered in user code."
                            });
                        }
                    }

                    i += 1;
                }

                if (softParseFailed)//Any error causes this, although all issues are in the logs.x
                {
                    if (log.IsInfoEnabled)
                    {
                        log.Info($"Soft Parser: User code has failed a soft parse: {parsedRule.OriginalRuleText}");
                    }
                }
            }
            catch (Exception ex)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Soft Parser: User code has failed a soft parse: {parsedRule.OriginalRuleText} and error {ex}");
                }
            }

            return parsedRule;
        }

        public ParsedRule WrapGatewayRule(ParsedRule parsedRule, bool tryCatchWrap)
        {
            var sb = new StringBuilder();

            var countLine = 1;
            sb.AppendLine("Imports System.IO");

            countLine += 1;
            sb.AppendLine("Imports System.Xml");

            countLine += 1;
            sb.AppendLine("Imports log4net");

            countLine += 1;
            sb.AppendLine("Imports System.Net");

            countLine += 1;
            sb.AppendLine("Imports Jube.Dictionary");

            countLine += 1;
            sb.AppendLine("Imports System.Collections.Generic");

            countLine += 1;
            sb.AppendLine("Imports System");

            countLine += 1;
            sb.AppendLine("Public Class GatewayRule");

            countLine += 1;
            sb.AppendLine(
                "Public Shared Function Match(Data As DictionaryNoBoxing,List As Dictionary(Of String, List(Of String)), KVP As PooledDictionary(Of String, Double),Log as ILog) As Boolean");

            countLine += 1;
            sb.AppendLine("Dim Matched as Boolean");

            if (tryCatchWrap)
            {
                sb.AppendLine("Try");
                countLine += 1;
            }

            parsedRule.CharOffset = sb.Length;
            parsedRule.LineOffset = countLine;
            sb.AppendLine(parsedRule.ParsedRuleText);

            if (tryCatchWrap)
            {
                sb.AppendLine("Catch ex As Exception");
                sb.AppendLine("Log.Info(ex.ToString)");
                sb.AppendLine("End Try");
            }

            sb.AppendLine("Return Matched");
            sb.AppendLine("End Function");
            sb.AppendLine("End Class");
            parsedRule.ParsedRuleText = sb.ToString();

            return parsedRule;
        }

        public ParsedRule WrapAbstractionRule(ParsedRule parsedRule, bool tryCatchWrap)
        {
            var sb = new StringBuilder();

            var countLine = 1;
            sb.AppendLine("Imports System.IO");

            countLine += 1;
            sb.AppendLine("Imports System.Xml");

            countLine += 1;
            sb.AppendLine("Imports log4net");

            countLine += 1;
            sb.AppendLine("Imports System.Net");

            countLine += 1;
            sb.AppendLine("Imports Jube.Dictionary");

            countLine += 1;
            sb.AppendLine("Imports System.Collections.Generic");

            countLine += 1;
            sb.AppendLine("Imports System");

            countLine += 1;
            sb.AppendLine("Public Class GatewayRule");

            countLine += 1;
            sb.AppendLine(
                "Public Shared Function Match(Data As DictionaryNoBoxing,List As Dictionary(Of String, List(Of String)), KVP As PooledDictionary(Of String, Double),Log as ILog) As Boolean");

            countLine += 1;
            sb.AppendLine("Dim Matched as Boolean");

            if (tryCatchWrap)
            {
                sb.AppendLine("Try");
                countLine += 1;
            }

            parsedRule.CharOffset = sb.Length;
            parsedRule.LineOffset = countLine;
            sb.AppendLine(parsedRule.ParsedRuleText);

            if (tryCatchWrap)
            {
                sb.AppendLine("Catch ex As Exception");
                sb.AppendLine("Log.Info(ex.ToString)");
                sb.AppendLine("End Try");
            }

            sb.AppendLine("Return Matched");
            sb.AppendLine("End Function");
            sb.AppendLine("End Class");
            parsedRule.ParsedRuleText = sb.ToString();

            return parsedRule;
        }

        public ParsedRule WrapActivationRule(ParsedRule parsedRule, bool tryCatchWrap)
        {
            var sb = new StringBuilder();

            var countLine = 1;
            sb.AppendLine("Imports System.IO");

            countLine += 1;
            sb.AppendLine("Imports System.Xml");

            countLine += 1;
            sb.AppendLine("Imports log4net");

            countLine += 1;
            sb.AppendLine("Imports System.Net");

            countLine += 1;
            sb.AppendLine("Imports System.Collections.Generic");

            countLine += 1;
            sb.AppendLine("Imports Jube.Dictionary");

            countLine += 1;
            sb.AppendLine("Imports System");

            countLine += 1;
            sb.AppendLine("Public Class GatewayRule");

            countLine += 1;
            sb.AppendLine(
                "Public Shared Function Match(Data As DictionaryNoBoxing,TTLCounter As PooledDictionary(Of String, Integer),Abstraction As PooledDictionary(Of String, Double),HttpAdaptation As Dictionary(Of String, Double),ExhaustiveAdaptation As PooledDictionary(Of String, Double),List as PooledDictionary(Of String,List(Of String)),Deviation as PooledDictionary(Of String, Double),Calculation As PooledDictionary(Of String, Double),Sanctions As PooledDictionary(Of String, Double),KVP As PooledDictionary(Of String, Double),Log as ILog) As Boolean");

            countLine += 1;
            sb.AppendLine("Dim Matched as Boolean");

            if (tryCatchWrap)
            {
                sb.AppendLine("Try");
                countLine += 1;
            }

            parsedRule.CharOffset = sb.Length;
            parsedRule.LineOffset = countLine;
            sb.AppendLine(parsedRule.ParsedRuleText);

            if (tryCatchWrap)
            {
                sb.AppendLine("Catch ex As Exception");
                sb.AppendLine("Log.Info(ex.ToString)");
                sb.AppendLine("End Try");
            }

            sb.AppendLine("Return Matched");
            sb.AppendLine("End Function");
            sb.AppendLine("End Class");
            parsedRule.ParsedRuleText = sb.ToString();

            return parsedRule;
        }

        public ParsedRule WrapAbstractionCalculation(ParsedRule parsedRule, bool tryCatchWrap)
        {
            var sb = new StringBuilder();

            var countLine = 1;
            sb.AppendLine("Imports System.IO");

            countLine += 1;
            sb.AppendLine("Imports System.Xml");

            countLine += 1;
            sb.AppendLine("Imports log4net");

            countLine += 1;
            sb.AppendLine("Imports System.Net");

            countLine += 1;
            sb.AppendLine("Imports System.Collections.Generic");

            countLine += 1;
            sb.AppendLine("Imports Jube.Dictionary");

            countLine += 1;
            sb.AppendLine("Imports System");

            countLine += 1;
            sb.AppendLine("Public Class GatewayRule");

            countLine += 1;
            sb.AppendLine(
                "Public Shared Function Match(Data As DictionaryNoBoxing,TTLCounter As PooledDictionary(Of String, Integer),Abstraction As PooledDictionary(Of String, Double),List as Dictionary(Of String,List(Of String)),Deviation as PooledDictionary(Of String, Double),Calculation As PooledDictionary(Of String, Double),Sanctions As PooledDictionary(Of String, Double),KVP As PooledDictionary(Of String, Double),Log as ILog) As Boolean");

            countLine += 1;
            sb.AppendLine("Dim Matched as Boolean");

            if (tryCatchWrap)
            {
                sb.AppendLine("Try");
                countLine += 1;
            }

            parsedRule.CharOffset = sb.Length;
            parsedRule.LineOffset = countLine;
            sb.AppendLine(parsedRule.ParsedRuleText);

            if (tryCatchWrap)
            {
                sb.AppendLine("Catch ex As Exception");
                sb.AppendLine("Log.Info(ex.ToString)");
                sb.AppendLine("End Try");
            }

            sb.AppendLine("Return Matched");
            sb.AppendLine("End Function");
            sb.AppendLine("End Class");
            parsedRule.ParsedRuleText = sb.ToString();

            return parsedRule;
        }

        public ParsedRule WrapInlineFunction(ParsedRule parsedRule, bool tryCatchWrap)
        {
            var sb = new StringBuilder();

            var countLine = 1;
            sb.AppendLine("Imports System.IO");

            countLine += 1;
            sb.AppendLine("Imports System.Xml");

            countLine += 1;
            sb.AppendLine("Imports log4net");

            countLine += 1;
            sb.AppendLine("Imports System.Net");

            countLine += 1;
            sb.AppendLine("Imports System.Collections.Generic");

            countLine += 1;
            sb.AppendLine("Imports Jube.Dictionary");

            countLine += 1;
            sb.AppendLine("Imports System");

            countLine += 1;
            sb.AppendLine("Public Class InlineFunction");

            countLine += 1;
            sb.AppendLine(
                "Public Shared Function Match(Data As DictionaryNoBoxing,TTLCounter as PooledDictionary(Of String,Integer),List as Dictionary(Of String,List(Of String)),KVP As PooledDictionary(Of String, Double),Log as ILog) As Boolean");

            countLine += 1;
            sb.AppendLine("Dim Matched as Double");

            if (tryCatchWrap)
            {
                sb.AppendLine("Try");
                countLine += 1;
            }

            parsedRule.CharOffset = sb.Length;
            parsedRule.LineOffset = countLine;
            sb.AppendLine(parsedRule.ParsedRuleText);

            if (tryCatchWrap)
            {
                sb.AppendLine("Catch ex As Exception");
                sb.AppendLine("Log.Info(ex.ToString)");
                sb.AppendLine("End Try");
            }

            sb.AppendLine("Return Matched");
            sb.AppendLine("End Function");
            sb.AppendLine("End Class");
            parsedRule.ParsedRuleText = sb.ToString();

            return parsedRule;
        }

        public ParsedRule TranslateFromDotNotation(ParsedRule parsedRule)
        {
            var sb = new StringBuilder();
            var lines = parsedRule.OriginalRuleText.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
            for (var i = 0; i < lines.Length; i++)
            {
                var originalLine = lines[i];
                var dictionaryFindReplace = new Dictionary<string, string>();
                var separators = new[]
                {
                    " ", ",", "=", ">", "(", ")", "<", ">=", "<=", "<>", "not"
                };
                var specialTokens = new[]
                {
                    "in", "like"
                };

                var tokens = lines[i].Split(separators, StringSplitOptions.RemoveEmptyEntries);
                foreach (var t in tokens)
                {
                    var replaceString = String.Empty;
                    var findString = String.Empty;

                    if (specialTokens.Contains(t, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var elements = t.Split(".", StringSplitOptions.RemoveEmptyEntries);

                    if (elements.Length <= 0)
                    {
                        continue;
                    }

                    var firstString = elements[0];

                    for (var k = 1; k < elements.Length; k++)
                    {
                        if (k == 1)
                        {
                            if (String.Equals("payload", firstString, StringComparison.OrdinalIgnoreCase))
                            {
                                findString = firstString + "." + elements[k];

                                var asFunction = ".AsString()";
                                var databaseCast = "";
                                var defaultValue = "";
                                if (EntityAnalysisModelRequestXPaths != null)
                                {
                                    if (EntityAnalysisModelRequestXPaths.ContainsKey(elements[k]))
                                    {
                                        asFunction = EntityAnalysisModelRequestXPaths[elements[k]].DataTypeId switch
                                        {
                                            1 => "AsString()",
                                            2 => "AsInt()",
                                            3 => "AsDouble()",
                                            4 => "AsDateTime()",
                                            5 => "AsBool",
                                            6 => "AsDouble()",
                                            7 => "AsDouble()",
                                            _ => "AsString()"
                                        };

                                        switch (EntityAnalysisModelRequestXPaths[elements[k]].DataTypeId)
                                        {
                                            case 2:
                                                databaseCast = "::int";
                                                defaultValue = EntityAnalysisModelRequestXPaths[elements[k]]
                                                    .DefaultValue;

                                                if (!Int32.TryParse(defaultValue, out _))
                                                {
                                                    defaultValue = "0";
                                                }

                                                break;
                                            case 3:
                                                databaseCast = "::float8";
                                                defaultValue = EntityAnalysisModelRequestXPaths[elements[k]]
                                                    .DefaultValue;

                                                if (!Double.TryParse(defaultValue, out _))
                                                {
                                                    defaultValue = "0";
                                                }

                                                break;
                                            case 4:
                                                databaseCast = "::timestamp";
                                                defaultValue = EntityAnalysisModelRequestXPaths[elements[k]]
                                                    .DefaultValue;

                                                if (!DateTime.TryParse(defaultValue, out _))
                                                {
                                                    defaultValue = "'" + DateTime.Now.ToString("O") + "'";
                                                }

                                                break;
                                            case 5:
                                                databaseCast = "::boolean";
                                                defaultValue = EntityAnalysisModelRequestXPaths[elements[k]]
                                                    .DefaultValue;

                                                if (!Boolean.TryParse(defaultValue, out _))
                                                {
                                                    defaultValue = "false";
                                                }

                                                break;
                                            case 6:
                                                databaseCast = "::float8";

                                                if (!Double.TryParse(defaultValue, out _))

                                                {
                                                    defaultValue = EntityAnalysisModelRequestXPaths[elements[k]]
                                                        .DefaultValue;
                                                }

                                                break;
                                            case 7:
                                                databaseCast = "::float8";
                                                defaultValue = EntityAnalysisModelRequestXPaths[elements[k]]
                                                    .DefaultValue;

                                                if (!Double.TryParse(defaultValue, out _))
                                                {
                                                    defaultValue = "0";
                                                }

                                                break;
                                            default:
                                                defaultValue = "'" + EntityAnalysisModelRequestXPaths[elements[k]]
                                                    .DefaultValue + "'";
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        var errorSpan = new ErrorSpan
                                        {
                                            Message =
                                                $"Line {i + 1}: Request XPath does not exist for {elements[k]}.",
                                            Line = i
                                        };
                                        parsedRule.ErrorSpans.Add(errorSpan);

                                        asFunction = "String";
                                    }
                                }

                                if (!parsedRule.SelectedPayloadData.ContainsKey(elements[k]))
                                {
                                    var selectedPayloadData = new SelectedPayloadData
                                    {
                                        Name = elements[k],
                                        DatabaseCast = databaseCast,
                                        DefaultValue = defaultValue
                                    };
                                    parsedRule.SelectedPayloadData.Add(elements[k], selectedPayloadData);
                                }

                                replaceString = replaceString + "Data(\"" + elements[k] + "\")." + asFunction;
                            }
                            else if (String.Equals("TTLCounter", firstString,
                                         StringComparison.OrdinalIgnoreCase))
                            {
                                findString = firstString + "." + elements[k];

                                if (EntityAnalysisModelsTtlCounters != null)
                                {
                                    if (EntityAnalysisModelsTtlCounters.All(w => w != elements[k]))
                                    {
                                        var errorSpan = new ErrorSpan
                                        {
                                            Message =
                                                $"Line {i + 1}: TTL Counter does not exist for {elements[k]}.",
                                            Line = i
                                        };
                                        parsedRule.ErrorSpans.Add(errorSpan);
                                    }
                                }

                                replaceString = replaceString + "TTLCounter(\"" + elements[k] + "\")";
                            }
                            else if (String.Equals("abstraction", firstString,
                                         StringComparison.OrdinalIgnoreCase))
                            {
                                findString = firstString + "." + elements[k];

                                if (EntityAnalysisModelsAbstractionRule != null)
                                {
                                    if (EntityAnalysisModelsAbstractionRule.All(w => w != elements[k]))
                                    {
                                        var errorSpan = new ErrorSpan
                                        {
                                            Message =
                                                $"Line {i + 1}: Abstraction does not exist for {elements[k]}.",
                                            Line = i
                                        };
                                        parsedRule.ErrorSpans.Add(errorSpan);
                                    }
                                }

                                replaceString = replaceString + "Abstraction(\"" + elements[k] +
                                                "\")";
                            }
                            else if (String.Equals("dictionary", firstString,
                                         StringComparison.OrdinalIgnoreCase))
                            {
                                findString = firstString + "." + elements[k];

                                if (EntityAnalysisModelsDictionaries != null)
                                {
                                    if (EntityAnalysisModelsDictionaries.All(w => w != elements[k]))
                                    {
                                        var errorSpan = new ErrorSpan
                                        {
                                            Message =
                                                $"Line {i + 1}: Dictionary does not exist for {elements[k]}.",
                                            Line = i
                                        };
                                        parsedRule.ErrorSpans.Add(errorSpan);
                                    }
                                }

                                replaceString = replaceString + "KVP(\"" + elements[k] + "\")";
                            }
                            else if (String.Equals("Sanction", firstString,
                                         StringComparison.OrdinalIgnoreCase))
                            {
                                findString = firstString + "." + elements[k];

                                if (EntityAnalysisModelsSanctions != null)
                                {
                                    if (EntityAnalysisModelsSanctions.All(w => w != elements[k]))
                                    {
                                        var errorSpan = new ErrorSpan
                                        {
                                            Message =
                                                $"Line {i + 1}: Dictionary does not exist for {elements[k]}.",
                                            Line = i
                                        };
                                        parsedRule.ErrorSpans.Add(errorSpan);
                                    }
                                }

                                replaceString = replaceString + "Sanctions(\"" + elements[k] + "\")";
                            }
                            else if (String.Equals("abstractionCalculation", firstString,
                                         StringComparison.OrdinalIgnoreCase))
                            {
                                findString = firstString + "." + elements[k];

                                if (EntityAnalysisModelAbstractionCalculations != null)
                                {
                                    if (EntityAnalysisModelAbstractionCalculations.All(w => w != elements[k]))
                                    {
                                        var errorSpan = new ErrorSpan
                                        {
                                            Message =
                                                $"Line {i + 1}: Dictionary does not exist for {elements[k]}.",
                                            Line = i
                                        };
                                        parsedRule.ErrorSpans.Add(errorSpan);
                                    }
                                }

                                replaceString = replaceString + "Calculation(\"" + elements[k] + "\")";
                            }
                            else if (String.Equals("exhaustiveAdaptation", firstString,
                                         StringComparison.OrdinalIgnoreCase))
                            {
                                findString = firstString + "." + elements[k];

                                if (EntityAnalysisModelsExhaustiveAdaptations != null)
                                {
                                    if (EntityAnalysisModelsExhaustiveAdaptations.All(w => w != elements[k]))
                                    {
                                        var errorSpan = new ErrorSpan
                                        {
                                            Message =
                                                $"Line {i + 1}: Dictionary does not exist for {elements[k]}.",
                                            Line = i
                                        };
                                        parsedRule.ErrorSpans.Add(errorSpan);
                                    }
                                }

                                replaceString = replaceString + "ExhaustiveAdaptation(\"" + elements[k] + "\")";
                            }
                            else if (String.Equals("HTTPAdaptation", firstString,
                                         StringComparison.OrdinalIgnoreCase))
                            {
                                findString = firstString + "." + elements[k];

                                if (EntityAnalysisModelsHttpAdaptations != null)
                                {
                                    if (EntityAnalysisModelsHttpAdaptations.All(w => w != elements[k]))
                                    {
                                        var errorSpan = new ErrorSpan
                                        {
                                            Message =
                                                $"Line {i + 1}: Dictionary does not exist for {elements[k]}.",
                                            Line = i
                                        };
                                        parsedRule.ErrorSpans.Add(errorSpan);
                                    }
                                }

                                replaceString = replaceString + "HTTPAdaptation(\"" + elements[k] + "\")";
                            }

                            else if (String.Equals("list", firstString,
                                         StringComparison.OrdinalIgnoreCase))
                            {
                                findString = firstString + "." + elements[k];

                                if (EntityAnalysisModelsLists != null)
                                {
                                    if (EntityAnalysisModelsLists.All(w => w != elements[k]))
                                    {
                                        var errorSpan = new ErrorSpan
                                        {
                                            Message =
                                                $"Line {i + 1}: Dictionary does not exist for {elements[k]}.",
                                            Line = i
                                        };
                                        parsedRule.ErrorSpans.Add(errorSpan);
                                    }
                                }

                                replaceString = replaceString + "List(\"" + elements[k] + "\")";
                            }
                            else
                            {
                                findString = firstString + "." + elements[k];
                                replaceString = replaceString + firstString + "." + elements[k];
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (String.IsNullOrEmpty(findString))
                    {
                        continue;
                    }

                    if (!dictionaryFindReplace.ContainsKey(findString))
                    {
                        dictionaryFindReplace.Add(elements[0] + "." + elements[1], replaceString);
                    }
                }

                var newLine = dictionaryFindReplace.Aggregate(originalLine,
                    (current, kvp) => current.Replace(kvp.Key, kvp.Value));
                sb.AppendLine(newLine);
            }

            parsedRule.ParsedRuleText = sb.ToString();
            return parsedRule;
        }
    }
}
