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

namespace Jube.Migrations.Branches.GitHubIssueBranch139
{
    using FluentMigrator;

    [Migration(20263107240000)]
    public class UpdateExampleFraudScoreLocalEndpointToMockHttpAdaptation : Migration
    {
        private const string OldHttpEndpoint = "/api/invoke/ExampleFraudScoreLocalEndpoint";
        private const string NewHttpEndpoint = "/api/MockHttpAdaptation/BayesianNetworkBootstrapStrength";

        public override void Up()
        {
            Update.Table("EntityAnalysisModelHttpAdaptation")
                .Set(new
                {
                    HttpEndpoint = NewHttpEndpoint
                })
                .Where(new
                {
                    HttpEndpoint = OldHttpEndpoint
                });
        }

        public override void Down()
        {
            Update.Table("EntityAnalysisModelHttpAdaptation")
                .Set(new
                {
                    HttpEndpoint = OldHttpEndpoint
                })
                .Where(new
                {
                    HttpEndpoint = NewHttpEndpoint
                });
        }
    }
}
