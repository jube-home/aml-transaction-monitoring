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
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using StackExchange.Redis;

    public sealed class RedisServiceChangeBus(IConnectionMultiplexer connectionMultiplexer) : IServiceChangeBus
    {
        private const string ChannelPrefix = "jube:svc-change:";

        public Task PublishAsync(ServiceChangeEvent change, CancellationToken token = default)
        {
            var subscriber = connectionMultiplexer.GetSubscriber();
            var channel = RedisChannel.Literal(ChannelPrefix + change.TenantRegistryId);
            var json = JsonSerializer.Serialize(change);
            return subscriber.PublishAsync(channel, json);
        }

        public IDisposable Subscribe(Func<ServiceChangeEvent, Task> handler)
        {
            var subscriber = connectionMultiplexer.GetSubscriber();
            var pattern = RedisChannel.Pattern(ChannelPrefix + "*");

#pragma warning disable VSTHRD101
#pragma warning disable AsyncFixer03
            subscriber.Subscribe(pattern, (receivedOnChannel, value) =>
            {
                var json = (string?)value;
                var change = json is null ? null : JsonSerializer.Deserialize<ServiceChangeEvent>(json);
                if (change != null)
                {
                    _ = handler(change);
                }
            });
#pragma warning restore AsyncFixer03
#pragma warning restore VSTHRD101

            return new Subscription(() => subscriber.Unsubscribe(pattern));
        }

        private sealed class Subscription(Action onDispose) : IDisposable
        {
            public void Dispose() => onDispose();
        }
    }
}