// Copyright (c) Christopher Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kestrel.middleware.unhandled
{
    #region Using Clauses
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using praxicloud.core.security;
    using System.Threading.Tasks;
    #endregion

    /// <summary>
    /// A middleware component that responds once no other pipeline component has completed
    /// </summary>
    public sealed class UnhandledMiddleware : IUnhandledMiddleware
    {
        #region Delegates
        /// <summary>
        /// The signature of a method invoked when the middleware is invoked
        /// </summary>
        /// <param name="context">The HTTP Context</param>
        /// <returns>True if it is considered handled</returns>
        public delegate Task<bool> InvocationCallback(HttpContext context);
        #endregion
        #region Variables
        /// <summary>
        /// The next module to call
        /// </summary>
        private readonly RequestDelegate _next;

        /// <summary>
        /// The logger to write debugging and diagnostics information to
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The content type of the response
        /// </summary>
        private readonly string _contentType;

        /// <summary>
        /// The content to be written in the response
        /// </summary>
        private readonly string _content;

        /// <summary>
        /// The response code
        /// </summary>
        private readonly int _responseCode;

        /// <summary>
        /// A method invoked when the middleware is invoked
        /// </summary>
        private readonly InvocationCallback _invocationCallback;
        #endregion
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the type
        /// </summary>
        /// <param name="next">The next middleware component to execute</param>
        /// <param name="loggerFactory">A factory that loggers can be created from</param>
        /// <param name="unhandledConfiguration">Contains information about the response the handler returns</param>
        /// <param name="invocationCallback">A method invoked when the middleware is invoked</param>
        public UnhandledMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IUnhandledConfiguration unhandledConfiguration = null, InvocationCallback invocationCallback = null)
        {
            Guard.NotNull(nameof(loggerFactory), loggerFactory);

            _logger = loggerFactory.CreateLogger("Unhandled middleware");


            using (_logger.BeginScope("Unhandled CTOR"))
            {
                if (unhandledConfiguration == null)
                {
                    _contentType = UnhandledDefaults.ContentType;
                    _content = UnhandledDefaults.Response;
                    _responseCode = UnhandledDefaults.ResponseCode;
                }
                else
                {
                    _contentType = unhandledConfiguration.ContentType ?? UnhandledDefaults.ContentType;
                    _content = unhandledConfiguration.Response ?? UnhandledDefaults.Response;
                    _responseCode = unhandledConfiguration.ResponseCode ?? UnhandledDefaults.ResponseCode;
                }

                _invocationCallback = invocationCallback;

                _logger.LogInformation("Initializing authentication middleware");

                _next = next;
            }
        }
        #endregion
        #region Methods
        /// <inheritdoc />
        public async Task Invoke(HttpContext context)
        {
            using (_logger.BeginScope("Unhandled Invoke"))
            {
                _logger.LogInformation("Unhandled middleware invoked");

                var writeOutput = true;

                if(_invocationCallback != null)
                {
                    _logger.LogDebug("Beginning callback");
                    writeOutput = !await _invocationCallback(context).ConfigureAwait(true);
                    _logger.LogDebug("Completed callback");
                }

                if (writeOutput)
                {
                    context.Response.StatusCode = _responseCode;
                    context.Response.ContentType = _contentType;
                    await context.Response.WriteAsync(_content).ConfigureAwait(true);
                }
                else
                {
                    _logger.LogInformation("No information written, callback handled");
                }

                _logger.LogDebug("Unhandled middleware invoked");
            }
        }
        #endregion
    }
}
