// *********************************************************************
// Copyright (c). All rights reserved.
// See license file in root dir for details.
// *********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using Serilog.Core;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Sinks.Buffer.Extensions;

namespace Serilog.Sinks.Buffer.Internal
{
    public sealed class BufferSink : ILogEventSink
    {
        /// <summary>
        ///     Create a buffered sink. <seealso cref="BufferSinkBuilder" />
        /// </summary>
        /// <param name="configs">
        ///     A collection of configs: ("source to match on", minLevelAlways: always emit events with this
        ///     level or above, minLevelOnDetailed: emit events with this level or above when a trigger level event had occurred)
        /// </param>
        /// <param name="bufferSinkConfig">The configuration of the sink.</param>
        public BufferSink(
            IEnumerable<(string SourceToMatchOn, LogEventLevel MinLevelAlways, LogEventLevel MinLevelOnDetailed)>
                configs, BufferSinkConfig bufferSinkConfig)
        {
            Configs = configs.Select(
                    x => new SourceConfig_Internal(Matching.FromSource(x.SourceToMatchOn), x.MinLevelAlways)
                )
                .ToList();
            BufferSinkConfig = bufferSinkConfig;
        }

        /// <summary>
        ///     Create a buffered sink. <seealso cref="BufferSinkBuilder" />
        /// </summary>
        /// <param name="configs">
        ///     A collection of configs: ("source to match on", minLevelAlways: always emit events with this
        ///     level or above, minLevelOnDetailed: emit events with this level or above when a trigger level event had occurred)
        /// </param>
        /// <param name="bufferSinkConfig">The configuration of the sink.</param>
        public BufferSink(IEnumerable<SourceConfig> configs, BufferSinkConfig bufferSinkConfig)
        {
            Configs = configs.Select(
                    x => new SourceConfig_Internal(Matching.FromSource(x.SourceToMatchOn), x.MinLevelAlways)
                )
                .ToList();
            BufferSinkConfig = bufferSinkConfig;
        }

        private BufferSinkConfig BufferSinkConfig { get; }
        private List<SourceConfig_Internal> Configs { get; }

        public void Emit(LogEvent logEvent)
        {
            var matches = Configs.Where(x => x.Match(logEvent)).ToList();
            var writeToSink = !matches.Any() || matches.All(x => x.MinLevelAlways.IsSatisfiedBy(logEvent));

            if (!logEvent.Properties.TryGetValue(LogBuffer.Key, out var logBuffer))
            {
                return;
            }

            var buffer = (logBuffer as LogBuffer.LogBufferValue)?.LogBuffer;
            buffer?.LogEvent(logEvent, writeToSink, BufferSinkConfig);
        }

        private struct SourceConfig_Internal
        {
            public Func<LogEvent, bool> Match { get; }
            public LogEventLevel MinLevelAlways { get; }

            public SourceConfig_Internal(Func<LogEvent, bool> Match, LogEventLevel MinLevelAlways)
            {
                this.Match = Match;
                this.MinLevelAlways = MinLevelAlways;
            }
        }
    }
}
