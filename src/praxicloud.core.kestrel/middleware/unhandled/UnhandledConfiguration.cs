// Copyright (c) Chris Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kestrel.middleware.unhandled
{
    /// <summary>
    /// A configuration objec that describes the behavior of the unhandled middleware component
    /// </summary>
    public class UnhandledConfiguration : IUnhandledConfiguration
    {
        /// <inheritdoc />
        public string ContentType { get; set; }

        /// <inheritdoc />
        public string Response { get; set; }

        /// <inheritdoc />
        public int? ResponseCode { get; set; }
    }
}
