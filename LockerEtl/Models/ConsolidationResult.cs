using System;

namespace LockerEtl.Models
{
    /// <summary>Output of consolidation: one state per parcel plus how many duplicates were dropped.</summary>
    public class ConsolidationResult
    {
        public required IReadOnlyList<ParcelState> Parcels { get; init; }
        public required int DuplicatesRemoved { get; init; }
    }
}