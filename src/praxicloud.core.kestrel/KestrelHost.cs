// Copyright (c) Christopher Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kestrel
{
    #region Using Clauses
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Server.Kestrel.Core;
    using Microsoft.AspNetCore.Server.Kestrel.Https;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Nito.AsyncEx;
    using praxicloud.core.containers;
    using praxicloud.core.kestrel.probes;
    using praxicloud.core.security;
    using System;
    using System.Security.Authentication;
    using System.Threading;
    using System.Threading.Tasks;
    #endregion

    /// <summary>
    /// A basic kestrel host
    /// </summary>
    /// <typeparam name="T">The startup type</typeparam>
    public class KestrelHost<T> : IKestrelHost where T : class, IKestrelStartup
    {
        #region Delegates
        /// <summary>
        /// A factory instance
        /// </summary>
        /// <returns>Creats the startup type</returns>
        public delegate T StartupFactory();
        #endregion
        #region Variables
        /// <summary>
        /// The factory used to create the startup instance
        /// </summary>
        private readonly StartupFactory _factory;

        /// <summary>
        /// A logger for writing debugging and diagnostics information to
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// A completion source that is completed when the server terminates
        /// </summary>
        private readonly TaskCompletionSource<bool> _completionSource = new TaskCompletionSource<bool>();

        /// <summary>
        /// The kestrel startup 
        /// </summary>
        private readonly IKestrelHostConfiguration _configuration;
        #endregion
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the type
        /// </summary>
        /// <param name="configuration">The configuration for the Kestrel Server</param>
        /// <param name="loggerFactory">A factory to create loggers from</param>
        /// <param name="factory">An optional factory for the startup type</param>
        public KestrelHost(IKestrelHostConfiguration configuration, ILoggerFactory loggerFactory, StartupFactory factory = null)
        {
            Guard.NotNull(nameof(configuration), configuration);
            Guard.NotNull(nameof(configuration.Address), configuration.Address);
            Guard.NotLessThan(nameof(configuration.KeepAlive), configuration.KeepAlive, TimeSpan.FromSeconds(1));
            Guard.NotLessThan(nameof(configuration.MaximumConcurrentConnections), configuration.MaximumConcurrentConnections, 1);
            Guard.NotNull(nameof(loggerFactory), loggerFactory);

            _logger = loggerFactory.CreateLogger("Kestrel Host");
            _configuration = configuration;
            _factory = factory;
        }
        #endregion
        #region Properties
        /// <summary>
        /// A control used manage access to the lifecycle events
        /// </summary>
        private readonly AsyncLock _control = new AsyncLock();

        /// <summary>
        /// The web host to operate on
        /// </summary>
        protected IWebHost _host;

        /// <inheritdoc />
        public Task Task { get; private set; } = Task.FromResult(true);
        #endregion
        #region Methods
        /// <inheritdoc />
        public async Task<bool> StartAsync(CancellationToken cancellationToken = default)
        {
            var started = false;

            if (_host == null)
            {
                using (await _control.LockAsync().ConfigureAwait(false))
                {
                    if (_host == null)
                    {
                        _host = GetHostBuilder().Build();

                        await _host.StartAsync(cancellationToken).ConfigureAwait(true);

                        Task = _host.WaitForShutdownAsync(cancellationToken).ContinueWith(t =>
                        {
                            _completionSource.SetResult(t.IsCompletedSuccessfully);

                            return Task.IsCompleted;
                        });

                        started = true;
                    }
                }
            }

            return started;
        }

        /// <inheritdoc />
        public async Task<bool> StopAsync(CancellationToken cancellationToken = default)
        {
            var stopped = false;

            if (_host != null)
            {
                using (await _control.LockAsync().ConfigureAwait(false))
                {
                    if (_host != null)
                    {
                        try
                        {
                            await _host.StopAsync(cancellationToken).ConfigureAwait(false);
                            stopped = true;
                        }
                        finally
                        {
                            _host.Dispose();
                            _host = null;
                        }
                    }
                }
            }

            return stopped;
        }

        /// <summary>
        /// Can be overridden to configure extra logging options
        /// </summary>
        /// <param name="logBuilder">A log builder</param>
        protected virtual void ConfigureLogging(ILoggingBuilder logBuilder)
        {
           
        }

        /// <summary>
        /// Overridden to configure dependency injection for the Kestrel instance
        /// </summary>
        /// <param name="serviceCollection">The service collection to add instances to</param>
        protected virtual void ConfigureServices(IServiceCollection serviceCollection)
        {
        }

        /// <summary>
        /// A method that can be overridden to configure the listening options of the Kestrel server
        /// </summary>
        /// <param name="options">The options instance that can be configured</param>
        protected virtual void ConfigureKestrelListenOptions(ListenOptions options)
        {

        }

        /// <summary>
        /// Creates and configures the host
        /// </summary>
        /// <returns>A web host builder</returns>
        private IWebHostBuilder GetHostBuilder()
        {
            var webHost = WebHost.CreateDefaultBuilder()
                .ConfigureServices(serviceCollection =>
                {
                    if (_factory != null)
                    {
                        serviceCollection.AddSingleton<T>(_factory());
                    }
                    else
                    {
                        serviceCollection.AddSingleton<T>();
                    }

                    ConfigureServices(serviceCollection);
                })
                .ConfigureLogging(logBuilder =>
                {
                    ConfigureLogging(logBuilder);
                })
                .UseStartup<T>()
                .UseKestrel(options =>
                {                    
                    options.Listen(_configuration.Address, _configuration.Port, listenOptions =>
                    {
                        listenOptions.KestrelServerOptions.Limits.MaxConcurrentConnections = _configuration.MaximumConcurrentConnections;
                        listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
                        
                  //      listenOptions.NoDelay = !_configuration.UseNagle;
                        if (_configuration.Certificate != null) listenOptions.UseHttps(new HttpsConnectionAdapterOptions 
                        { 
                            SslProtocols = _configuration.AllowedProtocols ?? SslProtocols.Tls12, ServerCertificate = _configuration.Certificate                            
                        });

                        ConfigureKestrelListenOptions(listenOptions);
                    });
                });

                //.ConfigureKestrel(serverOptions =>
                //{
                //    serverOptions.Limits.MaxConcurrentConnections = _configuration.MaximumConcurrentConnections;
                //    serverOptions.Limits.KeepAliveTimeout = _configuration.KeepAlive;
                //});

            return webHost;
        }
        #endregion
    }
}
