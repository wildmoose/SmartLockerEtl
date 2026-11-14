using System;
using System.Net;
using LockerEtl.Api;
using LockerEtl.Api.Models;

namespace LockerEtl.Tests.Api
{
    public class LockerApiClientTests
    {
        private static AllocationRequest Request() => new()
        {
            ParcelId = "PARCEL-001",
            SizeRequired = "S",
            RecipientId = "RCP-001",
            PinCode = "123456"
        };

        [Fact]
        public async Task Allocate_Created_ReturnsAllocation()
        {
            var handler = new StubHttpMessageHandler(HttpStatusCode.Created,
                """{ "allocation_id": "A-1", "locker_id": "LOCKER-CPH-01", "compartment_id": "C-01", "pin_code": "123456", "allocated_at": "2026-10-27T06:00:00Z", "expires_at": "2026-10-30T06:00:00Z" }""");
            var client = new LockerApiClient(handler.CreateClient());

            var result = await client.AllocateAsync("LOCKER-CPH-01", Request(), CancellationToken.None);

            Assert.Equal("C-01", result!.CompartmentId);
            Assert.EndsWith("lockers/LOCKER-CPH-01/allocations", handler.Request!.RequestUri!.AbsolutePath);
            Assert.Contains("\"size_required\":\"S\"", handler.RequestBody);
            Assert.Contains("\"pin_code\":\"123456\"", handler.RequestBody);
        }

        [Fact]
        public async Task Allocate_Conflict_ReturnsNull()
        {
            var client = new LockerApiClient(new StubHttpMessageHandler(HttpStatusCode.Conflict, """{ "code": "CAPACITY_FULL" }""").CreateClient());

            var result = await client.AllocateAsync("LOCKER-CPH-01", Request(), CancellationToken.None);

            Assert.Null(result);
        }

        [Fact]
        public async Task Allocate_ServerError_Throws()
        {
            var client = new LockerApiClient(new StubHttpMessageHandler(HttpStatusCode.InternalServerError).CreateClient());

            await Assert.ThrowsAsync<HttpRequestException>(() =>
                client.AllocateAsync("LOCKER-CPH-01", Request(), CancellationToken.None));
        }

        [Fact]
        public async Task GetCompartments_MapsSnakeCaseFields()
        {
            var client = new LockerApiClient(new StubHttpMessageHandler(HttpStatusCode.OK,
                """[ { "compartment_id": "C-01", "size": "S", "current_parcel_id": "PARCEL-001" } ]""").CreateClient());

            var result = await client.GetCompartmentsAsync("LOCKER-CPH-01", CancellationToken.None);

            var compartment = Assert.Single(result);
            Assert.Equal("PARCEL-001", compartment.CurrentParcelId);
        }
    }
}