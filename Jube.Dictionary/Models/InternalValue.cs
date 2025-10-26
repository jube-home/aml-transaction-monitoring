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

namespace Jube.Dictionary.Models
{
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit, Pack = 1)]
    public readonly struct InternalValue : IEquatable<InternalValue>
    {
        public enum ValueType : byte
        {
            None = 0,
            String = 1,
            Int = 2,
            Double = 3,
            Bool = 4,
            DateTime = 5
        }

        [FieldOffset(8)]
        private readonly long _value;

        [FieldOffset(16)]
        private readonly string? _stringValue;

        [field: FieldOffset(0)]
        public ValueType Type
        {
            get;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InternalValue(string? value)
        {
            Unsafe.SkipInit(out this);
            Type = ValueType.String;
            _value = 0;// Initialize shared value space
            _stringValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InternalValue(int value)
        {
            Unsafe.SkipInit(out this);
            Type = ValueType.Int;
            _value = value;
            _stringValue = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InternalValue(double value)
        {
            Unsafe.SkipInit(out this);
            Type = ValueType.Double;
            _value = Unsafe.As<double, long>(ref value);
            _stringValue = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InternalValue(bool value)
        {
            Unsafe.SkipInit(out this);
            Type = ValueType.Bool;
            _value = value ? 1L : 0L;
            _stringValue = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InternalValue(DateTime value)
        {
            Unsafe.SkipInit(out this);
            Type = ValueType.DateTime;

            _value = value.ToUniversalTime().Ticks;
            _stringValue = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string AsString()
        {
            return Type == ValueType.String ? _stringValue ?? String.Empty : String.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int AsInt()
        {
            if (Type == ValueType.Int)
            {
                return (int)_value;
            }
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double AsDouble()
        {
            return Type == ValueType.Double ? Unsafe.As<long, double>(ref Unsafe.AsRef(in _value)) : 0d;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AsBool()
        {
            if (Type == ValueType.Bool)
            {
                return _value != 0;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DateTime AsDateTime()
        {
            return Type == ValueType.DateTime ? new DateTime(_value, DateTimeKind.Utc).ToLocalTime() : default(DateTime);
        }

        public override string ToString()
        {
            return Type switch
            {
                ValueType.String => _stringValue ?? "null",
                ValueType.Int => AsInt().ToString(),
                ValueType.Double => AsDouble().ToString(CultureInfo.InvariantCulture),
                ValueType.Bool => AsBool().ToString(),
                ValueType.DateTime => AsDateTime().ToString("o"),
                _ => "None"
            };
        }

        public override int GetHashCode()
        {
            return Type switch
            {
                ValueType.String => _stringValue?.GetHashCode() ?? 0,
                ValueType.Int => _value.GetHashCode(),
                ValueType.Double => AsDouble().GetHashCode(),
                ValueType.Bool => _value.GetHashCode(),
                ValueType.DateTime => _value.GetHashCode(),
                _ => 0
            };
        }

        public override bool Equals(object? obj)
        {
            return obj is InternalValue other && Equals(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(InternalValue other)
        {
            if (Type != other.Type)
            {
                return false;
            }

            return Type switch
            {
                ValueType.String => String.Equals(_stringValue, other._stringValue),
                ValueType.Int => _value == other._value,
                ValueType.Double => Math.Abs(AsDouble() - other.AsDouble()) < 0.0001,
                ValueType.Bool => _value == other._value,
                ValueType.DateTime => _value == other._value,
                _ => true
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(InternalValue left, InternalValue right)
        {
            return left.Equals(right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(InternalValue left, InternalValue right)
        {
            return !(left == right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator string(InternalValue value)
        {
            return value.AsString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator int(InternalValue value)
        {
            return value.AsInt();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator double(InternalValue value)
        {
            return value.AsDouble();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator bool(InternalValue value)
        {
            return value.AsBool();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator DateTime(InternalValue value)
        {
            return value.AsDateTime();
        }
    }
}
