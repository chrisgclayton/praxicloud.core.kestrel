// Copyright (c) Christopher Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kestrel
{
    #region Using Clauses
    using Microsoft.Extensions.Logging;
    using praxicloud.core.containers;
    using praxicloud.core.kestrel.probes;
    #endregion

    /// <summary>
    /// An availability probe based on Kestrel for HTTP
    /// </summary>
    public class KestrelAvailabilityProbe : KestrelProbeHost<MultiProbeStartup>, IAvailabilityContainerProbe
    {
        /// <summary>
        /// Initializes a new instance of the type
        /// </summary>
        /// <param name="configuration">The kestrel host configuration</param>
        /// <param name="loggerFactory">A factory that can be used to create loggers</param>
        /// <param name="availabilityCheck">The logic to validate success results</param>
        public KestrelAvailabilityProbe(IKestrelHostConfiguration configuration, ILoggerFactory loggerFactory, IAvailabilityCheck availabilityCheck)
            : base(configuration, loggerFactory, () => new MultiProbeStartup(new MultiProbeConfiguration { EndpointList = AvailabilityProbeLogic.EndpointList }, loggerFactory, new AvailabilityProbeLogic(loggerFactory, availabilityCheck)))
        {

        }
    }
}
