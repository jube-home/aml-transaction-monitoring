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

    // Reads /proc/stat and /proc/meminfo directly rather than shelling out, so it has no dependency
    // on the docker CLI or socket. Docker containers see the *host's* /proc by default (it isn't
    // cgroup-namespaced), so this reports genuine host-wide CPU/RAM even when this tool itself runs
    // inside a container - it just won't be reachable on non-Linux hosts or heavily sandboxed
    // runtimes, in which case sampling is skipped rather than thrown.
    public sealed class HostStatsSampler
    {
        private const string ProcStatPath = "/proc/stat";
        private const string ProcMemInfoPath = "/proc/meminfo";

        private readonly bool available = File.Exists(ProcStatPath) && File.Exists(ProcMemInfoPath);
        private (long Idle, long Total)? lastCpuSample;

        // Returns null when /proc isn't reachable at all. CpuPercent reads 0 on the very first
        // sample of a run, since it's a delta over consecutive samples and there's no prior baseline.
        public (double CpuPercent, double MemUsedMiB)? Sample()
        {
            if (!available)
            {
                return null;
            }

            try
            {
                return (SampleCpuPercent(), SampleMemUsedMiB());
            }
            catch (IOException)
            {
                return null;
            }
        }

        private double SampleCpuPercent()
        {
            var line = File.ReadLines(ProcStatPath).First(l => l.StartsWith("cpu ", StringComparison.Ordinal));
            var fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Skip(1)
                .Select(field => Int64.Parse(field, CultureInfo.InvariantCulture))
                .ToArray();

            // user, nice, system, idle, iowait, irq, softirq, steal, guest, guest_nice
            var idle = fields[3] + (fields.Length > 4 ? fields[4] : 0);
            var total = fields.Sum();

            if (lastCpuSample is not {} last)
            {
                lastCpuSample = (idle, total);
                return 0;
            }

            lastCpuSample = (idle, total);

            var totalDelta = total - last.Total;
            if (totalDelta <= 0)
            {
                return 0;
            }

            var idleDelta = idle - last.Idle;
            return 100.0 * (1.0 - (double)idleDelta / totalDelta);
        }

        private static double SampleMemUsedMiB()
        {
            var values = new Dictionary<string, long>();
            foreach (var line in File.ReadLines(ProcMemInfoPath))
            {
                var separatorIndex = line.IndexOf(':');
                if (separatorIndex < 0)
                {
                    continue;
                }

                var key = line[..separatorIndex];
                var rest = line[(separatorIndex + 1)..].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (rest.Length > 0 && Int64.TryParse(rest[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var kb))
                {
                    values[key] = kb;
                }
            }

            var totalKb = values.GetValueOrDefault("MemTotal");

            // MemAvailable needs kernel >= 3.14; fall back to the classic free+buffers+cached estimate.
            var availableKb = values.TryGetValue("MemAvailable", out var available)
                ? available
                : values.GetValueOrDefault("MemFree") + values.GetValueOrDefault("Buffers") + values.GetValueOrDefault("Cached");

            return (totalKb - availableKb) / 1024.0;
        }
    }
}
