using System;
using System.Text.Json;

namespace LockerEtl.Api
{
    /// <summary>Shared JSON settings: the APIs use snake_case property names.</summary>
    public static class ApiJson
    {
        public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };
    }
}