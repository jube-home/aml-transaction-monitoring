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

using FluentMigrator;

namespace Jube.Migrations.Baseline
{
    [Migration(20220430135800)]
    public class AddEntityAnalysisModelSearchKeyCacheEntryFk : Migration
    {
        public override void Up()
        {
            Create.ForeignKey("FK_Truncated_EAMSKCE_EAMSKDVCI").FromTable("ArchiveEntityAnalysisModelAbstractionEntry")
                .ForeignColumn("EntityAnalysisModelSearchKeyDistinctValueCalculationInstanceId")
                .ToTable("EntityAnalysisModelSearchKeyDistinctValueCalculationInstance").PrimaryColumn("Id");
        }

        public override void Down()
        {
            Delete.ForeignKey("FK_Truncated_EAMSKCE_EAMSKDVCI");
        }
    }
}