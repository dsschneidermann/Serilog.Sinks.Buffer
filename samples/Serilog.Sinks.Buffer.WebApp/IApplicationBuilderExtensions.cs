// *********************************************************************
// Copyright (c). All rights reserved.
// See license file in root dir for details.
// *********************************************************************

using System;
using Microsoft.AspNetCore.Builder;

namespace Serilog.Sinks.Buffer.WebApp
{
    public static class IApplicationBuilderExtensions
    {
        /// <summary>
        ///     Use a LogBufferScope in the request pipeline for each request. The earlier in the pipeline
        ///     the middleware is added, the more debug information will be captured.
        /// </summary>
        public static void UseLogBufferScope(this IApplicationBuilder app)
        {
            app.Use(
                async (context, next) => {
                    using (LogBuffer.BeginScope())
                    {
                        try
                        {
                            await next.Invoke();
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Exception details: {ExceptionMessage}", ex.Message);
                            throw;
                        }
                    }
                }
            );
        }
    }
}
