// Copyright (c) Chris Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kestrel.probes
{
    #region Using Clauses
    using System.Collections.Generic;
    #endregion

    /// <summary>
    /// Configuration details for the probe endpoints
    /// </summary>
    public class MultiProbeConfiguration : IMultiProbeConfiguration
    {
        #region Properties
        /// <summary>
        /// The list of endpoints prefixed with / and the integer used to represent the index in handlers
        /// </summary>
        public Dictionary<int, string> EndpointList { get; set; }
        #endregion
    }
}
