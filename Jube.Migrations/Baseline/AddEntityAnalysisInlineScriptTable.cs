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

using System;
using FluentMigrator;

namespace Jube.Migrations.Baseline
{
    [Migration(20220429124922)]
    public class AddEntityAnalysisInlineScriptTable : Migration
    {
        public override void Up()
        {
            Create.Table("EntityAnalysisInlineScript")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("Code").AsString().Nullable()
                .WithColumn("Dependency").AsString().Nullable()
                .WithColumn("ClassName").AsString().Nullable()
                .WithColumn("Name").AsString().Nullable()
                .WithColumn("MethodName").AsString().Nullable()
                .WithColumn("CreatedDate").AsDateTime2().Nullable();

            Insert.IntoTable("EntityAnalysisInlineScript").Row(new
            {
                Code = "Imports log4net" + Environment.NewLine +
                       "Imports System" + Environment.NewLine +
                       "Imports System.Collections.Generic" + Environment.NewLine +
                       "Imports Microsoft.VisualBasic" + Environment.NewLine +
                       "Public Class IssueOTP" + Environment.NewLine +
                       "   Inherits System.Attribute" + Environment.NewLine + Environment.NewLine +
                       "   <ReportTable>" + Environment.NewLine +
                       "   Public Property OTP As String" + Environment.NewLine +
                       "   Private _log as ILog" + Environment.NewLine +
                       "   Public Sub New(Log As ILog)" + Environment.NewLine +
                       "       _log = Log" + Environment.NewLine +
                       "   End Sub" + Environment.NewLine + Environment.NewLine +
                       "   Public Sub Execute(Data As Dictionary(Of String,Object), Log As ILog)" + Environment.NewLine +
                       "       Data.Add(\"OTP\", RandomDigits(6))" + Environment.NewLine +
                       "   End Sub" + Environment.NewLine + Environment.NewLine +
                       "   Private Function RandomDigits(ByVal length As Integer) As String" + Environment.NewLine +
                       "       Dim random = New Random()" + Environment.NewLine +
                       "       Dim s As String = String.Empty" + Environment.NewLine +
                       "       For i As Integer = 0 To length - 1" + Environment.NewLine +
                       "           s = String.Concat(s, random.[Next](10).ToString())" + Environment.NewLine +
                       "       Next" + Environment.NewLine +
                       "       Return s" + Environment.NewLine +
                       "   End Function" + Environment.NewLine +
                       "End Class",
                Dependency = "",
                ClassName = "IssueOTP",
                Name = "Issue OTP",
                MethodName = "Execute",
                CreatedDate = DateTime.Now
            });
        }

        public override void Down()
        {
            Delete.Table("EntityAnalysisInlineScript");
        }
    }
}