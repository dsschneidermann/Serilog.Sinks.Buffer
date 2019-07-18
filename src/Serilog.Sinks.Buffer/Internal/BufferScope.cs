// *********************************************************************
// Copyright (c). All rights reserved.
// See license file in root dir for details.
// *********************************************************************

using System;

namespace Serilog.Sinks.Buffer.Internal
{
    public class BufferScope : IDisposable
    {
        private readonly LogBuffer _capturedObject = AsyncLocalLogBuffer.LogBufferObject.Value;
        private readonly bool _collapseOnTriggered;

        public BufferScope(bool collapseOnTriggered)
        {
            _collapseOnTriggered = collapseOnTriggered;

            AsyncLocalLogBuffer.LogBufferObject.Value = null;
            LogBuffer.EnsureCreated();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_collapseOnTriggered && AsyncLocalLogBuffer.LogBufferObject.Value.IsTriggered)
            {
                _capturedObject.TriggerFlush();
            }

            AsyncLocalLogBuffer.LogBufferObject.Value = _capturedObject;
        }
    }
}
