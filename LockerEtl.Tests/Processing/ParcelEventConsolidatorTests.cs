using System;
using LockerEtl.Models;
using LockerEtl.Models.Enums;
using LockerEtl.Processing;

namespace LockerEtl.Tests.Processing
{
    public class ParcelEventConsolidatorTests
    {
        private static ParcelEvent Event(string id, int hour, string parcel = "P-1",
            string? locker = null, ParcelSize? size = null, string? phone = null) => new()
            {
                EventId = id,
                Timestamp = new DateTimeOffset(2026, 10, 27, hour, 0, 0, TimeSpan.Zero),
                ParcelId = parcel,
                RecipientId = "R-1",
                TargetLockerId = locker,
                Size = size,
                RecipientPhone = phone
            };

        [Fact]
        public void Consolidate_ExactDuplicatesWithDifferentEventIds_AreRemoved()
        {
            var events = new[] { Event("EVT-1", 17, locker: "L-1"), Event("EVT-2", 17, locker: "L-1") };

            var result = new ParcelEventConsolidator().Consolidate(events);

            Assert.Equal(1, result.DuplicatesRemoved);
            Assert.Equal(1, Assert.Single(result.Parcels).EventCount);
        }

        [Fact]
        public void Consolidate_UsesTimestampNotFileOrder()
        {
            // The later event appears first in the file.
            var events = new[] { Event("EVT-2", 12, locker: "L-NEW"), Event("EVT-1", 9, locker: "L-OLD") };

            var parcel = Assert.Single(new ParcelEventConsolidator().Consolidate(events).Parcels);

            Assert.Equal("L-NEW", parcel.TargetLockerId);
        }

        [Fact]
        public void Consolidate_EmptyFieldInLaterEvent_KeepsEarlierValue()
        {
            var events = new[] { Event("EVT-1", 9, size: ParcelSize.M), Event("EVT-2", 12, locker: "L-1") };

            var parcel = Assert.Single(new ParcelEventConsolidator().Consolidate(events).Parcels);

            Assert.Equal(ParcelSize.M, parcel.Size);
            Assert.Equal("L-1", parcel.TargetLockerId);
        }

        [Fact]
        public void Consolidate_ChangedValues_LatestWins()
        {
            var events = new[]
            {
                Event("EVT-1", 9, size: ParcelSize.S, phone: "+4550112233"),
                Event("EVT-2", 12, size: ParcelSize.M, phone: "+4550998877")
            };

            var parcel = Assert.Single(new ParcelEventConsolidator().Consolidate(events).Parcels);

            Assert.Equal(ParcelSize.M, parcel.Size);
            Assert.Equal("+4550998877", parcel.RecipientPhone);
        }

        [Fact]
        public void Consolidate_GroupsPerParcel()
        {
            var events = new[] { Event("EVT-1", 9, parcel: "P-1"), Event("EVT-2", 9, parcel: "P-2") };

            Assert.Equal(2, new ParcelEventConsolidator().Consolidate(events).Parcels.Count);
        }
    }
}