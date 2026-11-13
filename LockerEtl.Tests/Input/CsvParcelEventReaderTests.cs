using System;
using LockerEtl.Input;
using LockerEtl.Models.Enums;

namespace LockerEtl.Tests.Input
{
    public class CsvParcelEventReaderTests
    {
        private const string Header = "\"event_id\";\"timestamp\";\"parcel_id\";\"recipient_id\";\"recipient_name\";\"recipient_phone\";\"recipient_email\";\"recipient_postal_code\";\"retailer_id\";\"target_locker_id\";\"size\"";

        private static CsvParcelEventReader Reader => new();

        [Fact]
        public void Read_ValidRow_MapsAllFields()
        {
            var csv = Header + "\n\"EVT-1\";\"2026-10-27T06:00:00Z\";\"P-1\";\"R-1\";\"Mette\";\"+4520123456\";\"m@example.dk\";\"2200\";\"COOLSHOP\";\"LOCKER-CPH-01\";\"S\"";

            var result = Reader.Read(new StringReader(csv));

            var e = Assert.Single(result.Events);
            Assert.Equal("P-1", e.ParcelId);
            Assert.Equal(ParcelSize.S, e.Size);
            Assert.Equal("LOCKER-CPH-01", e.TargetLockerId);
            Assert.Equal(new DateTimeOffset(2026, 10, 27, 6, 0, 0, TimeSpan.Zero), e.Timestamp);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Read_EmptyFields_BecomeNull()
        {
            var csv = Header + "\n\"EVT-1\";\"2026-10-27T06:00:00Z\";\"P-1\";\"R-1\";\"Sofie\";;\"s@example.dk\";\"8000\";\"MATAS\";;";

            var e = Assert.Single(Reader.Read(new StringReader(csv)).Events);

            Assert.Null(e.RecipientPhone);
            Assert.Null(e.TargetLockerId);
            Assert.Null(e.Size);
        }

        [Theory]
        [InlineData("\"EVT-1\";\"not-a-date\";\"P-1\";\"R-1\";;;;;;;\"S\"")]
        [InlineData("\"EVT-1\";\"2026-10-27T06:00:00Z\";\"P-1\";\"R-1\";;;;;;;\"XL\"")]
        [InlineData("\"EVT-1\";\"2026-10-27T06:00:00Z\";\"P-1\"")]
        public void Read_BadRow_IsReportedAndDoesNotStopBatch(string badRow)
        {
            var good = "\"EVT-2\";\"2026-10-27T06:00:00Z\";\"P-2\";\"R-2\";;;;;;;\"M\"";
            var csv = $"{Header}\n{badRow}\n{good}";

            var result = Reader.Read(new StringReader(csv));

            Assert.Single(result.Errors);
            Assert.Equal("P-2", Assert.Single(result.Events).ParcelId);
        }
    }
}