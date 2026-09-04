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

namespace Jube.Service.EntityAnalysisModel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Jube.Dto.EntityAnalysisModel;
    using ModelPoco = Jube.Data.Poco.EntityAnalysisModel;

    internal static class EntityAnalysisModelMapper
    {
        public static EntityAnalysisModelDto? ToDto(ModelPoco? entityAnalysisModel) => entityAnalysisModel is null
            ? null
            : new EntityAnalysisModelDto
            {
                Id = entityAnalysisModel.Id,
                Name = entityAnalysisModel.Name,
                Guid = entityAnalysisModel.Guid,
                Active = entityAnalysisModel.Active == 1,
                Locked = entityAnalysisModel.Locked == 1,
                EntryXPath = entityAnalysisModel.EntryXPath,
                EntryName = entityAnalysisModel.EntryName,
                ReferenceDateXPath = entityAnalysisModel.ReferenceDateXPath,
                ReferenceDateName = entityAnalysisModel.ReferenceDateName,
                ReferenceDatePayloadLocationTypeId =
                    entityAnalysisModel.ReferenceDatePayloadLocationTypeId.GetValueOrDefault(),
                CacheFetchLimit = entityAnalysisModel.CacheFetchLimit.GetValueOrDefault(),
                CacheTtlInterval = entityAnalysisModel.CacheTtlInterval.GetValueOrDefault(),
                CacheTtlIntervalValue = entityAnalysisModel.CacheTtlIntervalValue.GetValueOrDefault(),
                MaxResponseElevation = entityAnalysisModel.MaxResponseElevation.GetValueOrDefault(),
                MaxResponseElevationInterval = entityAnalysisModel.MaxResponseElevationInterval.GetValueOrDefault(),
                MaxResponseElevationValue = entityAnalysisModel.MaxResponseElevationValue.GetValueOrDefault(),
                MaxResponseElevationThreshold = entityAnalysisModel.MaxResponseElevationThreshold.GetValueOrDefault(),
                EnableResponseElevationLimit = entityAnalysisModel.EnableResponseElevationLimit == 1,
                MaxActivationWatcherInterval = entityAnalysisModel.MaxActivationWatcherInterval.GetValueOrDefault(),
                MaxActivationWatcherValue = entityAnalysisModel.MaxActivationWatcherValue.GetValueOrDefault(),
                MaxActivationWatcherThreshold = entityAnalysisModel.MaxActivationWatcherThreshold.GetValueOrDefault(),
                ActivationWatcherSample = entityAnalysisModel.ActivationWatcherSample.GetValueOrDefault(),
                EnableCache = entityAnalysisModel.EnableCache == 1,
                EnableSanctionCache = entityAnalysisModel.EnableSanctionCache == 1,
                EnableTtlCounter = entityAnalysisModel.EnableTtlCounter == 1,
                EnableRdbmsArchive = entityAnalysisModel.EnableRdbmsArchive == 1,
                EnableActivationArchive = entityAnalysisModel.EnableActivationArchive == 1,
                EnableActivationWatcher = entityAnalysisModel.EnableActivationWatcher == 1,
                CreatedUser = entityAnalysisModel.CreatedUser,
                CreatedDate = ToOffset(entityAnalysisModel.CreatedDate),
                UpdatedUser = entityAnalysisModel.UpdatedUser,
                UpdatedDate = ToOffset(entityAnalysisModel.UpdatedDate),
                Version = entityAnalysisModel.Version.GetValueOrDefault(),
                DeletedUser = entityAnalysisModel.DeletedUser,
                DeletedDate = ToOffset(entityAnalysisModel.DeletedDate)
            };

        public static List<EntityAnalysisModelDto> ToDto(IEnumerable<ModelPoco>? source) =>
            (source ?? Enumerable.Empty<ModelPoco>()).Select(p => ToDto(p)!).ToList();

        public static ModelPoco ToPoco(EntityAnalysisModelDto dto) => new()
        {
            Id = dto.Id,
            Name = dto.Name,
            Active = (byte)(dto.Active ? 1 : 0),
            Locked = (byte)(dto.Locked ? 1 : 0),
            EntryXPath = dto.EntryXPath,
            EntryName = dto.EntryName,
            ReferenceDateXPath = dto.ReferenceDateXPath,
            ReferenceDateName = dto.ReferenceDateName,
            ReferenceDatePayloadLocationTypeId = dto.ReferenceDatePayloadLocationTypeId,
            CacheFetchLimit = dto.CacheFetchLimit,
            CacheTtlInterval = dto.CacheTtlInterval,
            CacheTtlIntervalValue = dto.CacheTtlIntervalValue,
            MaxResponseElevation = dto.MaxResponseElevation,
            MaxResponseElevationInterval = dto.MaxResponseElevationInterval,
            MaxResponseElevationValue = dto.MaxResponseElevationValue,
            MaxResponseElevationThreshold = dto.MaxResponseElevationThreshold,
            EnableResponseElevationLimit = (byte)(dto.EnableResponseElevationLimit ? 1 : 0),
            MaxActivationWatcherInterval = dto.MaxActivationWatcherInterval,
            MaxActivationWatcherValue = dto.MaxActivationWatcherValue,
            MaxActivationWatcherThreshold = dto.MaxActivationWatcherThreshold,
            ActivationWatcherSample = dto.ActivationWatcherSample,
            EnableCache = (byte)(dto.EnableCache ? 1 : 0),
            EnableSanctionCache = (byte)(dto.EnableSanctionCache ? 1 : 0),
            EnableTtlCounter = (byte)(dto.EnableTtlCounter ? 1 : 0),
            EnableRdbmsArchive = (byte)(dto.EnableRdbmsArchive ? 1 : 0),
            EnableActivationArchive = (byte)(dto.EnableActivationArchive ? 1 : 0),
            EnableActivationWatcher = (byte)(dto.EnableActivationWatcher ? 1 : 0)
        };

        private static DateTimeOffset? ToOffset(DateTime? value) =>
            value.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                : null;
    }
}