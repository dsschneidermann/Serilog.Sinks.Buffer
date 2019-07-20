// *********************************************************************
// Copyright (c). All rights reserved.
// See license file in root dir for details.
// *********************************************************************

using System.Linq;
using Serilog.Events;

namespace Serilog.Sinks.Buffer
{
    public static class LoggerConfigurationExtensions
    {
        /// <summary>
        ///     Configure a buffered sink.
        /// </summary>
        /// <param name="lc">The logging configuration.</param>
        /// <param name="configs">
        ///     A collection of configs: ("source to match on", minLevelAlways: always emit events with this
        ///     level or above, minLevelOnDetailed: emit events with this level or above when a triggering event occurs)
        ///     <see cref="SourceConfig" />.
        /// </param>
        public static BufferSinkBuilder UseLogBuffer(
            this LoggerConfiguration lc,
            params (string SourceToMatchOn, LogEventLevel MinLevelAlways, LogEventLevel MinLevelOnDetailed)[] configs)
        {
            return UseLogBuffer(
                lc,
                configs.Select(x => new SourceConfig(x.SourceToMatchOn, x.MinLevelAlways, x.MinLevelOnDetailed))
                    .ToArray()
            );
        }

        /// <summary>
        ///     Configure a buffered sink.
        /// </summary>
        /// <param name="lc">The logging configuration.</param>
        /// <param name="configs">
        ///     A collection of configs: ("source to match on", minLevelAlways: always emit events with this
        ///     level or above, minLevelOnDetailed: emit events with this level or above when a triggering event occurs)
        /// </param>
        public static BufferSinkBuilder UseLogBuffer(this LoggerConfiguration lc, params SourceConfig[] configs)
        {
            return BufferSinkBuilder.Default.With(lc, configs);
        }
    }
}
