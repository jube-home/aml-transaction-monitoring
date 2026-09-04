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
        private static List<ServiceToolDescriptor>? all;

        public static IReadOnlyList<ServiceToolDescriptor> All
        {
            get
            {
                if (all != null)
                {
                    return all;
                }

                var tools = new List<ServiceToolDescriptor>();
                
                AddEntityAnalysisModel(tools);
                all = tools;
                
                return all;
            }
        }

        static partial void AddEntityAnalysisModel(List<ServiceToolDescriptor> tools);
    }
}