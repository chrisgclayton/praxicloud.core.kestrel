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
    public interface IKestrelHostConfiguration
    {
        #region Properties
        /// <summary>
        /// The address to listen for requests on
        /// </summary>
        IPAddress Address { get; }

        /// <summary>
        /// The port to listen for requests on
        /// </summary>
        ushort Port { get; }

        /// <summary>
        /// If not null HTTPS will be used on the specified port
        /// </summary>
        X509Certificate2 Certificate { get; }

        /// <summary>
        /// True if the NAGLE algorithm should be used, or false for no delay
        /// </summary>
        bool UseNagle { get; }

        /// <summary>
        /// The keep alive duration
        /// </summary>
        TimeSpan KeepAlive { get; }

        /// <summary>
        /// The maximum concurrent connections allowed
        /// </summary>
        long MaximumConcurrentConnections { get; }

        /// <summary>
        /// The allowed SSL security protocols or none for TLS
        /// </summary>
        SslProtocols? AllowedProtocols { get; }
        #endregion
    }
}
