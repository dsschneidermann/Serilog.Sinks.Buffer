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
        ///     Source to match on as a string, same as the usual Serilog config.
        /// </summary>
        public string SourceToMatchOn { get; set; }

        /// <summary>
        ///     Always emit events with this level or above.
        /// </summary>
        public LogEventLevel MinLevelAlways { get; set; }

        /// <summary>
        ///     Emit events with this level or above when a trigger level event occurs.
        /// </summary>
        public LogEventLevel MinLevelOnDetailed { get; set; }
    }
}
