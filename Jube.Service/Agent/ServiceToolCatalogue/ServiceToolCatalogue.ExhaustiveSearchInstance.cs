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

namespace Jube.Service.Agent.ServiceToolCatalogue
{
    public static partial class ServiceToolCatalogue
    {
        static partial void AddExhaustiveSearchInstance(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "ExhaustiveSearchInstanceList", OperationKind.Read, true, false,
                    "Lists Exhaustive Adaptations for the caller's tenant, keyset-paged and capped."),
                new ServiceToolDescriptor(
                    "ExhaustiveSearchInstanceGet", OperationKind.Read, true, false,
                    "Returns one Exhaustive Adaptation by id, scoped to the caller's tenant."),
                new ServiceToolDescriptor(
                    "ExhaustiveSearchInstanceGetByEntityAnalysisModelId", OperationKind.Read, true, false,
                    "Lists Exhaustive Adaptations belonging to a given Model, scoped to the caller's tenant."),
                new ServiceToolDescriptor(
                    "ExhaustiveSearchInstanceCreate", OperationKind.Write, false, false,
                    "Registers an Exhaustive Adaptation under a Model in the caller's tenant; calling twice creates two."),
                new ServiceToolDescriptor(
                    "ExhaustiveSearchInstanceUpdate", OperationKind.Write, true, false,
                    "Updates an Exhaustive Adaptation in the caller's tenant by id, only while training has not started."),
                new ServiceToolDescriptor(
                    "ExhaustiveSearchInstanceStop", OperationKind.Write, true, false,
                    "Requests that an in-progress or queued training run stop, by Guid."),
                new ServiceToolDescriptor(
                    "ExhaustiveSearchInstanceDelete", OperationKind.Delete, true, true,
                    "Soft-deletes an Exhaustive Adaptation in the caller's tenant by id.")
            ]);
        }
    }
}
