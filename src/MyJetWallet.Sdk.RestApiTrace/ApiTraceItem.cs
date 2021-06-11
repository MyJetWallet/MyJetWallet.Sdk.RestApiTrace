using System;
using System.Collections.Generic;
using MyJetWallet.Sdk.Service;

namespace MyJetWallet.Sdk.RestApiTrace
{
    public class ApiTraceItem : Dictionary<string, object>
    {
        private ApiTraceItem()
        {
            this["timestamp"] = DateTime.UtcNow;
            this["DateTime"] = this["timestamp"];
            this["Level"] = "api_trace";
            this["Env"] = ApplicationEnvironment.EnvInfo;
            this["Version"] = ApplicationEnvironment.AppVersion;
            this["AppName"] = ApplicationEnvironment.AppName;
        }

        public static ApiTraceItem Create()
        {
            return new ApiTraceItem();
        }

        public string Path()
        {
            if (this.TryGetValue("Path", out var pathObj))
            {
                return pathObj as string;
            }
            return null;
        }

        public string Host()
        {
            if (this.TryGetValue("Host", out var hostObj))
            {
                return hostObj as string;
            }
            return null;
        }
    }
}