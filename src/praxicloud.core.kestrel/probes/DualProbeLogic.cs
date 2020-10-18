// Copyright (c) Chris Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kestrel
{
    #region Using Clauses
    using Microsoft.Extensions.Logging;
    using praxicloud.core.containers;
    using praxicloud.core.kestrel.probes;
    using praxicloud.core.security;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    #endregion

    /// <summary>
    /// A dual probe that handles both health and availability requests
    /// </summary>
    public class DualProbeLogic : IMultiProbeValidator
    {
        #region Constants
        /// <summary>
        /// The availability endpoint
        /// </summary>
        public const string AvailabilityEndpoint = "/Availability";

        /// <summary>
        /// The health endpoint
        /// </summary>
        public const string HealthEndpoint = "/Health";

        /// <summary>
        /// The index of the availability endpoint
        /// </summary>
        private const int AvailabilityIndex = 1;

        /// <summary>
        /// The index of the health endpoint
        /// </summary>
        private const int HealthIndex = 2;
        #endregion
        #region Variables
        /// <summary>
        /// The endpoints that the probe listens on
        /// </summary>
        public static Dictionary<int, string> EndpointList = new Dictionary<int, string> { { AvailabilityIndex, AvailabilityEndpoint }, { HealthIndex, HealthEndpoint } };

        /// <summary>
        /// The logic to confirm availability
        /// </summary>
        private readonly IAvailabilityCheck _availabilityLogic;

        /// <summary>
        /// The logic to confirm health
        /// </summary>
        private readonly IHealthCheck _healthLogic;

        /// <summary>
        /// The logger to write debugging and diagnostics information to
        /// </summary>
        private readonly ILogger _logger;
        #endregion
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the type
        /// </summary>
        /// <param name="loggerFactory">Factory to create loggers</param>
        /// <param name="availabilityCheck">Logic to perform availability checks</param>
        /// <param name="healthCheck">Logic to perform health checks</param>
        public DualProbeLogic(ILoggerFactory loggerFactory, IAvailabilityCheck availabilityCheck, IHealthCheck healthCheck)
        {
            Guard.NotNull(nameof(loggerFactory), loggerFactory);
            Guard.NotNull(nameof(availabilityCheck), availabilityCheck);
            Guard.NotNull(nameof(healthCheck), healthCheck);

            _availabilityLogic = availabilityCheck;
            _healthLogic = healthCheck;
            _logger = loggerFactory.CreateLogger(Name);
        }
        #endregion
        #region Properties
        /// <inheritdoc />
        public string Name => "Kestrel Dual Probe";
        #endregion
        #region Methods
        /// <inheritdoc />
        public Task<bool> UnknownEndpointAsync(string endpoint, CancellationToken cancellationToken)
        {
            _logger.LogWarning("Bad request made to probe endpoint: {endpoint}", endpoint);

            return Task.FromResult(true);
        }

        /// <inheritdoc />
        public async Task<bool> ValidateAsync(int indexValue, string endpoint, CancellationToken cancellationToken)
        {
            var success = false;

            switch(indexValue)
            {
                case AvailabilityIndex:
                    success = await _availabilityLogic.IsAvailableAsync().ConfigureAwait(false);
                    break;

                case HealthIndex:
                    success = await _healthLogic.IsHealthyAsync().ConfigureAwait(false);
                    break;

                default:
                    _logger.LogWarning("Unknown probe index");
                    break;
            }

            return success;
        }

        #endregion
    }
}
