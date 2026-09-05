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
        static partial void AddEntityAnalysisModelGatewayRule(List<ServiceToolDescriptor> tools)
        {
            tools.AddRange(
            [
                new ServiceToolDescriptor(
                    "EntityAnalysisModelGatewayRuleList", OperationKind.Read, true, false,
                    "Lists Gateway Rules for the caller's tenant, keyset-paged and capped."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelGatewayRuleGet", OperationKind.Read, true, false,
                    "Returns one Gateway Rule by id, scoped to the caller's tenant."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelGatewayRuleGetByEntityAnalysisModelId", OperationKind.Read, true,
                    false,
                    "Lists Gateway Rules belonging to a given Model, scoped to the caller's tenant."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelGatewayRuleCreate", OperationKind.Write, false, false,
                    "Registers a Gateway Rule under a Model in the caller's tenant; calling twice creates two."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelGatewayRuleUpdate", OperationKind.Write, true, false,
                    "Updates a Gateway Rule in the caller's tenant by id."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelGatewayRuleDelete", OperationKind.Delete, true, true,
                    "Soft-deletes a Gateway Rule in the caller's tenant by id."),
                new ServiceToolDescriptor(
                    "EntityAnalysisModelGatewayRuleResetCounter", OperationKind.Write, true, true,
                    "Resets the activation and evaluation counters for a Gateway Rule in the caller's tenant to zero.")
            ]);
        }
    }
}