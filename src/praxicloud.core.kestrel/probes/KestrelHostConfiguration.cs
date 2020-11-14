// Copyright (c) Christopher Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kestrel.probes
{
    #region Using Clauses
    using System;
    using System.Net;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.Extensions.Logging;
    #endregion

    /// <summary>
    /// A basic configuration for a Kestrel server
    /// </summary>
    public class KestrelHostConfiguration : IKestrelHostConfiguration
    {
        #region Properties
        /// <summary>
        /// The address to listen for requests on
        /// </summary>
        public IPAddress Address { get; set; }

        /// <summary>
        /// The port to listen for requests on
        /// </summary>
        public ushort Port { get; set; }

        /// <summary>
        /// If not null HTTPS will be used on the specified port
        /// </summary>
        public X509Certificate2 Certificate { get; set; }

        /// <summary>
        /// True if the NAGLE algorithm should be used, or false for no delay
        /// </summary>
        public bool UseNagle { get; set; }

        /// <summary>
        /// The keep alive duration
        /// </summary>
        public TimeSpan KeepAlive { get; set; }

        /// <summary>
        /// The maximum concurrent connections allowed
        /// </summary>
        public long MaximumConcurrentConnections { get; set; }

        /// <inheritdoc />
        public SslProtocols? AllowedProtocols { get; set; }
        #endregion
    }
}
