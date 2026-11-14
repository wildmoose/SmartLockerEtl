using System;
using LockerEtl.Models;
using LockerEtl.Models.Enums;

namespace LockerEtl.Tests.Models
{
    public class ParcelEventTests
    {
        private static ParcelEvent Event(string eventId = "EVT-1", string? phone = "+4550112233",
            string? email = "jonas.berg@example.dk", ParcelSize? size = ParcelSize.L) => new()
            {
                EventId = eventId,
                Timestamp = new DateTimeOffset(2026, 10, 27, 17, 30, 0, TimeSpan.Zero),
                ParcelId = "PARCEL-008",
                RecipientId = "RCP-004",
                RecipientName = "Jonas Berg",
                RecipientPhone = phone,
                RecipientEmail = email,
                RecipientPostalCode = "8000",
                RetailerId = "COOLSHOP",
                TargetLockerId = "LOCKER-AAR-01",
                Size = size
            };

        [Fact]
        public void Equals_SameContentDifferentEventId_IsEqual()
        {
            // Mirrors EVT-1047 / EVT-1048 in the real batch.
            var a = Event("EVT-1047");
            var b = Event("EVT-1048");

            Assert.True(a.Equals(b));
            Assert.True(a == b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Fact]
        public void Equals_EmailDiffersOnlyInCase_IsEqual()
        {
            var a = Event(email: "jonas.berg@example.dk");
            var b = Event(email: "Jonas.Berg@Example.DK");

            Assert.Equal(a, b);
            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Theory]
        [InlineData("+4550998877", ParcelSize.L)]
        [InlineData("+4550112233", ParcelSize.M)]
        [InlineData(null, ParcelSize.L)]
        public void Equals_DifferentContent_IsNotEqual(string? phone, ParcelSize size)
        {
            Assert.NotEqual(Event(), Event(phone: phone, size: size));
        }

        [Fact]
        public void Equals_Null_IsFalse()
        {
            Assert.False(Event().Equals(null));
            Assert.False(Event() == null);
        }

        [Fact]
        public void Equals_SubclassWithSameContent_IsNotEqual()
        {
            var plain = Event();
            var derived = new DerivedEvent
            {
                EventId = plain.EventId,
                Timestamp = plain.Timestamp,
                ParcelId = plain.ParcelId,
                RecipientId = plain.RecipientId,
                RecipientName = plain.RecipientName,
                RecipientPhone = plain.RecipientPhone,
                RecipientEmail = plain.RecipientEmail,
                RecipientPostalCode = plain.RecipientPostalCode,
                RetailerId = plain.RetailerId,
                TargetLockerId = plain.TargetLockerId,
                Size = plain.Size
            };

            Assert.False(plain.Equals(derived));
            Assert.False(derived.Equals(plain));
        }

        private class DerivedEvent : ParcelEvent
        {
        }

        [Fact]
        public void Distinct_RemovesContentDuplicates()
        {
            var events = new[] { Event("EVT-1047"), Event("EVT-1048"), Event("EVT-1049", size: ParcelSize.S) };

            Assert.Equal(2, events.Distinct().Count());
        }
    }
}