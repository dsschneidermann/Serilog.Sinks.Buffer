// *********************************************************************
// Copyright (c). All rights reserved.
// See license file in root dir for details.
// *********************************************************************

using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.Buffer.Internal
{
    public class BufferSinkConfig
    {
        public BufferSinkConfig(LogEventLevel TriggerEventLevel, ILogEventSink InnerSink, int BufferCapacity)
        {
            this.TriggerEventLevel = TriggerEventLevel;
            this.InnerSink = InnerSink;
            this.BufferCapacity = BufferCapacity;
        }

        /// <summary>
        ///     The event level on which to trigger detailed logs, usually Error.
        /// </summary>
        public LogEventLevel TriggerEventLevel { get; }

        /// <summary>
        ///     The sink to emit to.
        /// </summary>
        public ILogEventSink InnerSink { get; }

        /// <summary>
        ///     The maximum amount of log entries to buffer before dropping the older half.
        /// </summary>
        public int BufferCapacity { get; }
    }
}
