// *********************************************************************
// Copyright (c). All rights reserved.
// See license file in root dir for details.
// *********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Buffer.Internal.Extensions;

namespace Serilog.Sinks.Buffer.Internal
{
    internal sealed class BufferSink : ILogEventSink
    {
        /// <summary>
        ///     Create a buffered sink. See <see cref="BufferSinkBuilder" />.
        /// </summary>
        /// <param name="BufferSinkConfig">The configuration of the sink.</param>
        /// <param name="Configs">A collection of configs.</param>
        /// <param name="FallbackConfig">The config to use if none match.</param>
        public BufferSink(
            BufferSinkConfig BufferSinkConfig, List<SourceConfig_Internal> Configs,
            SourceConfig_Internal FallbackConfig)
        {
            this.BufferSinkConfig = BufferSinkConfig;
            this.Configs = Configs;
            this.FallbackConfig = FallbackConfig;
        }

        public BufferSinkConfig BufferSinkConfig { get; }
        public List<SourceConfig_Internal> Configs { get; }
        public SourceConfig_Internal FallbackConfig { get; }

        public void Emit(LogEvent logEvent)
        {
            bool ShouldWriteToSink(LogEvent e, SourceConfig_Internal c)
            {
                return c.MinLevelAlwaysSwitch?.MinimumLevel.IsSatisfiedBy(e) ?? c.MinLevelAlways.IsSatisfiedBy(e);
            }

            var matches = Configs.Where(x => x.Match(logEvent)).ToList();
            var writeToSink = matches.Any() && ShouldWriteToSink(logEvent, matches.Last());

            if (!matches.Any() && FallbackConfig != null)
            {
                writeToSink = ShouldWriteToSink(logEvent, FallbackConfig);
            }

            if (!logEvent.Properties.TryGetValue(LogBuffer.LogBufferContextPropertyName, out var logBufferScopeValue))
            {
                return;
            }

            var logBufferScope = (logBufferScopeValue as LogBuffer.LogBufferScopeValue)?.LogBufferScope;
            var logBuffer = logBufferScope?.LogBuffer;
            logBuffer?.LogEvent(logEvent, writeToSink, BufferSinkConfig);
        }

        internal class SourceConfig_Internal
        {
            public SourceConfig_Internal(
                Func<LogEvent, bool> Match, LogEventLevel MinLevelAlways, LoggingLevelSwitch MinLevelAlwaysSwitch)
            {
                this.Match = Match;
                this.MinLevelAlways = MinLevelAlways;
                this.MinLevelAlwaysSwitch = MinLevelAlwaysSwitch;
            }

            public Func<LogEvent, bool> Match { get; }
            public LogEventLevel MinLevelAlways { get; }
            public LoggingLevelSwitch MinLevelAlwaysSwitch { get; }
        }
    }
}
