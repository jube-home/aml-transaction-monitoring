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
        static partial void AddEntityAnalysisModelAbstractionCalculation(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionCalculationList", OperationKind.Read, true, false,
                    "Lists Abstraction Calculations for the caller's tenant, keyset-paged and capped."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionCalculationGet", OperationKind.Read, true, false,
                    "Returns one Abstraction Calculation by id, scoped to the caller's tenant."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionCalculationGetByEntityAnalysisModelId", OperationKind.Read, true,
                    false,
                    "Lists Abstraction Calculations belonging to a given Model, scoped to the caller's tenant."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionCalculationCreate", OperationKind.Write, false, false,
                    "Registers an Abstraction Calculation under a Model in the caller's tenant; calling twice creates two."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionCalculationUpdate", OperationKind.Write, true, false,
                    "Updates an Abstraction Calculation in the caller's tenant by id."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelAbstractionCalculationDelete", OperationKind.Delete, true, true,
                    "Soft-deletes an Abstraction Calculation in the caller's tenant by id.")
            ]);
        }
    }
}