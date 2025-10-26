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

namespace Jube.Engine.Model
{
    public class EntityAnalysisModelRequestXPath
    {
        public string Name { get; set; }
        public int DataTypeId { get; set; }
        public string XPath { get; set; }
        public bool SearchKey { get; set; }
        public bool SearchKeyCache { get; set; }
        public bool SearchKeyCacheSample { get; set; }
        public string SearchKeyCacheInterval { get; set; }
        public int SearchKeyCacheValue { get; set; }
        public string SearchKeyCacheTtlIntervalType { get; set; }
        public int SearchKeyCacheTtlValue { get; set; }
        public int Id { get; init; }
        public bool ResponsePayload { get; set; }
        public int SearchKeyCacheFetchLimit { get; set; }
        public bool ReportTable { get; set; }
        public bool EnableSuppression { get; set; }
        public string DefaultValue { get; set; }
        public int SearchKeyFetchLimit { get; set; }
        public string SearchKeyTtlInterval { get; set; }
        public int SearchKeyTtlIntervalValue { get; set; }
    }
}