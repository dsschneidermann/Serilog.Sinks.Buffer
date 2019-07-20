// *********************************************************************
// Copyright (c). All rights reserved.
// See license file in root dir for details.
// *********************************************************************

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Buffer.Internal;
using Serilog.Sinks.Buffer.Internal.Extensions;

namespace Serilog.Sinks.Buffer
{
    public class LogBuffer
    {
        /// <summary>
        ///     Serilog LogBufferContext property name with the Id of the buffers LogBufferScope.
        /// </summary>
        public const string LogBufferContextPropertyName = "LogBufferContext";

        public LogBuffer(LogBufferScope logBufferScope)
        {
            LogBufferScope = logBufferScope;
        }

        private LogBufferScope LogBufferScope { get; }
        private ConcurrentBag<LogEvent> Bag { get; set; } = new ConcurrentBag<LogEvent>();

        /// <summary>
        ///     Capturing the BufferSinkConfig, we will use it for calling TriggerFlush from
        ///     outside without having knowledge about which InnerSink should be given.
        ///     If it never gets set, it's because there were no events to log anyway.
        /// </summary>
        private BufferSinkConfig CapturedConfig { get; set; }

        public bool IsTriggered { get; private set; }

        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
        public void LogEvent(LogEvent logEvent, bool writeToSink, BufferSinkConfig config)
        {
            CapturedConfig = config;
            var shouldTrigger = config.TriggerEventLevel.IsSatisfiedBy(logEvent);

            if (IsTriggered || writeToSink)
            {
                config.InnerSink.Emit(logEvent);
            }

            if (shouldTrigger)
            {
                // Notify the LogBufferScope of the triggering event
                LogBufferScope.Trigger();
            }

            if (!IsTriggered && shouldTrigger)
            {
                TriggerFlush();
            }
            else if (!IsTriggered && !writeToSink)
            {
                Bag.Add(logEvent);

                if (config.BufferCapacity > 0 && Bag.Count > config.BufferCapacity)
                {
                    PruneLogEvents();
                }
            }

            void PruneLogEvents()
            {
                lock (this)
                {
                    var logEvents = Bag.ToArray().OrderBy(x => x.Timestamp);

                    // Create new Bag with half the items
                    Bag = new ConcurrentBag<LogEvent>(logEvents.Take(Math.Max(1, config.BufferCapacity / 2)));
                }
            }
        }

        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
        public void TriggerFlush(bool collapsingScope = false)
        {
            // Log.Information("Trigger Flushing: {LogBufferScopeId}", LogBufferScope.Id);

            // Set IsTriggered if the cause is a triggering event in the current LogBufferScope
            IsTriggered = IsTriggered || !collapsingScope;

            if (Bag.Count == 0)
            {
                return;
            }

            IOrderedEnumerable<LogEvent> logEvents = null;
            lock (this)
            {
                if (Bag.Count > 0)
                {
                    logEvents = Bag.ToArray().OrderBy(x => x.Timestamp);
                    // Release for GC
                    Bag = new ConcurrentBag<LogEvent>();
                }
            }

            foreach (var entry in logEvents ?? Enumerable.Empty<LogEvent>())
            {
                CapturedConfig?.InnerSink.Emit(entry);
            }
        }

        internal class LogBufferContextProperty : ILogEventEnricher
        {
            /// <inheritdoc />
            public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
            {
                var prop = new LogEventProperty(
                    LogBufferContextPropertyName,
                    new LogBufferScopeValue {LogBufferScope = LogBufferScope.EnsureCreated()}
                );
                logEvent.AddOrUpdateProperty(prop);
            }
        }

        internal class LogBufferScopeValue : LogEventPropertyValue
        {
            public LogBufferScope LogBufferScope { get; set; }

            /// <inheritdoc />
            public override void Render(TextWriter output, string format = null, IFormatProvider formatProvider = null)
            {
                output.Write(LogBufferScope.Id);
            }
        }

        /// <summary>
        ///     Begins a disposable scope for a LogBuffer instance.
        /// </summary>
        /// <param name="collapseOnTriggered">
        ///     If a log event triggers the detailed output and collapseOnTriggered is <c>true</c>,
        ///     it will cause the parent LogBugger.BeginScope (if any) to also trigger detailed output. This can be useful if you
        ///     are running tasks in parallel and want to get debug logs from the task orchestrator when any child task fails. If
        ///     <c>false</c> (the default), the parent scope will not be notified.
        /// </param>
        /// <returns>A disposable LogBufferScope.</returns>
        public static LogBufferScope BeginScope(bool collapseOnTriggered = false)
        {
            return new LogBufferScope(collapseOnTriggered).BeginScope();
        }
    }
}
