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
    /// The Kestrel host that implements a container probe interface
    /// </summary>
    public class KestrelProbeHost<T> : KestrelHost<T>, IContainerProbe where T : class, IProbeStartup
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the type
        /// </summary>
        /// <param name="configuration">The configuration for the Kestrel Server</param>
        /// <param name="loggerFactory">A factory to create loggers from</param>
        /// <param name="factory">An optional factory for the startup type</param>
        public KestrelProbeHost(IKestrelHostConfiguration configuration, ILoggerFactory loggerFactory, StartupFactory factory = null)
            : base(configuration, loggerFactory, factory)
        {
        }
        #endregion
    }
}
