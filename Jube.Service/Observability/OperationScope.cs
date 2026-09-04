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

namespace Jube.Service.Observability
{
    using System;
    using System.Diagnostics;
    using log4net;
    using Reactivity;

    public sealed class OperationScope : IDisposable
    {
        private readonly string area;
        private readonly string operation;
        private readonly string actor;
        private readonly int? tenantId;
        private readonly ILog audit;
        private readonly ILog log;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly Activity? activity;
        private readonly long startTs;
        private string outcome = "ok";
        private int? entityId;
        private int? rowCount;
        private int? version;
        private ServiceChangeKind? kind;

        private OperationScope(string area, string operation, string? actor, int? tenantId, ILog audit, ILog log,
            IServiceChangeBus serviceChangeBus)
        {
            this.area = area;
            this.operation = operation;
            this.actor = actor ?? "(anonymous)";
            this.tenantId = tenantId;
            this.audit = audit;
            this.log = log;
            this.serviceChangeBus = serviceChangeBus;
            activity = ServiceDiagnostics.ActivitySource.StartActivity($"{area}.{operation}");
            activity?.SetTag("jube.area", area);
            activity?.SetTag("jube.operation", operation);
            activity?.SetTag("jube.actor", this.actor);
            if (tenantId is { } t)
            {
                activity?.SetTag("jube.tenant.id", t);
            }

            startTs = Stopwatch.GetTimestamp();
        }

        public static OperationScope Start(string area, string operation, string? actor, int? tenantId, ILog audit,
            ILog log, IServiceChangeBus serviceChangeBus) =>
            new(area, operation, actor, tenantId, audit, log, serviceChangeBus);

        public void Outcome(string value) => outcome = value;

        public void Entity(int id)
        {
            entityId = id;
            activity?.SetTag("jube.entity.id", id);
        }

        public void Rows(int n)
        {
            rowCount = n;
            activity?.SetTag("jube.row.count", n);
        }

        public void Version(int v)
        {
            version = v;
            activity?.SetTag("jube.version", v);
        }

        public void Error(Exception ex)
        {
            outcome = "error";
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        }

        public void Created() => kind = ServiceChangeKind.Created;
        public void Updated() => kind = ServiceChangeKind.Updated;
        public void Deleted() => kind = ServiceChangeKind.Deleted;

        public void Dispose()
        {
            var ms = Stopwatch.GetElapsedTime(startTs).TotalMilliseconds;

            var tags = new TagList
            {
                { "area", area }, { "operation", operation }, { "outcome", outcome }
            };
            ServiceDiagnostics.OperationCount.Add(1, tags);
            ServiceDiagnostics.OperationDuration.Record(ms, tags);

            activity?.SetTag("jube.outcome", outcome);
            activity?.SetTag("jube.duration.ms", ms);

            if (audit.IsInfoEnabled)
            {
                audit.Info(
                    $"area={area} op={operation} actor={actor} tenant={tenantId?.ToString() ?? "-"} " +
                    $"outcome={outcome} ms={ms:F1} " +
                    $"entityId={entityId?.ToString() ?? "-"} rows={rowCount?.ToString() ?? "-"} " +
                    $"version={version?.ToString() ?? "-"} " +
                    $"trace={activity?.TraceId.ToString() ?? "-"} span={activity?.SpanId.ToString() ?? "-"}");
            }

            if (outcome == "ok" && entityId is { } id && kind is { } k)
            {
                try
                {
                    _ = serviceChangeBus.PublishAsync(new ServiceChangeEvent(
                        area, tenantId ?? 0, k, id, version, actor, DateTimeOffset.UtcNow,
                        activity?.TraceId.ToString() ?? "-"));
                }
                catch (Exception ex)
                {
                    if (log.IsWarnEnabled)
                    {
                        log.Warn($"{area}.{operation}: change-event publish failed", ex);
                    }
                }
            }

            activity?.Dispose();
        }
    }
}