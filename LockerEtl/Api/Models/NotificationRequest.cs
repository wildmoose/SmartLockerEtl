using System;

namespace LockerEtl.Api.Models
{
    public class NotificationRequest
    {
        public required string RecipientId { get; init; }
        public required string ParcelId { get; init; }
        public required string LockerId { get; init; }
        public required string PinCode { get; init; }
        public required string Channel { get; init; }
        public string? TextBody { get; init; }
    }
}
