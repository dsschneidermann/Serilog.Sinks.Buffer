// *********************************************************************
// Copyright (c). All rights reserved.
// See license file in root dir for details.
// *********************************************************************

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Buffer.Extensions;
using Serilog.Sinks.Buffer.Internal;

namespace Serilog.Sinks.Buffer
{
    public class LogBuffer
    {
        public const string Key = "LogBufferContext";

        private static readonly object GlobalLock = new object();

        internal readonly string Id = ObjectIdGen.GetNext();

        private BufferSinkConfig _capturedConfig;

        private ConcurrentBag<LogEvent> Bag = new ConcurrentBag<LogEvent>();

        public bool IsTriggered { get; private set; }

        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField")]
        public void LogEvent(LogEvent logEvent, bool writeToSink, BufferSinkConfig config)
        {
            // Capture the BufferSinkConfig, we will use this for allowing to call TriggerFlush
            // from outside without having knowledge about which InnerSink should be used.
            _capturedConfig = config;
            var shouldTrigger = config.TriggerEventLevel.IsSatisfiedBy(logEvent);

            if (IsTriggered || writeToSink)
            {
                config.InnerSink.Emit(logEvent);
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
        public void TriggerFlush()
        {
            IsTriggered = true;

            if (Bag.Count <= 0)
            {
                return;
            }

            // If there is no captured config, no log events were given to the LogBuffer.
            var config = _capturedConfig;

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

            lock (GlobalLock)
            {
                foreach (var entry in logEvents ?? Enumerable.Empty<LogEvent>())
                {
                    config?.InnerSink.Emit(entry);
                }
            }
        }

        internal class LogBufferEnricher : ILogEventEnricher
        {
            /// <inheritdoc />
            public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
            {
                var prop = new LogEventProperty(Key, new LogBufferValue {LogBuffer = EnsureCreated()});
                logEvent.AddOrUpdateProperty(prop);
            }
        }

        internal class LogBufferValue : LogEventPropertyValue
        {
            public LogBuffer LogBuffer { get; set; }

            /// <inheritdoc />
            public override void Render(TextWriter output, string format = null, IFormatProvider formatProvider = null)
            {
                output.Write(LogBuffer.Id);
            }
        }

        public static BufferScope BeginScope(bool collapseOnTriggered = false) => new BufferScope(collapseOnTriggered);

        internal static LogBuffer EnsureCreated()
        {
            return AsyncLocalLogBuffer.LogBufferObject.Value ??
                (AsyncLocalLogBuffer.LogBufferObject.Value = new LogBuffer());
        }

        private static class ObjectIdGen
        {
            private static int objIdCounter;

            public static string GetNext()
            {
                return $"#{Interlocked.Increment(ref objIdCounter):X}";
            }
        }
    }
}
