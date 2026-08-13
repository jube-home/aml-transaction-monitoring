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

namespace Jube.LoadTest
{
    using System.Globalization;
    using StackExchange.Redis;

    public sealed class RedisStatsSampler(string connectionString) : IDisposable
    {
        private static readonly TimeSpan SampleTimeout = TimeSpan.FromSeconds(1);
        private Task<IConnectionMultiplexer>? connecting;
        private (long Hits, long Misses)? lastSample;

        private IConnectionMultiplexer? multiplexer;

        public void Dispose()
        {
            multiplexer?.Dispose();
        }

        public async Task<(double ConnectedClients, double UsedMemMiB, double OpsPerSec, double HitRatePct)?> SampleAsync()
        {
            IConnectionMultiplexer? mux;
            try
            {
                mux = await GetMultiplexerAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"redis stats sampling failed to connect: {ex.Message}").ConfigureAwait(false);
                return null;
            }

            var server = mux?.GetServers().FirstOrDefault(s => s.IsConnected);
            if (server == null)
            {
                return null;
            }

            try
            {
                var sections = await server.InfoAsync().ConfigureAwait(false);
                var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var pair in sections.SelectMany(section => section))
                {
                    values[pair.Key] = pair.Value;
                }

                double GetDouble(string key)
                {
                    return values.TryGetValue(key, out var raw) &&
                           Double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                        ? value
                        : 0;
                }

                var connectedClients = GetDouble("connected_clients");
                var usedMemoryBytes = GetDouble("used_memory");
                var opsPerSec = GetDouble("instantaneous_ops_per_sec");
                var hits = (long)GetDouble("keyspace_hits");
                var misses = (long)GetDouble("keyspace_misses");

                var hitRatePct = 0.0;
                if (lastSample is {} last)
                {
                    var hitDelta = hits - last.Hits;
                    var missDelta = misses - last.Misses;
                    var totalDelta = hitDelta + missDelta;
                    hitRatePct = totalDelta > 0 ? 100.0 * hitDelta / totalDelta : 0;
                }

                lastSample = (hits, misses);

                return (connectedClients, usedMemoryBytes / 1024.0 / 1024.0, opsPerSec, hitRatePct);
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"redis stats sampling failed: {ex.Message}").ConfigureAwait(false);
                return null;
            }
        }

        private async Task<IConnectionMultiplexer?> GetMultiplexerAsync()
        {
            if (multiplexer != null)
            {
                return multiplexer;
            }

            connecting ??= ConnectAsync();

            var completed = await Task.WhenAny(connecting, Task.Delay(SampleTimeout)).ConfigureAwait(false);
            if (completed != connecting)
            {
                return null;
            }

            try
            {
                multiplexer = await connecting.ConfigureAwait(false);
                return multiplexer;
            }
            finally
            {
                connecting = null;
            }
        }

        private async Task<IConnectionMultiplexer> ConnectAsync()
        {
            var options = ConfigurationOptions.Parse(connectionString);
            options.AllowAdmin = true;
            return await ConnectionMultiplexer.ConnectAsync(options).ConfigureAwait(false);
        }
    }
}
