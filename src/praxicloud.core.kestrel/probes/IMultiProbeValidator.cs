// Copyright (c) Christopher Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kestrel.probes
{
    #region Using Clauses
    using System.Threading;
    using System.Threading.Tasks;
    #endregion

    /// <summary>
    /// The interface of a probe validation instance
    /// </summary>
    public interface IMultiProbeValidator
    {
        #region Properties
        /// <summary>
        /// The name of the probe for diagnostics and logging
        /// </summary>
        string Name { get; }
        #endregion
        #region Methods
        /// <summary>
        /// Validates whether the state of the probe should return true or false
        /// </summary>
        /// <param name="indexValue">The index of the endpoint that was triggered</param>
        /// <param name="endpoint">The endpoint string that was triggered</param>
        /// <param name="cancellationToken">A token that can be monitored for abort requests</param>
        /// <returns>True if validation resulted in a success</returns>
        Task<bool> ValidateAsync(int indexValue, string endpoint, CancellationToken cancellationToken);

        /// <summary>
        /// Identifies an endpoint that was called and did not match any of the known probe endpoints
        /// </summary>
        /// <param name="endpoint">The endpoint that was called</param>
        /// <param name="cancellationToken">A token that can be monitored for abort requests</param>
        /// <returns>True to continue to the next middleware component if it exists, false to return a failure</returns>
        Task<bool> UnknownEndpointAsync(string endpoint, CancellationToken cancellationToken);
        #endregion
    }
}
