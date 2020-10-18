// Copyright (c) Chris Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kestrel
{
    #region Using Clauses
    using Microsoft.Extensions.Logging;
    using praxicloud.core.containers;
    using praxicloud.core.kestrel.probes;
    #endregion

    /// <summary>
    /// A health probe taht uses kestrel for HTTP
    /// </summary>
    public class KestrelHealthProbe : KestrelProbeHost<MultiProbeStartup>, IHealthContainerProbe
    {
        /// <summary>
        /// Initializes a new instance of the type
        /// </summary>
        /// <param name="configuration">Kestrel host configuration</param>
        /// <param name="loggerFactory">A factory that can be used to create loggers</param>
        /// <param name="healthCheck">Logic that determines if the service is considered healthy</param>
        public KestrelHealthProbe(IKestrelHostConfiguration configuration, ILoggerFactory loggerFactory, IHealthCheck healthCheck)
            : base(configuration, loggerFactory, () => new MultiProbeStartup(new MultiProbeConfiguration { EndpointList = HealthProbeLogic.EndpointList }, loggerFactory, new HealthProbeLogic(loggerFactory, healthCheck)))
        {

        }
    }
}
