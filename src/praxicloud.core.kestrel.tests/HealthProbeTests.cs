// Copyright (c) Christopher Clayton. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace praxicloud.core.kestrel.tests
{
    #region Using Clauses
    using System;
    using System.Net;
    using Microsoft.Extensions.Logging;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Net.Http;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.IO;
    using praxicloud.core.containers;
    using Microsoft.Extensions.DependencyInjection;
    using praxicloud.core.kestrel.probes;
    #endregion

    /// <summary>
    /// Tests for the availability and health probes
    /// </summary>
    [TestClass]
    public class HealthProbeTests
    {
        /// <summary>
        /// Simple start and stop of the host
        /// </summary>
        [TestMethod]
        public void StartStopHost()
        {
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


            host.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
            Task.Delay(3000).GetAwaiter().GetResult();
            host.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
            host.Task.GetAwaiter().GetResult();
        }

        /// <summary>
        /// 10 success of each health and availability using HTTP
        /// </summary>
        [TestMethod]
        public void AllSuccessesHttp()
        {
            const int Port = 10580;
            const string HostAddress = "http://127.0.0.1";

            var testComplete = new TaskCompletionSource<bool>();
            var healthResponses = new List<bool>();
            var isHostStarted = false;
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

            var collector = new Thread(new ThreadStart(() =>
            {
                var messageHandler = new HttpClientHandler();
                messageHandler.ServerCertificateCustomValidationCallback += (httpRequest, certificate, chain, policyErrors) => true;

                var client = new HttpClient(messageHandler);

                var maximumWaitTime = DateTime.UtcNow.AddSeconds(10);

                // Wait for the host to start
                while (DateTime.UtcNow < maximumWaitTime && !isHostStarted)
                {
                    Thread.Sleep(100);
                }

                for (var index = 0; index < 10; index++)
                {
                    try
                    {
                        var response = client.GetAsync($"{HostAddress}:{ Port }{ DualProbeLogic.HealthEndpoint }").GetAwaiter().GetResult();
                        healthResponses.Add(response.IsSuccessStatusCode);
                    }
                    catch (Exception)
                    {

                    }

                    Thread.Sleep(1000);
                }

                testComplete.TrySetResult(true);
            }));

            collector.Start();
            host.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
            isHostStarted = true;

            // Wait up to 30 seconds for the test to complete
            Task.WhenAny(testComplete.Task, Task.Delay(30000)).GetAwaiter().GetResult();

            host.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
            host.Task.GetAwaiter().GetResult();

            Assert.IsTrue(healthResponses.Count(item => item) == 10, "Health success count incorrect");
            Assert.IsTrue(healthResponses.Count(item => !item) == 0, "Health failures count incorrect");
            Assert.IsTrue(invocationCounter.HealthCount == 10, "Health invocation count correct");
        }

        /// <summary>
        /// 10 success of each health and availability using HTTP
        /// </summary>
        [TestMethod]
        public void AllSuccessesHttps()
        {
            const int Port = 10580;
            const string HostAddress = "https://127.0.0.1";

            var path = Path.Combine(Environment.CurrentDirectory, ContainerEnvironment.IsLinux ? "my_contoso_local.pfx" : "TestValidationCertificate.pfx");
            var certificate = new X509Certificate2(path, "abc123");
            var testComplete = new TaskCompletionSource<bool>();
            var healthResponses = new List<bool>();
            var isHostStarted = false;
            var invocationCounter = new ProbeInvocationCounter();
            var loggerFactory = GetLoggerFactory();
            var host = new KestrelHealthProbe(new KestrelHostConfiguration
            {
                Address = IPAddress.Any,
                Port = Port,
                Certificate = certificate,
                KeepAlive = TimeSpan.FromSeconds(180),
                MaximumConcurrentConnections = 100,
                UseNagle = false
            }, loggerFactory, invocationCounter);



            var collector = new Thread(new ThreadStart(() =>
            {
                var messageHandler = new HttpClientHandler();
                messageHandler.ServerCertificateCustomValidationCallback += (httpRequest, certificate, chain, policyErrors) => true;

                var client = new HttpClient(messageHandler);
                var maximumWaitTime = DateTime.UtcNow.AddSeconds(10);

                // Wait for the host to start
                while (DateTime.UtcNow < maximumWaitTime && !isHostStarted)
                {
                    Thread.Sleep(100);
                }

                for (var index = 0; index < 10; index++)
                {
                    try
                    {
                        var response = client.GetAsync($"{HostAddress}:{ Port }{ DualProbeLogic.HealthEndpoint }").GetAwaiter().GetResult();
                        healthResponses.Add(response.IsSuccessStatusCode);
                    }
                    catch (Exception)
                    {

                    }

                    Thread.Sleep(1000);
                }

                testComplete.TrySetResult(true);
            }));

            collector.Start();
            host.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
            isHostStarted = true;

            // Wait up to 30 seconds for the test to complete
            Task.WhenAny(testComplete.Task, Task.Delay(30000)).GetAwaiter().GetResult();

            host.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
            host.Task.GetAwaiter().GetResult();

            Assert.IsTrue(healthResponses.Count(item => item) == 10, "Health success count incorrect");
            Assert.IsTrue(healthResponses.Count(item => !item) == 0, "Health failures count incorrect");
            Assert.IsTrue(invocationCounter.HealthCount == 10, "Health invocation count correct");
        }

        /// <summary>
        /// 10 success of each health and availability using HTTP
        /// </summary>
        [DataTestMethod]
        [DataRow(10, 3, 2)]
        [DataRow(10, 2, 5)]
        public void PartialSuccessHttp(int iterations, int healthFailureModulus, int availabilityFailureModulus)
        {
            const int Port = 10580;
            const string HostAddress = "http://127.0.0.1";

            var testComplete = new TaskCompletionSource<bool>();
            var healthResponses = new List<bool>();
            var expectedAvailabilityResponses = new List<bool>();
            var expectedHealthResponses = new List<bool>();
            var isHostStarted = false;
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


            var collector = new Thread(new ThreadStart(() =>
            {
                var messageHandler = new HttpClientHandler();
                messageHandler.ServerCertificateCustomValidationCallback += (httpRequest, certificate, chain, policyErrors) => true;

                var client = new HttpClient(messageHandler);

                var maximumWaitTime = DateTime.UtcNow.AddSeconds(10);

                // Wait for the host to start
                while (DateTime.UtcNow < maximumWaitTime && !isHostStarted)
                {
                    Thread.Sleep(100);
                }

                for (var index = 0; index < iterations; index++)
                {
                    var isHealthy = (index + 1) % healthFailureModulus == 0;

                    invocationCounter.IsHealthy = isHealthy;

                    expectedHealthResponses.Add(isHealthy);

                    try
                    {
                        var response = client.GetAsync($"{HostAddress}:{ Port }{ DualProbeLogic.HealthEndpoint }").GetAwaiter().GetResult();
                        healthResponses.Add(response.IsSuccessStatusCode);
                    }
                    catch (Exception)
                    {

                    }

                    Thread.Sleep(200);
                }

                testComplete.TrySetResult(true);
            }));

            collector.Start();
            host.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
            isHostStarted = true;

            // Wait up to 30 seconds for the test to complete
            Task.WhenAny(testComplete.Task, Task.Delay(iterations * 350)).GetAwaiter().GetResult();

            host.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
            host.Task.GetAwaiter().GetResult();

            Assert.IsTrue(healthResponses.Count(item => item) == expectedHealthResponses.Count(item => item), "Health success count incorrect");
            Assert.IsTrue(healthResponses.Count(item => !item) == expectedHealthResponses.Count(item => !item), "Health failures count incorrect");
            Assert.IsTrue(invocationCounter.HealthCount == iterations, "Health invocation count correct");
        }


        /// <summary>
        /// Builds the logger factory for the tests
        /// </summary>
        /// <returns>Logger factory that writes to the console</returns>
        private ILoggerFactory GetLoggerFactory()
        {
            var collection = new ServiceCollection();

            collection.AddLogging((configure) =>
            {
                configure.AddConsole();

                configure.SetMinimumLevel(LogLevel.Trace);
            });

            var provider = collection.BuildServiceProvider();

            return provider.GetRequiredService<ILoggerFactory>();
        }
    }
}


//// Copyright (c) Christopher Clayton. All rights reserved.
//// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//namespace praxicloud.core.kestrel.tests
//{
//    #region Using Clauses
//    using System;
//    using System.Net;
//    using Microsoft.Extensions.Logging;
//    using System.Threading;
//    using System.Security.Cryptography;
//    using System.Threading.Tasks;
//    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
//    using Microsoft.VisualStudio.TestTools.UnitTesting;
//    using praxicloud.core.kestrel.probes;
//    using System.Net.Http;
//    using System.Collections.Generic;
//    using System.Linq;
//    using System.IO;
//    using System.Security.Cryptography.X509Certificates;
//    using System.Net.Security;
//    using praxicloud.core.containers;
//    #endregion

//    /// <summary>
//    /// Tests for the health probes
//    /// </summary>
//    [TestClass]
//    public class HealthProbeTests
//    {
//        /// <summary>
//        /// Simple start and stop of the host
//        /// </summary>
//        [TestMethod]
//        public void StartStopHost()
//        {
//            var index = 0;
//            var host = new HealthProbe(IPAddress.Any, 10080, LogLevel.Debug, "health", (string name, object state) => 
//            {
//                return Task.FromResult(Interlocked.Increment(ref index) % 4 != 0);
//            });

//            host.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
//            Task.Delay(3000).GetAwaiter().GetResult();
//            host.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
//            host.Task.GetAwaiter().GetResult();
//        }

//        /// <summary>
//        /// Simple test of the host to ensure check results are reflected
//        /// </summary>
//        [TestMethod]
//        public void CheckResultsHost()
//        {
//            const int Port = 10080;

//            var content = new List<(int, string)>();
//            var testComplete = new TaskCompletionSource<bool>();
//            var index = 0;
//            var host = new HealthProbe(IPAddress.Any, Port, LogLevel.Debug, "health", (string name, object state) =>
//            {
//                return Task.FromResult(Interlocked.Increment(ref index) % 4 != 0);
//            });

//            var collector = new Thread(new ThreadStart(() => 
//            {
//                var client = new HttpClient();

//                Thread.Sleep(3000);

//                for (var index = 0; index < 10; index++)
//                {
//                    try
//                    {
//                        var response = client.GetAsync($"http://127.0.0.1:{ Port }/health").GetAwaiter().GetResult();

//                        content.Add(((int)response.StatusCode, response.Content.ReadAsStringAsync().GetAwaiter().GetResult()));
//                    }
//                    catch(Exception)
//                    {

//                    }

//                    Thread.Sleep(1000);
//                }

//                testComplete.TrySetResult(true);
//            }));

//            var startTask = host.StartAsync(CancellationToken.None);

//            collector.Start();
//            startTask.GetAwaiter().GetResult();
//            Task.WhenAny(testComplete.Task, Task.Delay(30000)).GetAwaiter().GetResult();          
//            host.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
//            host.Task.GetAwaiter().GetResult();

//            Assert.IsTrue(content.Count == 10, "Count incorrect");
//            Assert.IsTrue(content.Count(item => item.Item1 == 200) == 8, "Success count incorrect");
//            Assert.IsTrue(content.Count(item => item.Item1 == 500) == 2, "Faioure count incorrect");

//        }


//        /// <summary>
//        /// Simple test of the host to ensure check results are reflected in HTTPS mode
//        /// </summary>
//        [TestMethod]
//        public void CheckResultsHostSSL()
//        {
//            const int Port = 10080;

//            var path = Path.Combine(Environment.CurrentDirectory, ContainerEnvironment.IsLinux ? "my_contoso_local.pfx" : "TestValidationCertificate.pfx");

////            var path = Path.Combine(Environment.CurrentDirectory, "TestValidationCertificate.pfx");
//            var certificate = new X509Certificate2(path, "abc123");
//            var content = new List<(int, string)>();
//            var testComplete = new TaskCompletionSource<bool>();
//            var index = 0;
//            var host = new HealthProbe(IPAddress.Any, Port, LogLevel.Debug, "health", (string name, object state) =>
//            {
//                return Task.FromResult(Interlocked.Increment(ref index) % 4 != 0);
//            }, certificate);

//            var collector = new Thread(new ThreadStart(() =>
//            {
//                using(var httpClientHandler = new HttpClientHandler())
//                {
//                    httpClientHandler.ServerCertificateCustomValidationCallback = (HttpRequestMessage message, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors policyErrors) => true;

//                    var client = new HttpClient(httpClientHandler);

//                    Thread.Sleep(3000);

//                    for (var index = 0; index < 10; index++)
//                    {
//                        try
//                        {
//                            var response = client.GetAsync($"https://127.0.0.1:{ Port }/health").GetAwaiter().GetResult();

//                            content.Add(((int)response.StatusCode, response.Content.ReadAsStringAsync().GetAwaiter().GetResult()));
//                        }
//                        catch (Exception)
//                        {
//                        }

//                        Thread.Sleep(1000);
//                    }
//                }

//                testComplete.TrySetResult(true);
//            }));

//            var startTask = host.StartAsync(CancellationToken.None);

//            collector.Start();
//            startTask.GetAwaiter().GetResult();
//            Task.WhenAny(testComplete.Task, Task.Delay(30000)).GetAwaiter().GetResult();
//            host.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
//            host.Task.GetAwaiter().GetResult();

//            Assert.IsTrue(content.Count == 10, "Count incorrect");
//            Assert.IsTrue(content.Count(item => item.Item1 == 200) == 8, "Success count incorrect");
//            Assert.IsTrue(content.Count(item => item.Item1 == 500) == 2, "Faioure count incorrect");
//        }
//    }
//}
