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
    /// A Kestrel based probe that handles both availability and health probes
    /// </summary>
    public class KestrelDualProbe : KestrelProbeHost<MultiProbeStartup>, IAvailabilityContainerProbe, IHealthContainerProbe
    {
        /// <summary>
        /// Initializes an instance of the type
        /// </summary>
        /// <param name="configuration">The kestrel host configuration</param>
        /// <param name="loggerFactory">A factory that can be used to create loggers</param>
        /// <param name="availabilityCheck">Logic that determines if the availability probe should return success</param>
        /// <param name="healthCheck">Logic that determines if the health probe should return success</param>
        public KestrelDualProbe(IKestrelHostConfiguration configuration, ILoggerFactory loggerFactory, IAvailabilityCheck availabilityCheck, IHealthCheck healthCheck)
            : base(configuration, loggerFactory, () =>  new MultiProbeStartup(new MultiProbeConfiguration { EndpointList = DualProbeLogic.EndpointList }, loggerFactory, new DualProbeLogic(loggerFactory, availabilityCheck, healthCheck)))
        {

        }
    }
}
