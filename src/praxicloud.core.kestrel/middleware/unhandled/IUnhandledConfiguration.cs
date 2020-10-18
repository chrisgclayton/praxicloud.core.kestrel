// Copyright (c) Chris Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kestrel.middleware.unhandled
{
    /// <summary>
    /// A configuration objec that describes the behavior of the unhandled middleware component
    /// </summary>
    public interface IUnhandledConfiguration
    {
        /// <summary>
        /// The response content type
        /// </summary>
        string ContentType { get; }

        /// <summary>
        /// The response content
        /// </summary>
        string Response { get; }

        /// <summary>
        /// HTTP Response code
        /// </summary>
        int? ResponseCode { get; }
    }
}
