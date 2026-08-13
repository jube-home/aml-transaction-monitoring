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

namespace Jube.Data.SyntaxTree
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.VisualBasic;
    using Microsoft.CodeAnalysis.VisualBasic.Syntax;
    using GenericNameSyntax=Microsoft.CodeAnalysis.VisualBasic.Syntax.GenericNameSyntax;
    using LanguageVersion=Microsoft.CodeAnalysis.CSharp.LanguageVersion;
    using NullableTypeSyntax=Microsoft.CodeAnalysis.CSharp.Syntax.NullableTypeSyntax;

    public static class SyntaxTreeHelpers
    {
        public static Dictionary<string, SyntaxTreeProperty> GetPublicProperties(string code, bool cSharp = false)
        {
            var value = new Dictionary<string, SyntaxTreeProperty>(StringComparer.OrdinalIgnoreCase);

            if (cSharp)
            {
                var tree = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(LanguageVersion.Latest));

                foreach (var @class in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    foreach (var prop in @class.Members.OfType<PropertyDeclarationSyntax>())
                    {
                        if (prop.Modifiers.All(m => m.Text != "public"))
                        {
                            continue;
                        }

                        var typeSyntax = prop.Type is NullableTypeSyntax nullable
                            ? nullable.ElementType
                            : prop.Type;

                        value[prop.Identifier.Text] = new SyntaxTreeProperty
                        {
                            DataTypeId = typeSyntax.ToString().ToLower() switch
                            {
                                "string" => 1,
                                "int" or "int32" => 2,
                                "double" => 3,
                                "datetime" => 4,
                                "bool" or "boolean" => 5,
                                _ => 1
                            },
                            SearchKey = prop.AttributeLists
                                .SelectMany(a => a.Attributes)
                                .Any(attr => attr?.Name.ToString() == "SearchKey")
                        };
                    }
                }
            }
            else
            {
                var tree = VisualBasicSyntaxTree.ParseText(code,
                    new VisualBasicParseOptions(Microsoft.CodeAnalysis.VisualBasic.LanguageVersion.VisualBasic16));

                foreach (var @class in tree.GetRoot().DescendantNodes().OfType<ClassBlockSyntax>())
                {
                    foreach (var prop in @class.Members.OfType<PropertyStatementSyntax>())
                    {
                        if (prop.Modifiers.All(m => m.Text != "Public"))
                        {
                            continue;
                        }

                        var rawType = prop.AsClause?.Type();
                        var typeSyntax = rawType switch
                        {
                            Microsoft.CodeAnalysis.VisualBasic.Syntax.NullableTypeSyntax nullable => nullable.ElementType,
                            GenericNameSyntax { Identifier.Text: "Nullable" } generic
                                => generic.TypeArgumentList.Arguments.FirstOrDefault() ?? rawType,
                            _ => rawType
                        };

                        value[prop.Identifier.Text] = new SyntaxTreeProperty
                        {
                            DataTypeId = typeSyntax?.ToString().ToLower() switch
                            {
                                "string" => 1,
                                "integer" => 2,
                                "double" => 3,
                                "datetime" => 4,
                                "boolean" => 5,
                                _ => 1
                            },
                            SearchKey = prop.AttributeLists
                                .SelectMany(a => a.Attributes)
                                .Any(attr => attr?.Name.ToString() == "SearchKey")
                        };
                    }
                }
            }

            return value;
        }
    }
}
