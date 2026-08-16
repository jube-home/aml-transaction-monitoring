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

    [Migration(20260725122500)]
    public class SanctionsStopTokens : Migration
    {

        private static readonly string[] Honorifics =
        {
            "ustadh", "ustadha", "ustadhah", "ustaz", "ustaza", "ustad", "ustada", "ustaadh", "ostad", "ostada", "ostaad", "muhtaram", "muhtarama", "mohtaram", "mohtarama", "mohtarameh", "janab", "janabe", "jenab", "jenabe", "jinab", "hadrat", "hadhrat", "hadrah", "hazrat", "hazret", "hanim", "hanum", "hanoum", "hanimefendi", "khanum", "khanom", "khanoum", "khanem", "beyefendi", "efendi", "effendi", "effendy", "effendim", "afandi", "afendi", "mohandes", "mohandis", "muhandis", "oliajenab"
        };

        private static readonly string[] ReligiousCulturalTitles =
        {
            "hajj", "hadj", "haj", "alhajj", "alhaj", "elhajj", "elhaj", "hajji", "haji", "hadji", "hajjy", "haci", "hacci", "hajjah", "hajja", "hajjeh", "hajjia", "hajiya", "haciye", "karbalai", "karbalaei", "karbalayi", "sheikh", "shaikh", "shaykh", "sheik", "shekh", "cheikh", "sheikha", "sheikhah", "shaykhah", "imam", "imaam", "mullah", "mulla", "molla", "molah", "mullo", "maulana", "mawlana", "mowlana", "molana", "moulana", "maulvi", "molvi", "moulvi", "maulawi", "mawlawi", "mawlavi", "mufti", "mufty", "qari", "qaari", "hafiz", "hafez", "hafeez", "hafidh", "hafiza", "allamah", "allama", "alama", "allameh", "ayatollah", "ayatullah", "hojjatoleslam", "hojatoleslam", "hujjatalislam", "seghatoleslam", "thiqatalislam", "akhund", "akhond", "akhoond", "agha", "aga", "aqa", "aghaa"
        };

        public override void Up()
        {
            Create.Table("SanctionStopToken")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
                .WithColumn("Token").AsString(64).NotNullable()
                .WithColumn("CategoryId").AsInt32().NotNullable();

            Create.Index()
                .OnTable("SanctionStopToken")
                .OnColumn("Token").Ascending()
                .WithOptions().Unique();

            InsertInto(Honorifics, StopTokenCategory.Honorific);
            InsertInto(ReligiousCulturalTitles, StopTokenCategory.ReligiousCulturalTitle);
        }

        public override void Down()
        {

        }

        private void InsertInto(string[] tokens, StopTokenCategory category)
        {
            foreach (var token in tokens)
            {
                Insert.IntoTable("SanctionStopToken")
                    .Row(new
                    {
                        Token = token,
                        CategoryId = (int)category
                    });
            }
        }

        private enum StopTokenCategory
        {
            Honorific = 1,
            ReligiousCulturalTitle = 2
        }
    }
}
