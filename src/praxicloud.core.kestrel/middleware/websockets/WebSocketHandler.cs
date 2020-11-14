// Copyright (c) Christopher Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kestrel.middleware.websockets
{
    #region Using Clauses
    using System;
    using System.Collections.Generic;
    using System.Net.WebSockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using praxicloud.core.buffers;
    using praxicloud.core.configuration;
    using praxicloud.core.containers;
    using praxicloud.core.metrics;
    using praxicloud.core.security;
    #endregion

    /// <summary>
    /// A basic web socket session handler
    /// </summary>
    public abstract class WebSocketHandler : IWebSocketHandler
    {
        #region Variables
        /// <summary>
        /// The web socket that is in use
        /// </summary>
        private WebSocket _socket;

        /// <summary>
        /// A metrics gauge that tracks session data
        /// </summary>
        private readonly IGauge _serviceSessionGauge;

        /// <summary>
        /// A metric that tracks the time 
        /// </summary>
        private readonly ISummary _sessionTimes;

        /// <summary>
        /// Tracks the time taken to receive a message
        /// </summary>
        private readonly ISummary _receiveTimes;

        /// <summary>
        /// The number of errors that have occurred
        /// </summary>
        private readonly ICounter _errorCounter;
        #endregion
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the type
        /// </summary>
        /// <param name="dependencyService">A service that can be used to retrieve dependencies that were injected</param>
        /// <param name="loggerFactory">A factory that can be used to create loggers</param>
        /// <param name="metricFactory">A factory that can be used to create metrics</param>
        /// <param name="webSocketConfiguration">The configuration of the web socket</param>  
        /// <param name="bufferPool">The buffer pool used to retreive arrays</param>
        public WebSocketHandler(IDependencyService dependencyService, IWebSocketConfiguration webSocketConfiguration, IMetricFactory metricFactory, ILoggerFactory loggerFactory, IBufferPool bufferPool)
        {
            Guard.NotNull(nameof(metricFactory), metricFactory);
            Guard.NotNull(nameof(loggerFactory), loggerFactory);
            Guard.NotNull(nameof(bufferPool), bufferPool);
            Guard.NotNull(nameof(webSocketConfiguration), webSocketConfiguration);

            WebSocketConfiguration = webSocketConfiguration;
            BufferPool = bufferPool;
            SessionId = Guid.NewGuid();
            MetricFactory = metricFactory;
            Logger = loggerFactory.CreateLogger($"Session Handler: {SessionId:N}");

            DependencyService = dependencyService;

            _errorCounter = metricFactory.CreateCounter("WebSocket error counter", "Tracks the number of errors that have occurred since the start of the service", false, new string[0]);
            _serviceSessionGauge = metricFactory.CreateGauge("WebSocket sessions in progress", "Tracks the number of sessions underway", false, new string[0]);
            _sessionTimes = metricFactory.CreateSummary("WebSocket session iteration times", "Tracks the time taken to execute an iteration in the session", 10, false, new string[0]);
            _receiveTimes = metricFactory.CreateSummary("WebSocket message receive time", "Tracks the time taken to receive a message", 10, false, new string[0]);
        }
        #endregion
        #region Properties
        /// <summary>
        /// The buffer pool to be used for allocations
        /// </summary>
        protected IBufferPool BufferPool { get; }

        /// <summary>
        /// A unique session id for correlation in tracing and metrics
        /// </summary>
        protected Guid SessionId { get; }

        /// <summary>
        /// The current status of the web socket
        /// </summary>
        protected WebSocketState SocketState => _socket.State;

        /// <summary>
        /// The metric factory used to create counters with
        /// </summary>
        protected IMetricFactory MetricFactory { get; }

        /// <summary>
        /// Web socket configuration details
        /// </summary>
        protected IWebSocketConfiguration WebSocketConfiguration { get; }

        /// <summary>
        /// A logger that can be used to write debugging and diagnostics information
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// A service used to get dependencies
        /// </summary>
        protected IDependencyService DependencyService { get; }

        /// <summary>
        /// The last time a send or receive was successfully performed for session timing detection
        /// </summary>
        protected DateTime LastActivityTime { get; set; }
        #endregion
        #region Methods
        /// <summary>
        /// Processes the web socket session
        /// </summary>
        /// <param name="socket">The web socket that is in use</param>
        /// <param name="cancellationToken">A token to monitor for abort requests</param>
        public async Task ProcessAsync(WebSocket socket, CancellationToken cancellationToken)
        {
            using (Logger.BeginScope("Hander Process"))
            {
                Logger.LogInformation("Web socket handlers invoked");

                using (_serviceSessionGauge.TrackExecution())
                {
                    Task receiverTask = null;

                    try
                    {
                        _socket = socket;
                        LastActivityTime = DateTime.UtcNow;

                        Logger.LogDebug("Web socket receiver starting");
                        receiverTask = ReceiveMessagesAsync(socket, cancellationToken);

                        using (_sessionTimes.Time())
                        {
                            while (SocketState == WebSocketState.Open && LastActivityTime.Add(WebSocketConfiguration.SessionTimeout ?? TimeSpan.FromSeconds(30)) > DateTime.UtcNow)
                            {
                                await IterationAsync(ContainerLifecycle.CancellationToken).ConfigureAwait(false);
                            }
                        }

                        Logger.LogInformation("Web socket session completed");
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, "Error processing session, aborting");
                        _errorCounter.Increment();
                    }

                    Logger.LogDebug("Web socket iteration complete, closing");
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Session closed", CancellationToken.None).ConfigureAwait(false);
                    if (receiverTask != null) await receiverTask.ConfigureAwait(false);
                }

                Logger.LogInformation("Web socket handlers ending");
            }
        }

        /// <summary>
        /// Receive loop for getting messages
        /// </summary>
        /// <param name="socket">The web socket that is associated with the session</param>
        /// <param name="cancellationToken">A token to monitor for abort requests</param>
        private async Task ReceiveMessagesAsync(WebSocket socket, CancellationToken cancellationToken)
        {
            using (Logger.BeginScope("Receive Message Loop"))
            {
                var buffer = BufferPool.Take();
                var bufferList = new List<(byte[], int)>();
                Task<WebSocketReceiveResult> receiveTask = null;

                while (SocketState == WebSocketState.Open)
                {
                    try
                    {
                        if (receiveTask == null) receiveTask = socket.ReceiveAsync(buffer, CancellationToken.None);
                        var completedTask = await Task.WhenAny(receiveTask, Task.Delay(250)).ConfigureAwait(false);

                        if (receiveTask.IsCompleted)
                        {
                            var result = receiveTask.Result;

                            LastActivityTime = DateTime.UtcNow;

                            if (result.Count > 0)
                            {
                                bufferList.Add((buffer, result.Count));
                                buffer = BufferPool.Take();
                            }

                            if (result.EndOfMessage)
                            {
                                var byteCount = 0;

                                foreach (var currentBuffer in bufferList)
                                {
                                    byteCount += currentBuffer.Item2;
                                }

                                var finalBuffer = new byte[byteCount];
                                var position = 0;

                                foreach (var currentBuffer in bufferList)
                                {
                                    Buffer.BlockCopy(currentBuffer.Item1, 0, finalBuffer, position, currentBuffer.Item2);
                                    position += currentBuffer.Item2;
                                }

                                foreach (var currentBuffer in bufferList)
                                {
                                    BufferPool.Return(currentBuffer.Item1);
                                }

                                bufferList.Clear();

                                try
                                {
                                    using (_receiveTimes.Time())
                                    {
                                        await MessageReceivedAsync(finalBuffer, cancellationToken).ConfigureAwait(false);
                                    }
                                }
                                catch (Exception e)
                                {
                                    _errorCounter.Increment();
                                    Logger.LogError(e, "Error processing received message");
                                }
                            }

                            receiveTask = null;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, "Error in receive loop");
                        _errorCounter.Increment();

                        if (bufferList.Count > 0)
                        {
                            foreach (var currentBuffer in bufferList)
                            {
                                BufferPool.Return(currentBuffer.Item1);
                            }
                        }
                    }
                }

                if (bufferList.Count > 0)
                {
                    foreach (var currentBuffer in bufferList)
                    {
                        BufferPool.Return(currentBuffer.Item1);
                    }
                }

                BufferPool.Return(buffer);
            }
        }

        /// <summary>
        /// A method that is invoked when a new message is received
        /// </summary>
        /// <param name="message">The message that was received</param>
        /// <param name="cancellationToken">A token to monitor for abort requests</param>
        protected abstract Task MessageReceivedAsync(byte[] message, CancellationToken cancellationToken);

        /// <summary>
        /// A method that is executed on each iteraction while waiting the web socket session is alive
        /// </summary>
        /// <param name="cancellationToken">A token to monitor for abort requests</param>
        protected virtual Task IterationAsync(CancellationToken cancellationToken)
        {
            return Task.Delay(100);
        }

        /// <summary>
        /// Sends a message using the web socket
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <param name="isBinary">True if the message is binary, false if text</param>
        /// <param name="cancellationToken">A token to monitor for abort requests</param>
        protected async Task<bool> SendMessageAsync(byte[] message, bool isBinary, CancellationToken cancellationToken)
        {
            var success = false;

            using (Logger.BeginScope("Send message"))
            {
                try
                {
                    Logger.LogInformation("Sending message with {byteCount} bytes", message.Length);
                    await _socket.SendAsync(message, isBinary ? WebSocketMessageType.Binary : WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
                    LastActivityTime = DateTime.UtcNow;
                    success = true;

                    Logger.LogDebug("Completed sending message with {byteCount} bytes", message.Length);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Error sending message");
                }
            }

            return success;
        }
        #endregion
    }
}
