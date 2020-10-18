// Copyright (c) Chris Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kestrel.middleware.authentication
{
    #region Using Clauses
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using praxicloud.core.configuration;
    using praxicloud.core.containers;
    using praxicloud.core.metrics;
    using praxicloud.core.security;
    using System;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using praxicloud.core.kestrel.middleware.websockets;
    using System.Runtime.CompilerServices;
    #endregion

    /// <summary>
    /// A middleware component that does basic SAS validation
    /// </summary>
    public abstract class SasAuthenticationMiddleware : IAuthenticationMiddleware
    {
        #region Constants
        /// <summary>
        /// The response content type
        /// </summary>
        private const string ContentType = "application/json";

        /// <summary>
        /// Success payload for availability and health probes
        /// </summary>
        private const string SuccessResponse = "{\"Value\": true}";

        /// <summary>
        /// Failure payload for availability and health probes
        /// </summary>
        private const string FailureResponse = "{\"Value\": false}";
        #endregion
        #region Variables
        /// <summary>
        /// The next module to call
        /// </summary>
        private readonly RequestDelegate _next;

        /// <summary>
        /// A summary metric to record authentication timing
        /// </summary>
        private readonly ISummary _authenticationTiming;

        /// <summary>
        /// Web socket configuration
        /// </summary>
        private readonly IWebSocketConfiguration _configuration;

        /// <summary>
        /// The configuration values for the web socket operations
        /// </summary>
        private readonly IWebSocketConfiguration _webSocketConfiguration;

        /// <summary>
        /// The logger to write debugging and diagnostics information to
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// A metric used to track successful authentication rate
        /// </summary>
        private readonly ISummary _authenticationSuccess;

        /// <summary>
        /// A metric used to track unsuccessful authentication rate
        /// </summary>
        private readonly ISummary _authenticationFailure;

        /// <summary>
        /// A metric used to track successf validation rate
        /// </summary>
        private readonly ISummary _validateSuccess;

        /// <summary>
        /// A metric used to track unsuccessful validation rate
        /// </summary>
        private readonly ISummary _validateFailure;

        /// <summary>
        /// The timeout for each SAS token
        /// </summary>
        private readonly TimeSpan _sasTokenTimeout;

        /// <summary>
        /// The name of the policy that was used to generate the SAS
        /// </summary>
        private readonly string _sasTokenPolicyName;

        /// <summary>
        /// The scheme that the SAS token uses
        /// </summary>
        private readonly string _sasTokenScheme;

        /// <summary>
        /// The key used to sign the SAS tokens
        /// </summary>
        private readonly string _sasSigningKey;

        /// <summary>
        /// The HTTP header that contains the authorization details
        /// </summary>
        private readonly string _authorizationHeaderName;

        /// <summary>
        /// The HTTP header that contains the resource URI information
        /// </summary>
        private readonly string _resourceUriHeaderName;

        /// <summary>
        /// The HTTP header that contains the policy header
        /// </summary>
        private readonly string _authorizationPolicyHeaderName;

        private readonly string _authenticationPath;
        #endregion
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the type
        /// </summary>
        /// <param name="authenticationPath">The HTTP path used for authentication</param>
        /// <param name="authorizationHeaderName">The name of the header where the authorization token is stored</param>
        /// <param name="authorizationPolicyHeaderName">The name of the header where the policy is stored</param>
        /// <param name="dependencyService">A service that can be used to retrieve dependencies that were injected</param>
        /// <param name="loggerFactory">A factory that can be used to create loggers</param>
        /// <param name="metricFactory">A factory that can be used to create metrics</param>
        /// <param name="policyName">The name of the policy</param>
        /// <param name="resourceUriHeaderName">The name of the header that stores the resource URL</param>
        /// <param name="sasSigningKey">A Shared Access Signature signing key</param>
        /// <param name="sasTokenPolicyName">A Shared Access Signature token policy name</param>
        /// <param name="sasTokenTimeout">The Shared Access Signature token timeout</param>
        /// <param name="webSocketConfiguration">The configuration of the web socket</param>   
        /// <param name="next">The next middleware component to execute</param>
        public SasAuthenticationMiddleware(RequestDelegate next, IDependencyService dependencyService, IMetricFactory metricFactory, ILoggerFactory loggerFactory, IWebSocketConfiguration webSocketConfiguration, TimeSpan? sasTokenTimeout, string sasTokenPolicyName, string policyName, string sasSigningKey, string authenticationPath, string authorizationHeaderName = "Authorization", string resourceUriHeaderName = "AuthResourceUri", string authorizationPolicyHeaderName = "AuthPolicyName")
        {
            Guard.NotNull(nameof(metricFactory), metricFactory);
            Guard.NotNull(nameof(loggerFactory), loggerFactory);
            Guard.NotNull(nameof(dependencyService), dependencyService);
            Guard.NotNull(nameof(webSocketConfiguration), webSocketConfiguration);
            Guard.NotNullOrWhitespace(nameof(sasTokenPolicyName), sasTokenPolicyName);
            Guard.NotNullOrWhitespace(nameof(policyName), policyName);
            Guard.NotNullOrWhitespace(nameof(sasSigningKey), sasSigningKey);
            Guard.NotNullOrWhitespace(nameof(authorizationHeaderName), authorizationHeaderName);
            Guard.NotNullOrWhitespace(nameof(resourceUriHeaderName), resourceUriHeaderName);
            Guard.NotNullOrWhitespace(nameof(authorizationPolicyHeaderName), authorizationPolicyHeaderName);
            Guard.NotNullOrWhitespace(nameof(authenticationPath), authenticationPath);

            _logger = loggerFactory.CreateLogger("SAS Authentication");

            using (_logger.BeginScope("SAS CTOR"))
            {
                _logger.LogInformation("Initializing authentication middleware");

                _next = next;

                _webSocketConfiguration = webSocketConfiguration;
                _sasTokenTimeout = sasTokenTimeout ?? TimeSpan.FromHours(1);
                _sasTokenPolicyName = sasTokenPolicyName;
                _sasTokenScheme = sasTokenPolicyName;
                _sasSigningKey = sasSigningKey;
                _authorizationHeaderName = authorizationHeaderName;
                _resourceUriHeaderName = resourceUriHeaderName;
                _authorizationPolicyHeaderName = authorizationPolicyHeaderName;
                _authenticationPath = authenticationPath;

                _configuration = webSocketConfiguration; // dependencyService.Services.GetRequiredService<WebSocketConfiguration>();

                _authenticationTiming = metricFactory.CreateSummary("sas-timing", "Timing summary for authentication timing", 10, false, new string[0]);
                _authenticationSuccess = metricFactory.CreateSummary("sas-authentication-success", "Successful authorization count", 10, false, new string[0]);
                _authenticationFailure = metricFactory.CreateSummary("sas-authentication-failure", "Failure authorization count", 10, false, new string[0]);
                _validateSuccess = metricFactory.CreateSummary("sas-validation-success", "Successful validation count", 10, false, new string[0]);
                _validateFailure = metricFactory.CreateSummary("sas-validation-failure", "Failure validation count", 10, false, new string[0]);
            }
        }
        #endregion
        #region Methods
        /// <summary>
        /// Validates a token that has already been granted
        /// </summary>
        /// <param name="context">The HTTP Context that generated the request</param>
        private async Task ValidateTokenAsync(HttpContext context)
        {
            var failedAuthentication = true;

            using (_logger.BeginScope("Validate Token"))
            {
                if (context.Request.Headers.ContainsKey("Authorization"))
                {
                    var accessToken = context.Request.Headers["Authorization"][0];

                    if (accessToken.StartsWith("Bearer "))
                    {
                        accessToken = accessToken.Substring("Bearer ".Length);

                        if (SharedAccessTokens.DecomposeSasToken(accessToken, out var resourceUri, out var policyName, out var expiresAt, out var stringToValidate, out var signature))
                        {
                            if (string.Equals(policyName, _sasTokenPolicyName, StringComparison.Ordinal))
                            {
                                if (SharedAccessTokens.IsSignatureValid(signature, _sasSigningKey, stringToValidate))
                                {
                                    if (DateTime.UtcNow < expiresAt)
                                    {
                                        context.Request.Headers.Add(_resourceUriHeaderName, resourceUri);
                                        context.Request.Headers.Add(_authorizationPolicyHeaderName, policyName);

                                        await _next.Invoke(context).ConfigureAwait(true);

                                        failedAuthentication = false;
                                        _authenticationSuccess.Observe(1.0);
                                    }
                                }
                            }
                        }
                    }
                }

                if (failedAuthentication)
                {
                    _authenticationFailure.Observe(1.0);
                    _logger.LogInformation("Attempted authentication failed");

                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    context.Response.ContentType = ContentType;
                    await context.Response.WriteAsync(FailureResponse).ConfigureAwait(true);
                    _validateFailure.Observe(1.0);
                }
                else
                {
                    _validateSuccess.Observe(1.0);
                }
            }
        }

        /// <summary>
        /// Validates the authentication request
        /// </summary>
        /// <param name="context">The HTTP Context that generated the request</param>
        private async Task ValidateAuthenticationRequestAsync(HttpContext context)
        {
            using (_logger.BeginScope("Validate Token"))
            {
                _logger.LogDebug("Non Web Socket Request received");

                if (string.Equals(context.Request.Path, _authenticationPath, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Authentication request started");

                    string authenticationResponse = string.Empty;

                    var responsePayload = await AuthenticateClientAsync(context.Request, ContainerLifecycle.CancellationToken).ConfigureAwait(true);

                    if (responsePayload != null)
                    {
                        authenticationResponse = JsonConvert.SerializeObject(responsePayload);
                    }
                    else
                    {
                        _logger.LogError("Error authenticating device id and key");
                    }

                    if (!string.IsNullOrWhiteSpace(authenticationResponse))
                    {
                        _logger.LogInformation("Authentication result success");

                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                        context.Response.ContentType = ContentType;
                        await context.Response.WriteAsync(authenticationResponse).ConfigureAwait(true);
                    }
                    else
                    {
                        _logger.LogInformation("Authentication result failure");

                        context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                        context.Response.ContentType = ContentType;
                        await context.Response.WriteAsync(FailureResponse).ConfigureAwait(true);
                    }

                    _logger.LogDebug("Authentication request completed");
                }
                else
                {
                    await _next.Invoke(context).ConfigureAwait(true);
                }
            }
        }

        /// <inheritdoc />
        public async Task Invoke(HttpContext context)
        {
            using (_logger.BeginScope("Authentication Invoke"))
            {
                if (!context.WebSockets.IsWebSocketRequest)
                {
                    await ValidateAuthenticationRequestAsync(context).ConfigureAwait(true);
                }
                else
                {
                    await ValidateTokenAsync(context).ConfigureAwait(true);
                }
            }
        }

        /// <summary>
        /// Authenticates the client credentials against the hub
        /// </summary>
        /// <param name="request">The request</param>
        /// <param name="cancellationToken">A token to monitor for abort requests</param>
        /// <returns>The authentication response</returns>
        private async Task<AuthenticationResponsePayload> AuthenticateClientAsync(HttpRequest request, CancellationToken cancellationToken)
        {
            AuthenticationResponsePayload response = null;

            using (var reader = new StreamReader(request.Body))
            {
                var requestPayload = JsonConvert.DeserializeObject<AuthenticationRequestPayload>(reader.ReadToEnd());

                using (_authenticationTiming.Time())
                {
                    if(await AuthenticateAsync(request, requestPayload, cancellationToken).ConfigureAwait(false))
                    {
                        var accessToken = SharedAccessTokens.GenerateSasToken($"{_sasTokenScheme}://{requestPayload.UserName}", _sasSigningKey, _sasTokenPolicyName, (int)_sasTokenTimeout.TotalSeconds);

                        response = new AuthenticationResponsePayload
                        {
                            TokenType = "bearer",
                            AccessToken = accessToken
                        };
                    }
                }
            }

            return response;
        }

        /// <summary>
        /// Performs authentication of the client
        /// </summary>
        /// <param name="request">The HTTP Request</param>
        /// <param name="requestPayload">Payload of the HTTP request</param>
        /// <param name="cancellationToken">A token that can be used to monitor for abort requests</param>
        /// <returns>True if authentication is successful</returns>
        protected abstract Task<bool> AuthenticateAsync(HttpRequest request, AuthenticationRequestPayload requestPayload, CancellationToken cancellationToken);
        #endregion
    }
}
