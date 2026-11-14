using System;
using LockerEtl.Configuration;
using LockerEtl.Input;
using LockerEtl.Models;
using Microsoft.Extensions.Options;

namespace LockerEtl.Processing
{
    /// <summary>Runs the full pipeline: read, consolidate, validate, process.</summary>
    public class EtlRunner
    {
        private readonly CsvParcelEventReader _reader;
        private readonly ParcelEventConsolidator _consolidator;
        private readonly ParcelValidator _validator;
        private readonly ParcelProcessor _processor;
        private readonly DataOptions _data;

        public EtlRunner(
            CsvParcelEventReader reader,
            ParcelEventConsolidator consolidator,
            ParcelValidator validator,
            ParcelProcessor processor,
            IOptions<DataOptions> data)
        {
            _reader = reader;
            _consolidator = consolidator;
            _validator = validator;
            _processor = processor;
            _data = data.Value;
        }

        public async Task<EtlReport> RunAsync(CancellationToken ct)
        {
            ReadResult read;
            using (var file = File.OpenText(_data.ResolvePath(AppContext.BaseDirectory)))
            {
                read = _reader.Read(file);
            }

            var consolidated = _consolidator.Consolidate(read.Events);
            var validated = _validator.Validate(consolidated.Parcels);
            var outcomes = await _processor.ProcessAsync(validated.Valid, ct);

            return new EtlReport
            {
                RowsRead = read.Events.Count,
                ParseErrors = read.Errors,
                DuplicatesRemoved = consolidated.DuplicatesRemoved,
                ParcelCount = consolidated.Parcels.Count,
                Rejected = validated.Rejected,
                Outcomes = outcomes,
                Cancelled = ct.IsCancellationRequested
            };
        }
    }
}