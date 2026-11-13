using System;

namespace LockerEtl.Models
{
    /// <summary>A parcel that could not be processed, with a human-readable reason.</summary>
    public class RejectedParcel
    {
        public required string ParcelId { get; init; }
        public required string Reason { get; init; }
    }
}