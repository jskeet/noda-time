﻿// Copyright 2017 The Noda Time Authors. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NodaTime.TimeZones;
using NUnit.Framework;

namespace NodaTime.Test.TimeZones
{
    public class TransitionTest
    {
        [Test]
        public void Equality()
        {
            var equal1 = new Transition(Instant.FromUnixTimeSeconds(100), Offset.FromHours(1));
            var equal2 = new Transition(Instant.FromUnixTimeSeconds(100), Offset.FromHours(1));
            var unequal1 = new Transition(Instant.FromUnixTimeSeconds(101), Offset.FromHours(1));
            var unequal2 = new Transition(Instant.FromUnixTimeSeconds(100), Offset.FromHours(2));
            TestHelper.TestEqualsStruct(equal1, equal2, unequal1);
            TestHelper.TestEqualsStruct(equal1, equal2, unequal2);
            TestHelper.TestOperatorEquality(equal1, equal2, unequal1);
            TestHelper.TestOperatorEquality(equal1, equal2, unequal2);
        }
    }
}
