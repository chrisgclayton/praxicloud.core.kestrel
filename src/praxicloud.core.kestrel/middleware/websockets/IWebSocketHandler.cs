// Copyright (c) Chris Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kestrel.middleware.websockets
{
    #region Using Clauses
    using System.Net.WebSockets;
    using System.Threading;
    using System.Threading.Tasks;
    #endregion

    /// <summary>
    /// A web socket session handler
    /// </summary>
    public interface IWebSocketHandler
    {
        /// <summary>
        /// Processes the web socket session
        /// </summary>
        /// <param name="socket">The web socket that is in use</param>
        /// <param name="cancellationToken">A token to monitor for abort requests</param>
        Task ProcessAsync(WebSocket socket, CancellationToken cancellationToken);
    }
}
