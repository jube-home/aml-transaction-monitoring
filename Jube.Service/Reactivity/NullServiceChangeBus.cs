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

using Jube.Service.Reactivity.Interfaces;

namespace Jube.Service.Reactivity
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class NullServiceChangeBus : IServiceChangeBus
    {
        public Task PublishAsync(ServiceChangeEvent change, CancellationToken token = default) => Task.CompletedTask;

        public IDisposable Subscribe(Func<ServiceChangeEvent, Task> handler) => NullSubscription.Instance;

        private sealed class NullSubscription : IDisposable
        {
            public static readonly NullSubscription Instance = new();

            public void Dispose()
            {
            }
        }
    }
}