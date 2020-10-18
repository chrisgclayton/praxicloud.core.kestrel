// Copyright (c) Chris Clayton. All rights reserved.
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
    public class WebSocketConfiguration : IWebSocketConfiguration
    {
        #region Properties
        /// <inheritdoc />
        public TimeSpan? KeepAliveInterval { get; set; }

        /// <inheritdoc />
        public ushort? ReceiveBufferSize { get; set; }

        /// <inheritdoc />
        public IList<string> AllowedOrigins { get; set; }

        /// <inheritdoc />
        public string WebSocketPath { get; set; }

        /// <inheritdoc />
        public TimeSpan? SessionTimeout { get; set; }
        #endregion
    }
}
