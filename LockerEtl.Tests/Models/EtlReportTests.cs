using System;
using LockerEtl.Models;
using LockerEtl.Models.Enums;

namespace LockerEtl.Tests.Models
{
    public class EtlReportTests
    {
        private static EtlReport Report(
            IReadOnlyList<ParcelOutcome>? outcomes = null,
            IReadOnlyList<RejectedParcel>? rejected = null,
            IReadOnlyList<string>? parseErrors = null) => new()
            {
                RowsRead = 1,
                ParseErrors = parseErrors ?? [],
                DuplicatesRemoved = 0,
                ParcelCount = 1,
                Rejected = rejected ?? [],
                Outcomes = outcomes ?? []
            };

        private static ParcelOutcome Outcome(ParcelOutcomeStatus status) => new() { ParcelId = "P-1", Status = status };

        [Theory]
        [InlineData(ParcelOutcomeStatus.Delivered)]
        [InlineData(ParcelOutcomeStatus.AlreadyAllocated)]
        public void NeedsAttention_OnlySuccessfulOutcomes_IsFalse(ParcelOutcomeStatus status)
        {
            Assert.False(Report(outcomes: [Outcome(status)]).NeedsAttention);
        }

        [Theory]
        [InlineData(ParcelOutcomeStatus.NoCapacity)]
        [InlineData(ParcelOutcomeStatus.NotificationFailed)]
        [InlineData(ParcelOutcomeStatus.Error)]
        public void NeedsAttention_ProblemOutcome_IsTrue(ParcelOutcomeStatus status)
        {
            Assert.True(Report(outcomes: [Outcome(status)]).NeedsAttention);
        }

        [Fact]
        public void NeedsAttention_RejectedParcel_IsTrue()
        {
            var rejected = new[] { new RejectedParcel { ParcelId = "P-5", Reason = "Missing parcel size." } };

            Assert.True(Report(rejected: rejected).NeedsAttention);
        }

        [Fact]
        public void NeedsAttention_ParseError_IsTrue()
        {
            Assert.True(Report(parseErrors: ["Line 3: invalid timestamp 'x'."]).NeedsAttention);
        }
    }
}