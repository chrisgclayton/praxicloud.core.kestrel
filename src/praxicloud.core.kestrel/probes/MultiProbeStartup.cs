// Copyright (c) Christopher Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kestrel.probes
{
    #region Using Clauses
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Logging;
    using praxicloud.core.security;
    using System.Threading;
    using System.Threading.Tasks;
    #endregion

    /// <summary>
    /// Startup type for multiprobe instances
    /// </summary>
    public class MultiProbeStartup : IProbeStartup
    {
        #region Delegates
        /// <summary>
        /// A siganture of the method that performs the validation check
        /// </summary>
        /// <param name="indexValue">The index of the endpoint</param>
        /// <param name="endpoint">The endpoint string</param>
        /// <param name="cancellation">A token that can be monitored for abort requests</param>
        /// <returns>True if success should be returned</returns>
        public delegate Task<bool> CheckAsync(int indexValue, string endpoint, CancellationToken cancellation);
        #endregion
        #region Variables
        /// <summary>
        /// The configuration of the probe
        /// </summary>
        private readonly IMultiProbeConfiguration _configuration;

        /// <summary>
        /// The validation instance
        /// </summary>
        private readonly IMultiProbeValidator _validator;

        /// <summary>
        /// A factory used to create loggers from
        /// </summary>
        private readonly ILoggerFactory _loggerFactory;
        #endregion
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the type
        /// </summary>
        /// <param name="configuration">The configuration of the probe</param>
        /// <param name="loggerFactory">A factory used to create loggers from</param>
        /// <param name="validator">The validation instance</param>
        public MultiProbeStartup(IMultiProbeConfiguration configuration, ILoggerFactory loggerFactory, IMultiProbeValidator validator)
        {
            Guard.NotNull(nameof(configuration), configuration);
            Guard.NotNull(nameof(validator), validator);
            Guard.NotNull(nameof(loggerFactory), loggerFactory);

            _configuration = configuration;
            _validator = validator;
            _loggerFactory = loggerFactory;
        }
        #endregion
        #region Methods
        /// <inheritdoc />
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseMultiProbe(_configuration, _loggerFactory, _validator);
        }
        #endregion
    }
}
