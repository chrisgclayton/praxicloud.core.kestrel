// Copyright (c) Christopher Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kestrel.middleware.websockets
{
    #region Using Clause
    using System;
    using System.Collections.Generic;
    #endregion

    /// <summary>
    /// Configuration for the web socket host
    /// </summary>
    public interface IWebSocketConfiguration
    {
        #region Properties
        /// <summary>
        /// The interval that the keep alive ping pong should be sent
        /// </summary>
        TimeSpan? KeepAliveInterval { get; }

        /// <summary>
        /// The size of the receive buffer in bytes
        /// </summary>
        ushort? ReceiveBufferSize { get; }

        /// <summary>
        /// A list of origins that are allowed or null for all
        /// </summary>
        IList<string> AllowedOrigins { get; }

        /// <summary>
        /// The path starting with / that hosts web sockets
        /// </summary>
        string WebSocketPath { get; }

        /// <summary>
        /// The maximum time a session can remain idle before disconnecting
        /// </summary>
        TimeSpan? SessionTimeout { get; }
        #endregion
    }
}
