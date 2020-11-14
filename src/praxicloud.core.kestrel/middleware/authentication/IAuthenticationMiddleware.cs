// Copyright (c) Christopher Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kestrel.middleware.authentication
{
    #region Using Clauses
    using Microsoft.AspNetCore.Http;
    using System.Threading.Tasks;
    #endregion

    /// <summary>
    /// A middleware component used to authenticate requests in the Kestrel pipeline
    /// </summary>
    public interface IAuthenticationMiddleware
    {
        /// <summary>
        /// Processes the HTTP requests
        /// </summary>
        /// <param name="context">The HTTP Context associated with the request</param>
        Task Invoke(HttpContext context);
    }
}