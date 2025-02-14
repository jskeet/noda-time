// Copyright 2011 The Noda Time Authors. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using System;
using NodaTime.Text;
using NUnit.Framework;

namespace NodaTime.Test
{
    public class TimeIntervalTest
    {
        private static readonly LocalTime SampleStart = new LocalTime(10, 00);
        private static readonly LocalTime SampleEnd = new LocalTime(17, 00);
        
        private static readonly LocalTime SampleStartInverted = new LocalTime(23, 00);
        private static readonly LocalTime SampleEndInverted = new LocalTime(1, 00);

        [Test]
        public void Duration_True()
        {
            var interval = new TimeInterval(SampleStart, SampleEnd);
            Assert.AreEqual(Duration.FromHours(7), interval.Duration);
        }
        [Test]
        public void Period_True()
        {
            var interval = new TimeInterval(SampleStart, SampleEnd);
            Assert.AreEqual(Period.FromHours(7), interval.Period);
        }
        
        [Test]
        public void InvertedTimeInterval_Duration_True()
        {
            var interval = new TimeInterval(SampleStartInverted, SampleEndInverted);
            Assert.AreEqual(Duration.FromHours(2), interval.Duration);
        }
        
        [Test]
        public void InvertedTimeInterval_Period_True()
        {
            var interval = new TimeInterval(SampleStartInverted, SampleEndInverted);
            Assert.AreEqual(Period.FromHours(2), interval.Period);
        }
        
        [Test]
        public void InvertedTimeInterval_Contains_True()
        {
            var interval = new TimeInterval(SampleStartInverted, SampleEndInverted);
            Assert.IsTrue(interval.Contains(new LocalTime(23, 00)));
            Assert.IsTrue(interval.Contains(new LocalTime(23, 30)));
            Assert.IsTrue(interval.Contains(new LocalTime(00, 00)));
            Assert.IsTrue(interval.Contains(new LocalTime(00, 30)));
            Assert.IsTrue(interval.Contains(new LocalTime(00, 59)));
        }
        
        [Test]
        public void InvertedTimeInterval_Contains_False()
        {
            var interval = new TimeInterval(SampleStartInverted, SampleEndInverted);
            Assert.IsFalse(interval.Contains(new LocalTime(1, 00)));
            Assert.IsFalse(interval.Contains(new LocalTime(1, 01)));
            Assert.IsFalse(interval.Contains(new LocalTime(13, 00)));
            Assert.IsFalse(interval.Contains(new LocalTime(22, 30)));
            Assert.IsFalse(interval.Contains(new LocalTime(22, 59)));
        }


        [Test]
        public void ToString_True()
        {
            var sampleStart = SampleStart.PlusMilliseconds(230).PlusNanoseconds(139);
            var interval = new TimeInterval(sampleStart, SampleEnd);
            Assert.AreEqual("10:00:00.230000139/17:00:00", interval.ToString());
        }
        [Test]
        public void Parse_True()
        {
            var sampleStart = SampleStart.PlusMilliseconds(230).PlusNanoseconds(139);
            var interval = new TimeInterval(sampleStart, SampleEnd);
            Assert.AreEqual(interval, TimeInterval.Parse("10:00:00.230000139/17:00:00"));
        }
        
        [Test]
        public void Parse_False()
        {
            var interval = new TimeInterval(SampleStart, SampleEnd);
            Assert.AreNotEqual(interval, TimeInterval.Parse("10:00:00.230000139/17:00:00"));
        }
        
        [Test]
        public void Parse_ThrowsException()
        {
            Assert.Throws<FormatException>(() => TimeInterval.Parse("10:00:0017:00:00"));
        }
        
        [Test]
        public void ToStringAndParse_True()
        {
            var sampleStart = SampleStart.PlusMilliseconds(230).PlusNanoseconds(139);
            var interval = new TimeInterval(sampleStart, SampleEnd);
            Assert.AreEqual(interval, TimeInterval.Parse(interval.ToString()));
        }

    }
}
