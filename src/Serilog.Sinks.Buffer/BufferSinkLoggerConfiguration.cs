// *********************************************************************
// Copyright (c). All rights reserved.
// See license file in root dir for details.
// *********************************************************************
namespace Serilog.Sinks.Buffer
{
    public class BufferSinkLoggerConfiguration
    {
        private readonly LoggerConfiguration _loggerConfiguration;

        public BufferSinkLoggerConfiguration(LoggerConfiguration loggerConfiguration)
        {
            _loggerConfiguration = loggerConfiguration;
        }

        /// <summary>
        ///     Create a logger using the configured sources and outputs.
        /// </summary>
        public ILogger CreateLogger() => _loggerConfiguration.CreateLogger();
    }
}
