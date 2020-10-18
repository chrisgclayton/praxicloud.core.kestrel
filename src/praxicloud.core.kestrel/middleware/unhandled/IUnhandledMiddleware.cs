// Copyright (c) Chris Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kestrel.middleware.unhandled
{
    #region Using Clauses
    using Microsoft.AspNetCore.Http;
    using System.Threading.Tasks;
    #endregion

    /// <summary>
    /// A middleware component used when no other pipeline middleware handlers have completed
    /// </summary>
    public interface IUnhandledMiddleware
    {
        /// <summary>
        /// Processes the HTTP requests
        /// </summary>
        /// <param name="context">The HTTP Context associated with the request</param>
        Task Invoke(HttpContext context);
    }
}
