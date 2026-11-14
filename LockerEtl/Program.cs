using LockerEtl.Api;
using LockerEtl.Configuration;
using LockerEtl.Input;
using LockerEtl.Models;
using LockerEtl.Processing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace LockerEtl
{
    public class Program
    {
        private const int ExitCodeOk = 0;
        private const int ExitCodeNeedsAttention = 1;
        private const int ExitCodeConfigurationError = 2;
        private const int ExitCodeCancelled = 130; // Convention: 128 + SIGINT (2).

        public static async Task<int> Main(string[] args)
        {
            // Look for appsettings.json next to the executable, not in the current directory,
            // so `dotnet run --project LockerEtl` also works from the repository root.
            var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                Args = args,
                ContentRootPath = AppContext.BaseDirectory
            });

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

            // First Ctrl+C: finish the current parcel, then stop and print the report.
            // Second Ctrl+C: the default behaviour applies and the process ends immediately.
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                if (cts.IsCancellationRequested) return;
                e.Cancel = true;
                cts.Cancel();
                Console.Error.WriteLine("Stopping after the current parcel... press Ctrl+C again to force quit.");
            };

            try
            {
                var report = await host.Services.GetRequiredService<EtlRunner>().RunAsync(cts.Token);
                PrintReport(report);

                if (report.Cancelled) return ExitCodeCancelled;

                // Non-zero exit code lets a scheduler see that something needs attention.
                return report.NeedsAttention ? ExitCodeNeedsAttention : ExitCodeOk;
            }
            catch (OptionsValidationException ex)
            {
                Console.Error.WriteLine($"Configuration error: {string.Join(" ", ex.Failures)}");
                return ExitCodeConfigurationError;
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine($"Input file not found: {ex.FileName}");
                return ExitCodeConfigurationError;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // DirectoryNotFoundException is an IOException, so a bad folder is covered too.
                Console.Error.WriteLine($"Input file could not be read: {ex.Message}");
                return ExitCodeConfigurationError;
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

            if (report.Cancelled)
            {
                var toProcess = report.ParcelCount - report.Rejected.Count;
                Console.WriteLine($"  CANCELLED    {report.Outcomes.Count} of {toProcess} parcels processed. Run again to continue.");
            }
        }
    }
}