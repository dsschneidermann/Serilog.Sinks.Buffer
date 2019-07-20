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
using Serilog.Sinks.Buffer.Internal;
using Serilog.Sinks.Buffer.Internal.Extensions;

namespace Serilog.Sinks.Buffer
{
    public class BufferSinkBuilder
    {
        /// <summary>
        ///     A builder instance with default settings. Atleast one source (or the fallback source: "*") must be specified for
        ///     the Configs parameter for the sink to do any buffering, ie. it does not buffer by default.
        /// </summary>
        public static readonly BufferSinkBuilder Default = new BufferSinkBuilder(
            new LoggerConfiguration(), Enumerable.Empty<SourceConfig>(), null, LogEventLevel.Error, 100
        );

        private readonly List<Action<LoggerConfiguration>> ConfigureOutputs = new List<Action<LoggerConfiguration>>();

        /// <summary>
        ///     Create a buffered sink. See <see cref="Default" />.
        /// </summary>
        /// <param name="LoggerConfiguration">The user defined logger configuration</param>
        /// <param name="Configs">See <see cref="SourceConfig" /></param>
        /// <param name="FallbackLevelSwitch">The level switch override for events that match the fallback source.</param>
        /// <param name="TriggeringLevel">The event level on which to trigger detailed logs.</param>
        /// <param name="BufferCapacity">The maximum amount of log entries to buffer before dropping the older half.</param>
        public BufferSinkBuilder(
            LoggerConfiguration LoggerConfiguration, IEnumerable<SourceConfig> Configs,
            LoggingLevelSwitch FallbackLevelSwitch, LogEventLevel TriggeringLevel, int BufferCapacity)
        {
            this.LoggerConfiguration = LoggerConfiguration;
            this.Configs = Configs;
            this.FallbackLevelSwitch = FallbackLevelSwitch;
            this.TriggeringLevel = TriggeringLevel;
            this.BufferCapacity = BufferCapacity;
        }

        public LoggerConfiguration LoggerConfiguration { get; }
        public IEnumerable<SourceConfig> Configs { get; }
        public LoggingLevelSwitch FallbackLevelSwitch { get; }
        public LogEventLevel TriggeringLevel { get; }
        public int BufferCapacity { get; }

        /// <summary>
        ///     Create a logger using the configured sources and outputs.
        /// </summary>
        public ILogger CreateLogger()
        {
            // Create a LogBufferScope as early as possible to capture the most general AsyncLocal context
            LogBufferScope.EnsureCreated();

            return CreateBufferSink(
                    LoggerConfiguration, Configs, FallbackLevelSwitch, TriggeringLevel, BufferCapacity, ConfigureOutputs
                )
                .CreateLogger();
        }

        /// <summary>
        ///     Create a buffered sink.
        /// </summary>
        /// <param name="LoggerConfiguration">
        ///     The user defined logger configuration.
        /// </param>
        /// <param name="Configs">
        ///     See <see cref="SourceConfig" />
        /// </param>
        /// <param name="FallbackLevelSwitch">
        ///     The level switch override for events that match the fallback source.
        /// </param>
        /// <param name="TriggeringLevel">
        ///     The event level on which to trigger detailed logs, default is Error.
        /// </param>
        /// <param name="BufferCapacity">
        ///     The maximum amount of log entries to buffer before dropping the older half, default 100.
        /// </param>
        public BufferSinkBuilder With(
            LoggerConfiguration LoggerConfiguration = null, IEnumerable<SourceConfig> Configs = null,
            LoggingLevelSwitch FallbackLevelSwitch = null, LogEventLevel? TriggeringLevel = null,
            int? BufferCapacity = null)
        {
            return new BufferSinkBuilder(
                LoggerConfiguration ?? this.LoggerConfiguration, Configs ?? this.Configs,
                FallbackLevelSwitch ?? this.FallbackLevelSwitch, TriggeringLevel ?? this.TriggeringLevel,
                BufferCapacity ?? this.BufferCapacity
            );
        }

        /// <summary>
        ///     Add output configuration for the the buffered sink. Repeat to add as many as desired.
        /// </summary>
        /// <param name="configureOutput">Logger configuration setup to handle writing events to outputs.</param>
        public BufferSinkBuilder WriteTo(Action<LoggerConfiguration> configureOutput)
        {
            var newInstance = new BufferSinkBuilder(
                LoggerConfiguration, Configs, FallbackLevelSwitch, TriggeringLevel, BufferCapacity
            );

            newInstance.ConfigureOutputs.AddRange(ConfigureOutputs);
            newInstance.ConfigureOutputs.Add(configureOutput);
            return newInstance;
        }

        private static LoggerConfiguration CreateBufferSink(
            LoggerConfiguration sourceConfig, IEnumerable<SourceConfig> configSet,
            LoggingLevelSwitch fallbackLevelSwitch, LogEventLevel triggerEventLevel, int bufferCapacity,
            IEnumerable<Action<LoggerConfiguration>> configureOutput)
        {
            var configSetList = configSet.ToList();
            var userLc = new LoggerConfiguration().MinimumLevel.Verbose();
            foreach (var action in configureOutput)
            {
                action?.Invoke(userLc);
            }

            var outputSink = userLc.CreateLogger();

            sourceConfig.MinimumLevel.Verbose().Enrich.WithBufferContext();

            bool IsFallbackSourceConfig(SourceConfig s)
            {
                return string.IsNullOrEmpty(s.SourceToMatchOn) || s.SourceToMatchOn == "*";
            }

            var sourceConfigs = configSetList.Where(x => !IsFallbackSourceConfig(x)).ToList();
            var fallbackConfig = configSetList.LastOrDefault(IsFallbackSourceConfig);

            // Create source configs and fallback for the BufferSink
            var bufferConfigs = sourceConfigs.Select(
                    x => new BufferSink.SourceConfig_Internal(
                        Matching.FromSource(x.SourceToMatchOn), x.MinLevelAlways, null
                    )
                )
                .ToList();
            var bufferFallbackConfig = fallbackConfig != null
                ? new BufferSink.SourceConfig_Internal(_ => true, fallbackConfig.MinLevelAlways, fallbackLevelSwitch)
                : null;

            // Create BufferSink with configs and outputSink
            var bufferSink = new BufferSink(
                new BufferSinkConfig(triggerEventLevel, outputSink, bufferCapacity), bufferConfigs, bufferFallbackConfig
            );

            foreach (var c in sourceConfigs)
            {
                // Events matching a source config will obey the lower of the two minimum levels
                var minimumLevel = c.MinLevelAlways < c.MinLevelOnDetailed ? c.MinLevelAlways : c.MinLevelOnDetailed;
                // NB: MinimumLevel.Override not usable in sub-loggers, see Serilog issue #967
                // We implement the equivalent filter
                sourceConfig.WriteTo.Logger(
                    l => l.Filter.ByIncludingOnly(Matching.FromSource(c.SourceToMatchOn))
                        .MinimumLevel.Is(minimumLevel)
                        .WriteTo.Sink(bufferSink)
                );
            }

            // Handle anything without a matching source config
            sourceConfig.WriteTo.Logger(
                l => {
                    // Exclude events that match any config
                    foreach (var c in sourceConfigs)
                    {
                        l.Filter.ByExcluding(Matching.FromSource(c.SourceToMatchOn));
                    }

                    ILogEventSink baseSink;
                    if (fallbackConfig != null)
                    {
                        // Events matching a source config will obey the lower of the two minimum levels
                        l.MinimumLevel.Is(
                            fallbackConfig.MinLevelAlways < fallbackConfig.MinLevelOnDetailed
                                ? fallbackConfig.MinLevelAlways
                                : fallbackConfig.MinLevelOnDetailed
                        );
                        baseSink = bufferSink;
                    }
                    else
                    {
                        // No fallback config given, so write to output immediately
                        baseSink = outputSink;
                    }

                    l.WriteTo.Sink(baseSink);
                }
            );

            return sourceConfig;
        }
    }
}
