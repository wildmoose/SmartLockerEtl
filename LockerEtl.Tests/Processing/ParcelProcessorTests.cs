using System;
using LockerEtl.Api;
using LockerEtl.Api.Models;
using LockerEtl.Models;
using LockerEtl.Models.Enums;
using LockerEtl.Processing;
using Microsoft.Extensions.Logging.Abstractions;

namespace LockerEtl.Tests.Processing
{
    public class ParcelProcessorTests
    {
        private class FakeLockers : ILockerApiClient
        {
            public List<CompartmentStatus> Compartments { get; } = [];
            public bool HasCapacity { get; set; } = true;
            public List<AllocationRequest> Allocations { get; } = [];
            public int CompartmentRequests { get; private set; }
            public Exception? AllocateException { get; set; }
            public CancellationTokenSource? CancelWhenAllocating { get; set; }

            public Task<IReadOnlyList<CompartmentStatus>> GetCompartmentsAsync(string lockerId, CancellationToken ct)
            {
                CompartmentRequests++;
                return Task.FromResult<IReadOnlyList<CompartmentStatus>>(Compartments);
            }

            public Task<AllocationResponse?> AllocateAsync(string lockerId, AllocationRequest request, CancellationToken ct)
            {
                CancelWhenAllocating?.Cancel(); // Simulates Ctrl+C while a parcel is in progress.
                if (AllocateException is not null && request.ParcelId == "P-1") throw AllocateException;

                Allocations.Add(request);
                return Task.FromResult(HasCapacity
                    ? new AllocationResponse { AllocationId = "A-1", LockerId = lockerId, CompartmentId = "C01", PinCode = request.PinCode }
                    : null);
            }
        }

        private class FakeNotifications : INotificationApiClient
        {
            public HashSet<string> RejectedChannels { get; } = [];
            public List<NotificationRequest> Sent { get; } = [];

            public Task<bool> SendAsync(NotificationRequest request, CancellationToken ct)
            {
                Sent.Add(request);
                return Task.FromResult(!RejectedChannels.Contains(request.Channel));
            }
        }

        private class FixedPin : IPinGenerator
        {
            public string Generate() => "123456";
        }

        private static ParcelState Parcel(string? phone = "+4520123456", string? email = "a@example.dk", string id = "P-1") => new()
        {
            ParcelId = id,
            RecipientId = "R-1",
            Size = ParcelSize.S,
            TargetLockerId = "L-1",
            RecipientPhone = phone,
            RecipientEmail = email,
            LastUpdated = DateTimeOffset.UtcNow,
            EventCount = 1
        };

        private static ParcelProcessor Processor(FakeLockers lockers, FakeNotifications notifications) =>
            new(lockers, notifications, new FixedPin(), NullLogger<ParcelProcessor>.Instance);

        [Fact]
        public async Task Process_HappyPath_SendsSamePinToBothApis()
        {
            var lockers = new FakeLockers();
            var notifications = new FakeNotifications();

            var outcome = Assert.Single(await Processor(lockers, notifications).ProcessAsync([Parcel()], CancellationToken.None));

            Assert.Equal(ParcelOutcomeStatus.Delivered, outcome.Status);
            Assert.Equal("123456", Assert.Single(lockers.Allocations).PinCode);
            Assert.Equal("123456", Assert.Single(notifications.Sent).PinCode);
        }

        [Fact]
        public async Task Process_AlreadyInCompartment_DoesNotAllocateOrNotify()
        {
            var lockers = new FakeLockers();
            lockers.Compartments.Add(new CompartmentStatus { CompartmentId = "C02", CurrentParcelId = "P-1" });
            var notifications = new FakeNotifications();

            var outcome = Assert.Single(await Processor(lockers, notifications).ProcessAsync([Parcel()], CancellationToken.None));

            Assert.Equal(ParcelOutcomeStatus.AlreadyAllocated, outcome.Status);
            Assert.Empty(lockers.Allocations);
            Assert.Empty(notifications.Sent);
        }

        [Fact]
        public async Task Process_NoPhone_UsesEmail()
        {
            var notifications = new FakeNotifications();

            var outcome = Assert.Single(await Processor(new FakeLockers(), notifications).ProcessAsync([Parcel(phone: null)], CancellationToken.None));

            Assert.Equal("EMAIL", outcome.Channel);
        }

        [Fact]
        public async Task Process_SmsRejected_FallsBackToEmail()
        {
            var notifications = new FakeNotifications();
            notifications.RejectedChannels.Add("SMS");

            var outcome = Assert.Single(await Processor(new FakeLockers(), notifications).ProcessAsync([Parcel()], CancellationToken.None));

            Assert.Equal("EMAIL", outcome.Channel);
            Assert.Equal(2, notifications.Sent.Count);
        }

        [Fact]
        public async Task Process_NoCapacity_IsReportedWithoutNotifying()
        {
            var notifications = new FakeNotifications();

            var outcome = Assert.Single(await Processor(new FakeLockers { HasCapacity = false }, notifications).ProcessAsync([Parcel()], CancellationToken.None));

            Assert.Equal(ParcelOutcomeStatus.NoCapacity, outcome.Status);
            Assert.Empty(notifications.Sent);
        }

        [Fact]
        public async Task Process_AllChannelsRejected_IsNotificationFailed()
        {
            var notifications = new FakeNotifications();
            notifications.RejectedChannels.UnionWith(["SMS", "EMAIL"]);

            var outcome = Assert.Single(await Processor(new FakeLockers(), notifications).ProcessAsync([Parcel()], CancellationToken.None));

            Assert.Equal(ParcelOutcomeStatus.NotificationFailed, outcome.Status);
        }

        [Fact]
        public async Task Process_HttpFailure_IsReportedAsErrorAndBatchContinues()
        {
            var lockers = new FakeLockers { AllocateException = new HttpRequestException("500") };

            var outcomes = await Processor(lockers, new FakeNotifications())
                .ProcessAsync([Parcel(id: "P-1"), Parcel(id: "P-2")], CancellationToken.None);

            Assert.Equal(ParcelOutcomeStatus.Error, outcomes[0].Status);
            Assert.Equal("L-1", outcomes[0].LockerId);
            Assert.Equal(ParcelOutcomeStatus.Delivered, outcomes[1].Status);
        }

        [Fact]
        public async Task Process_HttpTimeout_IsReportedAsError()
        {
            // HttpClient reports a timeout as TaskCanceledException even though nobody pressed Ctrl+C.
            var lockers = new FakeLockers { AllocateException = new TaskCanceledException("timeout") };

            var outcome = Assert.Single(await Processor(lockers, new FakeNotifications()).ProcessAsync([Parcel()], CancellationToken.None));

            Assert.Equal(ParcelOutcomeStatus.Error, outcome.Status);
        }

        [Fact]
        public async Task Process_UnexpectedException_IsNotHidden()
        {
            var lockers = new FakeLockers { AllocateException = new InvalidOperationException("bug") };

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                Processor(lockers, new FakeNotifications()).ProcessAsync([Parcel()], CancellationToken.None));
        }

        [Fact]
        public async Task Process_CancelledBeforeStart_TouchesNothing()
        {
            var lockers = new FakeLockers();
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var outcomes = await Processor(lockers, new FakeNotifications()).ProcessAsync([Parcel()], cts.Token);

            Assert.Empty(outcomes);
            Assert.Empty(lockers.Allocations);
        }

        [Fact]
        public async Task Process_CancelledDuringParcel_FinishesThatParcelAndStops()
        {
            using var cts = new CancellationTokenSource();
            var lockers = new FakeLockers { CancelWhenAllocating = cts };
            var notifications = new FakeNotifications();

            var outcomes = await Processor(lockers, notifications)
                .ProcessAsync([Parcel(id: "P-1"), Parcel(id: "P-2")], cts.Token);

            // P-1 is allocated AND notified; P-2 is never started.
            var outcome = Assert.Single(outcomes);
            Assert.Equal(ParcelOutcomeStatus.Delivered, outcome.Status);
            Assert.Single(lockers.Allocations);
            Assert.Single(notifications.Sent);
        }

        [Fact]
        public async Task Process_SeveralParcelsInSameLocker_ReadsCompartmentsOnce()
        {
            var lockers = new FakeLockers();

            await Processor(lockers, new FakeNotifications())
                .ProcessAsync([Parcel(id: "P-1"), Parcel(id: "P-2"), Parcel(id: "P-3")], CancellationToken.None);

            Assert.Equal(1, lockers.CompartmentRequests);
            Assert.Equal(3, lockers.Allocations.Count);
        }
    }
}