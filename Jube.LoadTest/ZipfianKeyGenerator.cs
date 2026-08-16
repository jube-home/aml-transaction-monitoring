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
    public sealed class ZipfianKeyGenerator
    {
        private readonly double exponent;
        private readonly double hIntegralNumberOfElements;
        private readonly long numberOfElements;
        private readonly Random random;
        private readonly double s;

        public ZipfianKeyGenerator(long numberOfElements, double exponent, Random random)
        {
            if (numberOfElements < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(numberOfElements), "Key pool must contain at least one key.");
            }

            if (exponent <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(exponent), "Zipfian skew (s) must be greater than zero.");
            }

            this.numberOfElements = numberOfElements;
            this.exponent = exponent;
            this.random = random;

            hIntegralNumberOfElements = HIntegral(numberOfElements + 0.5);
            s = 2.0 - HIntegralInverse(HIntegral(2.5) - H(2));
        }

        public long Next()
        {
            while (true)
            {
                var u = hIntegralNumberOfElements +
                        random.NextDouble() * (HIntegral(0.5) - hIntegralNumberOfElements);
                var x = HIntegralInverse(u);
                var k = (long)(x + 0.5);

                if (k < 1)
                {
                    k = 1;
                }
                else if (k > numberOfElements)
                {
                    k = numberOfElements;
                }

                if (k - x <= s || u >= HIntegral(k + 0.5) - H(k))
                {
                    return k;
                }
            }
        }

        public static double EstimateTopFractionShare(long numberOfElements, double exponent, double topFraction)
        {
            var topCount = Math.Clamp((long)(numberOfElements * topFraction), 1, numberOfElements);

            double topSum = 0;
            double totalSum = 0;

            for (var k = 1; k <= numberOfElements; k++)
            {
                var weight = Math.Pow(k, -exponent);
                totalSum += weight;
                if (k <= topCount)
                {
                    topSum += weight;
                }
            }

            return topSum / totalSum;
        }

        private double H(double x)
        {
            return Math.Exp(-exponent * Math.Log(x));
        }

        private double HIntegral(double x)
        {
            var logX = Math.Log(x);
            return Helper2((1.0 - exponent) * logX) * logX;
        }

        private double HIntegralInverse(double x)
        {
            var t = x * (1.0 - exponent);
            if (t < -1.0)
            {
                t = -1.0;
            }

            return Math.Exp(Helper1(t) * x);
        }

        private static double Helper1(double x)
        {
            return Math.Abs(x) > 1e-8
                ? Math.Log(1.0 + x) / x
                : 1.0 - x * (0.5 - x * (1.0 / 3 - 0.25 * x));
        }

        private static double Helper2(double x)
        {
            return Math.Abs(x) > 1e-8
                ? (Math.Exp(x) - 1.0) / x
                : 1.0 + x * 0.5 * (1.0 + x / 3 * (1.0 + 0.25 * x));
        }
    }
}
