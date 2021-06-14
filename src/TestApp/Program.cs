using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.RestApiTrace;
using MyJetWallet.Sdk.Service;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var settings = new LogElkSettings()
            {
                Urls = new Dictionary<string, string>()
            };

            settings.Urls["node1"] = "https://192.168.11.4:9200";
            settings.Urls["node2"] = "https://192.168.11.5:9200";
            settings.Urls["node3"] = "https://192.168.11.6:9200";
            settings.User = "spot";
            settings.User = "63glAuxUz7h6TUbIR79TOVVcp9vX0id2";

            using ILoggerFactory loggerFactory =
                LoggerFactory.Create(builder =>
                    builder.AddSimpleConsole(options =>
                    {
                        options.IncludeScopes = true;
                        options.SingleLine = true;
                        options.TimestampFormat = "hh:mm:ss ";
                    }));

            ILogger<Program> logger = loggerFactory.CreateLogger<Program>();

            var manager = new ApiTraceManager(settings, "api-trace", logger);

            manager.Start();

            var item = ApiTraceItem.Create()
                .LogRestCall("POST", "myservice.com", "address/11/1", HttpStatusCode.OK, 100, "my agent");


            manager.LogMethodCall(item);

            Console.WriteLine("Done");
            Console.ReadLine();
        }
    }
}
