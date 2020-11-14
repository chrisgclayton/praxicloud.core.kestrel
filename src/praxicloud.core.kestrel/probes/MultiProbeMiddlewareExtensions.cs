// Copyright (c) Christopher Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kestrel.probes
{
    #region Using Clauses
    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.Logging;
    #endregion

    /// <summary>
    /// An extension class used to make use of the Web Socket Middleware component easier
    /// </summary>
    public static class MultiProbeMiddlewareExtensions
    {
        #region Extension Methods
        /// <summary>
        /// Adds the probe middleware
        /// </summary>
        /// <param name="app">The application builder to add the middleware to</param>
        /// <param name="configuration">Probe configuration</param>
        /// <param name="loggerFactory">A factory to create loggers from</param>
        /// <param name="validator">Validation logic for the probes</param>
        /// <returns>The builder to enable pipelining</returns>
        public static IApplicationBuilder UseMultiProbe(this IApplicationBuilder app, IMultiProbeConfiguration configuration, ILoggerFactory loggerFactory, IMultiProbeValidator validator)
        {
            app.UseMiddleware(typeof(MultiProbeMiddleware), configuration, loggerFactory, validator);

            return app;
        }
        #endregion
    }
}
