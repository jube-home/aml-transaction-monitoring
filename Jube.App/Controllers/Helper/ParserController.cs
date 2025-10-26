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
    using Code;
    using Data.Context;
    using Data.Repository;
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
                DataConnectionDbContext.GetDbContextDataConnection(dynamicEnvironment.AppSettings("ConnectionString"));
            permissionValidation = new PermissionValidation(dbContext, userName);
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
        public ActionResult<ParseRuleResultDto> Post([FromBody] ParseRuleRequestDto parseRuleRequestDto)
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
                    _ => EntityAnalysisModelRequestXPaths(parseRuleRequestDto.EntityAnalysisModelId)
                };

                var entityAnalysisModelsLists
                    = EntityAnalysisModelsLists(parseRuleRequestDto.EntityAnalysisModelId);

                var entityAnalysisModelsDictionaries
                    = EntityAnalysisModelsDictionaries(parseRuleRequestDto.EntityAnalysisModelId);

                List<string> entityAnalysisModelsTtlCounters = null;
                List<string> entityAnalysisModelsAbstractionRule = null;
                List<string> entityAnalysisModelsSanctions = null;
                if (parseRuleRequestDto.RuleParseType > 3)
                {
                    entityAnalysisModelsTtlCounters
                        = EntityAnalysisModelsTtlCounters(parseRuleRequestDto.EntityAnalysisModelId);

                    entityAnalysisModelsAbstractionRule
                        = EntityAnalysisModelsAbstractionRules(parseRuleRequestDto.EntityAnalysisModelId);

                    entityAnalysisModelsSanctions
                        = EntityAnalysisModelsSanctions(parseRuleRequestDto.EntityAnalysisModelId);
                }

                List<string> entityAnalysisModelAbstractionCalculations = null;
                List<string> entityAnalysisModelsHttpAdaptations = null;
                List<string> entityAnalysisModelsExhaustiveAdaptations = null;
                if (parseRuleRequestDto.RuleParseType > 4)
                {
                    entityAnalysisModelAbstractionCalculations
                        = EntityAnalysisModelAbstractionCalculations(parseRuleRequestDto.EntityAnalysisModelId);

                    entityAnalysisModelsHttpAdaptations
                        = EntityAnalysisModelsHttpAdaptations(parseRuleRequestDto.EntityAnalysisModelId);

                    entityAnalysisModelsExhaustiveAdaptations
                        = EntityAnalysisModelsExhaustiveAdaptations(parseRuleRequestDto.EntityAnalysisModelId);
                }

                var parser = new Parser(log,
                    tokens
                )
                {
                    EntityAnalysisModelRequestXPaths = entityAnalysisModelRequestXPaths,
                    EntityAnalysisModelAbstractionCalculations = entityAnalysisModelAbstractionCalculations,
                    EntityAnalysisModelsAbstractionRule = entityAnalysisModelsAbstractionRule,
                    EntityAnalysisModelsTtlCounters = entityAnalysisModelsTtlCounters,
                    EntityAnalysisModelsSanctions = entityAnalysisModelsSanctions,
                    EntityAnalysisModelsLists = entityAnalysisModelsLists,
                    EntityAnalysisModelsDictionaries = entityAnalysisModelsDictionaries,
                    EntityAnalysisModelsHttpAdaptations = entityAnalysisModelsHttpAdaptations,
                    EntityAnalysisModelsExhaustiveAdaptations = entityAnalysisModelsExhaustiveAdaptations
                };

                var errorSpans = new List<ErrorSpan>();
                var parsedRule = new ParsedRule
                {
                    ErrorSpans = errorSpans,
                    OriginalRuleText = parseRuleRequestDto.RuleText
                };
                parsedRule = parser.TranslateFromDotNotation(parsedRule);
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
                        compile.CompileCode(parsedRule.ParsedRuleText, log, refs);

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

        private List<string> EntityAnalysisModelsHttpAdaptations(int entityAnalysisModelId)
        {
            var entityAnalysisModelHttpAdaptationRepository =
                new EntityAnalysisModelHttpAdaptationRepository(dbContext, userName);

            return entityAnalysisModelHttpAdaptationRepository
                .GetByEntityAnalysisModelIdOrderById(entityAnalysisModelId)
                .Select(s => s.Name).ToList();
        }

        private List<string> EntityAnalysisModelsExhaustiveAdaptations(int entityAnalysisModelId)
        {
            var entityAnalysisModelExhaustiveRepository =
                new ExhaustiveSearchInstanceRepository(dbContext, userName);

            return entityAnalysisModelExhaustiveRepository
                .GetByEntityAnalysisModelIdOrderById(entityAnalysisModelId)
                .Select(s => s.Name).ToList();
        }

        private List<string> EntityAnalysisModelsDictionaries(int entityAnalysisModelId)
        {
            var entityAnalysisModelDictionaryRepository =
                new EntityAnalysisModelDictionaryRepository(dbContext, userName);

            return entityAnalysisModelDictionaryRepository
                .GetByEntityAnalysisModelIdOrderById(entityAnalysisModelId)
                .Select(s => s.Name).ToList();
        }

        private List<string> EntityAnalysisModelsLists(int entityAnalysisModelId)
        {
            var entityAnalysisModelListRepository =
                new EntityAnalysisModelListRepository(dbContext, userName);

            return entityAnalysisModelListRepository
                .GetByEntityAnalysisModelIdOrderById(entityAnalysisModelId)
                .Select(s => s.Name).ToList();
        }

        private List<string> EntityAnalysisModelsSanctions(int entityAnalysisModelId)
        {
            var entityAnalysisModelSanctionRepository =
                new EntityAnalysisModelSanctionRepository(dbContext, userName);

            return entityAnalysisModelSanctionRepository
                .GetByEntityAnalysisModelIdOrderById(entityAnalysisModelId)
                .Select(s => s.Name).ToList();
        }

        private List<string> EntityAnalysisModelsTtlCounters(int entityAnalysisModelId)
        {
            var entityAnalysisModelTtlCounterRepository =
                new EntityAnalysisModelTtlCounterRepository(dbContext, userName);

            return entityAnalysisModelTtlCounterRepository
                .GetByEntityAnalysisModelIdOrderById(entityAnalysisModelId)
                .Select(s => s.Name).ToList();
        }

        private List<string> EntityAnalysisModelAbstractionCalculations(int entityAnalysisModelId)
        {
            var entityAnalysisModelAbstractionCalculationRepository =
                new EntityAnalysisModelAbstractionCalculationRepository(dbContext, userName);

            return entityAnalysisModelAbstractionCalculationRepository
                .GetByEntityAnalysisModelIdOrderByIdDesc(entityAnalysisModelId)
                .Select(s => s.Name).ToList();
        }

        private List<string> EntityAnalysisModelsAbstractionRules(int entityAnalysisModelId)
        {
            var entityAnalysisModelAbstractionRuleRepository =
                new EntityAnalysisModelAbstractionRuleRepository(dbContext, userName);

            return entityAnalysisModelAbstractionRuleRepository
                .GetByEntityAnalysisModelIdOrderByIdDesc(entityAnalysisModelId)
                .Select(s => s.Name).ToList();
        }

        private Dictionary<string, EntityAnalysisModelRequestXPath> EntityAnalysisModelRequestXPaths(
            int entityAnalysisModelId)
        {
            var entityAnalysisModelRequestXPathRepository =
                new EntityAnalysisModelRequestXPathRepository(dbContext, userName);

            var values = new Dictionary<string, EntityAnalysisModelRequestXPath>();
            foreach (var entityAnalysisModelRequestXPath in entityAnalysisModelRequestXPathRepository
                         .GetByEntityAnalysisModelIdOrderById(entityAnalysisModelId))
            {
                if (!values.ContainsKey(entityAnalysisModelRequestXPath.Name))
                {
                    values.Add(entityAnalysisModelRequestXPath.Name,
                        new EntityAnalysisModelRequestXPath
                        {
                            DataTypeId = entityAnalysisModelRequestXPath.DataTypeId ?? 1,
                            DefaultValue = entityAnalysisModelRequestXPath.DefaultValue
                        }
                    );
                }
            }

            return values;
        }
    }
}
