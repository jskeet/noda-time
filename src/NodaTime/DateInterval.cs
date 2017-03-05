﻿// Copyright 2014 The Noda Time Authors. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using NodaTime.Annotations;
using NodaTime.Text;
using NodaTime.Utility;

namespace NodaTime
{
    /// <summary>
    /// An interval between two dates.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The two dates must be in the same calendar, and the end date must not be earlier than the start date.
    /// </para>
    /// <para>
    /// By default, the end date is deemed to be part of the range, as this matches many real life uses of
    /// date ranges. For example, if someone says "I'm going to be on holiday from Monday to Friday," they
    /// usually mean that Friday is part of their holiday. This can be configured via a constructor parameter,
    /// as occasionally an exclusive end date can be useful. For example, to create an interval covering a
    /// whole month, you can simply provide the first day of the month as the start and the first day of the
    /// next month as the exclusive end.
    /// </para>
    /// <para>
    /// Values can be compared for equality, but note that end-inclusive intervals and end-exclusive intervals are always
    /// considered to differ, even if they cover the same range of dates.
    /// </para>
    /// </remarks>
    /// <threadsafety>This type is immutable reference type. See the thread safety section of the user guide for more information.</threadsafety>
    [Immutable]
    public sealed class DateInterval : IEquatable<DateInterval>
    {
        /// <summary>
        /// Returns an equality comparer which compares intervals by first normalizing them to exclusive intervals.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This comparer considers date intervals to be equal if they have the same start and end dates when considered
        /// as exclusive intervals.  Alternatively: this comparer is identical to the built-in equality for
        /// <c>DateInterval</c>, except that it also considers an exclusive date interval of [2001-01-01, 2001-02-01) as
        /// equal to an end-inclusive date interval of [2001-01-01, 2001-01-31].
        /// </para>
        /// <para>
        /// Note that intervals with different start dates are still considered unequal by this comparer (even empty
        /// intervals that contain no dates), as are intervals containing dates from different calendars.
        /// </para>
        /// </remarks>
        /// <value>An equality comparer which compares intervals by first normalizing them to exclusive
        /// intervals.</value>
        /// <seealso cref="ContainedDatesEqualityComparer"/>
        public static IEqualityComparer<DateInterval> NormalizingEqualityComparer => NormalizingDateIntervalEqualityComparer.Instance;

        /// <summary>
        /// Returns an equality comparer which compares intervals by comparing the set of dates contained within them.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This comparer considers date intervals to be equal if they would contain the same dates.  Alternatively:
        /// this comparer is identical to the built-in equality for <c>DateInterval</c>, except that it also considers
        /// an exclusive date interval of [2001-01-01, 2001-02-01) as equal to an end-inclusive date interval of
        /// [2001-01-01, 2001-01-31] and considers all empty intervals — e.g. [2001-01-01, 2001-01-01) or [1900-01-01,
        /// 1900-01-01) — to be equal to each other (as every empty interval contains the same set of dates).
        /// </para>
        /// <para>
        /// Note that intervals containing dates from different calendars are still considered unequal by this comparer.
        /// </para>
        /// </remarks>
        /// <value>An equality comparer which compares intervals by comparing the set of dates contained within
        /// them.</value>
        /// <seealso cref="NormalizingEqualityComparer"/>
        public static IEqualityComparer<DateInterval> ContainedDatesEqualityComparer => ContainedDatesDateIntervalEqualityComparer.Instance;

        /// <summary>
        /// Gets the start date of the interval, which is always included in the interval.
        /// </summary>
        /// <value>The start date of the interval.</value>
        public LocalDate Start { get; }

        /// <summary>
        /// Gets the end date of the interval.
        /// </summary>
        /// <remarks>
        /// Use the <see cref="EndInclusive"/> property to determine whether or not the end
        /// date is considered part of the interval.
        /// </remarks>
        /// <value>The end date of the interval.</value>
        public LocalDate End { get; }

        /// <summary>
        /// Indicates whether or not this interval includes its end date.
        /// </summary>
        /// <value>Whether or not this interval includes its end date.</value>
        public bool EndInclusive { get; }

        /// <summary>
        /// Constructs a date interval from a start date and an end date, and an indication
        /// of whether the end date should be included in the interval.
        /// </summary>
        /// <param name="start">Start date of the interval</param>
        /// <param name="end">End date of the interval</param>
        /// <param name="endInclusive"><c>true</c> to include the end date in the interval;
        /// <c>false</c> to exclude it.
        /// </param>
        /// <exception cref="ArgumentException"><paramref name="end"/> is earlier than <paramref name="start"/>
        /// or the two dates are in different calendars.
        /// </exception>
        /// <returns>A date interval between the specified dates, with the specified inclusivity.</returns>
        public DateInterval(LocalDate start, LocalDate end, bool endInclusive)
        {
            Preconditions.CheckArgument(start.Calendar.Equals(end.Calendar), nameof(end),
                "Calendars of start and end dates must be the same.");
            Preconditions.CheckArgument(!(end < start), nameof(end), "End date must not be earlier than the start date");
            this.Start = start;
            this.End = end;
            this.EndInclusive = endInclusive;
        }

        /// <summary>
        /// Constructs a date interval from a start date and an inclusive end date.
        /// </summary>
        /// <param name="start">Start date of the interval</param>
        /// <param name="end">End date of the interval, inclusive</param>
        /// <exception cref="ArgumentException"><paramref name="end"/> is earlier than <paramref name="start"/>
        /// or the two dates are in different calendars.
        /// </exception>
        /// <returns>An end-inclusive date interval between the specified dates.</returns>
        public DateInterval(LocalDate start, LocalDate end)
            : this(start, end, true)
        {
        }

        /// <summary>
        /// Returns the hash code for this interval, consistent with <see cref="Equals(DateInterval)"/>.
        /// </summary>
        /// <returns>The hash code for this interval.</returns>
        public override int GetHashCode() =>
            HashCodeHelper.Initialize()
                .Hash(Start)
                .Hash(End)
                .Hash(EndInclusive)
                .Value;

        /// <summary>
        /// Compares two <see cref="DateInterval" /> values for equality.
        /// </summary>
        /// <remarks>
        /// Date intervals are equal if they have the same start and end dates and are both end-inclusive or both end-exclusive:
        /// an end-exclusive date interval of [2001-01-01, 2001-02-01) is not equal to the end-inclusive date interval of
        /// [2001-01-01, 2001-01-31], even though both contain the same range of dates.
        /// </remarks>
        /// <param name="lhs">The first value to compare</param>
        /// <param name="rhs">The second value to compare</param>
        /// <returns>True if the two date intervals have the same properties; false otherwise.</returns>
        public static bool operator ==(DateInterval lhs, DateInterval rhs)
        {
            if (ReferenceEquals(lhs, rhs))
            {
                return true;
            }
            if (ReferenceEquals(lhs, null) || ReferenceEquals(rhs, null))
            {
                return false;
            }
            return lhs.Start == rhs.Start && lhs.End == rhs.End && lhs.EndInclusive == rhs.EndInclusive;
        }

        /// <summary>
        /// Compares two <see cref="DateInterval" /> values for inequality.
        /// </summary>
        /// <remarks>
        /// Date intervals are equal if they have the same start and end dates and are both end-inclusive or both exclusive:
        /// an end-exclusive date interval of [2001-01-01, 2001-02-01) is not equal to the end-inclusive date interval of
        /// [2001-01-01, 2001-01-31], even though both contain the same range of dates.
        /// </remarks>
        /// <param name="lhs">The first value to compare</param>
        /// <param name="rhs">The second value to compare</param>
        /// <returns>False if the two date intervals have the same properties; true otherwise.</returns>
        public static bool operator !=(DateInterval lhs, DateInterval rhs) => !(lhs == rhs);

        /// <summary>
        /// Compares the given date interval for equality with this one.
        /// </summary>
        /// <remarks>
        /// Date intervals are equal if they have the same start and end dates and are both end-inclusive or both end-exclusive:
        /// an end-exclusive date interval of [2001-01-01, 2001-02-01) is not equal to the end-inclusive date interval of
        /// [2001-01-01, 2001-01-31], even though both contain the same range of dates.
        /// </remarks>
        /// <param name="other">The date interval to compare this one with.</param>
        /// <returns>True if this date interval has the same properties as the one specified.</returns>
        public bool Equals(DateInterval other) => this == other;

        /// <summary>
        /// Compares the given object for equality with this one, as per <see cref="Equals(DateInterval)"/>.
        /// </summary>
        /// <param name="obj">The value to compare this one with.</param>
        /// <returns>true if the other object is a date interval equal to this one, consistent with <see cref="Equals(DateInterval)"/>.</returns>
        public override bool Equals(object obj) => this == (obj as DateInterval);

        /// <summary>
        /// Checks whether the given date is within this date interval. This requires
        /// that the date is not earlier than the start date, and not later than the end
        /// date. If the given date is exactly equal to the end date, it is considered
        /// to be within the interval if and only if the interval is <see cref="EndInclusive"/>.
        /// </summary>
        /// <param name="date">The date to check for containment within this interval.</param>
        /// <exception cref="ArgumentException"><paramref name="date"/> is not in the same
        /// calendar as the start and end date of this interval.</exception>
        /// <returns><c>true</c> if <paramref name="date"/> is within this interval; <c>false</c> otherwise.</returns>
        public bool Contains(LocalDate date)
        {
            Preconditions.CheckArgument(date.Calendar.Equals(Start.Calendar), nameof(date),
                "The date to check must be in the same calendar as the start and end dates");
            return Start <= date && (EndInclusive ? date <= End : date < End);
        }

        /// <summary>
        /// Gets the length of this date interval in days.
        /// </summary>
        /// <remarks>
        /// The end date is included or excluded according to the <see cref="EndInclusive"/>
        /// property. For example, an end-inclusive interval where the start and end date are the
        /// same has a length of 1, whereas an exclusive interval for the same dates has a
        /// length of 0.
        /// </remarks>
        /// <value>The length of this date interval in days.</value>
        public int Length =>
            // Period.Between will give us the exclusive result, so we need to add 1
            // if this period is end-inclusive.
            Period.Between(Start, End, PeriodUnits.Days).Days + (EndInclusive ? 1 : 0);

        /// <summary>
        /// Returns a string representation of this interval.
        /// </summary>
        /// <returns>
        /// A string representation of this interval, as <c>[start, end]</c> for end-inclusive intervals, or <c>[start, end)</c> for
        /// exclusive intervals, where "start" and "end" are the dates formatted using an ISO-8601 compatible pattern.
        /// </returns>
        public override string ToString()
        {
            string start = LocalDatePattern.Iso.Format(Start);
            string end = LocalDatePattern.Iso.Format(End);
            string endType = EndInclusive ? "]" : ")";
            return $"[{start}, {end}{endType}";
        }

        /// <summary>
        /// Equality comparer that normalizes intervals before comparing them.
        /// </summary>
        private sealed class NormalizingDateIntervalEqualityComparer : EqualityComparer<DateInterval>
        {
            internal static readonly NormalizingDateIntervalEqualityComparer Instance = new NormalizingDateIntervalEqualityComparer();

            private NormalizingDateIntervalEqualityComparer()
            {
            }

            public override bool Equals(DateInterval x, DateInterval y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }
                if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                {
                    return false;
                }
                return Normalize(x) == Normalize(y);
            }

            public override int GetHashCode([NotNull] DateInterval obj) =>
                Normalize(Preconditions.CheckNotNull(obj, nameof(obj))).GetHashCode();

            /// <summary>
            /// Returns a normalized version of this interval such that intervals that have the same start date and
            /// length are mapped to values that compare equal (while remaining unequal to intervals with a different
            /// start date or length).  This is conceptually equivalent to converting each passed-in interval to an
            /// equivalent exclusive interval.
            /// </summary>
            /// <remarks>
            /// <para>
            /// This method does not convert the passed-in interval to an exclusive interval.  That would be too easy.
            /// For example, consider the interval [..., 9999-12-31] (in the ISO calendar).  This is equivalent to [...,
            /// 10000-01-01), which cannot be represented as a DateInterval.
            /// </para>
            /// <para>
            /// Instead, this method maps all non-empty intervals to end-inclusive intervals, and retains all empty
            /// intervals as empty intervals (which are exclusive by definition, and unequal to any non-empty interval).
            /// </para>
            /// </remarks>
            /// <returns>The normalized interval.</returns>
            [NotNull]
            internal static DateInterval Normalize([NotNull] DateInterval obj)
            {
                return obj.EndInclusive || obj.Length == 0 ? obj : new DateInterval(obj.Start, obj.End.PlusDays(-1), true);
            }
        }

        /// <summary>
        /// Equality comparer that considers the dates contained within intervals.
        /// </summary>
        private sealed class ContainedDatesDateIntervalEqualityComparer : EqualityComparer<DateInterval>
        {
            internal static readonly ContainedDatesDateIntervalEqualityComparer Instance = new ContainedDatesDateIntervalEqualityComparer();
            private static readonly DateInterval CanonicalIsoEmptyInterval = CreateCanonicalEmptyInterval(CalendarSystem.Iso);

            private ContainedDatesDateIntervalEqualityComparer()
            {
            }

            public override bool Equals(DateInterval x, DateInterval y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }
                if (ReferenceEquals(x, null) || ReferenceEquals(y, null))
                {
                    return false;
                }
                return Normalize(x) == Normalize(y);
            }

            public override int GetHashCode([NotNull] DateInterval obj) =>
                Normalize(Preconditions.CheckNotNull(obj, nameof(obj))).GetHashCode();

            /// <summary>
            /// Returns a normalized version of this interval, by converting non-empty exclusive intervals to end-inclusive
            /// ones, and canonicalizing empty intervals to the same start date.
            /// </summary>
            /// <returns>The normalized interval.</returns>
            [NotNull]
            private static DateInterval Normalize([NotNull] DateInterval obj)
            {
                if (obj.Length > 0)
                {
                    return NormalizingDateIntervalEqualityComparer.Normalize(obj);
                }
                var calendar = obj.Start.Calendar;
                return calendar == CalendarSystem.Iso ? CanonicalIsoEmptyInterval : CreateCanonicalEmptyInterval(calendar);
            }

            private static DateInterval CreateCanonicalEmptyInterval(CalendarSystem calendar)
            {
                // This can use any arbitrary date for the given calendar, so long as it's a valid date.
                var date = new LocalDate(calendar.MinYear, 1, 1, calendar);
                return new DateInterval(date, date, false);
            }
        }
    }
}
