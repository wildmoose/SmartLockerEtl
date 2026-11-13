using System;
using LockerEtl.Models.Enums;

namespace LockerEtl.Models
{
    /// <summary>The current, consolidated state of one parcel after all its events are applied.</summary>
    public class ParcelState
    {
        public required string ParcelId { get; init; }
        public required string RecipientId { get; init; }
        public string? RecipientName { get; init; }
        public string? RecipientPhone { get; init; }
        public string? RecipientEmail { get; init; }
        public string? RecipientPostalCode { get; init; }
        public string? RetailerId { get; init; }
        public string? TargetLockerId { get; init; }
        public ParcelSize? Size { get; init; }

        /// <summary>Timestamp of the newest event that contributed to this state.</summary>
        public required DateTimeOffset LastUpdated { get; init; }

        /// <summary>Number of distinct events behind this state, useful for reporting.</summary>
        public required int EventCount { get; init; }
    }
}