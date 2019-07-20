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
        private static int FailCounter = 1501;
        private static int TaskCounter;

        private static void Main(string[] args)
        {
            var consoleOutputFormat =
                $"[{{Level:u5}}] {{{LogBuffer.LogBufferContextPropertyName},-5}} {{Message:lj}}{{NewLine}}";

            void LoggerConfig(LoggerConfiguration lc)
            {
                lc.WriteTo.Console(
                    formatProvider: new CultureInfo("en-US"), theme: AnsiConsoleTheme.Literate,
                    outputTemplate: consoleOutputFormat
                );
            }

            Log.Logger = new LoggerConfiguration()
                .UseLogBuffer(
                    (SourceToMatchOn: "*", MinLevelAlways: LogEventLevel.Information,
                        MinLevelOnDetailed: LogEventLevel.Debug)
                )
                .With( /* more options: log level switch, trigger level, buffer capacity */)
                .WriteTo(LoggerConfig)
                .CreateLogger();

            Task.Run(
                    () => {
                        Console.WriteLine(
                            @"

----------
Simple task that errors and prints debug output:
----------
"
                        );
                        SimpleBufferExample();
                    }
                )
                .GetAwaiter()
                .GetResult();

            Task.Run(
                    async () => {
                        using (LogBuffer.BeginScope())
                        {
                            Console.WriteLine(
                                @"

----------
Collapsing scopes that print debug output from parents:
----------
"
                            );

                            Log.Debug("Detailed trace of the task spawns (in order of occurrence):");
                            await TasksWithScopedBuffer(2, 0);

                            Log.Information("Total tasks started and ended: {TaskCounter}", TaskCounter);

                            Log.Debug(" --> The parent scope does not *stay* triggered, so this is never printed");
                        }
                    }
                )
                .GetAwaiter()
                .GetResult();
        }

        private static void SimpleBufferExample()
        {
            Log.Debug("A debug message <-- Note how this is printed only after an error");
            Log.Information("Starting task");

            try
            {
                throw new Exception("Failed");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception thrown and caught");
            }

            Log.Debug("--> And debug messages after an error are also printed");

            /* The output is:
[Infor] #1    Starting task
[Error] #1    Exception thrown and caught
[Debug] #1    A debug message <-- Note how this is printed only after an error
[Debug] #1    --> And debug messages after an error are also printed
             */
        }

        private static async Task TasksWithScopedBuffer(int numTasksToStart, int parentTaskId)
        {
            using (LogBuffer.BeginScope(collapseOnTriggered: true))
            {
                var taskId = Interlocked.Increment(ref TaskCounter);
                try
                {
                    Log.Debug("Task {ParentTaskId}: Started the task {TaskId}", parentTaskId, taskId);

                    var failCounter = Interlocked.Decrement(ref FailCounter);

                    if (failCounter == 0)
                    {
                        // A single task will throw exception
                        Log.Debug("Task {TaskId} <-- was the one that failed", taskId);
                        throw new Exception("Failed");
                    }

                    if (failCounter < 0)
                    {
                        // Rest of the tasks end silently
                        return;
                    }

                    var tasks = Enumerable.Range(0, numTasksToStart)
                        .Select(
                            async x => {
                                await Task.Yield(); // yield first so we return the tasks before calling to make more 
                                await TasksWithScopedBuffer(2, taskId);
                            }
                        );

                    await Task.WhenAll(tasks);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Task {TaskId}: Exception thrown and caught", taskId);
                }
            }

            /* The output is (something like):
[Error] #5E4  Task 1501: Exception thrown and caught
[Debug] #2    Detailed trace of the task spawns (in order of occurrence):
[Debug] #3    Task 0: Started the task 1
[Debug] #4    Task 1: Started the task 2
[Debug] #6    Task 2: Started the task 4
[Debug] #B    Task 4: Started the task 9
[Debug] #14   Task 9: Started the task 18
[Debug] #1E   Task 18: Started the task 28
[Debug] #34   Task 28: Started the task 49
[Debug] #62   Task 49: Started the task 96
[Debug] #C0   Task 96: Started the task 189
[Debug] #17D  Task 189: Started the task 377
[Debug] #2F5  Task 377: Started the task 753
[Debug] #5E4  Task 753: Started the task 1501
[Debug] #5E4  Task 1501 <-- was the one that failed
[Infor] #2    Total tasks started and ended: 3001
            */
        }
    }
}
