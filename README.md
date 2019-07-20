# Serilog.Sinks.Buffer ![NuGet Version](https://img.shields.io/nuget/v/Serilog.Sinks.Buffer.svg?logo=nuget)

A serilog sink to store debug information and emit it only if an error occurs. Inspired by [serilog-sinks-buffered](https://github.com/timgaunt/serilog-sinks-buffered).

### Getting started

Install the [Serilog.Sinks.Buffer](https://nuget.org/packages/serilog.sinks.buffer) package from NuGet:

```shell
dotnet add package Serilog.Sinks.Buffer
```

To configure the sink, call `UseLogBuffer()` on a new `LoggerConfiguration`:

```csharp
Log.Logger = new LoggerConfiguration()
    .UseLogBuffer(("*", LogEventLevel.Information, LogEventLevel.Debug))
    .WriteTo(lc => lc.WriteTo.Console())
    .CreateLogger();
```

The above configuration will buffer all `Debug` messages and only write them to the Console *after* an `Error` or `Fatal` event happens. `"*"` is used to match any source. Log messages with `Information` or above are written to the Console right away.

The parameters to the configuration tuples are:
```csharp
/// <param name="SourceToMatchOn">
///     Source to match on as a string, same as the usual Serilog config. Specify the fallback source: "*"
///     (or null or empty) to buffer events that are not matched by any other source config.
/// </param>
/// <param name="MinLevelAlways">
///     Always emit events with this level or above.
/// </param>
/// <param name="MinLevelOnDetailed">
///     Emit events with this level or above when a triggering event occurs.
/// </param>
```

### What does it do?

Observe the following program:
```csharp
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
}
```

Running this with the shown configuration produces the output:

```
[Infor] #1    Starting task
[Error] #1    Exception thrown and caught
[Debug] #1    A debug message <-- Note how this is printed only after an error
[Debug] #1    --> And debug messages after an error are also printed
```

### Multiple source configs

Specify multiple source configs:

```csharp
Log.Logger = new LoggerConfiguration()
    .UseLogBuffer(
        ("*", LogEventLevel.Information, LogEventLevel.Debug  /* My application logs */),
        ("Microsoft", LogEventLevel.Warning, LogEventLevel.Debug  /* ASP.NET debug logs, always warning */)
        ("Microsoft.EntityFrameworkCore", LogEventLevel.Error, LogEventLevel.Error  /* EF errors only */)
    )
    .WriteTo(lc => lc.WriteTo.Console())
    .CreateLogger()
```

The last matching source takes precedence over earlier ones (except the fallback `"*"` which is always used as a last resort).

### Configuration options

Further options can be specified with:
```csharp
Log.Logger = new LoggerConfiguration()
    .UseLogBuffer(("*", LogEventLevel.Information, LogEventLevel.Debug))
    .With(/* more options */)
```

```csharp
/// <param name="FallbackLevelSwitch">
///     The level switch override for events that match the fallback source.
/// </param>
/// <param name="TriggeringLevel">
///     The event level on which to trigger detailed logs, default is Error.
/// </param>
/// <param name="BufferCapacity">
///     The maximum amount of log entries to buffer before dropping the older half, default 100.
/// </param>
```

### Behavior on a triggering event

When a log event happens that has the `TriggeringLevel` or above for a source, it is considered a triggering event and the buffer is flushed to the output sink and memory released for GC.

 Any log events that happen after a triggering event are sent immediately to the output sink, ie. the buffer goes to a 'triggered' state and is not buffering anymore. Usually this is desirable, so we can log debug information about errors even after the exception has been caught and handled.
 
 Use `LogBuffer.BeginScope` to control how events are put in buffers. A triggering event by default only affects the wrapping scope.

### Advanced usage of buffer scopes

See [samples/Serilog.Sinks.Buffer.TestApp/Program.cs](https://github.com/dsschneidermann/Serilog.Sinks.Buffer/blob/master/samples/Serilog.Sinks.Buffer.TestApp/Program.cs) for a sample of using `LogBuffer.BeginScope` to control logging behavior with scopes.

The sample is a task spawner that creates 3001 tasks of which only 1 fails. The output is produced from collapsing scopes from the outside-in when triggered (the parameter `collapseOnTriggered: true`), and it lets us establish a clear history for the single task that failed.

```
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
```
*The number after [Debug] is* `LogBuffer.LogBufferContextPropertyName` *which is the Id of the scope.*

### ASP.NET Core integration

See [samples/Serilog.Sinks.Buffer.WebApp/](https://github.com/dsschneidermann/Serilog.Sinks.Buffer/blob/master/samples/Serilog.Sinks.Buffer.WebApp/) for the full sample of using this with ASP.NET Core.

The configuration used in the sample is similar to the first we saw:
```csharp
Log.Logger = new LoggerConfiguration().UseLogBuffer(
        ("*", LogEventLevel.Information, LogEventLevel.Debug),
        ("Microsoft", LogEventLevel.Warning, LogEventLevel.Debug /* ASP.NET Core */)
    )
    .With( /* more options: log level switch, trigger level, buffer capacity */)
    .WriteTo(LoggerConfig)
    .CreateLogger()
    .ForContext<Program>();
```

The sample shows how to add `LogBuffer.BeginScope` and exception handling to the ASP.NET Core pipeline. Only the top-most line is absolutely needed, but because ASP.NET Core developer/exception handlers swallow the exception details and we want to log that ourselves, we show it used both before and after:
```csharp
// ADD BEFORE DeveloperExceptionPage and ExceptionHandler (important)
app.UseLogBufferScope(); //      -> see IApplicationBuilderExtensions.cs

if (env.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
}

// ALSO ADD AFTER if we want to catch and print the exceptions too!
app.UseLogBufferScope();
//... all your middleware here
```

And the implementation of `UseLogBufferScope`:
```csharp
/// <summary>
///     Use a LogBufferScope in the request pipeline for each request. The earlier in the pipeline
///     the middleware is added, the more debug information will be captured.
/// </summary>
public static void UseLogBufferScope(this IApplicationBuilder app)
{
    app.Use(
        async (context, next) => {
            using (LogBuffer.BeginScope())
            {
                try
                {
                    await next.Invoke();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Exception details: {ExceptionMessage}", ex.Message);
                    throw;
                }
            }
        }
    );
}
```

The result of running the sample and navigating to the Index page is that all middleware `Debug` and `Information` is logged for the failing request:
![ASP.NET Core Screenshot](https://github.com/dsschneidermann/Serilog.Sinks.Buffer/raw/master/samples/Serilog.Sinks.Buffer.WebApp.Screenshot.png)

### Log event ordering

The log events that are flushed from a buffer are sent to the output sink only after the triggering event.

Also log events that should always be logged (due to the `MinLevelAlways` for a source) are logged immediately. The result is that the debug log events always end up being sent out of order, ie. after later events and after the triggering event.

If the logs are sent to an online log service such as Application Insights or Sentry.io, the timestamps added by Serilog will be used to sort and display the events in chronological order.
