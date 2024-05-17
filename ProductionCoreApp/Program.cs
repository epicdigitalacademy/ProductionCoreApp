using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using ProductionCoreApp.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Sinks.OpenTelemetry;
using Serilog.Sinks.OpenTelemetry.Exporters;
using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;
using Npgsql;
using Confluent.Kafka;
using Confluent.Kafka.Extensions.Diagnostics;
using Confluent.Kafka.Extensions.OpenTelemetry;
using Oracle.ManagedDataAccess.OpenTelemetry;





namespace ProductionCoreApp
{

    public class Program
    {
        Action<ResourceBuilder> configureResource = resourceBuilder =>
        {
            var serviceName = "OneBankTransfer";
            resourceBuilder.AddService(
                serviceName: "MovieDev",
                serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
                serviceInstanceId: Environment.MachineName);
        };
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

            try
            {
                Log.Information("Starting web application");

                var builder = WebApplication.CreateBuilder(args);
                builder.Services.AddSerilog(); // <-- Add this line


        



                builder.Services.AddOpenTelemetry() // Initialize the SDK
           .ConfigureResource(ResourceBuilder =>
            {
               ResourceBuilder.AddService(
                  serviceName: "OneBankTransfer",
                   serviceInstanceId: Environment.MachineName);
                   })
            .WithTracing(builder => builder
            .AddAspNetCoreInstrumentation(options =>
             {
                 options.EnrichWithHttpRequest = (activity, httpRequest) =>
                  {
                      activity.SetTag("requestProtocol", httpRequest.Protocol);
                      activity.SetTag("CorrelationId", httpRequest.Headers["CorrelationId"]);
                  };
                 options.RecordException = true;
                 options.EnrichWithHttpResponse = (activity, httpResponse) =>
               {
                   activity.SetTag("responseLength", httpResponse.ContentLength);
               };
                 options.EnrichWithException = (activity, exception) =>
                 {
                     activity.SetTag("exceptionType", exception.GetType().ToString());
                     activity.SetTag("stackTrace", exception.StackTrace);
                 };

             })

                  .AddSource(nameof(Program))
                 .AddMongoDBInstrumentation()
                  .AddNpgsql()  // PostGres Opentelemetry Instrumentation
                  .AddConfluentKafkaInstrumentation() // Kafka Opentelemetry Instrumentation

                 .AddSqlClientInstrumentation((options) =>  //
                 {
                     options.SetDbStatementForText = true;
                     options.SetDbStatementForStoredProcedure = true;
                     options.RecordException = true;
                     options.EnableConnectionLevelAttributes = true;
                 })
                 .AddOracleDataProviderInstrumentation(options =>
                 {
                     options.EnableConnectionLevelAttributes = true;
                     options.RecordException = true;
                     options.InstrumentOracleDataReaderRead = true;
                     options.SetDbStatementForText = true;
                 })

                .AddHttpClientInstrumentation((options) =>
                 {
                     options.EnrichWithHttpRequestMessage = (activity, httpRequestMessage) =>
                     {
                         activity.SetTag("requestVersion", httpRequestMessage.Version);
                     };
                     options.EnrichWithHttpResponseMessage = (activity, httpResponseMessage) =>
                     {
                         activity.SetTag("requestVersion", httpResponseMessage.Version);
                     };
                     options.EnrichWithException = (activity, exception) =>
                     {
                         activity.SetTag("stackTrace", exception.StackTrace);
                     };
                 })

                 .AddConsoleExporter()
                 .AddOtlpExporter());

                builder.Services.AddOpenTelemetry()
                 .ConfigureResource(ResourceBuilder =>
                 {
                     ResourceBuilder.AddService(
                         serviceName: "OneBankTransfer",
                         serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
                         serviceInstanceId: Environment.MachineName);
                 })

                 .WithMetrics(builder => builder
                     .SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                    .AddService(serviceName: "OneBankTransfer"))
                    .AddAspNetCoreInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddView(
                     "kafka_consumer_event_duration",
                    new ExplicitBucketHistogramConfiguration
                    {
                        Boundaries = new[]
                        {
                            0,
                            0.005,
                            0.01,
                            // ...
                            10
                        }
                    })
                .AddMeter(
                    "System.Runtime",
                    "Microsoft.AspNetCore.Hosting",
                    "Microsoft.AspNetCore.Server.Kestrel"
                    )

                      .AddConsoleExporter()
                    .AddOtlpExporter());

                builder.Logging.AddOpenTelemetry(options =>
                {
                    options.SetResourceBuilder(
                ResourceBuilder
                    .CreateDefault()
                    .AddService(
                        serviceName: "OneBankTransfer",
                        serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown",
                        serviceInstanceId: Environment.MachineName));
                    options.IncludeFormattedMessage = true;
                    options.IncludeScopes = true;
                    options.ParseStateValues = true;
                    options.AddConsoleExporter();
                    options.AddOtlpExporter();
                });




                // Add services to the container.
                builder.Services.AddDbContext<MovieContext>(options =>
                    options.UseSqlServer(builder.Configuration.GetConnectionString("MovieContext")));

                builder.Services.AddControllers();
                // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();
                builder.Services.AddSerilog(); // <-- Add this line
                var app = builder.Build();

                // Configure the HTTP request pipeline.
                //if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                app.UseHttpsRedirection();
                app.UseAuthorization();


                app.MapControllers();

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();

            }
        }
    }
}

