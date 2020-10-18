// Copyright (c) Chris Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kestrel.probes
{
    #region Using Clauses
    using Microsoft.Extensions.Logging;
    using praxicloud.core.security;
    using System.Threading;
    using System.Threading.Tasks;
    #endregion

    /// <summary>
    /// A probe that handles multiple endpoints
    /// </summary>
    public class MultiProbe : KestrelHost<MultiProbeStartup>
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the type
        /// </summary>
        /// <param name="kestrelConfiguration">The kestrel configuration details</param>
        /// <param name="loggerFactory">A factory to create loggers from</param>
        /// <param name="probeConfiguration">The probe configuration</param>
        /// <param name="validator">The validation routine</param>
        public MultiProbe(IKestrelHostConfiguration kestrelConfiguration, ILoggerFactory loggerFactory, IMultiProbeConfiguration probeConfiguration, IMultiProbeValidator validator)
            : base(kestrelConfiguration, loggerFactory, () => new MultiProbeStartup(probeConfiguration, loggerFactory, validator))
        {
        }
        #endregion
    }
}

