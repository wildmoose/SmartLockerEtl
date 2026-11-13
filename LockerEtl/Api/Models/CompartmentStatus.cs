using System;

namespace LockerEtl.Api.Models
{
    public class CompartmentStatus
    {
        public required string CompartmentId { get; init; }
        public string? Size { get; init; }
        public string? OccupancyStatus { get; init; }
        public string? DoorState { get; init; }
        public string? SensorStatus { get; init; }
        public string? CurrentParcelId { get; init; }
        public DateTimeOffset? AllocatedAt { get; init; }
    }
}
