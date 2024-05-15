using System;
using System.Threading.Tasks;
using IM.Common;
using Microsoft.AspNetCore.Builder;
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
            Log.Information("Starting IM.HttpApi.Host.");
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.AddAppSettingsSecretsJson()
                .UseApolloForConfigureHostBuilder()
                .UseAutofac()
                .UseSerilog();
            builder.Services.AddSignalR();
            await builder.AddApplicationAsync<ImHttpApiHostModule>();
            var app = builder.Build();
            //app.MapHub<CAHub>("im");
            await app.InitializeApplicationAsync();
            await app.RunAsync();
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
}
