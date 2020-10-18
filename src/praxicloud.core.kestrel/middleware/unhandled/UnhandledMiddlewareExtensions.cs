// Copyright (c) Chris Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kestrel.middleware.unhandled
{
    #region Using Clauses
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.Logging;
    using static praxicloud.core.kestrel.middleware.unhandled.UnhandledMiddleware;
    #endregion

    /// <summary>
    /// An extension class used to make use of the Web Socket Middleware component easier
    /// </summary>
    public static class UnhandledMiddlewareExtensions
    {
        #region Extension Methods
        /// <summary>
        /// Adds unhandled request middleware to the applicaiton builder
        /// </summary>
        /// <param name="app">The application builder to add the middleware to</param>
        /// <param name="loggerFactory">A factory that loggers can be created from</param>
        /// <param name="unhandledConfiguration">Contains information about the response the handler returns</param>
        /// <param name="invocationCallback">A method invoked when the middleware is invoked</param>
        /// <returns>The original application builder for pipelining</returns>
        public static IApplicationBuilder UseUnhandled(this IApplicationBuilder app, ILoggerFactory loggerFactory = null, IUnhandledConfiguration unhandledConfiguration = null, InvocationCallback invocationCallback = null)
        {
            app.UseMiddleware(typeof(UnhandledMiddleware), loggerFactory, unhandledConfiguration, invocationCallback);

            return app;
        }
        #endregion
    }
}
