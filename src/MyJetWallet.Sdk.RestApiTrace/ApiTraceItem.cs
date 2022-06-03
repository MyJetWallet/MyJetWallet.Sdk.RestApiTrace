using System;
using System.Collections.Generic;
using MyJetWallet.Sdk.Service;
using Nest;
using Newtonsoft.Json;

namespace MyJetWallet.Sdk.RestApiTrace
{
    public class ApiTraceItem 
    {
        [JsonProperty("@timestamp")]
        [PropertyName("@timestamp")]
        public DateTime timestamp { get; set; }
        
        public string index { get; set; }

        public string op_type { get; set; } = "create";
        
        public DateTime DateTime { get; set; }
        public string Level { get; set; }
        public string Env { get; set; }
        public string Version { get; set; }
        public string AppName { get; set; }
        public string Path { get; set; }
        public string HostUrl { get; set; }
        public string Method { get; set; }
        public string StatusCode { get; set; }
        public string Status { get; set; }
        public long ExecuteTimeMs { get; set; }
        public string UserAgent { get; set; }

        public string ExceptionType { get; set; }
        public string ExceptionStack { get; set; }
        public string ExceptionMsg { get; set; }
        public string BrokerId { get; set; }
        public string BrandId { get; set; }
        public string ClientId { get; set; }
        public string WalletId { get; set; }
        public string IP { get; set; }
        public string IPCountry { get; set; }


        public string SessionRootId { get; set; }
        public string SessionTokenId { get; set; }
        public string Span_Id { get; set; }
        public string Trace_Id { get; set; }
        
        public string RejectCode { get; set; }


        private ApiTraceItem()
        {
            timestamp= DateTime.UtcNow;
            DateTime = timestamp;
            Level = "api_trace";
            Env = ApplicationEnvironment.EnvInfo;
            Version = ApplicationEnvironment.AppVersion;
            AppName = ApplicationEnvironment.AppName;
        }

        public static ApiTraceItem Create()
        {
            return new ApiTraceItem();
        }
    }
}