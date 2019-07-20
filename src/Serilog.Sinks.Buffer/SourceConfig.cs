// *********************************************************************
// Copyright (c). All rights reserved.
// See license file in root dir for details.
// *********************************************************************

using Serilog.Events;

namespace Serilog.Sinks.Buffer
{
    public class SourceConfig
    {
        /// <summary>
        ///     Create a source config.
        /// </summary>
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
        public SourceConfig(string SourceToMatchOn, LogEventLevel MinLevelAlways, LogEventLevel MinLevelOnDetailed)
        {
            this.SourceToMatchOn = SourceToMatchOn;
            this.MinLevelAlways = MinLevelAlways;
            this.MinLevelOnDetailed = MinLevelOnDetailed;
        }

        /// <summary>
        ///     Source to match on as a string, same as the usual Serilog config. Specify the fallback source: "*"
        ///     (or null or empty) to buffer events that are not matched by any other source config.
        /// </summary>
        public string SourceToMatchOn { get; }

        /// <summary>
        ///     Always emit events with this level or above.
        /// </summary>
        public LogEventLevel MinLevelAlways { get; }

        /// <summary>
        ///     Emit events with this level or above when a triggering event occurs.
        /// </summary>
        public LogEventLevel MinLevelOnDetailed { get; }

        /// <summary>
        ///     Modify properties and return an new instance.
        /// </summary>
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
        /// <returns>A new instance with the properties set.</returns>
        public SourceConfig With(
            string SourceToMatchOn = null, LogEventLevel? MinLevelAlways = null,
            LogEventLevel? MinLevelOnDetailed = null)
        {
            return new SourceConfig(
                SourceToMatchOn ?? this.SourceToMatchOn, MinLevelAlways ?? this.MinLevelAlways,
                MinLevelOnDetailed ?? this.MinLevelOnDetailed
            );
        }
    }
}
