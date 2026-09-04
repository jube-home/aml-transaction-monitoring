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
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class InProcessServiceChangeBus : IServiceChangeBus
    {
        private readonly ConcurrentDictionary<Guid, Func<ServiceChangeEvent, Task>> handlers = new();

        public async Task PublishAsync(ServiceChangeEvent change, CancellationToken token = default)
        {
            foreach (var handler in handlers.Values)
            {
                await handler(change).ConfigureAwait(false);
            }
        }

        public IDisposable Subscribe(Func<ServiceChangeEvent, Task> handler)
        {
            var key = Guid.NewGuid();
            handlers[key] = handler;
            return new Subscription(() => handlers.TryRemove(key, out _));
        }

        private sealed class Subscription(Action onDispose) : IDisposable
        {
            public void Dispose() => onDispose();
        }
    }
}