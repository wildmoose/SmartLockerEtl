using System;

namespace LockerEtl.Models
{
    /// <summary>Parcels split into those ready for allocation and those rejected.</summary>
    public class ValidationResult
    {
        public required IReadOnlyList<ParcelState> Valid { get; init; }
        public required IReadOnlyList<RejectedParcel> Rejected { get; init; }
    }
}