using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using praxicloud.core.containers;
using praxicloud.core.kestrel;
using praxicloud.core.kestrel.probes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace praxicloud.kestreltest
{
    class Program
    {
        const int Port = 10580;
        const string HostAddress = "https://127.0.0.1";

        static void Main(string[] args)
        {
            Console.WriteLine("Starting Server");


            var path = Path.Combine(Environment.CurrentDirectory, ContainerEnvironment.IsLinux ? "my_contoso_local.pfx" : "TestValidationCertificate.pfx");
            var certificate = new X509Certificate2(path, "abc123");

            Console.WriteLine($"Certificate file is: {path}");
            Console.WriteLine($"Certificate null check: {certificate == null}");

            var testComplete = new TaskCompletionSource<bool>();
            var availabilityResponses = new List<bool>();
            var isHostStarted = false;
            var invocationCounter = new ProbeInvocationCounter();
            var loggerFactory = GetLoggerFactory();
            var host = new KestrelAvailabilityProbe(new KestrelHostConfiguration
            {
                Address = IPAddress.Any,
                Port = Port,
                Certificate = certificate,
                KeepAlive = TimeSpan.FromSeconds(180),
                MaximumConcurrentConnections = 100,
                UseNagle = false
            }, loggerFactory, invocationCounter);
            host.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

            var task = MainAsync();

            task.GetAwaiter().GetResult();

            

            Console.WriteLine("Press <ENTER> to end");
            Console.ReadLine();
        }

        private static async Task MainAsync()
        {
            var messageHandler = new HttpClientHandler();
            messageHandler.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            messageHandler.ServerCertificateCustomValidationCallback += (httpRequest, certificate, chain, policyErrors) => true;

            var client = new HttpClient(messageHandler);
            string line = null;

            do
            {
                Console.WriteLine("Type Quit to exit, any other command sends a request.");
                line = Console.ReadLine();

                try
                {
                    var response = await client.GetAsync($"{HostAddress}:{ Port }{ DualProbeLogic.AvailabilityEndpoint }").ConfigureAwait(false);

                    Console.WriteLine($"Response code was {response.StatusCode}");
                }
                catch (Exception e)
                {

                    while(e != null)
                    {
                        Console.WriteLine($"Error was {e.Message}");
                        e = e.InnerException;
                    }
                }
            } while (!string.Equals(line, "quit", StringComparison.OrdinalIgnoreCase));


        }

        /// <summary>
        /// Builds the logger factory for the tests
        /// </summary>
        /// <returns>Logger factory that writes to the console</returns>
        private static ILoggerFactory GetLoggerFactory()
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
