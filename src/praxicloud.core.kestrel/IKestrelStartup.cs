// Copyright (c) Christopher Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kestrel
{
    #region Using Clauses
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    #endregion

    /// <summary>
    /// A Kestrel startup implementation definition
    /// </summary>
    public interface IKestrelStartup
    {
        #region Methods
        /// <summary>
        /// Called by the Kestrel runtime to configure the pipeline
        /// </summary>
        /// <param name="app">Application Builder</param>
        /// <param name="env">Hosting Environment</param>
        void Configure(IApplicationBuilder app, IHostingEnvironment env);
        #endregion
    }
}


