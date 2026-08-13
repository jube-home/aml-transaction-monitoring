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

namespace Jube.App.Dto
{
    using System;
    using Interfaces;

    public class EntityAnalysisModelDto : IUpdated
    {
        public string Name { get; set; }
        public Guid Guid { get; set; }
        public bool Active { get; set; }
        public bool Locked { get; set; }
        public string EntryXPath { get; set; }
        public string ReferenceDateXPath { get; set; }
        public string EntryName { get; set; }
        public char MaxResponseElevationInterval { get; set; }
        public int MaxResponseElevationValue { get; set; }
        public int MaxResponseElevationThreshold { get; set; }
        public char MaxActivationWatcherInterval { get; set; }
        public int MaxActivationWatcherValue { get; set; }
        public int MaxActivationWatcherThreshold { get; set; }
        public double ActivationWatcherSample { get; set; }
        public bool EnableCache { get; set; }
        public bool EnableSanctionCache { get; set; }
        public bool EnableTtlCounter { get; set; }
        public bool EnableElasticsearchArchive { get; set; }
        public bool EnableRdbmsArchive { get; set; }
        public bool EnableActivationArchive { get; set; }
        public bool EnableActivationWatcher { get; set; }
        public bool EnableResponseElevationLimit { get; set; }
        public string ReferenceDateName { get; set; }
        public int CacheFetchLimit { get; set; }
        public char CacheTtlInterval { get; set; }
        public int CacheTtlIntervalValue { get; set; }
        public byte EntryPayloadLocationTypeId { get; set; }
        public byte ReferenceDatePayloadLocationTypeId { get; set; }
        public double MaxResponseElevation { get; set; }
        public int Id { get; set; }
        public string CreatedUser { get; set; }
        public DateTimeOffset? CreatedDate { get; set; }
        public string UpdatedUser { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
        public int Version { get; set; }
        public string DeletedUser { get; set; }
        public DateTimeOffset? DeletedDate { get; set; }
    }
}
