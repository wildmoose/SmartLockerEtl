using System;
using LockerEtl.Api;
using LockerEtl.Api.Models;
using LockerEtl.Models;
using LockerEtl.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LockerEtl.Processing
{
    /// <summary>
    /// Allocates a compartment and notifies the recipient for each valid parcel.
    /// A failure on one parcel never stops the batch.
    /// </summary>
    public class ParcelProcessor
    {
        private const string Sms = "SMS";
        private const string Email = "EMAIL";

        private readonly ILockerApiClient _lockers;
        private readonly INotificationApiClient _notifications;
        private readonly IPinGenerator _pins;
        private readonly ILogger<ParcelProcessor> _logger;

        public ParcelProcessor(
            ILockerApiClient lockers,
            INotificationApiClient notifications,
            IPinGenerator pins,
            ILogger<ParcelProcessor> logger)
        {
            _lockers = lockers;
            _notifications = notifications;
            _pins = pins;
            _logger = logger;
        }

        public async Task<IReadOnlyList<ParcelOutcome>> ProcessAsync(IEnumerable<ParcelState> parcels, CancellationToken ct)
        {
            var outcomes = new List<ParcelOutcome>();

            foreach (var parcel in parcels)
            {
                try
                {
                    outcomes.Add(await ProcessOneAsync(parcel, ct));
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
                {
                    // Transport or server errors: log, record, move on to the next parcel.
                    _logger.LogError(ex, "Failed to process {ParcelId}", parcel.ParcelId);
                    outcomes.Add(new ParcelOutcome { ParcelId = parcel.ParcelId, Status = ParcelOutcomeStatus.Error, Detail = ex.Message });
                }
            }

            return outcomes;
        }

        private async Task<ParcelOutcome> ProcessOneAsync(ParcelState parcel, CancellationToken ct)
        {
            var lockerId = parcel.TargetLockerId!; // Guaranteed by ParcelValidator.

            // 1. Idempotency: skip parcels already placed in this locker by an earlier run.
            var compartments = await _lockers.GetCompartmentsAsync(lockerId, ct);
            var existing = compartments.FirstOrDefault(c => c.CurrentParcelId == parcel.ParcelId);
            if (existing is not null)
            {
                return new ParcelOutcome
                {
                    ParcelId = parcel.ParcelId,
                    Status = ParcelOutcomeStatus.AlreadyAllocated,
                    LockerId = lockerId,
                    CompartmentId = existing.CompartmentId
                };
            }

            // 2. Allocate with a PIN we generate; the API accepts anything, so we own its quality.
            var pin = _pins.Generate();
            var allocation = await _lockers.AllocateAsync(lockerId, new AllocationRequest
            {
                ParcelId = parcel.ParcelId,
                SizeRequired = parcel.Size!.Value.ToString(),
                RecipientId = parcel.RecipientId,
                PinCode = pin
            }, ct);

            if (allocation is null)
            {
                return new ParcelOutcome
                {
                    ParcelId = parcel.ParcelId,
                    Status = ParcelOutcomeStatus.NoCapacity,
                    LockerId = lockerId,
                    Detail = $"No available {parcel.Size} compartment."
                };
            }

            // 3. Notify: SMS when we have a phone, otherwise email; fall back to email if SMS is rejected.
            var channel = await NotifyAsync(parcel, allocation, pin, ct);

            return new ParcelOutcome
            {
                ParcelId = parcel.ParcelId,
                Status = channel is null ? ParcelOutcomeStatus.NotificationFailed : ParcelOutcomeStatus.Delivered,
                LockerId = allocation.LockerId,
                CompartmentId = allocation.CompartmentId,
                Channel = channel,
                Detail = channel is null ? "Compartment allocated but recipient could not be notified." : null
            };
        }

        private async Task<string?> NotifyAsync(ParcelState parcel, AllocationResponse allocation, string pin, CancellationToken ct)
        {
            var channels = new List<string>();
            if (parcel.RecipientPhone is not null) channels.Add(Sms);
            if (parcel.RecipientEmail is not null) channels.Add(Email);

            foreach (var channel in channels)
            {
                var sent = await _notifications.SendAsync(new NotificationRequest
                {
                    RecipientId = parcel.RecipientId,
                    ParcelId = parcel.ParcelId,
                    LockerId = allocation.LockerId,
                    PinCode = pin,
                    Channel = channel,
                    TextBody = $"Your parcel {parcel.ParcelId} is ready in locker {allocation.LockerId}, " +
                               $"compartment {allocation.CompartmentId}. PIN: {pin}. Pick up before {allocation.ExpiresAt:yyyy-MM-dd HH:mm} UTC."
                }, ct);

                if (sent) return channel;
                _logger.LogWarning("{Channel} rejected for {ParcelId}, trying next channel", channel, parcel.ParcelId);
            }

            return null;
        }
    }
}