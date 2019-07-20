// *********************************************************************
// Copyright (c). All rights reserved.
// See license file in root dir for details.
// *********************************************************************

using System;
using System.Threading;

namespace Serilog.Sinks.Buffer.Internal
{
    public class LogBufferScope : IDisposable
    {
        public LogBufferScope(bool collapseOnTriggered)
        {
            CollapseOnTriggered = collapseOnTriggered;
            LogBuffer = new LogBuffer(this);
        }

        public LogBuffer LogBuffer { get; }
        private LogBufferScope ParentLogBufferScope { get; set; }
        private bool CollapseOnTriggered { get; }
        public string Id { get; } = ObjectIdGen.GetNext();

        /// <inheritdoc />
        public void Dispose()
        {
            AsyncLocalStore.LogBufferScope.Value = ParentLogBufferScope;
        }

        /// <summary>
        ///     Begins a new scope. See LogBuffer.BeginScope.
        /// </summary>
        public LogBufferScope BeginScope()
        {
            ParentLogBufferScope = AsyncLocalStore.LogBufferScope.Value;
            AsyncLocalStore.LogBufferScope.Value = this;

            return this;
        }

        internal void Trigger(bool isCollapsing = false)
        {
            if (CollapseOnTriggered)
            {
                ParentLogBufferScope?.Trigger(true);
            }

            if (isCollapsing)
            {
                LogBuffer.TriggerFlush(true);
            }
        }

        internal static LogBufferScope EnsureCreated()
        {
            return AsyncLocalStore.LogBufferScope.Value ??
                (AsyncLocalStore.LogBufferScope.Value = new LogBufferScope(false));
        }

        private static class ObjectIdGen
        {
            private static int objIdCounter;

            public static string GetNext()
            {
                return $"#{Interlocked.Increment(ref objIdCounter):X}";
            }
        }
    }
}
