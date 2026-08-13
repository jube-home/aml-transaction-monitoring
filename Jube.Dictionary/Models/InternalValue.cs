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
    public readonly struct InternalValue : IEquatable<InternalValue>, IComparable<InternalValue>
    {
        public enum ValueType : byte
        {
            None = 0,
            String = 1,
            Int = 2,
            Double = 3,
            Bool = 4,
            DateTime = 5,
            Guid = 6
        }

        [FieldOffset(8)]
        private readonly long _value;

        [FieldOffset(16)]
        private readonly string? _stringValue;

        [FieldOffset(24)]
        private readonly Guid _guidValue;

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
            _value = 0;
            _stringValue = value;
            _guidValue = Guid.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InternalValue(int value)
        {
            Unsafe.SkipInit(out this);
            Type = ValueType.Int;
            _value = value;
            _stringValue = null;
            _guidValue = Guid.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InternalValue(double value)
        {
            Unsafe.SkipInit(out this);
            Type = ValueType.Double;
            _value = Unsafe.As<double, long>(ref value);
            _stringValue = null;
            _guidValue = Guid.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InternalValue(bool value)
        {
            Unsafe.SkipInit(out this);
            Type = ValueType.Bool;
            _value = value ? 1L : 0L;
            _stringValue = null;
            _guidValue = Guid.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InternalValue(DateTime value)
        {
            Unsafe.SkipInit(out this);
            Type = ValueType.DateTime;
            _value = value.ToUniversalTime().Ticks;
            _stringValue = null;
            _guidValue = Guid.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public InternalValue(Guid value)
        {
            Unsafe.SkipInit(out this);
            Type = ValueType.Guid;
            _value = 0;
            _stringValue = null;
            _guidValue = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string AsString()
        {
            return Type == ValueType.String ? _stringValue ?? String.Empty : String.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int AsInt()
        {
            return Type == ValueType.Int ? (int)_value : 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double AsDouble()
        {
            return Type == ValueType.Double ? Unsafe.As<long, double>(ref Unsafe.AsRef(in _value)) : 0d;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AsBool()
        {
            return Type == ValueType.Bool && _value != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DateTime AsDateTime()
        {
            return Type == ValueType.DateTime
                ? new DateTime(_value, DateTimeKind.Utc)
                : default(DateTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Guid AsGuid()
        {
            return Type == ValueType.Guid ? _guidValue : Guid.Empty;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double AsNumeric()
        {
            return Type switch
            {
                ValueType.Double => AsDouble(),
                ValueType.Int => AsInt(),
                _ => 0d
            };
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
                ValueType.Guid => _guidValue.ToString(),
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
                ValueType.Guid => _guidValue.GetHashCode(),
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
                ValueType.Guid => _guidValue == other._guidValue,
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
        public int CompareTo(InternalValue other)
        {
            if (Type == other.Type)
            {
                return Type switch
                {
                    ValueType.String => String.CompareOrdinal(_stringValue, other._stringValue),
                    ValueType.Int => AsInt().CompareTo(other.AsInt()),
                    ValueType.Double => AsDouble().CompareTo(other.AsDouble()),
                    ValueType.Bool => AsBool().CompareTo(other.AsBool()),
                    ValueType.DateTime => AsDateTime().CompareTo(other.AsDateTime()),
                    ValueType.Guid => String.CompareOrdinal(_guidValue.ToString(), other._guidValue.ToString()),
                    _ => 0
                };
            }

            return AsNumeric().CompareTo(other.AsNumeric());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(InternalValue left, InternalValue right)
        {
            return left.CompareTo(right) < 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(InternalValue left, InternalValue right)
        {
            return left.CompareTo(right) > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(InternalValue left, InternalValue right)
        {
            return left.CompareTo(right) <= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(InternalValue left, InternalValue right)
        {
            return left.CompareTo(right) >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double operator +(InternalValue left, InternalValue right)
        {
            return left.AsNumeric() + right.AsNumeric();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double operator -(InternalValue left, InternalValue right)
        {
            return left.AsNumeric() - right.AsNumeric();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double operator *(InternalValue left, InternalValue right)
        {
            return left.AsNumeric() * right.AsNumeric();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double operator /(InternalValue left, InternalValue right)
        {
            return left.AsNumeric() / right.AsNumeric();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(InternalValue left, double right)
        {
            return left.CompareTo(new InternalValue(right)) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(InternalValue left, double right)
        {
            return !(left == right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(InternalValue left, double right)
        {
            return left.CompareTo(new InternalValue(right)) < 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(InternalValue left, double right)
        {
            return left.CompareTo(new InternalValue(right)) > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(InternalValue left, double right)
        {
            return left.CompareTo(new InternalValue(right)) <= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(InternalValue left, double right)
        {
            return left.CompareTo(new InternalValue(right)) >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(double left, InternalValue right)
        {
            return right == left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(double left, InternalValue right)
        {
            return right != left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(double left, InternalValue right)
        {
            return right > left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(double left, InternalValue right)
        {
            return right < left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(double left, InternalValue right)
        {
            return right >= left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(double left, InternalValue right)
        {
            return right <= left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double operator +(InternalValue left, double right)
        {
            return left.AsNumeric() + right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double operator -(InternalValue left, double right)
        {
            return left.AsNumeric() - right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double operator *(InternalValue left, double right)
        {
            return left.AsNumeric() * right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double operator /(InternalValue left, double right)
        {
            return left.AsNumeric() / right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double operator +(double left, InternalValue right)
        {
            return left + right.AsNumeric();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double operator -(double left, InternalValue right)
        {
            return left - right.AsNumeric();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double operator *(double left, InternalValue right)
        {
            return left * right.AsNumeric();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double operator /(double left, InternalValue right)
        {
            return left / right.AsNumeric();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(InternalValue left, int right)
        {
            return left.CompareTo(new InternalValue(right)) == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(InternalValue left, int right)
        {
            return !(left == right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(InternalValue left, int right)
        {
            return left.CompareTo(new InternalValue(right)) < 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(InternalValue left, int right)
        {
            return left.CompareTo(new InternalValue(right)) > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(InternalValue left, int right)
        {
            return left.CompareTo(new InternalValue(right)) <= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(InternalValue left, int right)
        {
            return left.CompareTo(new InternalValue(right)) >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(int left, InternalValue right)
        {
            return right == left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(int left, InternalValue right)
        {
            return right != left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(int left, InternalValue right)
        {
            return right > left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(int left, InternalValue right)
        {
            return right < left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(int left, InternalValue right)
        {
            return right >= left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(int left, InternalValue right)
        {
            return right <= left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double operator +(InternalValue left, int right)
        {
            return left.AsNumeric() + right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double operator -(InternalValue left, int right)
        {
            return left.AsNumeric() - right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double operator *(InternalValue left, int right)
        {
            return left.AsNumeric() * right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double operator /(InternalValue left, int right)
        {
            return left.AsNumeric() / right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double operator +(int left, InternalValue right)
        {
            return left + right.AsNumeric();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double operator -(int left, InternalValue right)
        {
            return left - right.AsNumeric();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double operator *(int left, InternalValue right)
        {
            return left * right.AsNumeric();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double operator /(int left, InternalValue right)
        {
            return left / right.AsNumeric();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(InternalValue left, string? right)
        {
            return left.Equals(new InternalValue(right));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(InternalValue left, string? right)
        {
            return !(left == right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(InternalValue left, string? right)
        {
            return left.CompareTo(new InternalValue(right)) < 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(InternalValue left, string? right)
        {
            return left.CompareTo(new InternalValue(right)) > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(InternalValue left, string? right)
        {
            return left.CompareTo(new InternalValue(right)) <= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(InternalValue left, string? right)
        {
            return left.CompareTo(new InternalValue(right)) >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(string? left, InternalValue right)
        {
            return right == left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(string? left, InternalValue right)
        {
            return right != left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(string? left, InternalValue right)
        {
            return right > left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(string? left, InternalValue right)
        {
            return right < left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(string? left, InternalValue right)
        {
            return right >= left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(string? left, InternalValue right)
        {
            return right <= left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(InternalValue left, DateTime right)
        {
            return left.Equals(new InternalValue(right));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(InternalValue left, DateTime right)
        {
            return !(left == right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(InternalValue left, DateTime right)
        {
            return left.CompareTo(new InternalValue(right)) < 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(InternalValue left, DateTime right)
        {
            return left.CompareTo(new InternalValue(right)) > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(InternalValue left, DateTime right)
        {
            return left.CompareTo(new InternalValue(right)) <= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(InternalValue left, DateTime right)
        {
            return left.CompareTo(new InternalValue(right)) >= 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(DateTime left, InternalValue right)
        {
            return right == left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(DateTime left, InternalValue right)
        {
            return right != left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(DateTime left, InternalValue right)
        {
            return right > left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(DateTime left, InternalValue right)
        {
            return right < left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(DateTime left, InternalValue right)
        {
            return right >= left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(DateTime left, InternalValue right)
        {
            return right <= left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(InternalValue left, bool right)
        {
            return left.Equals(new InternalValue(right));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(InternalValue left, bool right)
        {
            return !(left == right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(bool left, InternalValue right)
        {
            return right == left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(bool left, InternalValue right)
        {
            return right != left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(InternalValue left, Guid right)
        {
            return left.Equals(new InternalValue(right));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(InternalValue left, Guid right)
        {
            return !(left == right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Guid left, InternalValue right)
        {
            return right == left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Guid left, InternalValue right)
        {
            return right != left;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Guid(InternalValue value)
        {
            return value.AsGuid();
        }
    }
}
