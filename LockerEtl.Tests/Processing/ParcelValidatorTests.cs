using System;
using LockerEtl.Models;
using LockerEtl.Models.Enums;
using LockerEtl.Processing;

namespace LockerEtl.Tests.Processing
{
    public class ParcelValidatorTests
    {
        private static ParcelState Parcel(ParcelSize? size = ParcelSize.S, string? locker = "L-1",
            string? phone = "+4520123456", string? email = "a@example.dk") => new()
            {
                ParcelId = "P-1",
                RecipientId = "R-1",
                Size = size,
                TargetLockerId = locker,
                RecipientPhone = phone,
                RecipientEmail = email,
                LastUpdated = DateTimeOffset.UtcNow,
                EventCount = 1
            };

        [Fact]
        public void Validate_CompleteParcel_IsValid()
        {
            var result = new ParcelValidator().Validate([Parcel()]);

            Assert.Single(result.Valid);
            Assert.Empty(result.Rejected);
        }

        [Fact]
        public void Validate_EmailOnly_IsValid()
        {
            Assert.Single(new ParcelValidator().Validate([Parcel(phone: null)]).Valid);
        }

        [Fact]
        public void Validate_MultipleProblems_AreAllReported()
        {
            var rejected = Assert.Single(new ParcelValidator().Validate([Parcel(size: null, locker: null)]).Rejected);

            Assert.Contains("size", rejected.Reason);
            Assert.Contains("locker", rejected.Reason);
        }

        [Fact]
        public void Validate_NoContactChannel_IsRejected()
        {
            Assert.Single(new ParcelValidator().Validate([Parcel(phone: null, email: null)]).Rejected);
        }
    }
}