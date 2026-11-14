using System;
using System.Net;
using LockerEtl.Api;
using LockerEtl.Api.Models;

namespace LockerEtl.Tests.Api
{
    public class NotificationApiClientTests
    {
        private static NotificationRequest Request() => new()
        {
            RecipientId = "RCP-003",
            ParcelId = "PARCEL-006",
            LockerId = "LOCKER-CPH-02",
            PinCode = "123456",
            Channel = "EMAIL",
            TextBody = "Your parcel is ready."
        };

        [Fact]
        public async Task Send_Created_ReturnsTrue()
        {
            var handler = new StubHttpMessageHandler(HttpStatusCode.Created, "{}");
            var client = new NotificationApiClient(handler.CreateClient());

            var sent = await client.SendAsync(Request(), CancellationToken.None);

            Assert.True(sent);
            Assert.Contains("\"channel\":\"EMAIL\"", handler.RequestBody);
            Assert.Contains("\"recipient_id\":\"RCP-003\"", handler.RequestBody);
        }

        [Fact]
        public async Task Send_UnprocessableEntity_ReturnsFalse()
        {
            var client = new NotificationApiClient(new StubHttpMessageHandler(HttpStatusCode.UnprocessableEntity, """{ "code": "NO_PHONE" }""").CreateClient());

            Assert.False(await client.SendAsync(Request(), CancellationToken.None));
        }

        [Fact]
        public async Task Send_BadRequest_Throws()
        {
            var client = new NotificationApiClient(new StubHttpMessageHandler(HttpStatusCode.BadRequest).CreateClient());

            await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(Request(), CancellationToken.None));
        }
    }
}