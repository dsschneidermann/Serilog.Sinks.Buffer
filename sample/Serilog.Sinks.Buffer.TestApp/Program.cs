// *********************************************************************
// Copyright (c). All rights reserved.
// See license file in root dir for details.
// *********************************************************************

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace Serilog.Sinks.Buffer.TestApp
{
    internal class Program
    {
        private static int FailCounter = 9;
        private static int TaskCounter;

        private static void Main(string[] args)
        {
            var consoleOutputFormat =
                $"[{{Timestamp:HH:mm:ss}} {{Level:u3}}] {{{LogBuffer.Key},-5}} {{Message:lj}}{{NewLine}}";

            void LoggerConfig(LoggerConfiguration lc) =>
                lc.WriteTo.Console(
                    formatProvider: new CultureInfo("da-DK"), theme: AnsiConsoleTheme.Literate,
                    outputTemplate: consoleOutputFormat
                );

            Log.Logger = new LoggerConfiguration().Enrich.WithBufferContext()
                .UseBufferedLogger(("Serilog.Sinks.Buffer.TestApp", LogEventLevel.Information, LogEventLevel.Debug))
                //Log.Logger = BufferedSinkBuilder.Default
                //    .With(LoggerConfiguration: new LoggerConfiguration().Enrich.WithBuffering(), BufferCapacity: 6)
                .WriteTo(LoggerConfig)
                .CreateLogger()
                .ForContext<Program>();

            Log.Information("Simple task:");

            Task.Run(async () => { await MainAsyncSimple(); }).GetAwaiter().GetResult();

            Log.Information("Scope collapse:");

            Task.Run(async () => { await Task.WhenAll(MainAsyncScoped(2, 0), MainAsyncScoped(2, 0)); })
                .GetAwaiter()
                .GetResult();
        }

        private static async Task MainAsyncScoped(int numTasksToStart, int parentContext)
        {
            using (LogBuffer.BeginScope(collapseOnTriggered: true))
            {
                var taskContext = Interlocked.Increment(ref TaskCounter);
                var logger = Log.ForContext("ParentContext", parentContext).ForContext("TaskContext", taskContext);

                try
                {
                    await Task.Yield();

                    var failCounter = Interlocked.Decrement(ref FailCounter);
                    var tasks = Enumerable.Range(0, numTasksToStart)
                        .Select(
                            async x => {
                                logger.Debug("{ParentContext} -> {TaskContext}: Task started");

                                if (failCounter == 0)
                                {
                                    throw new Exception("Failed");
                                }

                                if (failCounter < 0)
                                {
                                    return;
                                }

                                await MainAsyncScoped(2, taskContext);
                            }
                        );

                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "{ParentContext} -> {TaskContext}: Exception thrown and caught");
                }
            }
        }

        private static async Task MainAsyncSimple()
        {
            using (LogBuffer.BeginScope())
            {
                Log.Information("Starting task");
                try
                {
                    Log.Debug("A debug message");
                    throw new Exception("Failed");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception thrown and caught");
                }
            }
        }
    }
}
