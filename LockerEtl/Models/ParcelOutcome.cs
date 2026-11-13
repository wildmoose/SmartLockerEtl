using System;
using LockerEtl.Models.Enums;

namespace LockerEtl.Models
{
    /// <summary>What happened to one parcel in this run.</summary>
    public class ParcelOutcome
    {
        public required string ParcelId { get; init; }
        public required ParcelOutcomeStatus Status { get; init; }
        public string? LockerId { get; init; }
        public string? CompartmentId { get; init; }
        public string? Channel { get; init; }
        public string? Detail { get; init; }
    }
}