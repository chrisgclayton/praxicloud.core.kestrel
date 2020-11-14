// Copyright (c) Christopher Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kestrel.probes
{
    #region Using Clauses
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using praxicloud.core.containers;
    using praxicloud.core.security;
    #endregion

    /// <summary>
    /// Middleware that performs basic probe logic
    /// </summary>
    public class MultiProbeMiddleware
    {
        #region Constants
        /// <summary>
        /// The JSON content type
        /// </summary>
        public const string JsonContentType = "application/json";

        /// <summary>
        /// JSON success payload for probes
        /// </summary>
        public const string JsonSuccessResponse = "{\"Value\": true}";

        /// <summary>
        /// JSON failure payload for probes
        /// </summary>
        public const string JsonFailureResponse = "{\"Value\": false}";
        #endregion
        #region Variables
        /// <summary>
        /// The next module to call
        /// </summary>
        private readonly RequestDelegate _next;

        /// <summary>
        /// The configuration object
        /// </summary>
        private readonly IMultiProbeConfiguration _configuration;

        /// <summary>
        /// Factory to create loggers
        /// </summary>
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// A logger to write debugging and diagnostics messages
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Logic used to check probe response
        /// </summary>
        private readonly IMultiProbeValidator _validator;
        #endregion
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the type
        /// </summary>
        /// <param name="next">The next module to pass the context to</param>
        /// <param name="loggerFactory">A factory that can be used to create loggers</param>
        /// <param name="validator">Logic that is used to validate probe requests</param>
        /// <param name="configuration">Configuration that specifies the operation and endpoints for multy logic probes</param>
        public MultiProbeMiddleware(RequestDelegate next, IMultiProbeConfiguration configuration, ILoggerFactory loggerFactory, IMultiProbeValidator validator)
        {
            Guard.NotNull(nameof(configuration), configuration);
            Guard.NotNull(nameof(validator), validator);
            Guard.NotNull(nameof(validator.Name), validator.Name);
            Guard.NotNull(nameof(loggerFactory), loggerFactory);
            
            _next = next;
            _configuration = configuration;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory.CreateLogger(validator.Name);
            _validator = validator;
        }
        #endregion
        #region Methods
        /// <summary>
        /// Processes the HTTP requests
        /// </summary>
        /// <param name="context">The HTTP Context associated with the request</param>
        public async Task Invoke(HttpContext context)
        {
            var processed = false;

            foreach (var pair in _configuration.EndpointList)
            {
                if(string.Equals(pair.Value, context.Request.Path, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        _logger.LogInformation("Validation endpoint found.");
                        
                        if(await _validator.ValidateAsync(pair.Key, pair.Value, ContainerLifecycle.CancellationToken).ConfigureAwait(true))
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.OK;
                            context.Response.ContentType = JsonContentType;
                            await context.Response.WriteAsync(JsonSuccessResponse, ContainerLifecycle.CancellationToken).ConfigureAwait(true);
                        }
                        else
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                            context.Response.ContentType = JsonContentType;
                            await context.Response.WriteAsync(JsonFailureResponse, ContainerLifecycle.CancellationToken).ConfigureAwait(true);
                        }
                    }
                    catch(Exception e)
                    {
                        _logger.LogError(e, "Error while executing validation logic");
                    }

                    processed = true;
                    continue;
                }
            }

            if(!processed && _next != null)
            {
                if (await _validator.UnknownEndpointAsync(context.Request.Path, ContainerLifecycle.CancellationToken).ConfigureAwait(true))
                {
                    await _next.Invoke(context).ConfigureAwait(false);
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.Response.ContentType = JsonContentType;
                    await context.Response.WriteAsync(JsonFailureResponse, ContainerLifecycle.CancellationToken).ConfigureAwait(true);
                }
            }            
        }
        #endregion
    }
}

