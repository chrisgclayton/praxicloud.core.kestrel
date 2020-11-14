// Copyright (c) Christopher Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kestrel.probes
{
    #region Using Clauses
    using System.Collections.Generic;
    #endregion

    /// <summary>
    /// Probe configuration
    /// </summary>
    public interface IMultiProbeConfiguration
    {
        #region Properties
        /// <summary>
        /// The endpoints that the probe listens on and the associated index for reference
        /// </summary>
        Dictionary<int, string> EndpointList { get; }
        #endregion
    }
}
