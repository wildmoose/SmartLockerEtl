using LockerEtl.Models;
using LockerEtl.Models.Enums;
using System;
using System.Globalization;

namespace LockerEtl.Input
{
    /// <summary>Reads the semicolon-separated, quoted CSV produced by the core system.</summary>
    public class CsvParcelEventReader
    {
        private const int ExpectedColumns = 11;

        public ReadResult Read(TextReader reader)
        {
            var events = new List<ParcelEvent>();
            var errors = new List<string>();

            reader.ReadLine(); // Skip header row.
            var lineNumber = 1;

            while (reader.ReadLine() is { } line)
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line)) continue;

                var fields = SplitLine(line);
                if (fields.Count != ExpectedColumns)
                {
                    errors.Add($"Line {lineNumber}: expected {ExpectedColumns} columns, got {fields.Count}.");
                    continue;
                }

                if (!DateTimeOffset.TryParse(fields[1], CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal, out var timestamp))
                {
                    errors.Add($"Line {lineNumber}: invalid timestamp '{fields[1]}'.");
                    continue;
                }

                ParcelSize? size = null;
                if (fields[10] is { } rawSize)
                {
                    if (!Enum.TryParse<ParcelSize>(rawSize, ignoreCase: true, out var parsed))
                    {
                        errors.Add($"Line {lineNumber}: invalid size '{rawSize}'.");
                        continue;
                    }
                    size = parsed;
                }

                if (fields[0] is null || fields[2] is null || fields[3] is null)
                {
                    errors.Add($"Line {lineNumber}: event_id, parcel_id and recipient_id are required.");
                    continue;
                }

                events.Add(new ParcelEvent
                {
                    EventId = fields[0]!,
                    Timestamp = timestamp,
                    ParcelId = fields[2]!,
                    RecipientId = fields[3]!,
                    RecipientName = fields[4],
                    RecipientPhone = fields[5],
                    RecipientEmail = fields[6],
                    RecipientPostalCode = fields[7],
                    RetailerId = fields[8],
                    TargetLockerId = fields[9],
                    Size = size
                });
            }

            return new ReadResult { Events = events, Errors = errors };
        }

        /// <summary>
        /// Splits one line on ';' while respecting double quotes.
        /// Empty fields become null so "missing" is explicit in the model.
        /// </summary>
        internal static List<string?> SplitLine(string line)
        {
            var fields = new List<string?>();
            var current = new System.Text.StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (c == '"')
                {
                    // A doubled quote inside a quoted field is an escaped quote.
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                    else inQuotes = !inQuotes;
                }
                else if (c == ';' && !inQuotes)
                {
                    fields.Add(ToNullable(current.ToString()));
                    current.Clear();
                }
                else current.Append(c);
            }

            fields.Add(ToNullable(current.ToString()));
            return fields;
        }

        private static string? ToNullable(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
