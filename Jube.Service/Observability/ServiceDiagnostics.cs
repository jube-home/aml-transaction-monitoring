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

namespace Jube.Service.Observability
{
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Reflection;

    public static class ServiceDiagnostics
    {
        public const string Name = "Jube.Service";
        private static readonly AssemblyName Asm = typeof(ServiceDiagnostics).Assembly.GetName();

        // ReSharper disable once MemberCanBePrivate.Global
        public static readonly string Version = Asm.Version?.ToString() ?? "0.0.0";
        public static readonly ActivitySource ActivitySource = new(Name, Version);

        // ReSharper disable once MemberCanBePrivate.Global
        public static readonly Meter Meter = new(Name, Version);

        public static readonly Counter<long> OperationCount =
            Meter.CreateCounter<long>("jube.service.operation.count", unit: "{operation}");

        public static readonly Histogram<double> OperationDuration =
            Meter.CreateHistogram<double>("jube.service.operation.duration", unit: "ms");
    }
}