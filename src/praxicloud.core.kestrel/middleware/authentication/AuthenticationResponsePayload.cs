// Copyright (c) Chris Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kestrel.middleware.authentication
{
    #region Using Clauses
    using Newtonsoft.Json;
    #endregion

    /// <summary>
    /// A response object from authentication requests
    /// </summary>
    public sealed class AuthenticationResponsePayload
    {
        /// <summary>
        /// The access token
        /// </summary>
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        /// <summary>
        /// The type of access token
        /// </summary>
        [JsonProperty("token_type")]
        public string TokenType { get; set; }
    }
}
