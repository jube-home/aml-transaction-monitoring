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

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Jube.Service.Agent
{
    using System;

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ServiceOperationAttribute(string name, OperationKind kind) : Attribute
    {
        public string Name { get; } = name;
        public OperationKind Kind { get; } = kind;
        public bool Idempotent { get; init; }
        public bool Destructive { get; init; }
    }
}