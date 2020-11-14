// Copyright (c) Christopher Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kestrel.middleware.authentication
{
    #region Using Clauses
    using Newtonsoft.Json;
    #endregion

    /// <summary>
    /// An authorization request payload
    /// </summary>
    public sealed class AuthenticationRequestPayload
    {
        /// <summary>
        /// Metadata associatd with the request
        /// </summary>
        [JsonProperty("meta")]
        public string Meta { get; set; }

        /// <summary>
        /// The type of access grant requested
        /// </summary>
        [JsonProperty("grant_type")]
        public string GrantType { get; set; }

        /// <summary>
        /// The user name
        /// </summary>
        [JsonProperty("username")]
        public string UserName { get; set; }

        /// <summary>
        /// The password
        /// </summary>
        [JsonProperty("password")]
        public string Password { get; set; }
    }
}
