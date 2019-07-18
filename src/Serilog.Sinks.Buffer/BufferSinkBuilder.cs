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

namespace Serilog.Sinks.Buffer
{
    public class BufferSinkBuilder
    {
        /// <summary>
        ///     A builder instance with default settings. Atleast the Configs parameter must be specified for the sink to do any
        ///     buffering, ie. it does not buffer by default.
        /// </summary>
        public static BufferSinkBuilder Default = new BufferSinkBuilder(
            new LoggerConfiguration().MinimumLevel.Verbose(), Enumerable.Empty<SourceConfig>(),
            new LoggingLevelSwitch {MinimumLevel = LogEventLevel.Verbose}, LogEventLevel.Error, 100
        );

        private readonly List<Action<LoggerConfiguration>> ConfigureOutputs = new List<Action<LoggerConfiguration>>();

        /// <summary>
        ///     Create a buffered sink. <seealso cref="Default" />
        /// </summary>
        /// <param name="LoggerConfiguration">The user defined logger configuration</param>
        /// <param name="Configs">See <see cref="SourceConfig" /></param>
        /// <param name="MinLevelSwitch">The minimum level for events that do not match a configured source.</param>
        /// <param name="TriggeringLevel">The event level on which to trigger detailed logs.</param>
        /// <param name="BufferCapacity">The maximum amount of log entries to buffer before dropping the older half.</param>
        public BufferSinkBuilder(
            LoggerConfiguration LoggerConfiguration, IEnumerable<SourceConfig> Configs,
            LoggingLevelSwitch MinLevelSwitch, LogEventLevel TriggeringLevel, int BufferCapacity)
        {
            this.LoggerConfiguration = LoggerConfiguration;
            this.Configs = Configs;
            this.MinLevelSwitch = MinLevelSwitch;
            this.TriggeringLevel = TriggeringLevel;
            this.BufferCapacity = BufferCapacity;
        }

        public LoggerConfiguration LoggerConfiguration { get; }
        public IEnumerable<SourceConfig> Configs { get; }
        public LoggingLevelSwitch MinLevelSwitch { get; }
        public LogEventLevel TriggeringLevel { get; }
        public int BufferCapacity { get; }

        /// <summary>
        ///     Build the logger for the config and output.
        /// </summary>
        public ILogger CreateLogger()
        {
            return CreateBufferSink(
                    LoggerConfiguration, Configs, MinLevelSwitch, TriggeringLevel, BufferCapacity, ConfigureOutputs
                )
                .CreateLogger();
        }

        /// <summary>
        ///     Create a buffered sink.
        /// </summary>
        /// <param name="LoggerConfiguration">The user defined logger configuration</param>
        /// <param name="Configs">See <see cref="SourceConfig" /></param>
        /// <param name="MinLevelSwitch">The minimum level for events that do not match a configured source.</param>
        /// <param name="TriggeringLevel">The event level on which to trigger detailed logs, default is Error.</param>
        /// <param name="BufferCapacity">The maximum amount of log entries to buffer before dropping the older half, default 100.</param>
        public BufferSinkBuilder With(
            LoggerConfiguration LoggerConfiguration = null, IEnumerable<SourceConfig> Configs = null,
            LoggingLevelSwitch MinLevelSwitch = null, LogEventLevel? TriggeringLevel = null, int? BufferCapacity = null)
        {
            return new BufferSinkBuilder(
                LoggerConfiguration ?? this.LoggerConfiguration, Configs ?? this.Configs,
                MinLevelSwitch ?? this.MinLevelSwitch, TriggeringLevel ?? this.TriggeringLevel,
                BufferCapacity ?? this.BufferCapacity
            );
        }

        /// <summary>
        ///     Add output configuration for the the buffered sink. You can add as many as desired.
        /// </summary>
        /// <param name="configureOutput">Logger configuration setup to handle writing events to outputs.</param>
        public BufferSinkBuilder WriteTo(Action<LoggerConfiguration> configureOutput)
        {
            var newInstance = new BufferSinkBuilder(
                LoggerConfiguration: LoggerConfiguration, Configs: Configs, MinLevelSwitch: MinLevelSwitch,
                TriggeringLevel: TriggeringLevel, BufferCapacity: BufferCapacity
            );

            newInstance.ConfigureOutputs.AddRange(ConfigureOutputs);
            newInstance.ConfigureOutputs.Add(configureOutput);
            return newInstance;
        }

        private static LoggerConfiguration CreateBufferSink(
            LoggerConfiguration sourceConfig, IEnumerable<SourceConfig> configSet, LoggingLevelSwitch minLevelSwitch,
            LogEventLevel triggerEventLevel, int bufferCapacity, List<Action<LoggerConfiguration>> configureOutput)
        {
            var configSetList = configSet.ToList();

            var userLc = new LoggerConfiguration().MinimumLevel.Verbose();
            foreach (var action in configureOutput)
            {
                action?.Invoke(userLc);
            }

            var outputSink = userLc.CreateLogger();

            sourceConfig.MinimumLevel.Verbose();

            var sinkConfig = new BufferSinkConfig(triggerEventLevel, outputSink, bufferCapacity);

            // BufferSink will write to the user defined outputSink
            var BufferSinkInstance = new BufferSink(configSetList, sinkConfig);

            // The events we write to the BufferSink will obey the MinLevelOnDetailed of the configs
            foreach (var c in configSetList)
            {
                // NB: MinimumLevel.Override not usable in sub-loggers, see Serilog issue #967
                // We implement the equivalent filter
                sourceConfig.WriteTo.Logger(
                    l => l.Filter.ByIncludingOnly(Matching.FromSource(c.SourceToMatchOn))
                        .MinimumLevel.Is(c.MinLevelOnDetailed)
                        .WriteTo.Sink(BufferSinkInstance)
                );
            }

            // A fallback logger is needed to write everything that is not defined by configs
            var fallbackLogger = new LoggerConfiguration().MinimumLevel.ControlledBy(minLevelSwitch);
            foreach (var c in configSetList)
            {
                fallbackLogger.Filter.ByExcluding(Matching.FromSource(c.SourceToMatchOn));
            }

            fallbackLogger.WriteTo.Sink(outputSink);
            sourceConfig.WriteTo.Logger(fallbackLogger.CreateLogger());
            return sourceConfig;
        }
    }
}
