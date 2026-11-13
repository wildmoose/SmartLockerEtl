using System;

namespace LockerEtl.Api.Models
{
    public class ErrorResponse
    {
        public string? Code { get; init; }
        public string? Message { get; init; }
    }
}
