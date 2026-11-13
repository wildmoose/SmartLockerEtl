using System;
using System.Net;
using System.Net.Http.Json;
using LockerEtl.Api.Models;

namespace LockerEtl.Api
{
    public interface INotificationApiClient
    {
        /// <summary>Returns true when dispatched; false when the channel cannot be delivered (422).</summary>
        Task<bool> SendAsync(NotificationRequest request, CancellationToken ct);
    }

    public class NotificationApiClient : INotificationApiClient
    {
        private readonly HttpClient _http;

        public NotificationApiClient(HttpClient http)
        {
            _http = http;
        }

        public async Task<bool> SendAsync(NotificationRequest request, CancellationToken ct)
        {
            using var response = await _http.PostAsJsonAsync("notifications", request, ApiJson.Options, ct);

            if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
                return false; // e.g. SMS requested but no phone on record.

            response.EnsureSuccessStatusCode();
            return true;
        }
    }
}