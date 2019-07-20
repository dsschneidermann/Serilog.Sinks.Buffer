// *********************************************************************
// Copyright (c). All rights reserved.
// See license file in root dir for details.
// *********************************************************************

using Serilog.Events;

namespace Serilog.Sinks.Buffer.Internal.Extensions
{
    public static class LogEventLevelExtensions
    {
        public static bool IsSatisfiedBy(this LogEventLevel @this, LogEvent logEvent)
        {
            return logEvent.Level >= @this;
        }
    }
}
