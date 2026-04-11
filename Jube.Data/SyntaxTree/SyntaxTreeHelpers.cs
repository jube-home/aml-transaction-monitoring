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
    using LanguageVersion=Microsoft.CodeAnalysis.CSharp.LanguageVersion;

    public static class SyntaxTreeHelpers
    {
        public static List<string> GetPublicPropertiesForSearchKey(string code, bool cSharp = false)
        {
            var value = new List<string>();

            if (cSharp)
            {
                var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
                var tree = CSharpSyntaxTree.ParseText(code, parseOptions);

                foreach (var @class in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    value.AddRange(@class.Members
                        .OfType<PropertyDeclarationSyntax>()
                        .Where(prop =>
                            prop.Modifiers.Any(m => m.Text == "public") &&
                            prop.AttributeLists
                                .SelectMany(a => a.Attributes)
                                .Any(attr => attr?.Name.ToString() == "SearchKey")
                        )
                        .Select(prop => prop.Identifier.Text));
                }
            }
            else
            {
                var parseOptions = new VisualBasicParseOptions(Microsoft.CodeAnalysis.VisualBasic.LanguageVersion.VisualBasic16);
                var tree = VisualBasicSyntaxTree.ParseText(code, parseOptions);

                foreach (var @class in tree.GetRoot().DescendantNodes().OfType<ClassBlockSyntax>())
                {
                    value.AddRange(@class.Members.OfType<PropertyStatementSyntax>()
                        .Where(prop =>
                            prop.Modifiers.Any(m => m.Text == "public") &&
                            prop.AttributeLists
                                .SelectMany(a => a.Attributes)
                                .Any(attr => attr?.Name.ToString() == "SearchKey")
                        )
                        .Select(prop => prop.Identifier.Text));
                }
            }

            return value;
        }

        public static Dictionary<string, int> GetPublicProperties(string code, bool cSharp = false)
        {
            var value = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            if (cSharp)
            {
                var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
                var tree = CSharpSyntaxTree.ParseText(code, parseOptions);

                foreach (var @class in tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>())
                {
                    foreach (var prop in @class.Members.OfType<PropertyDeclarationSyntax>())
                    {
                        if (prop.Modifiers.All(m => m.Text != "public"))
                        {
                            continue;
                        }

                        var typeSyntax = prop.Type is Microsoft.CodeAnalysis.CSharp.Syntax.NullableTypeSyntax nullable
                            ? nullable.ElementType
                            : prop.Type;

                        var typeId = typeSyntax.ToString().ToLower() switch
                        {
                            "string" => 1,
                            "int" or "int32" => 2,
                            "double" => 3,
                            "datetime" => 4,
                            "bool" or "boolean" => 5,
                            _ => 1
                        };

                        value[prop.Identifier.Text] = typeId;
                    }
                }
            }
            else
            {
                var parseOptions = new VisualBasicParseOptions(Microsoft.CodeAnalysis.VisualBasic.LanguageVersion.VisualBasic16);
                var tree = VisualBasicSyntaxTree.ParseText(code, parseOptions);

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
                            Microsoft.CodeAnalysis.VisualBasic.Syntax.GenericNameSyntax { Identifier.Text: "Nullable" } generic
                                => generic.TypeArgumentList.Arguments.FirstOrDefault() ?? rawType,
                            _ => rawType
                        };

                        var typeId = typeSyntax?.ToString().ToLower() switch
                        {
                            "string" => 1,
                            "integer" => 2,
                            "double" => 3,
                            "datetime" => 4,
                            "boolean" => 5,
                            _ => 1
                        };

                        value[prop.Identifier.Text] = typeId;
                    }
                }
            }

            return value;
        }
    }
}
