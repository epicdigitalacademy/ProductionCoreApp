using ProductionCoreApp.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace ProductionCoreApp
{

    public class Program
    {
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

