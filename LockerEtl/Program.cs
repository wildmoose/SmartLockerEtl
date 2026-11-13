using LockerEtl.Api;
using LockerEtl.Configuration;
using LockerEtl.Input;
using LockerEtl.Models;
using LockerEtl.Models.Enums;
using LockerEtl.Processing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LockerEtl
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddOptions<DataOptions>()
                .Bind(builder.Configuration.GetSection(DataOptions.SectionName))
                .Validate(o => !string.IsNullOrWhiteSpace(o.Path), "data:path is required.")
                .ValidateOnStart();

            builder.Services.AddOptions<ApiOptions>()
                .Bind(builder.Configuration.GetSection(ApiOptions.SectionName))
                .Validate(o => o.BaseUrl is not null, "api:baseUrl is required.")
                .ValidateOnStart();

            // Typed HttpClients via IHttpClientFactory: pooled handlers, no socket exhaustion.
            builder.Services.AddHttpClient<ILockerApiClient, LockerApiClient>(ConfigureApi);
            builder.Services.AddHttpClient<INotificationApiClient, NotificationApiClient>(ConfigureApi);

            builder.Services.AddSingleton<IPinGenerator>(_ => new PinGenerator(length: 6));
            builder.Services.AddTransient<CsvParcelEventReader>();
            builder.Services.AddTransient<ParcelEventConsolidator>();
            builder.Services.AddTransient<ParcelValidator>();
            builder.Services.AddTransient<ParcelProcessor>();
            builder.Services.AddTransient<EtlRunner>();

            using var host = builder.Build();

            // Ctrl+C cancels the run gracefully instead of killing it mid-request.
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

            try
            {
                var report = await host.Services.GetRequiredService<EtlRunner>().RunAsync(cts.Token);
                PrintReport(report);

                // Non-zero exit code lets a scheduler see that something needs attention.
                return report.Outcomes.Any(o => o.Status is ParcelOutcomeStatus.Error or ParcelOutcomeStatus.NotificationFailed) ? 1 : 0;
            }
            catch (OptionsValidationException ex)
            {
                Console.Error.WriteLine($"Configuration error: {string.Join(" ", ex.Failures)}");
                return 2;
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine($"Input file not found: {ex.FileName}");
                return 2;
            }
        }

        private static void ConfigureApi(IServiceProvider services, HttpClient client)
        {
            var options = services.GetRequiredService<IOptions<ApiOptions>>().Value;
            client.BaseAddress = options.BaseUrl;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        }

        private static void PrintReport(EtlReport report)
        {
            Console.WriteLine();
            Console.WriteLine($"Rows read: {report.RowsRead}, parse errors: {report.ParseErrors.Count}, " +
                              $"duplicates removed: {report.DuplicatesRemoved}, parcels: {report.ParcelCount}");

            foreach (var error in report.ParseErrors)
                Console.WriteLine($"  PARSE ERROR  {error}");

            foreach (var rejected in report.Rejected)
                Console.WriteLine($"  REJECTED     {rejected.ParcelId,-12} {rejected.Reason}");

            foreach (var o in report.Outcomes)
                Console.WriteLine($"  {o.Status,-18} {o.ParcelId,-12} {o.LockerId}/{o.CompartmentId} {o.Channel} {o.Detail}");
        }
    }
}