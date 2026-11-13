using System;
using System.Net;
using System.Net.Http.Json;
using LockerEtl.Api.Models;

namespace LockerEtl.Api
{
    public interface ILockerApiClient
    {
        Task<IReadOnlyList<CompartmentStatus>> GetCompartmentsAsync(string lockerId, CancellationToken ct);

        /// <summary>Returns the allocation, or null when the locker has no matching compartment (409).</summary>
        Task<AllocationResponse?> AllocateAsync(string lockerId, AllocationRequest request, CancellationToken ct);
    }

    public class LockerApiClient : ILockerApiClient
    {
        private readonly HttpClient _http;

        public LockerApiClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<IReadOnlyList<CompartmentStatus>> GetCompartmentsAsync(string lockerId, CancellationToken ct)
        {
            var url = $"lockers/{Uri.EscapeDataString(lockerId)}/compartments";
            var result = await _http.GetFromJsonAsync<List<CompartmentStatus>>(url, ApiJson.Options, ct);
            return result ?? [];
        }

        public async Task<AllocationResponse?> AllocateAsync(string lockerId, AllocationRequest request, CancellationToken ct)
        {
            var url = $"lockers/{Uri.EscapeDataString(lockerId)}/allocations";
            using var response = await _http.PostAsJsonAsync(url, request, ApiJson.Options, ct);

            if (response.StatusCode == HttpStatusCode.Conflict)
                return null; // Capacity full or size unavailable: an expected business outcome, not a crash.

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AllocationResponse>(ApiJson.Options, ct);
        }
    }
}