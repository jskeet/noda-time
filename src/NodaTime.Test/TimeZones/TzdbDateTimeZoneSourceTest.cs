// Copyright 2012 The Noda Time Authors. All rights reserved.
// Use of this source code is governed by the Apache License 2.0,
// as found in the LICENSE.txt file.

using NodaTime.TimeZones;
using NodaTime.TimeZones.Cldr;
using NodaTime.TimeZones.IO;
using NodaTime.Utility;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace NodaTime.Test.TimeZones
{
    public class TzdbDateTimeZoneSourceTest
    {
        private static readonly List<NamedWrapper<TimeZoneInfo>> SystemTimeZones =
            TimeZoneInfo.GetSystemTimeZones().Select(zone => new NamedWrapper<TimeZoneInfo>(zone, zone.Id)).ToList();

        /// <summary>
        /// Tests that we can load (and exercise) the binary Tzdb resource file distributed with Noda Time 1.1.0.
        /// This is effectively a black-box regression test that ensures that the stream format has not changed in a
        /// way such that a custom tzdb compiled with ZoneInfoCompiler from 1.1 would become unreadable.
        /// </summary>
        [Test]
        public void CanLoadNodaTimeResourceFromOnePointOneRelease()
        {
            var assembly = typeof(TzdbDateTimeZoneSourceTest).GetTypeInfo().Assembly;
            TzdbDateTimeZoneSource source;
            using (Stream stream = assembly.GetManifestResourceStream("NodaTime.Test.TestData.Tzdb2013bFromNodaTime1.1.nzd"))
            {
                source = TzdbDateTimeZoneSource.FromStream(stream);
            }
            Assert.AreEqual("TZDB: 2013b (mapping: 8274)", source.VersionId);

            var utc = Instant.FromUtc(2007, 8, 24, 9, 30, 0);

            // Test a regular zone with rules.
            var london = source.ForId("Europe/London");
            var inLondon = new ZonedDateTime(utc, london);
            var expectedLocal = new LocalDateTime(2007, 8, 24, 10, 30);
            Assert.AreEqual(expectedLocal, inLondon.LocalDateTime);

            // Test a fixed-offset zone.
            var utcFixed = source.ForId("Etc/UTC");
            var inUtcFixed = new ZonedDateTime(utc, utcFixed);
            expectedLocal = new LocalDateTime(2007, 8, 24, 9, 30);
            Assert.AreEqual(expectedLocal, inUtcFixed.LocalDateTime);

            // Test an alias.
            var jersey = source.ForId("Japan");  // Asia/Tokyo
            var inJersey = new ZonedDateTime(utc, jersey);
            expectedLocal = new LocalDateTime(2007, 8, 24, 18, 30);
            Assert.AreEqual(expectedLocal, inJersey.LocalDateTime);

            // Test ZoneLocations.
            var france = source.ZoneLocations.Single(g => g.CountryName == "France");
            // Tolerance of about 2 seconds
            Assert.AreEqual(48.86666, france.Latitude, 0.00055);
            Assert.AreEqual(2.3333, france.Longitude, 0.00055);
            Assert.AreEqual("Europe/Paris", france.ZoneId);
            Assert.AreEqual("FR", france.CountryCode);
            Assert.AreEqual("", france.Comment);
        }

        /// <summary>
        /// Simply tests that every ID in the built-in database can be fetched. This is also
        /// helpful for diagnostic debugging when we want to check that some potential
        /// invariant holds for all time zones...
        /// </summary>
        [Test]
        public void ForId_AllIds()
        {
            var source = TzdbDateTimeZoneSource.Default;
            foreach (string id in source.GetIds())
            {
                Assert.IsNotNull(source.ForId(id));
            }
        }

        [Test]
        public void ForId_Null()
        {
            Assert.Throws<ArgumentNullException>(() => TzdbDateTimeZoneSource.Default.ForId(null!));
        }

        [Test]
        public void ForId_Unknown()
        {
            Assert.Throws<ArgumentException>(() => TzdbDateTimeZoneSource.Default.ForId("unknown"));
        }

        [Test]
        public void UtcEqualsBuiltIn()
        {
            var zone = TzdbDateTimeZoneSource.Default.ForId("UTC");
            Assert.AreEqual(DateTimeZone.Utc, zone);
        }

        // The following tests all make assumptions about the built-in TZDB data.
        // This is simpler than constructing fake data, and validates that the creation
        // mechanism matches the reading mechanism, too.
        [Test]
        public void Aliases()
        {
            var aliases = TzdbDateTimeZoneSource.Default.Aliases;
            CollectionAssert.AreEqual(new[] { "Europe/Belfast", "Europe/Guernsey", "Europe/Isle_of_Man", "Europe/Jersey", "GB", "GB-Eire" },
                                      aliases["Europe/London"].ToArray()); // ToArray call makes diagnostics more useful
            CollectionAssert.IsOrdered(aliases["Europe/London"]);
            CollectionAssert.IsEmpty(aliases["Europe/Jersey"]);
        }

        [Test]
        public void CanonicalIdMap_Contents()
        {
            var map = TzdbDateTimeZoneSource.Default.CanonicalIdMap;
            Assert.AreEqual("Europe/London", map["Europe/Jersey"]);
            Assert.AreEqual("Europe/London", map["Europe/London"]);
        }

        [Test]
        public void CanonicalIdMap_IsReadOnly()
        {
            var map = TzdbDateTimeZoneSource.Default.CanonicalIdMap;
            Assert.Throws<NotSupportedException>(() => map.Add("Foo", "Bar"));
        }

        // Sample zone location checks to ensure we've serialized and deserialized correctly
        // Input line: FR	+4852+00220	Europe/Paris
        [Test]
        public void ZoneLocations_ContainsFrance()
        {
            var zoneLocations = TzdbDateTimeZoneSource.Default.ZoneLocations;
            var france = zoneLocations.Single(g => g.CountryName == "France");
            // Tolerance of about 2 seconds
            Assert.AreEqual(48.86666, france.Latitude, 0.00055);
            Assert.AreEqual(2.3333, france.Longitude, 0.00055);
            Assert.AreEqual("Europe/Paris", france.ZoneId);
            Assert.AreEqual("FR", france.CountryCode);
            Assert.AreEqual("", france.Comment);
        }

        // Sample zone location checks to ensure we've serialized and deserialized correctly
        // Input line: GB,GG,IM,JE	+513030-0000731	Europe/London
        [Test]
        public void Zone1970Locations_ContainsBritain()
        {
            var zoneLocations = TzdbDateTimeZoneSource.Default.Zone1970Locations;
            var britain = zoneLocations.Single(g => g.ZoneId == "Europe/London");
            // Tolerance of about 2 seconds
            Assert.AreEqual(51.5083, britain.Latitude, 0.00055);
            Assert.AreEqual(-0.1253, britain.Longitude, 0.00055);
            Assert.AreEqual("Europe/London", britain.ZoneId);
            CollectionAssert.AreEqual(
                new[]
                {
                    new TzdbZone1970Location.Country("Britain (UK)", "GB"),
                    new TzdbZone1970Location.Country("Guernsey", "GG"),
                    new TzdbZone1970Location.Country("Isle of Man", "IM"),
                    new TzdbZone1970Location.Country("Jersey", "JE")
                },
                britain.Countries.ToArray());
            Assert.AreEqual("", britain.Comment);
        }

        // Input line: CA	+744144-0944945	America/Resolute	Central - NU (Resolute)
        // (Note: prior to 2016b, this was "Central Time - Resolute, Nunavut".)
        // (Note: prior to 2014f, this was "Central Standard Time - Resolute, Nunavut".)
        [Test]
        public void ZoneLocations_ContainsResolute()
        {
            var zoneLocations = TzdbDateTimeZoneSource.Default.ZoneLocations;
            var resolute = zoneLocations.Single(g => g.ZoneId == "America/Resolute");
            // Tolerance of about 2 seconds
            Assert.AreEqual(74.69555, resolute.Latitude, 0.00055);
            Assert.AreEqual(-94.82916, resolute.Longitude, 0.00055);
            Assert.AreEqual("Canada", resolute.CountryName);
            Assert.AreEqual("CA", resolute.CountryCode);
            Assert.AreEqual("Central - NU (Resolute)", resolute.Comment);
        }

        [Test]
        public void TzdbVersion()
        {
            var source = TzdbDateTimeZoneSource.Default;
            StringAssert.StartsWith("201", source.TzdbVersion);
        }

        [Test]
        public void FixedDateTimeZoneName()
        {
            var zulu = DateTimeZoneProviders.Tzdb["Etc/Zulu"];
            Assert.AreEqual("UTC", zulu.GetZoneInterval(NodaConstants.UnixEpoch).Name);
        }

        [Test]
        public void VersionId()
        {
            var source = TzdbDateTimeZoneSource.Default;
            StringAssert.StartsWith("TZDB: " + source.TzdbVersion, source.VersionId);
        }

        [Test]
        public void ValidateDefault() => TzdbDateTimeZoneSource.Default.Validate();

        // By retrieving this once, we can massively speed up GuessZoneIdByTransitionsUncached. We don't need to
        // reload the time zones for each test, and CachedDateTimeZone will speed things up after that too.
        private static readonly List<DateTimeZone> TzdbDefaultZonesForIdGuessZoneIdByTransitionsUncached =
            TzdbDateTimeZoneSource.Default.CanonicalIdMap.Values.Select(TzdbDateTimeZoneSource.Default.ForId).ToList();

        // We should be able to use TestCaseSource to call TimeZoneInfo.GetSystemTimeZones directly,
        // but that appears to fail under Mono.
        [Test]
        [TestCaseSource(nameof(SystemTimeZones))]
        public void GuessZoneIdByTransitionsUncached(NamedWrapper<TimeZoneInfo> bclZoneWrapper)
        {
            var bclZone = bclZoneWrapper.Value;
            // As of May 4th 2018, the Windows time zone database on Jon's laptop has caught up with this,
            // but the one on AppVeyor hasn't. Keep skipping it for now.
            if (bclZone.Id == "Namibia Standard Time")
            {
                return;
            }

            // As of May 4th 2018, the Windows time zone database hasn't caught up
            // with the North Korea change in TZDB 2018e.
            if (bclZone.Id == "North Korea Standard Time")
            {
                return;
            }

            string? id = TzdbDateTimeZoneSource.Default.GuessZoneIdByTransitionsUncached(bclZone,
                TzdbDefaultZonesForIdGuessZoneIdByTransitionsUncached);

            // Unmappable zones may not be mapped, or may be mapped to something reasonably accurate.
            // We don't mind either way.
            if (!TzdbDateTimeZoneSource.Default.WindowsMapping.PrimaryMapping.ContainsKey(bclZone.Id))
            {
                return;
            }

            Assert.IsNotNull(id, $"Unable to guess time zone for {bclZone.Id}");
            var tzdbZone = TzdbDateTimeZoneSource.Default.ForId(id!);

            var thisYear = SystemClock.Instance.GetCurrentInstant().InUtc().Year;
            LocalDate? lastIncorrectDate = null;
            Offset? lastIncorrectBclOffset = null;
            Offset? lastIncorrectTzdbOffset = null;

            int total = 0;
            int correct = 0;
            // From the start of this year to the end of next year, we should have an 80% hit rate or better.
            // That's stronger than the 70% we limit to in the code, because if it starts going between 70% and 80% we
            // should have another look at the algorithm. (And this is dealing with 80% of days, not 80% of transitions,
            // so it's not quite equivalent anyway.)
            for (var date = new LocalDate(thisYear, 1, 1); date.Year < thisYear + 2; date = date.PlusDays(1))
            {
                Instant startOfUtcDay = date.AtMidnight().InUtc().ToInstant();
                Offset tzdbOffset = tzdbZone.GetUtcOffset(startOfUtcDay);
                Offset bclOffset = Offset.FromTimeSpan(bclZone.GetUtcOffset(startOfUtcDay.ToDateTimeOffset()));
                if (tzdbOffset == bclOffset)
                {
                    correct++;
                }
                else
                {
                    // Useful for debugging (by having somewhere to put a breakpoint) as well as for the message.
                    lastIncorrectDate = date;
                    lastIncorrectBclOffset = bclOffset;
                    lastIncorrectTzdbOffset = tzdbOffset;
                }
                total++;
            }
            Assert.That(correct * 100.0 / total, Is.GreaterThanOrEqualTo(75.0),
                "Last incorrect date for {0} vs {1}: {2} (BCL: {3}; TZDB: {4})",
                bclZone.Id,
                id,
                lastIncorrectDate, lastIncorrectBclOffset, lastIncorrectTzdbOffset);
        }

        [Test]
        public void LocalZoneIsNull()
        {
            // Use the existing system time zones, but just make TimeZoneInfo.Local return null.
            using (TimeZoneInfoReplacer.Replace(null, TimeZoneInfo.GetSystemTimeZones().ToArray()))
            {
                // If we have no system time zone, we have no ID to map it to.
                Assert.Null(TzdbDateTimeZoneSource.Default.GetSystemDefaultId());
            }
        }

        [Test]
        [TestCase("Pacific Standard Time", 0, "America/Los_Angeles", Description = "Windows ID")]
        [TestCase("America/Los_Angeles", 0, "America/Los_Angeles", Description = "TZDB ID")]
        // Both Pacific/Honolulu and Etc/GMT+10 match with the same score; we use Etc/GMT+10
        // as it comes first lexically.
        [TestCase("Lilo and Stitch", -10, "Etc/GMT+10", Description = "Guess by transitions")]
        public void MapTimeZoneInfoId(string timeZoneInfoId, int standardUtc, string expectedId)
        {
            var zoneInfo = TimeZoneInfo.CreateCustomTimeZone(timeZoneInfoId, TimeSpan.FromHours(standardUtc),
                "Ignored display name", "Standard name for " + timeZoneInfoId);
            var mappedId = TzdbDateTimeZoneSource.Default.MapTimeZoneInfoId(zoneInfo);
            Assert.AreEqual(expectedId, mappedId);

            // Do it again, and expect to get the same result again.
            // In the case of the "Lilo and Stitch" zone, this means hitting the cache rather than guessing
            // via transitions again.
            Assert.AreEqual(mappedId, TzdbDateTimeZoneSource.Default.MapTimeZoneInfoId(zoneInfo));
        }

        [Test]
        public void CanonicalIdMapValueIsNotAKey()
        {
            var builder = CreateSampleBuilder();
            builder.tzdbIdMap!["zone3"] = "missing-zone";
            AssertInvalid(builder);
        }

        [Test]
        public void CanonicalIdMapValueIsNotCanonical()
        {
            var builder = CreateSampleBuilder();
            builder.tzdbIdMap!["zone4"] = "zone3"; // zone3 is an alias for zone1
            AssertInvalid(builder);
        }

        [Test]
        public void WindowsMappingWithoutPrimaryTerritory()
        {
            var builder = CreateSampleBuilder();
            builder.windowsMapping = new WindowsZones("cldr-version", "tzdb-version", "windows-version",
                new[] { new MapZone("windows-id", "nonprimary", new[] { "zone1", "zone2" }) });
            AssertInvalid(builder);
        }

        [Test]
        public void WindowsMappingUsesMissingId()
        {
            var builder = CreateSampleBuilder();
            builder.windowsMapping = new WindowsZones("cldr-version", "tzdb-version", "windows-version",
                new[] { new MapZone("windows-id", MapZone.PrimaryTerritory, new[] { "zone4" }) });
            AssertInvalid(builder);
        }

        [Test]
        public void WindowsMapping_PrimaryTerritoryIgnoredForDuplicateDetection()
        {
            var builder = CreateSampleBuilder();
            builder.windowsMapping = new WindowsZones("cldr-version", "tzdb-version", "windows-version",
                new[]
                {
                    new MapZone("windows-id", MapZone.PrimaryTerritory, new[] { "zone1" }),
                    new MapZone("windows-id", "UK", new[] { "zone1" }),
                });
            var streamData = new TzdbStreamData(builder);
            var source = new TzdbDateTimeZoneSource(streamData);
            source.Validate();
        }

        [Test]
        public void WindowsMappingContainsDuplicateTzdbIds_SameTerritory()
        {
            var builder = CreateSampleBuilder();
            builder.windowsMapping = new WindowsZones("cldr-version", "tzdb-version", "windows-version",
                new[]
                {
                    new MapZone("windows-id", MapZone.PrimaryTerritory, new[] { "zone1" }),
                    new MapZone("windows-id", "UK", new[] { "zone1" }),
                    new MapZone("windows-id", "CA", new[] { "zone2", "zone2" })
                });
            AssertInvalid(builder);
        }

        [Test]
        public void WindowsMappingContainsDuplicateTzdbIds_SameWindowsIdDifferentTerritory()
        {
            var builder = CreateSampleBuilder();
            builder.windowsMapping = new WindowsZones("cldr-version", "tzdb-version", "windows-version",
                new[]
                {
                    new MapZone("windows-id", MapZone.PrimaryTerritory, new[] { "zone1" }),
                    new MapZone("windows-id", "UK", new[] { "zone1" }),
                    new MapZone("windows-id", "CA", new[] { "zone1" })
                });
            AssertInvalid(builder);
        }

        [Test]
        public void WindowsMappingContainsDuplicateTzdbIds_DifferentTerritories()
        {
            var builder = CreateSampleBuilder();
            builder.windowsMapping = new WindowsZones("cldr-version", "tzdb-version", "windows-version",
                new[]
                {
                    new MapZone("windows-id1", MapZone.PrimaryTerritory, new[] { "zone1" }),
                    new MapZone("windows-id1", "CA", new[] { "zone1" }),
                    new MapZone("windows-id2", MapZone.PrimaryTerritory, new[] { "zone1" }),
                    new MapZone("windows-id2", "UK", new[] { "zone1" }),
                });
            AssertInvalid(builder);
        }

        [Test]
        public void ZoneLocationsContainsMissingId()
        {
            var builder = CreateSampleBuilder();
            builder.zoneLocations = new List<TzdbZoneLocation>
            {
                new TzdbZoneLocation(0, 0, "country", "xx", "zone4", "comment")
            }.AsReadOnly();
            AssertInvalid(builder);
        }

        [Test]
        public void Zone1970LocationsContainsMissingId()
        {
            var builder = CreateSampleBuilder();
            var country = new TzdbZone1970Location.Country("country", "xx");
            builder.zone1970Locations = new List<TzdbZone1970Location>
            {
                new TzdbZone1970Location(0, 0, new[] { country }, "zone4", "comment")
            }.AsReadOnly();
            AssertInvalid(builder);
        }

        /// <summary>
        /// Creates a sample builder with two canonical zones (zone1 and zone2), and a link from zone3 to zone1.
        /// There's a single Windows mapping, but no zone locations.
        /// </summary>
        private static TzdbStreamData.Builder CreateSampleBuilder()
        {
            var builder = new TzdbStreamData.Builder
            {
                stringPool = new List<string>(),
                tzdbIdMap = new Dictionary<string, string> { { "zone3", "zone1" } },
                tzdbVersion = "tzdb-version",
                windowsMapping = new WindowsZones("cldr-version", "tzdb-version", "windows-version",
                    new[] { new MapZone("windows-id", MapZone.PrimaryTerritory, new[] { "zone1" }) })                
            };
            PopulateZoneFields(builder, "zone1", "zone2");
            return builder;
        }

        private static void PopulateZoneFields(TzdbStreamData.Builder builder, params string[] zoneIds)
        {
            foreach (var id in zoneIds)
            {
                builder.zoneFields[id] = new TzdbStreamField(TzdbStreamFieldId.TimeZone, new byte[0]);
            }
        }

        private static void AssertInvalid(TzdbStreamData.Builder builder)
        {
            var streamData = new TzdbStreamData(builder);
            var source = new TzdbDateTimeZoneSource(streamData);
            Assert.Throws<InvalidNodaDataException>(source.Validate);
        }
    }
}
