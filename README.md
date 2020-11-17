# PraxiCloud Core Kestrel
PraxiCloud Libraries are a set of common utilities and tools for general software development that simplify common development efforts for software development. The core kestrel library contains easy to use tools such as middleware components, availability and health probes.



# Installing via NuGet

Install-Package PraxiCloud-Core-Kestrel



# Kestrel Middleware



## Key Types and Interfaces

|Class| Description | Notes |
| ------------- | ------------- | ------------- |
|**SasAuthenticationMiddleware**|A base class that issues Shared Access Tokens and validates them when provided in the header.| The authentication middleware uses a specialized authentication path. |
|**UnhandledMiddleware**|A middleware component that can be added to the pipeline to respond if no other components have.|  |
|**MultiProbeMiddleware**|A middleware component that custom HTTP GET probes that are fully customized by the implementer.|  |

## Sample Usage

### Create Custom SAS Authentication Middleware

```csharp
namespace mydemoauthentication
{
    #region Using Clauses
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;
    using praxicloud.core.security;
    using System.Threading;
    using System.Threading.Tasks;
    using praxicloud.core.kestrel.middleware.authentication;
    #endregion

    /// <summary>
    /// A middleware component that does basic SAS validation
    /// </summary>
    public sealed class DemoSasAuthenticationMiddleware : SasAuthenticationMiddleware
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the type
        /// </summary>
        /// <param name="next">The next middleware component to execute</param>
        public DemoSasAuthenticationMiddleware(RequestDelegate next, IDependencyService dependencyService, IMetricFactory metricFactory, ILoggerFactory loggerFactory) 
            :base(next, dependencyService, metricFactory, loggerFactory, TimeSpan.FromMinutes(60),  "demopolicy", sasTokenPolicyKey, "Authorization")
        {

        }
        #endregion
        #region Methods
        /// <inheritdoc />
        protected override Task<bool> AuthenticateAsync(HttpRequest request, AuthenticationRequestPayload requestPayload, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
        #endregion
    }
}
```


### Create Unhandled Request Middleware

```csharp
// Added as the last pipeline middleware
app.UseUnhandled(logger, new UnhandledConfiguration
                 {
                     ContentType = "application/json",
                     Response = "{ \"Result": \"Nobody is home" }",
                     ResponseCode = 404
                 }, null);
```

### Create Multi Probe Middleware

```csharp
public class MyCustomLogic : IMultiProbeValidator
{
    /// <inheritdoc />
    public Task<bool> ValidateAsync(int indexValue, string endpoint, CancellationToken cancellationToken)
    {
        var success = false;
        
        switch(indexValue)
        {
            case 1:
                Debug.Print("Received health probe request");
                success = true;
                break;
                
            case 2:
                Debug.Print("Received availability probe request");                
                success = true;
                break;
        }
        
        return Task.FromResult(success);
    }
    
    public Task<bool> UnknownEndpointAsync(string endpoint, CancellationToken cancellationToken)
    {
        // Perform logic to handle unknown probes
        
        return Task.FromResult(true);
    }
}

app.UseMultiProbe(new MultiProbeConfiguration
                  {
                      {1, "health"},
                      {2, "availability"}
                  }, loggerFactory, new MyCustomLogic());
```

## Additional Information

For additional information the Visual Studio generated documentation found [here](./documents/praxicloud.core.kestrel/praxicloud.core.kestrel.xml), can be viewed using your favorite documentation viewer.

# Web Sockets



## Key Types and Interfaces

|Class| Description | Notes |
| ------------- | ------------- | ------------- |
|**WebSocketHandler**|A base class for web socket sessions that provides functionality such as buffer pooling, metrics and send / receive methods.<br />***BufferPool*** exposes the buffer pool for use with allocations to protect memory.<br />***Logger*** provides access to the logger for the session, to write debugging and diagnostics information to.<br />***MetricFactory*** provides access to the factory to create metrics.<br />***ReceiveMessagesAsync*** is a message pump that handles the buffer management etc. around receiving a message.<br />***SendMessageAsync*** sends a message as a byte array to the client.| To use this with the Web Socket middleware create a new instance on accepting a new socket and pass the socket to the ProcessAsync method. |

## Sample Usage

### Create Custom Web Socket Handler

```csharp
/// <summary>
/// The demo web socket handler
/// </summary>
public class DemoWebSocketHandler : WebSocketHandler
{
    #region Methods
    /// <summary>
    /// Initializes a new instance of the type
    /// </summary>
    /// <param name="metricRecorder">The metric recorder to write to</param>
    /// <param name="loggerFactory">A logger that can be used to write debugging and diagnostics information</param> 
    public DemoWebSocketHandler(IDependencyService dependencyService, configuration.IWebSocketConfiguration webSocketConfiguration, IMetricFactory metricFactory, ILoggerFactory loggerFactory, IBufferPool bufferPool) : base(dependencyService, webSocketConfiguration, metricFactory, loggerFactory, bufferPool)
    {
    }
    
    /// <inheritdoc />
    protected override async Task MessageReceivedAsync(byte[] message, CancellationToken cancellationToken)
    {
        using (Logger.BeginScope("Received message"))
        {
    	    if ((message?.Length ?? 0) > 0)
    	    {
    	        try
    	        {
    	    	    Logger.LogDebug("Deserializing telemetry message with {byteCount} bytes", message.Length);
    	    	    
    	    	    await SendMessageAsync(Encoding.ASCII.GetBytes("Received that message"), false, cancellationToken).ConfigureAwait(false);
    	        }
    	        catch (Exception e)
    	        {
    	    	    Logger.LogError(e, "Error processing received message");
    	        }
    	    }
    	    else
    	    {
    	        Logger.LogWarning("Message receied that was null or 0 length");
    	    }
        }
    }
    #endregion
}

```


## Additional Information

For additional information the Visual Studio generated documentation found [here](./documents/praxicloud.core.kestrel/praxicloud.core.kestrel.xml), can be viewed using your favorite documentation viewer.


# Probes



## Key Types and Interfaces

|Class| Description | Notes |
| ------------- | ------------- | ------------- |
|**KestrelAvailabilityProbe**|A probe host that returns successful HTTP GET responses when queried if the service should be considered ready to accept traffic. This leverages a Kestrel server for HTTP logic.|  |
|**KestrelHealthProbe**|A probe host that returns successful HTTP GET responses when queried if the service should be considered healthy. This leverages a Kestrel server for HTTP logic.|  |
|**KestrelDualProbe**|A dual probe that implements the availability and health probe logic. The single implementation allows for the hosting of two endpoints on the same port and saves resources on the Kestrel usage. This leverages a Kestrel server for HTTP logic.| It is recommended to use this implementation over the individual health probes in scenarios where both will be implemented. |

## Sample Usage

### Create Availability HTTP Probe 

```csharp
const int Port = 10580;

var host = new KestrelAvailabilityProbe(new KestrelHostConfiguration
{
    Address = IPAddress.Any,
    Port = Port,
    Certificate = null,
    KeepAlive = TimeSpan.FromSeconds(180),
    MaximumConcurrentConnections = 100,
    UseNagle = false
}, loggerFactory, invocationCounter);


await host.StartAsync(CancellationToken.None).ConfigureAwait(false);

// Perform application logic until and end the probe when shutdown

await host.StopAsync(CancellationToken.None).ConfigureAwait(false);
await host.Task.ConfigureAwait(false);


public sealed class ProbeInvocationCounter : IAvailabilityCheck
{
    #region Variables
    /// <summary>
    /// The number of times the availability handler has been invoked
    /// </summary>
    private long _availabilityCount;
    #endregion
    #region Properties
    /// <summary>
    /// True if the availability results should return success
    /// </summary>
    public bool IsAvailable { get; set; } = true;
    
    /// <summary>
    /// The number of times the availability handler has been invoked
    /// </summary>
    public long AvailabiltyCount => _availabilityCount;
    #endregion
    #region Methods
    /// <inheritdoc />
    public Task<bool> IsAvailableAsync()
    {
        Interlocked.Increment(ref _availabilityCount);
    
        return Task.FromResult(IsAvailable);
    }
    #endregion
}
```

### Create Health HTTP Probe

```csharp
const int Port = 10580;

var invocationCounter = new ProbeInvocationCounter();
var loggerFactory = GetLoggerFactory();
var host = new KestrelHealthProbe(new KestrelHostConfiguration
{
    Address = IPAddress.Any,
    Port = Port,
    Certificate = null,
    KeepAlive = TimeSpan.FromSeconds(180),
    MaximumConcurrentConnections = 100,
    UseNagle = false
}, loggerFactory, invocationCounter);


await host.StartAsync(CancellationToken.None).ConfigureAwait(false);

// Perform application logic until and end the probe when shutdown

await host.StopAsync(CancellationToken.None).ConfigureAwait(false);
await host.Task.ConfigureAwait(false);


public sealed class ProbeInvocationCounter : IHealthCheck
{
    #region Variables
    /// <summary>
    /// The number of times the health handler has been invoked
    /// </summary>
    private long _healthCount;
    #endregion
    #region Properties
    /// <summary>
    /// True if the health results should return success
    /// </summary>
    public bool IsHealthy { get; set; } = true;
    
    /// <summary>
    /// The number of times the health handler has been invoked
    /// </summary>
    public long HealthCount => _healthCount;
    #endregion
    #region Methods
    /// <inheritdoc />
    /// <inheritdoc />
    public Task<bool> IsHealthyAsync()
    {
        Interlocked.Increment(ref _healthCount);
    
        return Task.FromResult(IsHealthy);
    }
    #endregion
}
```

### Create Dual HTTP Probes 

```csharp
const int Port = 10580;

var invocationCounter = new ProbeInvocationCounter();
var loggerFactory = GetLoggerFactory();
var host = new KestrelDualProbe(new KestrelHostConfiguration
{
    Address = IPAddress.Any,
    Port = Port,
    Certificate = null,
    KeepAlive = TimeSpan.FromSeconds(180),
    MaximumConcurrentConnections = 100,
    UseNagle = false
}, loggerFactory, invocationCounter, invocationCounter);


public sealed class ProbeInvocationCounter : IHealthCheck, IAvailabilityCheck
{
    #region Variables
    /// <summary>
    /// The number of times the availability handler has been invoked
    /// </summary>
    private long _availabilityCount;
    
    /// <summary>
    /// The number of times the health handler has been invoked
    /// </summary>
    private long _healthCount;
    #endregion
    #region Properties
    /// <summary>
    /// True if the availability results should return success
    /// </summary>
    public bool IsAvailable { get; set; } = true;
    
    /// <summary>
    /// True if the health results should return success
    /// </summary>
    public bool IsHealthy { get; set; } = true;
    
    /// <summary>
    /// The number of times the availability handler has been invoked
    /// </summary>
    public long AvailabiltyCount => _availabilityCount;
    
    /// <summary>
    /// The number of times the health handler has been invoked
    /// </summary>
    public long HealthCount => _healthCount;
    #endregion
    #region Methods
    /// <inheritdoc />
    public Task<bool> IsAvailableAsync()
    {
        Interlocked.Increment(ref _availabilityCount);
    
        return Task.FromResult(IsAvailable);
    }
    
    /// <inheritdoc />
    public Task<bool> IsHealthyAsync()
    {
        Interlocked.Increment(ref _healthCount);
    
        return Task.FromResult(IsHealthy);
    }
    #endregion
}
```

## Additional Information

For additional information the Visual Studio generated documentation found [here](./documents/praxicloud.core.kestrel/praxicloud.core.kestrel.xml), can be viewed using your favorite documentation viewer.

# Kestrel Host



## Key Types and Interfaces

|Class| Description | Notes |
| ------------- | ------------- | ------------- |
|**KestrelHost**|A base  type that can be used to quickly setup new Kestrel web servers with the basic configuration options used by default. The type provides some thread safety around start and stop operations as well as expose convenience instances such as loggers, tasks and cancellation tokens that can be used to indicate shutdown.| This type implements the basic plumbing of the Kestrel web servers. Probes rely on this for consistent startup. |

## Sample Usage

### Start a Kestrel Web Server 

```csharp
var host = new KestrelHost<DemoStartup>(new KestrelHostConfiguration
{
    Address = IPAddress.Any,
    Port = 10080,
    Certificate = null,
    UseNagle = true,
    KeepAlive = TimeSpan.FromSeconds(120),
    MaximumConcurrentConnections = 100,
    AllowedProtocols = SslProtocols.Tls12 
}, loggerFactory);

await host.StartAsync().ConfigureAwait(false);

// Perform logic or wait here until ready to shutdown web server

await host.StopAsync().ConfigureAwait(false);
await host.Task.ConfigureAwait(false);

public class DemoStartup : IKestrelStartup
{
    #region Methods
    /// <inheritdoc />
    public void Configure(IApplicationBuilder app, IHostingEnvironment env)
    {
        // Add middleware and build processing pipeline for Kestrel here
    }
    #endregion
}
```


## Additional Information

For additional information the Visual Studio generated documentation found [here](./documents/praxicloud.core.kestrel/praxicloud.core.kestrel.xml), can be viewed using your favorite documentation viewer.