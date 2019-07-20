// *********************************************************************
// Copyright (c). All rights reserved.
// See license file in root dir for details.
// *********************************************************************

using System;
using System.Globalization;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using SerilogConstants = Serilog.Core.Constants;

namespace Serilog.Sinks.Buffer.WebApp
{
    public class Program
    {
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args).UseStartup<Startup>().UseSerilog();

        public static void Main(string[] args)
        {
            var consoleOutputFormat =
                $"[{{{SerilogConstants.SourceContextPropertyName},-65}} {{Level:u3}}] {{Message:lj}}{{NewLine}}";

            void LoggerConfig(LoggerConfiguration lc) =>
                lc.WriteTo.Console(
                    formatProvider: new CultureInfo("en-US"), theme: AnsiConsoleTheme.Literate,
                    outputTemplate: consoleOutputFormat
                );

            Log.Logger = new LoggerConfiguration().UseLogBuffer(
                    ("*", LogEventLevel.Information, LogEventLevel.Debug),
                    ("Microsoft", LogEventLevel.Warning, LogEventLevel.Debug /* ASP.NET Core */)
                )
                .With( /* more options: log level switch, trigger level, buffer capacity */)
                .WriteTo(LoggerConfig)
                .CreateLogger()
                .ForContext<Program>();

            try
            {
                Log.Debug("Will only be printed if the main loop ever errors.");
                Log.Information("Starting web host");
                CreateWebHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
