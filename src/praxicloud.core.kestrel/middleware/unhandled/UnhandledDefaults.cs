// Copyright (c) Chris Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kestrel.middleware.unhandled
{
    /// <summary>
    /// Default values for the unhandled request middleware
    /// </summary>
    public static class UnhandledDefaults
    {
        #region Constants
        /// <summary>
        /// The response content type
        /// </summary>
        public const string ContentType = "application/json";

        /// <summary>
        /// Payload for unhandled responses
        /// </summary>
        public const string Response = "{\"Value\": false}";

        /// <summary>
        /// Http Bad request code
        /// </summary>
        public const int ResponseCode = 400;
        #endregion
    }
}
