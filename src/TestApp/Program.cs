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

            settings.Urls["node1"] = "";
            settings.Urls["node2"] = "";
            settings.Urls["node3"] = "";
            settings.User = "";
            settings.Password = "";

            using ILoggerFactory loggerFactory =
                LoggerFactory.Create(builder =>
                    builder.AddSimpleConsole(options =>
                    {
                        options.IncludeScopes = true;
                        options.SingleLine = true;
                        options.TimestampFormat = "hh:mm:ss ";
                    }));

            ILogger<Program> logger = loggerFactory.CreateLogger<Program>();

            var manager = new ApiTraceManager(settings, "api-trace-jet-logs-uat", logger);

            manager.Start();

            var item = ApiTraceItem.Create()
                .LogRestCall("POST", "myservice.com", "address/11/1", HttpStatusCode.OK, 100, "my agent");


            manager.LogMethodCall(item);
            manager.LogMethodCall(item);
            manager.LogMethodCall(item);
            manager.LogMethodCall(item);
            

            Console.WriteLine("Done");
            Console.ReadLine();
        }
    }
}
