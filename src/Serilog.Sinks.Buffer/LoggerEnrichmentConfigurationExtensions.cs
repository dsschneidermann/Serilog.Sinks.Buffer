// *********************************************************************
// Copyright (c). All rights reserved.
// See license file in root dir for details.
// *********************************************************************

using System;
using Serilog.Configuration;

namespace Serilog.Sinks.Buffer
{
    public static class LoggerEnrichmentConfigurationExtensions
    {
        /// <summary>
        ///     Enrich log events with a buffer of the log events for that logical call context.
        /// </summary>
        /// <param name="loggerEnrichmentConfiguration">The enrichment configuration.</param>
        /// <returns>Configuration object allowing method chaining.</returns>
        public static LoggerConfiguration WithBufferContext(
            this LoggerEnrichmentConfiguration loggerEnrichmentConfiguration)
        {
            if (loggerEnrichmentConfiguration == null)
            {
                throw new ArgumentNullException(nameof(loggerEnrichmentConfiguration));
            }

            return loggerEnrichmentConfiguration.With(new LogBuffer.LogBufferEnricher());
        }
    }
}
