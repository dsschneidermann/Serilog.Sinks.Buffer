// *********************************************************************
// Copyright (c). All rights reserved.
// See license file in root dir for details.
// *********************************************************************

using System.Threading;

namespace Serilog.Sinks.Buffer.Internal
{
    internal static class AsyncLocalStore
    {
        internal static readonly AsyncLocal<LogBufferScope> LogBufferScope = new AsyncLocal<LogBufferScope>();
    }
}
