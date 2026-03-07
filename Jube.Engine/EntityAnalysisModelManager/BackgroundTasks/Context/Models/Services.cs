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

namespace Jube.Engine.EntityAnalysisModelManager.BackgroundTasks.Context.Models
{
    using Cache;
    using DynamicEnvironment;
    using log4net;
    using RabbitMQ.Client;
    using TaskCancellation;

    public class Services
    {
        public ILog Log { get; set; }
        public DynamicEnvironment DynamicEnvironment { get; set; }
        public ITaskCoordinator TaskCoordinator { get; set; }
        public IConnection RabbitMqConnection { get; set; }
        public CacheService CacheService { get; set; }
        public string ReportConnectionString { get; set; }
    }
}
