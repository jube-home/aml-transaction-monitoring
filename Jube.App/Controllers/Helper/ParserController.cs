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

namespace Jube.App.Controllers.Helper
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Code;
    using Data.Context;
    using Data.Repository;
    using Data.SyntaxTree;
    using Dto;
    using Dto.Requests;
    using DynamicEnvironment;
    using log4net;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Parser;
    using Parser.Compiler;

    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class ParserController : Controller
    {
        private readonly DbContext dbContext;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly string userName;

        public ParserController(ILog log, IHttpContextAccessor httpContextAccessor,
            DynamicEnvironment dynamicEnvironment)
        {
            if (httpContextAccessor.HttpContext?.User.Identity != null)
            {
                userName = httpContextAccessor.HttpContext.User.Identity.Name;
            }

            this.log = log;

            dbContext =
                DataConnectionDbContext.GetResilientDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"), log);
            permissionValidation = new PermissionValidation(dbContext, userName, log);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                dbContext.Close();
                dbContext.Dispose();
            }

            base.Dispose(disposing);
        }

        [HttpPost]
        public async Task<ActionResult<ParseRuleResultDto>> PostAsync([FromBody] ParseRuleRequestDto parseRuleRequestDto, CancellationToken token = default)
        {
            try
            {
                if (!permissionValidation.Validate(new[]
                    {
                        8, 10, 13, 14, 16, 17, 25, 26
                    }))
                {
                    return Forbid();
                }

                var tokens = dbContext.RuleScriptToken.Select(s => s.Token).ToList();

                var entityAnalysisModelRequestXPaths = parseRuleRequestDto.RuleParseType switch
                {
                    _ => await EntityAnalysisModelRequestXPathsAsync(parseRuleRequestDto.EntityAnalysisModelId, token).ConfigureAwait(false)
                };

                var entityAnalysisModelInlineScriptProperties = await EntityAnalysisModelInlineScriptPropertiesAsync(parseRuleRequestDto.EntityAnalysisModelId, token).ConfigureAwait(false);

                var entityAnalysisModelsLists
                    = await EntityAnalysisModelListsAsync(parseRuleRequestDto.EntityAnalysisModelId, token).ConfigureAwait(false);

                var entityAnalysisModelsDictionaries
                    = await EntityAnalysisModelDictionariesAsync(parseRuleRequestDto.EntityAnalysisModelId, token).ConfigureAwait(false);

                List<string> entityAnalysisModelsTtlCounters = null;
                List<string> entityAnalysisModelsAbstractionRule = null;
                List<string> entityAnalysisModelsSanctions = null;

                if (parseRuleRequestDto.RuleParseType > 3)
                {
                    entityAnalysisModelsTtlCounters
                        = await EntityAnalysisModelTtlCountersAsync(parseRuleRequestDto.EntityAnalysisModelId, token).ConfigureAwait(false);

                    entityAnalysisModelsAbstractionRule
                        = await EntityAnalysisModelAbstractionRulesAsync(parseRuleRequestDto.EntityAnalysisModelId, token).ConfigureAwait(false);

                    entityAnalysisModelsSanctions
                        = await EntityAnalysisModelSanctionsAsync(parseRuleRequestDto.EntityAnalysisModelId, token).ConfigureAwait(false);
                }

                List<string> entityAnalysisModelAbstractionCalculations = null;
                List<string> entityAnalysisModelsHttpAdaptations = null;
                List<string> entityAnalysisModelsExhaustiveAdaptations = null;
                List<string> entityAnalysisModelsActivationRules = null;

                if (parseRuleRequestDto.RuleParseType > 4)
                {
                    entityAnalysisModelAbstractionCalculations
                        = await EntityAnalysisModelAbstractionCalculationsAsync(parseRuleRequestDto.EntityAnalysisModelId, token).ConfigureAwait(false);

                    entityAnalysisModelsHttpAdaptations
                        = await EntityAnalysisModelsHttpAdaptationsAsync(parseRuleRequestDto.EntityAnalysisModelId, token).ConfigureAwait(false);

                    entityAnalysisModelsExhaustiveAdaptations
                        = await EntityAnalysisModelExhaustiveAdaptationsAsync(parseRuleRequestDto.EntityAnalysisModelId, token).ConfigureAwait(false);
                }

                if (parseRuleRequestDto.RuleParseType > 5)
                {
                    entityAnalysisModelsActivationRules = await EntityAnalysisModelActivationRulesAsync(parseRuleRequestDto.EntityAnalysisModelId, token).ConfigureAwait(false);
                }

                var parser = new Parser(log,
                    tokens
                )
                {
                    EntityAnalysisModelRequestXPaths = entityAnalysisModelRequestXPaths,
                    EntityAnalysisModelInlineScriptProperties = entityAnalysisModelInlineScriptProperties,
                    EntityAnalysisModelAbstractionCalculations = entityAnalysisModelAbstractionCalculations,
                    EntityAnalysisModelsAbstractionRule = entityAnalysisModelsAbstractionRule,
                    EntityAnalysisModelsTtlCounters = entityAnalysisModelsTtlCounters,
                    EntityAnalysisModelsSanctions = entityAnalysisModelsSanctions,
                    EntityAnalysisModelsLists = entityAnalysisModelsLists,
                    EntityAnalysisModelsDictionaries = entityAnalysisModelsDictionaries,
                    EntityAnalysisModelsHttpAdaptations = entityAnalysisModelsHttpAdaptations,
                    EntityAnalysisModelsExhaustiveAdaptations = entityAnalysisModelsExhaustiveAdaptations,
                    EntityAnalysisModelsActivationRules = entityAnalysisModelsActivationRules
                };

                var errorSpans = new List<ErrorSpan>();
                var parsedRule = new ParsedRule
                {
                    ErrorSpans = errorSpans,
                    OriginalRuleText = parseRuleRequestDto.RuleText
                };
                parsedRule = parser.TranslateFromDotNotation(parsedRule, parseRuleRequestDto.RuleParseType == 3);
                parsedRule = parser.Parse(parsedRule);

                var sb = new StringBuilder();
                foreach (var softParseErrorSpan in parsedRule.ErrorSpans)
                {
                    sb.AppendLine(softParseErrorSpan.Message);
                }

                var response = new ParseRuleResultDto
                {
                    ErrorSpans = errorSpans
                };

                parsedRule = parseRuleRequestDto.RuleParseType switch
                {
                    1 => parser.WrapInlineFunction(parsedRule, false),
                    2 => parser.WrapGatewayRule(parsedRule, false),
                    3 => parser.WrapAbstractionRule(parsedRule, false),
                    4 => parser.WrapAbstractionCalculation(parsedRule, false),
                    5 => parser.WrapActivationRule(parsedRule, false),
                    _ => parsedRule
                };

                var codeBase = Assembly.GetExecutingAssembly().Location;
                var strPathBinary = Path.GetDirectoryName(codeBase);
                var strPathFramework = Path.GetDirectoryName(typeof(object).Assembly.Location);

                if (strPathFramework != null)
                {
                    if (strPathBinary != null)
                    {
                        var refs = new[]
                        {
                            Path.Combine(strPathFramework, "mscorlib.dll"), Path.Combine(strPathFramework, "System.dll"), Path.Combine(strPathFramework, "Microsoft.VisualBasic.dll"), Path.Combine(strPathFramework, "System.Xml.dll"), Path.Combine(strPathBinary, "log4net.dll"), Path.Combine(strPathBinary, "Jube.Dictionary.dll"), Path.Combine(strPathFramework, "System.Collections.dll")
                        };

                        var compile = new Compile();
                        compile.CompileCode(parsedRule.ParsedRuleText, log, refs, Compile.Language.Vb);

                        if (!compile.Success)
                        {
                            foreach (var err in compile.Errors)
                            {
                                var line = err.Location.GetLineSpan().StartLinePosition.Line - parsedRule.LineOffset;
                                var message = $"Line {line + 1}: {err.GetMessage()}";
                                sb.AppendLine(message);

                                err.Location.GetLineSpan();

                                var errorSpan = new ErrorSpan
                                {
                                    Message = message,
                                    Start = err.Location.SourceSpan.Start - parsedRule.CharOffset,
                                    Length = err.Location.SourceSpan.Length,
                                    Line = line
                                };
                                errorSpans.Add(errorSpan);
                            }

                            response.Message = sb.ToString();
                            response.ErrorSpans = errorSpans;

                            return response;
                        }
                    }
                }

                if (errorSpans.Count > 0)
                {
                    return new ParseRuleResultDto
                    {
                        Message = "Error",
                        ErrorSpans = errorSpans
                    };
                }

                return new ParseRuleResultDto
                {
                    Message = "Compiled"
                };
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());

                return new ParseRuleResultDto
                {
                    Message = "Error"
                };
            }
        }

        private async Task<Dictionary<string, int>> EntityAnalysisModelInlineScriptPropertiesAsync(int entityAnalysisModelId, CancellationToken token = default)
        {
            var value = new Dictionary<string, int>();
            var entityAnalysisModelInlineScriptRepository = new EntityAnalysisModelInlineScriptRepository(dbContext, userName);
            var entityAnalysisModelInlineScripts = await entityAnalysisModelInlineScriptRepository.GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false);
            var entityAnalysisInlineScriptRepository = new EntityAnalysisInlineScriptRepository(dbContext);

            foreach (var entityAnalysisModelInlineScript in entityAnalysisModelInlineScripts)
            {
                if (!entityAnalysisModelInlineScript.EntityAnalysisInlineScriptId.HasValue)
                {
                    continue;
                }

                var entityAnalysisInlineScript = await entityAnalysisInlineScriptRepository.GetByIdAsync(entityAnalysisModelInlineScript.EntityAnalysisInlineScriptId.Value, token);
                foreach (var publicProperty in SyntaxTreeHelpers.GetPublicProperties(entityAnalysisInlineScript.Code, entityAnalysisInlineScript.LanguageId == 2))
                {
                    value.Add(publicProperty.Key, publicProperty.Value);
                }
            }
            return value;
        }

        private async Task<List<string>> EntityAnalysisModelsHttpAdaptationsAsync(int entityAnalysisModelId, CancellationToken token = default)
        {
            var entityAnalysisModelHttpAdaptationRepository =
                new EntityAnalysisModelHttpAdaptationRepository(dbContext, userName);

            var entityAnalysisModelsHttpAdaptations = await entityAnalysisModelHttpAdaptationRepository
                .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false);

            return entityAnalysisModelsHttpAdaptations.Select(s => s.Name).ToList();
        }

        private async Task<List<string>> EntityAnalysisModelExhaustiveAdaptationsAsync(int entityAnalysisModelId, CancellationToken token = default)
        {
            var entityAnalysisModelExhaustiveRepository =
                new ExhaustiveSearchInstanceRepository(dbContext, userName);

            var entityAnalysisModelsExhaustiveAdaptations = await entityAnalysisModelExhaustiveRepository
                .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false);

            return entityAnalysisModelsExhaustiveAdaptations.Select(s => s.Name).ToList();
        }

        private async Task<List<string>> EntityAnalysisModelDictionariesAsync(int entityAnalysisModelId, CancellationToken token = default)
        {
            var entityAnalysisModelDictionaryRepository =
                new EntityAnalysisModelDictionaryRepository(dbContext, userName);

            var entityAnalysisModelDictionaries = await entityAnalysisModelDictionaryRepository
                .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false);

            return entityAnalysisModelDictionaries.Select(s => s.Name).ToList();
        }

        private async Task<List<string>> EntityAnalysisModelListsAsync(int entityAnalysisModelId, CancellationToken token = default)
        {
            var entityAnalysisModelListRepository =
                new EntityAnalysisModelListRepository(dbContext, userName);

            var entityAnalysisModelLists = await entityAnalysisModelListRepository
                .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false);

            return entityAnalysisModelLists.Select(s => s.Name).ToList();
        }

        private async Task<List<string>> EntityAnalysisModelActivationRulesAsync(int entityAnalysisModelId, CancellationToken token = default)
        {
            var entityAnalysisModelActivationRuleRepository =
                new EntityAnalysisModelActivationRuleRepository(dbContext, userName);

            var entityAnalysisModelLists = await entityAnalysisModelActivationRuleRepository.GetByEntityAnalysisModelIdOrderByIdDescAsync(entityAnalysisModelId, token).ConfigureAwait(false);

            return entityAnalysisModelLists.Select(s => s.Name).ToList();
        }

        private async Task<List<string>> EntityAnalysisModelSanctionsAsync(int entityAnalysisModelId, CancellationToken token = default)
        {
            var entityAnalysisModelSanctionRepository =
                new EntityAnalysisModelSanctionRepository(dbContext, userName);

            var entityAnalysisModelSanctions = await entityAnalysisModelSanctionRepository
                .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false);

            return entityAnalysisModelSanctions.Select(s => s.Name).ToList();
        }

        private async Task<List<string>> EntityAnalysisModelTtlCountersAsync(int entityAnalysisModelId, CancellationToken token = default)
        {
            var entityAnalysisModelTtlCounterRepository =
                new EntityAnalysisModelTtlCounterRepository(dbContext, userName);

            var entityAnalysisModelTtlCounters = await entityAnalysisModelTtlCounterRepository
                .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false);

            return entityAnalysisModelTtlCounters.Select(s => s.Name).ToList();
        }

        private async Task<List<string>> EntityAnalysisModelAbstractionCalculationsAsync(int entityAnalysisModelId, CancellationToken token = default)
        {
            var entityAnalysisModelAbstractionCalculationRepository =
                new EntityAnalysisModelAbstractionCalculationRepository(dbContext, userName);

            var entityAnalysisModelAbstractionCalculations = await entityAnalysisModelAbstractionCalculationRepository
                .GetByEntityAnalysisModelIdOrderByIdDescAsync(entityAnalysisModelId, token).ConfigureAwait(false);

            return entityAnalysisModelAbstractionCalculations.Select(s => s.Name).ToList();
        }

        private async Task<List<string>> EntityAnalysisModelAbstractionRulesAsync(int entityAnalysisModelId, CancellationToken token = default)
        {
            var entityAnalysisModelAbstractionRuleRepository =
                new EntityAnalysisModelAbstractionRuleRepository(dbContext, userName);

            var entityAnalysisModelAbstractionRules = await entityAnalysisModelAbstractionRuleRepository
                .GetByEntityAnalysisModelIdOrderByIdDescAsync(entityAnalysisModelId, token).ConfigureAwait(false);

            return entityAnalysisModelAbstractionRules.Select(s => s.Name).ToList();
        }

        private async Task<Dictionary<string, EntityAnalysisModelRequestXPath>> EntityAnalysisModelRequestXPathsAsync(
            int entityAnalysisModelId, CancellationToken token = default)
        {
            var entityAnalysisModelRequestXPathRepository =
                new EntityAnalysisModelRequestXPathRepository(dbContext, userName);

            var values = new Dictionary<string, EntityAnalysisModelRequestXPath>();
            foreach (var entityAnalysisModelRequestXPaths in await entityAnalysisModelRequestXPathRepository
                         .GetByEntityAnalysisModelIdOrderByIdAsync(entityAnalysisModelId, token).ConfigureAwait(false))
            {
                if (!values.ContainsKey(entityAnalysisModelRequestXPaths.Name))
                {
                    values.Add(entityAnalysisModelRequestXPaths.Name,
                        new EntityAnalysisModelRequestXPath
                        {
                            DataTypeId = entityAnalysisModelRequestXPaths.DataTypeId ?? 1,
                            DefaultValue = entityAnalysisModelRequestXPaths.DefaultValue,
                            Cache = entityAnalysisModelRequestXPaths.Cache == 1
                        }
                    );
                }
            }

            return values;
        }
    }
}
