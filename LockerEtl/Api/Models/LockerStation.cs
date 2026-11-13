using System;

namespace LockerEtl.Api.Models
{
    public class LockerStation
    {
        public required string LockerId { get; init; }
        public string? Name { get; init; }
        public string? Address { get; init; }
        public int TotalCompartments { get; init; }
    }
}
