using System;

namespace LockerEtl.Api.Models
{
    public class AllocationResponse
    {
        public required string AllocationId { get; init; }
        public required string LockerId { get; init; }
        public required string CompartmentId { get; init; }
        public string? PinCode { get; init; }
        public DateTimeOffset AllocatedAt { get; init; }
        public DateTimeOffset ExpiresAt { get; init; }
    }
}
