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
    using System.Diagnostics;
    using System.Globalization;
    using System.Text.RegularExpressions;

    public sealed class ContainerStatsSampler
    {
        private static readonly Regex MemUnitRegex = new Regex(@"^([\d.]+)\s*([A-Za-z]+)$", RegexOptions.Compiled);
        private readonly Dictionary<string, List<Regex>> rolePatterns;

        private readonly IReadOnlyList<ContainerRole> roles;

        public ContainerStatsSampler(IReadOnlyList<ContainerRole> roles, bool patternsAreRegex)
        {
            this.roles = roles;
            rolePatterns = roles.ToDictionary(
                role => role.Name,
                role => role.Patterns
                    .Select(pattern => new Regex(
                        patternsAreRegex ? pattern : Regex.Escape(pattern),
                        RegexOptions.IgnoreCase | RegexOptions.Compiled))
                    .ToList());
        }

        public async Task<IReadOnlyDictionary<string, (double CpuPercent, double MemMiB)>?> SampleAsync(
            CancellationToken token = default)
        {
            List<(string Name, string CpuPercent, string MemUsage)> stats;
            try
            {
                stats = await RunDockerStatsAsync(token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"container stats sampling failed: {ex.Message}")
                    .ConfigureAwait(false);
                return null;
            }

            var result = roles.ToDictionary(role => role.Name, _ => (CpuPercent: 0.0, MemMiB: 0.0));

            foreach (var stat in stats)
            {
                foreach (var role in roles)
                {
                    if (!rolePatterns[role.Name].Any(pattern => pattern.IsMatch(stat.Name)))
                    {
                        continue;
                    }

                    var (cpuPercent, memMiB) = result[role.Name];
                    result[role.Name] = (
                        cpuPercent + ParseCpuPercent(stat.CpuPercent),
                        memMiB + ParseMemUsageToMiB(stat.MemUsage));
                }
            }

            return result;
        }

        private static double ParseCpuPercent(string cpuPerc)
        {
            var trimmed = cpuPerc.TrimEnd('%').Trim();
            return Double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                ? value
                : 0;
        }

        private static double ParseMemUsageToMiB(string memUsage)
        {
            var usedPart = memUsage.Split('/')[0].Trim();
            var match = MemUnitRegex.Match(usedPart);
            if (!match.Success ||
                !Double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                return 0;
            }

            return match.Groups[2].Value.ToUpperInvariant() switch
            {
                "B" => value / (1024 * 1024),
                "KIB" or "KB" => value / 1024,
                "MIB" or "MB" => value,
                "GIB" or "GB" => value * 1024,
                "TIB" or "TB" => value * 1024 * 1024,
                _ => value
            };
        }

        private static async Task<List<(string Name, string CpuPercent, string MemUsage)>> RunDockerStatsAsync(
            CancellationToken token)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "docker",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add("stats");
            startInfo.ArgumentList.Add("--no-stream");
            startInfo.ArgumentList.Add("--format");
            startInfo.ArgumentList.Add("{{.Name}}\t{{.CPUPerc}}\t{{.MemUsage}}");

            using var process = Process.Start(startInfo)
                                ?? throw new InvalidOperationException("Unable to start the docker process.");

            var standardOutput = await process.StandardOutput.ReadToEndAsync(token).ConfigureAwait(false);
            var standardError = await process.StandardError.ReadToEndAsync(token).ConfigureAwait(false);
            await process.WaitForExitAsync(token).ConfigureAwait(false);

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"docker stats exited with code {process.ExitCode}: {standardError.Trim()}");
            }

            var results = new List<(string, string, string)>();
            foreach (var line in standardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var columns = line.Split('\t');
                if (columns.Length != 3)
                {
                    continue;
                }

                results.Add((columns[0].Trim(), columns[1].Trim(), columns[2].Trim()));
            }

            return results;
        }
    }
}
