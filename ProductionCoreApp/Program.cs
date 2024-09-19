using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using ProductionCoreApp.Models;
using Microsoft.EntityFrameworkCore;
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
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var serviceName = "MovieDev";
            builder.Services.AddOpenTelemetry() // Initialize the SDK   
            .WithTracing(builder => builder
            .AddAspNetCoreInstrumentation(options =>
             {
                 options.EnrichWithHttpRequest = (activity, httpRequest) =>
                  {
                      activity.SetTag("requestProtocol", httpRequest.Protocol);
                  };
                 options.RecordException = true;
                 options.EnrichWithHttpResponse = (activity, httpResponse) =>
               {
                   activity.SetTag("responseLength", httpResponse.ContentLength);
               };

                })
                  
                 .AddMongoDBInstrumentation()
                  .AddNpgsql()  // PostGres Opentelemetry Instrumentation
                  .AddConfluentKafkaInstrumentation() // Kafka Opentelemetry Intrumentation
                 .AddSqlClientInstrumentation((options) =>  //
                 {
                     options.SetDbStatementForText = true;
                     options.SetDbStatementForStoredProcedure = true;
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
                     options.EnrichWithHttpRequestMessage = (activity, httpRequestMessage) => activity.SetTag("requestVersion",
                     httpRequestMessage.Version);
                     options.EnrichWithHttpResponseMessage = (activity, httpResponseMessage) => activity.SetTag("requestVersion",
                     httpResponseMessage.Version);
                     options.EnrichWithException = (activity, exception) => activity.SetTag("stackTrace", exception.StackTrace);
                 })
                   .AddSource(serviceName)
                .SetResourceBuilder(
                    ResourceBuilder.CreateDefault()
                        .AddService(serviceName: serviceName))
                .AddConsoleExporter()
                 .AddOtlpExporter());

            builder.Services.AddOpenTelemetry()
             .WithMetrics(builder => builder
                 .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                .AddService(serviceName: "MovieDev"))
             .AddAspNetCoreInstrumentation()
             .AddRuntimeInstrumentation()
             .AddProcessInstrumentation()
             .AddConsoleExporter() 
             .AddOtlpExporter());


            builder.Logging.AddOpenTelemetry(options =>
            {
                options.SetResourceBuilder(
            ResourceBuilder
                .CreateDefault()
                .AddService(
                    serviceName: "MovieDev",
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
        }
    }
