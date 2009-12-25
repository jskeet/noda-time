﻿#region Copyright and license information
// Copyright 2001-2009 Stephen Colebourne
// Copyright 2009 Jon Skeet
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion
namespace NodaTime.Format
{
    /// <summary>
    /// Factory that creates instances of PeriodFormatter for the ISO8601 standard.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Period formatting is performed by the <see cref="PeriodFormatter"/> class.
    /// Three classes provide factory methods to create formatters, and this is one.
    /// The others are <see cref="PeriodFormatterFactory"/> and <see cref="PeriodFormatterBuilder"/>.
    /// </para>
    /// <para>
    /// ISOPeriodFormat is thread-safe and immutable, and the formatters it
    /// returns are as well.
    /// </para>
    /// </remarks>
    public static class IsoPeriodFormatterFactory
    {
        /** Cache of standard format. */
        private static PeriodFormatter cStandard;
        /** Cache of alternate months format. */
        private static PeriodFormatter cAlternate;
        /** Cache of alternate extended months format. */
        private static PeriodFormatter cAlternateExtended;
        /** Cache of alternate weeks format. */
        private static PeriodFormatter cAlternateWithWeeks;
        /** Cache of alternate extended weeks format. */
        private static PeriodFormatter cAlternateExtendedWihWeeks;

        /// <summary>
        /// The standard ISO format - PyYmMwWdDThHmMsS.
        /// </summary>
        /// <returns>The formatter</returns>
        /// <remarks>
        /// Milliseconds are not output.
        /// Note that the ISO8601 standard actually indicates weeks should not
        /// be shown if any other field is present and vice versa.
        /// </remarks>
        public static PeriodFormatter Standard()
        {
            if (cStandard == null)
            {
                cStandard = new PeriodFormatterBuilder()
                    .AppendLiteral("P")
                    .AppendYears()
                    .AppendSuffix("Y")
                    .AppendMonths()
                    .AppendSuffix("M")
                    .AppendWeeks()
                    .AppendSuffix("W")
                    .AppendDays()
                    .AppendSuffix("D")
                    .AppendSeparatorIfFieldsAfter("T")
                    .AppendHours()
                    .AppendSuffix("H")
                    .AppendMinutes()
                    .AppendSuffix("M")
                    .AppendSecondsWithOptionalMillis()
                    .AppendSuffix("S")
                    .ToFormatter();
            }
            return cStandard;
        }

        /// <summary>
        /// The alternate ISO format, PyyyymmddThhmmss, which excludes weeks.
        /// </summary>
        /// <returns>The formatter</returns>
        /// <remarks>
        /// Even if weeks are present in the period, they are not output.
        /// Fractional seconds (milliseconds) will appear if required.
        /// </remarks>
        public static PeriodFormatter Alternate()
        {
            if (cAlternate == null)
            {
                cAlternate = new PeriodFormatterBuilder()
                    .AppendLiteral("P")
                    .PrintZeroAlways()
                    .MinimumPrintedDigits(4)
                    .AppendYears()
                    .MinimumPrintedDigits(2)
                    .AppendMonths()
                    .AppendDays()
                    .AppendSeparatorIfFieldsAfter("T")
                    .AppendHours()
                    .AppendMinutes()
                    .AppendSecondsWithOptionalMillis()
                    .ToFormatter();
            }
            return cAlternate;
        }

        /// <summary>
        /// The alternate ISO format, Pyyyy-mm-ddThh:mm:ss, which excludes weeks.
        /// </summary>
        /// <returns>The formatter</returns>
        /// <remarks>
        /// Even if weeks are present in the period, they are not output.
        /// Fractional seconds (milliseconds) will appear if required.
        /// </remarks>
        public static PeriodFormatter AlternateExtended()
        {
            if (cAlternateExtended == null)
            {
                cAlternateExtended = new PeriodFormatterBuilder()
                    .AppendLiteral("P")
                    .PrintZeroAlways()
                    .MinimumPrintedDigits(4)
                    .AppendYears()
                    .AppendSeparator("-")
                    .MinimumPrintedDigits(2)
                    .AppendMonths()
                    .AppendSeparator("-")
                    .AppendDays()
                    .AppendSeparatorIfFieldsAfter("T")
                    .AppendHours()
                    .AppendSeparator(":")
                    .AppendMinutes()
                    .AppendSeparator(":")
                    .AppendSecondsWithOptionalMillis()
                    .ToFormatter();
            }
            return cAlternateExtended;
        }

        /// <summary>
        /// The alternate ISO format, PyyyyWwwddThhmmss, which excludes months.
        /// </summary>
        /// <returns>The formatter</returns>
        /// <remarks>
        /// Even if months are present in the period, they are not output.
        /// Fractional seconds (milliseconds) will appear if required.
        /// </remarks>
        public static PeriodFormatter AlternateWithWeeks()
        {
            if (cAlternateWithWeeks == null)
            {
                cAlternateWithWeeks = new PeriodFormatterBuilder()
                    .AppendLiteral("P")
                    .PrintZeroAlways()
                    .MinimumPrintedDigits(4)
                    .AppendYears()
                    .MinimumPrintedDigits(2)
                    .AppendPrefix("W")
                    .AppendWeeks()
                    .AppendDays()
                    .AppendSeparatorIfFieldsAfter("T")
                    .AppendHours()
                    .AppendMinutes()
                    .AppendSecondsWithOptionalMillis()
                    .ToFormatter();
            }
            return cAlternateWithWeeks;
        }

        /// <summary>
        /// The alternate ISO format, Pyyyy-Www-ddThh:mm:ss, which excludes months.
        /// </summary>
        /// <returns>The formatter</returns>
        /// <remarks>
        /// Even if months are present in the period, they are not output.
        /// Fractional seconds (milliseconds) will appear if required.
        /// </remarks>
        public static PeriodFormatter AlternateExtendedWithWeeks()
        {
            if (cAlternateExtendedWihWeeks == null)
            {
                cAlternateExtendedWihWeeks = new PeriodFormatterBuilder()
                    .AppendLiteral("P")
                    .PrintZeroAlways()
                    .MinimumPrintedDigits(4)
                    .AppendYears()
                    .AppendSeparator("-")
                    .MinimumPrintedDigits(2)
                    .AppendPrefix("W")
                    .AppendWeeks()
                    .AppendSeparator("-")
                    .AppendDays()
                    .AppendSeparatorIfFieldsAfter("T")
                    .AppendHours()
                    .AppendSeparator(":")
                    .AppendMinutes()
                    .AppendSeparator(":")
                    .AppendSecondsWithOptionalMillis()
                    .ToFormatter();
            }
            return cAlternateExtendedWihWeeks;
        }
    }
}
