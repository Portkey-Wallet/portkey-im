﻿using System;
using System.Threading.Tasks;
using IM.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace IM.EntityEventHandler
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Log.Logger = LogHelper.CreateLogger(LogEventLevel.Debug);

            try
            {
                Log.Information("Starting CA.EntityEventHandler.");
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
                .ConfigureAppConfiguration(build =>
                {
                    build.AddJsonFile("appsettings.secrets.json", optional: true);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddApplication<ImEntityEventHandlerModule>();
                })
                .UseAutofac()
                .UseSerilog();
    }
}