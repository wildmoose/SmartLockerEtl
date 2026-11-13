using System;

namespace LockerEtl.Models
{
    /// <summary>Result of reading a batch: valid events plus rows that could not be parsed.</summary>
    public class ReadResult
    {
        public required IReadOnlyList<ParcelEvent> Events { get; init; }
        public required IReadOnlyList<string> Errors { get; init; }
    }
}