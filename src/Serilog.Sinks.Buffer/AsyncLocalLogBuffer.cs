// *********************************************************************
// Copyright (c). All rights reserved.
// See license file in root dir for details.
// *********************************************************************

using System.Threading;

namespace Serilog.Sinks.Buffer
{
    internal static class AsyncLocalLogBuffer
    {
        internal static readonly AsyncLocal<LogBuffer> LogBufferObject = new AsyncLocal<LogBuffer>();
    }
}
