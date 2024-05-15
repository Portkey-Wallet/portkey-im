using IM.Common;
using IM.Silo;
using IM.Silo.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace IM;
public class Program
{
    public async static Task<int> Main(string[] args)
    {
        Log.Logger = LogHelper.CreateLogger(LogEventLevel.Debug);
        
        try
        {
            Log.Information("Starting IM.Silo.");

            await CreateHostBuilder(args).RunConsoleAsync();

            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Host terminated unexpectedly!");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    internal static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseApolloForHostBuilder()
            .ConfigureServices((hostcontext, services) =>
            {
                services.AddApplication<ImOrleansSiloModule>();
            })
            .UseOrleansSnapshot()
            .UseAutofac()
            .UseSerilog();
    
}