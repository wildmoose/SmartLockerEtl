using System;

namespace LockerEtl.Configuration
{
    /// <summary>Binds the "api" section of appsettings.json.</summary>
    public class ApiOptions
    {
        public const string SectionName = "api";
        public required Uri BaseUrl { get; init; }
        public int TimeoutSeconds { get; init; } = 30;
    }
}
