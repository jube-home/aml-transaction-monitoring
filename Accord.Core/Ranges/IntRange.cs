// Accord Core Library
// The Accord.NET Framework
// http://accord-framework.net
//
// Copyright � AForge.NET, 2007-2011
// contacts@aforgenet.com
//
// Copyright � C�sar Souza, 2009-2017
// cesarsouza at gmail.com
//
//    This library is free software; you can redistribute it and/or
//    modify it under the terms of the GNU Lesser General Public
//    License as published by the Free Software Foundation; either
//    version 2.1 of the License, or (at your option) any later version.
//
//    This library is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//    Lesser General Public License for more details.
//
//    You should have received a copy of the GNU Lesser General Public
//    License along with this library; if not, write to the Free Software
//    Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
//

namespace Accord
{
    using Accord;
    using System;
    using System.Collections.Generic;
    using Accord.Compat;

    /// <summary>
    ///   Represents an integer range with minimum and maximum values.
    /// </summary>
    /// 
    /// <remarks>
    ///   The class represents an integer range with inclusive limits, where
    ///   both minimum and maximum values of the range are included into it.
    ///   Mathematical notation of such range is <b>[min, max]</b>.
    /// </remarks>
    /// 
    /// <example>
    /// <code>
    /// // create [1, 10] range
    /// var range1 = new IntRange(1, 10);
    /// 
    /// // create [5, 15] range
    /// var range2 = new IntRange(5, 15);
    /// 
    /// check if values is inside of the first range
    /// if (range1.IsInside(7))
    /// {
    ///     // ...
    /// }
    /// 
    /// // check if the second range is inside of the first range
    /// if (range1.IsInside(range2))
    /// {
    ///     // ...
    /// }
    /// 
    /// // check if two ranges overlap
    /// if (range1.IsOverlapping(range2))
    /// {
    ///     // ...
    /// }
    /// </code>
    /// </example>
    /// 
    /// <seealso cref="DoubleRange"/>
    /// <seealso cref="Range"/>
    /// <seealso cref="IntRange"/>
    ///
    [Serializable]
    public struct IntRange : IRange<int>, IEquatable<IntRange>, IEnumerable<int>
    {
        private int min, max;

        /// <summary>
        ///   Minimum value of the range.
        /// </summary>
        /// 
        /// <remarks>
        ///   Represents minimum value (left side limit) of the range [<b>min</b>, max].
        /// </remarks>
        /// 
        public int Min
        {
            get { return min; }
            set { min = value; }
        }

        /// <summary>
        ///   Maximum value of the range.
        /// </summary>
        /// 
        /// <remarks>
        ///   Represents maximum value (right side limit) of the range [min, <b>max</b>].
        /// </remarks>
        /// 
        public int Max
        {
            get { return max; }
            set { max = value; }
        }

        /// <summary>
        ///   Gets the length of the range, defined as (max - min).
        /// </summary>
        /// 
        public int Length
        {
            get { return max - min; }
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="IntRange"/> class.
        /// </summary>
        /// 
        /// <param name="min">Minimum value of the range.</param>
        /// <param name="max">Maximum value of the range.</param>
        /// 
        public IntRange(int min, int max)
        {
            this.min = min;
            this.max = max;
        }

        /// <summary>
        ///   Check if the specified value is inside of the range.
        /// </summary>
        /// 
        /// <param name="x">Value to check.</param>
        /// 
        /// <returns>
        ///   <b>True</b> if the specified value is inside of the range or <b>false</b> otherwise.
        /// </returns>
        /// 
        public bool IsInside(int x)
        {
            return ((x >= min) && (x <= max));
        }

        /// <summary>
        ///   Computes the intersection between two ranges.
        /// </summary>
        /// 
        /// <param name="range">The second range for which the intersection should be calculated.</param>
        /// 
        /// <returns>An new <see cref="IntRange"/> structure containing the intersection
        /// between this range and the <paramref name="range"/> given as argument.</returns>
        /// 
        public IntRange Intersection(IntRange range)
        {
            return new IntRange(System.Math.Max(this.Min, range.Min), System.Math.Min(this.Max, range.Max));
        }

        /// <summary>
        ///   Check if the specified range is inside of the range.
        /// </summary>
        /// 
        /// <param name="range">Range to check.</param>
        /// 
        /// <returns>
        ///   <b>True</b> if the specified range is inside of the range or <b>false</b> otherwise.
        /// </returns>
        /// 
        public bool IsInside(IntRange range)
        {
            return ((IsInside(range.min)) && (IsInside(range.max)));
        }

        /// <summary>
        ///   Check if the specified range overlaps with the range.
        /// </summary>
        /// 
        /// <param name="range">Range to check for overlapping.</param>
        /// 
        /// <returns>
        ///   <b>True</b> if the specified range overlaps with the range or <b>false</b> otherwise.
        /// </returns>
        /// 
        public bool IsOverlapping(IntRange range)
        {
            return ((IsInside(range.min)) || (IsInside(range.max)) ||
                     (range.IsInside(min)) || (range.IsInside(max)));
        }

        /// <summary>
        ///   Determines whether two instances are equal.
        /// </summary>
        /// 
        public static bool operator ==(IntRange range1, IntRange range2)
        {
            return ((range1.min == range2.min) && (range1.max == range2.max));
        }

        /// <summary>
        ///   Determines whether two instances are not equal.
        /// </summary>
        /// 
        public static bool operator !=(IntRange range1, IntRange range2)
        {
            return ((range1.min != range2.min) || (range1.max != range2.max));
        }

        /// <summary>
        ///   Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// 
        /// <param name="other">An object to compare with this object.</param>
        /// 
        /// <returns>
        ///   true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        /// 
        public bool Equals(IntRange other)
        {
            return this == other;
        }

        /// <summary>
        ///   Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// 
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// 
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        /// 
        public override bool Equals(object obj)
        {
            return (obj is IntRange) ? (this == (IntRange)obj) : false;
        }

        /// <summary>
        ///   Returns a hash code for this instance.
        /// </summary>
        /// 
        /// <returns>
        ///   A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        /// 
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + min.GetHashCode();
                hash = hash * 31 + max.GetHashCode();
                return hash;
            }
        }

        /// <summary>
        ///   Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// 
        /// <returns>
        ///   A <see cref="System.String" /> that represents this instance.
        /// </returns>
        /// 
        public override string ToString()
        {
            return String.Format("[{0}, {1}]", min, max);
        }

        /// <summary>
        ///   Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// 
        /// <param name="format">The format.</param>
        /// <param name="formatProvider">The format provider.</param>
        /// 
        /// <returns>
        ///   A <see cref="System.String" /> that represents this instance.
        /// </returns>
        /// 
        public string ToString(string format, IFormatProvider formatProvider)
        {
            return String.Format("[{0}, {1}]", 
                min.ToString(format, formatProvider),
                max.ToString(format, formatProvider));
        }




        /// <summary>
        ///   Performs an implicit conversion from <see cref="IntRange"/> to <see cref="DoubleRange"/>.
        /// </summary>
        /// 
        /// <param name="range">The range.</param>
        /// 
        /// <returns>
        ///   The result of the conversion.
        /// </returns>
        /// 
        public static implicit operator DoubleRange(IntRange range)
        {
            return new DoubleRange(range.Min, range.Max);
        }

        /// <summary>
        ///   Performs an implicit conversion from <see cref="IntRange"/> to <see cref="Range"/>.
        /// </summary>
        /// 
        /// <param name="range">The range.</param>
        /// 
        /// <returns>
        ///   The result of the conversion.
        /// </returns>
        /// 
        public static implicit operator Range(IntRange range)
        {
            return new Range(range.Min, range.Max);
        }

        /// <summary>
        ///   Returns an enumerator that iterates through a collection.
        /// </summary>
        /// 
        /// <returns>
        ///   An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        /// 
        public IEnumerator<int> GetEnumerator()
        {
            for (int i = min; i < max; i++)
                yield return i;
        }

        /// <summary>
        ///   Returns an enumerator that iterates through a collection.
        /// </summary>
        /// 
        /// <returns>
        ///   An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        /// 
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            for (int i = min; i < max; i++)
                yield return i;
        }
    }
}
