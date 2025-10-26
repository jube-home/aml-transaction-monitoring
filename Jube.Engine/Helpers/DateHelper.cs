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

namespace Jube.Engine.Helpers
{
    using System;
    using System.Globalization;

    public static class DateHelper
    {
        public enum DateInterval
        {
            Day,
            DayOfYear,
            Hour,
            Minute,
            Month,
            Quarter,
            Second,
            Weekday,
            WeekOfYear,
            Year
        }

        public static int DateDiff(DateInterval intervalType, DateTime dateOne, DateTime dateTwo)
        {
            switch (intervalType)
            {
                case DateInterval.Day:
                case DateInterval.DayOfYear:
                    var spanForDays = dateTwo - dateOne;
                    return (int)spanForDays.TotalDays;
                case DateInterval.Hour:
                    var spanForHours = dateTwo - dateOne;
                    return (int)spanForHours.TotalHours;
                case DateInterval.Minute:
                    var spanForMinutes = dateTwo - dateOne;
                    return (int)spanForMinutes.TotalMinutes;
                case DateInterval.Month:
                    return (dateTwo.Year - dateOne.Year) * 12 + (dateTwo.Month - dateOne.Month);
                case DateInterval.Quarter:
                    var dateOneQuarter = (int)Math.Ceiling(dateOne.Month / 3.0);
                    var dateTwoQuarter = (int)Math.Ceiling(dateTwo.Month / 3.0);
                    return 4 * (dateTwo.Year - dateOne.Year) + dateTwoQuarter - dateOneQuarter;
                case DateInterval.Second:
                    var spanForSeconds = dateTwo - dateOne;
                    return (int)spanForSeconds.TotalSeconds;
                case DateInterval.Weekday:
                    var spanForWeekdays = dateTwo - dateOne;
                    return (int)(spanForWeekdays.TotalDays / 7.0);
                case DateInterval.WeekOfYear:
                    var dateOneModified = dateOne;
                    var dateTwoModified = dateTwo;
                    while (dateTwoModified.DayOfWeek != DateTimeFormatInfo.CurrentInfo.FirstDayOfWeek)
                    {
                        dateTwoModified = dateTwoModified.AddDays(-1);
                    }
                    while (dateOneModified.DayOfWeek != DateTimeFormatInfo.CurrentInfo.FirstDayOfWeek)
                    {
                        dateOneModified = dateOneModified.AddDays(-1);
                    }
                    var spanForWeekOfYear = dateTwoModified - dateOneModified;
                    return (int)(spanForWeekOfYear.TotalDays / 7.0);
                case DateInterval.Year:
                    return dateTwo.Year - dateOne.Year;
                default:
                    return 0;
            }
        }
    }
}
