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

namespace Jube.Test.Infrastructure
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using log4net;
    using log4net.Core;

    public sealed class TestLog(bool enabled = true) : ILog
    {
        public static readonly TestLog NoOp = new();

        private readonly ConcurrentQueue<Entry> entries = new();

        public IReadOnlyList<Entry> Entries => [.. entries];

        public sealed record Entry(string Level, string Message, Exception? Exception = null);

        public bool IsDebugEnabled => enabled;
        public bool IsInfoEnabled => enabled;
        public bool IsWarnEnabled => enabled;
        public bool IsErrorEnabled => true;
        public bool IsFatalEnabled => true;

        public void Debug(object? message) => Capture("DEBUG", message);
        public void Debug(object? message, Exception? exception) => Capture("DEBUG", message, exception);
        public void DebugFormat(string format, params object?[]? args) => Capture("DEBUG", Format(format, args));
        public void DebugFormat(string format, object? arg0) => Capture("DEBUG", Format(format, arg0));

        public void DebugFormat(string format, object? arg0, object? arg1) =>
            Capture("DEBUG", Format(format, arg0, arg1));

        public void DebugFormat(string format, object? arg0, object? arg1, object? arg2) =>
            Capture("DEBUG", Format(format, arg0, arg1, arg2));

        public void DebugFormat(IFormatProvider? provider, string format, params object?[]? args) =>
            Capture("DEBUG", String.Format(provider, format, args!));

        public void Info(object? message) => Capture("INFO", message);
        public void Info(object? message, Exception? exception) => Capture("INFO", message, exception);
        public void InfoFormat(string format, params object?[]? args) => Capture("INFO", Format(format, args));
        public void InfoFormat(string format, object? arg0) => Capture("INFO", Format(format, arg0));

        public void InfoFormat(string format, object? arg0, object? arg1) =>
            Capture("INFO", Format(format, arg0, arg1));

        public void InfoFormat(string format, object? arg0, object? arg1, object? arg2) =>
            Capture("INFO", Format(format, arg0, arg1, arg2));

        public void InfoFormat(IFormatProvider? provider, string format, params object?[]? args) =>
            Capture("INFO", String.Format(provider, format, args!));

        public void Warn(object? message) => Capture("WARN", message);
        public void Warn(object? message, Exception? exception) => Capture("WARN", message, exception);
        public void WarnFormat(string format, params object?[]? args) => Capture("WARN", Format(format, args));
        public void WarnFormat(string format, object? arg0) => Capture("WARN", Format(format, arg0));

        public void WarnFormat(string format, object? arg0, object? arg1) =>
            Capture("WARN", Format(format, arg0, arg1));

        public void WarnFormat(string format, object? arg0, object? arg1, object? arg2) =>
            Capture("WARN", Format(format, arg0, arg1, arg2));

        public void WarnFormat(IFormatProvider? provider, string format, params object?[]? args) =>
            Capture("WARN", String.Format(provider, format, args!));

        public void Error(object? message) => Capture("ERROR", message);
        public void Error(object? message, Exception? exception) => Capture("ERROR", message, exception);
        public void ErrorFormat(string format, params object?[]? args) => Capture("ERROR", Format(format, args));
        public void ErrorFormat(string format, object? arg0) => Capture("ERROR", Format(format, arg0));

        public void ErrorFormat(string format, object? arg0, object? arg1) =>
            Capture("ERROR", Format(format, arg0, arg1));

        public void ErrorFormat(string format, object? arg0, object? arg1, object? arg2) =>
            Capture("ERROR", Format(format, arg0, arg1, arg2));

        public void ErrorFormat(IFormatProvider? provider, string format, params object?[]? args) =>
            Capture("ERROR", String.Format(provider, format, args!));

        public void Fatal(object? message) => Capture("FATAL", message);
        public void Fatal(object? message, Exception? exception) => Capture("FATAL", message, exception);
        public void FatalFormat(string format, params object?[]? args) => Capture("FATAL", Format(format, args));
        public void FatalFormat(string format, object? arg0) => Capture("FATAL", Format(format, arg0));

        public void FatalFormat(string format, object? arg0, object? arg1) =>
            Capture("FATAL", Format(format, arg0, arg1));

        public void FatalFormat(string format, object? arg0, object? arg1, object? arg2) =>
            Capture("FATAL", Format(format, arg0, arg1, arg2));

        public void FatalFormat(IFormatProvider? provider, string format, params object?[]? args) =>
            Capture("FATAL", String.Format(provider, format, args!));

        public ILogger Logger => throw new NotSupportedException("TestLog does not back a real logger repository.");

        private void Capture(string level, object? message, Exception? exception = null) =>
            entries.Enqueue(new Entry(level, message?.ToString() ?? String.Empty, exception));

        private static string Format(string format, params object?[]? args) => String.Format(format, args!);
    }
}