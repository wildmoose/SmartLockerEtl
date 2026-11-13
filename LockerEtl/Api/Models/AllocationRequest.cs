using System;

namespace LockerEtl.Api.Models
{
    public class AllocationRequest
    {
        public required string ParcelId { get; init; }
        public required string SizeRequired { get; init; }
        public required string RecipientId { get; init; }
        public required string PinCode { get; init; }
    }
}
